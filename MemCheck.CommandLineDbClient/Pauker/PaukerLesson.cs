using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MemCheck.CommandLineDbClient.Pauker
{
    internal sealed class PaukerLesson
    {
        #region Fields
#pragma warning disable IDE0052 // Remove unread private members
        private readonly string lessonFormat;
        private readonly Dictionary<PaukerCard, List<PaukerStack>> cardsInStacks;
        private readonly FileInfo inputFile;
#pragma warning restore IDE0052 // Remove unread private members
        #endregion
        private static string StackNameFromIndex(int i)
        {
            return i switch
            {
                0 => "Non apprises",
                1 => "Ultra court terme",
                2 => "Court terme",
                _ => (i - 2).ToString(),
            };
        }
        private static PaukerStack ReadStack(XmlNode n, int stackIndex)
        {
            var name = StackNameFromIndex(stackIndex);
            var cards = new List<PaukerCard>();

            var cardNodes = n.SelectNodes("Card");
            if (cardNodes != null)
                for (int i = 0; i < cardNodes.Count; i++)
                    if (cardNodes[i] is XmlElement cardNode)
                        cards.Add(ReadCard(cardNode));
            return new PaukerStack(name, cards);
        }
        private static XmlElement FirstElem(XmlElement parent, string tagName)
        {
            XmlNodeList xmlNodeList = parent.GetElementsByTagName(tagName);
            return xmlNodeList != null && xmlNodeList[0] is XmlElement result ? result : throw new IOException();
        }
        private static PaukerCard ReadCard(XmlElement cardNode)
        {
            return new PaukerCard(ReadSide(FirstElem(cardNode, "FrontSide")), ReadSide(FirstElem(cardNode, "ReverseSide")));
        }
        public PaukerLesson(XmlDocument d, FileInfo inputFile)
        {
            this.inputFile = inputFile;
            Stacks = new List<PaukerStack>();
            cardsInStacks = new Dictionary<PaukerCard, List<PaukerStack>>();
            lessonFormat = d.DocumentElement!.GetAttribute("LessonFormat");
            var stackNodes = d.DocumentElement.SelectNodes("Batch");
            for (int i = 0; i < stackNodes!.Count; i++)
            {
                var currentStack = ReadStack(stackNodes[i]!, i);
                Stacks.Add(currentStack);
                foreach (var c in currentStack.Cards)
                    if (cardsInStacks.ContainsKey(c))
                        cardsInStacks[c].Add(currentStack);
                    else
                        cardsInStacks.Add(c, new List<PaukerStack>() { currentStack });
            }
        }
        private int TotalCount()
        {
            var result = 0;
            foreach (var list in cardsInStacks.Values)
                result += list.Count;
            return result;
        }
        private static PaukerCardSide ReadSide(XmlElement n)
        {
            var textNode = n.SelectSingleNode("Text");

            var tsAttrib = n.GetAttribute("LearnedTimestamp");
            long? learnedTimestamp = string.IsNullOrEmpty(tsAttrib) ? null : long.Parse(tsAttrib);

            return new PaukerCardSide(
                learnedTimestamp,
                n.GetAttribute("Orientation"),
                n.GetAttribute("RepeatByTyping"),
                textNode!.InnerText);
        }

        public void RemoveDoublons()
        {
            Console.WriteLine("There are {0} cards ({1} excluding doublons)", TotalCount(), cardsInStacks.Count);
            foreach (var c in cardsInStacks.Keys)
                if (cardsInStacks[c].Count > 1)
                    RemoveDoublons(c);
            Console.WriteLine($"Doublons removed, there are now {Stacks.Sum(stack => stack.Cards.Count)} cards");
        }

        private void RemoveDoublons(PaukerCard card)
        {
            var firstAlreadyFound = false;

            foreach (var stack in Stacks)
                firstAlreadyFound = stack.RemoveCard(card, !firstAlreadyFound) || firstAlreadyFound;
        }


#pragma warning disable IDE0051 // Remove unused private members
        private void Save(FileInfo outputFile)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement!;
            doc.InsertBefore(xmlDeclaration, root);

            doc.AppendChild(doc.CreateComment("This is a lesson file for Pauker (http://pauker.sourceforge.net)"));
            doc.AppendChild(doc.CreateComment("File saved by PaukerGenerator"));

            var lessonElem = doc.CreateElement("Lesson");
            doc.AppendChild(lessonElem);
            var lessonFormatAttrib = doc.CreateAttribute("LessonFormat");
            lessonFormatAttrib.Value = "1.7";
            lessonElem.Attributes.Append(lessonFormatAttrib);

            lessonElem.AppendChild(doc.CreateElement(string.Empty, "Description", string.Empty));

            foreach (var stack in Stacks)
                SaveStack(stack, lessonElem);

            doc.Save(outputFile.FullName);
        }

        private static void SaveStack(PaukerStack stack, XmlElement lessonElem)
        {
            var batchElem = lessonElem.OwnerDocument.CreateElement("Batch");
            lessonElem.AppendChild(batchElem);

            foreach (var card in stack.Cards)
                SaveCard(card, batchElem);
        }

        private static void SaveCard(PaukerCard card, XmlElement batchElem)
        {
            var cardElem = batchElem.OwnerDocument.CreateElement("Card");
            batchElem.AppendChild(cardElem);

            var frontSideElem = batchElem.OwnerDocument.CreateElement("FrontSide");
            cardElem.AppendChild(frontSideElem);
            SaveCardSide(card.Front, frontSideElem);

            var reverseSideElem = batchElem.OwnerDocument.CreateElement("ReverseSide");
            cardElem.AppendChild(reverseSideElem);
            SaveCardSide(card.Reverse, reverseSideElem);
        }

        private static void SaveCardSide(PaukerCardSide side, XmlElement targetElem)
        {
            if (side.LearnedTimestamp.HasValue)
            {
                var learnedTimestampAttrib = targetElem.OwnerDocument.CreateAttribute("LearnedTimestamp");
                learnedTimestampAttrib.Value = side.LearnedTimestamp.Value.ToString();
                targetElem.Attributes.Append(learnedTimestampAttrib);
            }

            var orientationAttrib = targetElem.OwnerDocument.CreateAttribute("Orientation");
            orientationAttrib.Value = side.Orientation;
            targetElem.Attributes.Append(orientationAttrib);

            var repeatByTypingAttrib = targetElem.OwnerDocument.CreateAttribute("RepeatByTyping");
            repeatByTypingAttrib.Value = side.RepeatByTyping;
            targetElem.Attributes.Append(repeatByTypingAttrib);

            var text = targetElem.OwnerDocument.CreateElement("Text");
            text.InnerText = side.Text;
            targetElem.AppendChild(text);

            SaveFont(targetElem);
        }
        private static void SaveFont(XmlElement targetElem)
        {
            var font = targetElem.OwnerDocument.CreateElement("Font");
            targetElem.AppendChild(font);

            var backgroundAttrib = targetElem.OwnerDocument.CreateAttribute("Background");
            backgroundAttrib.Value = "-1";
            font.Attributes.Append(backgroundAttrib);

            var boldAttrib = targetElem.OwnerDocument.CreateAttribute("Bold");
            boldAttrib.Value = "false";
            font.Attributes.Append(boldAttrib);

            var familyAttrib = targetElem.OwnerDocument.CreateAttribute("Family");
            familyAttrib.Value = "Dialog";
            font.Attributes.Append(familyAttrib);

            var foregroundAttrib = targetElem.OwnerDocument.CreateAttribute("Foreground");
            foregroundAttrib.Value = "-16777216";
            font.Attributes.Append(foregroundAttrib);

            var italicAttrib = targetElem.OwnerDocument.CreateAttribute("Italic");
            italicAttrib.Value = "false";
            font.Attributes.Append(italicAttrib);

            var sizeAttrib = targetElem.OwnerDocument.CreateAttribute("Size");
            sizeAttrib.Value = "12";
            font.Attributes.Append(sizeAttrib);
        }

        public List<PaukerStack> Stacks { get; }
    }
}
