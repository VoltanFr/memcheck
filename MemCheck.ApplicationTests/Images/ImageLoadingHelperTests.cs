using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MemCheck.Application.Images;

[TestClass()]
public class ImageLoadingHelperTests
{
    [DataTestMethod, DataRow(""), DataRow("x"), DataRow("[hop]"), DataRow("[Mnesios:img]"), DataRow("![Mnesios]"), DataRow("![Mnesio:img]"), DataRow("![Mnesio:img,size=bad]")]
    public void NoImage(string text)
    {
        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromText(text);
        Assert.IsTrue(imageNames.IsEmpty);
    }
    [DataTestMethod, DataRow("`![Mnesios:img]`"), DataRow("`![Mnesios:An image]`"), DataRow("text before `![Mnesios:a-b]`"),
        DataRow("`![Mnesios:a(b)] text after`"), DataRow("text before `![Mnesios`:$O&e] text after"), DataRow("`![Not an image] ![Mnesios:hé] []`"),
        DataRow("`!`[Mnesios:img,size=small]"), DataRow("`![Mnesios:An image,size=medium]`"), DataRow("text before` ![Mnesios:a-b,size=big] `"),
        DataRow("`![Mnesios:a(b),size=small] text` after"), DataRow("text before `![Mnesios:$O&e,size=medium]` text after"), DataRow("![Not an image,size=small] `![Mnesios:hé,size=big]` []")]
    public void ImageInQuote(string text)
    {
        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromText(text);
        Assert.IsTrue(imageNames.IsEmpty);
    }
    [DataTestMethod, DataRow("![Mnesios:img]", "img"), DataRow("![Mnesios:An image]", "An image"), DataRow("text before ![Mnesios:a-b]", "a-b"),
        DataRow("![Mnesios:a(b)] text after", "a(b)"), DataRow("text before ![Mnesios:$O&e] text after", "$O&e"), DataRow("![Not an image] ![Mnesios:hé] `Quote` []", "hé")]
    public void OneImageWithoutSize(string text, string imageName)
    {
        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromText(text);
        Assert.AreEqual(1, imageNames.Count);
        Assert.AreEqual(imageName, imageNames.First());
    }
    [DataTestMethod, DataRow("![Mnesios:img,size=small]", "img"), DataRow("![Mnesios:An image,size=medium]", "An image"), DataRow("text before ![Mnesios:a-b,size=big]", "a-b"),
        DataRow("![Mnesios:a(b),size=small] text after", "a(b)"), DataRow("text before ![Mnesios:$O&e,size=medium] text after", "$O&e"), DataRow("![Not an image,size=small] ![Mnesios:hé,size=big] []", "hé")]
    public void OneImageWithSize(string text, string imageName)
    {
        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromText(text);
        Assert.AreEqual(1, imageNames.Count);
        Assert.AreEqual(imageName, imageNames.First());
    }
    [DataTestMethod,
        DataRow(new string[] { "img1", "img2" }, "![Mnesios:img1]![Mnesios:img2]"),
        DataRow(new string[] { "a&B", "@o!", ";23" }, "bla \n\n ![Mnesios:a&B]![Mnesios:@o!]\n\t![Mnesios:;23]"),
        DataRow(new string[] { "1", "2", "3" }, "![Mnesios:2,size=medium] x ![Mnesios:1,size=small] ![No] ![Mnesios:3,size=big]"),
        ]
    public void MultipleImages(string[] expectedImageNames, string text)
    {
        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromText(text);
        Assert.IsTrue(imageNames.SetEquals(expectedImageNames));
    }
    [DataTestMethod,
        DataRow(new string[] { "img1" }, "![Mnesios:img1]![Mnesios:img1]"),
        DataRow(new string[] { "a&B", "@o!" }, "bla \n\n ![Mnesios:a&B]![Mnesios:@o!]\n\t![Mnesios:a&B]"),
        DataRow(new string[] { "1", "2", "3" }, "![Mnesios:2,size=small] ![Mnesios:2,size=medium] x ![Mnesios:1,size=small] ![No] ![Mnesios:3,size=big] ![Mnesios:2,size=big]"),
        ]
    public void ImageReused(string[] expectedImageNames, string text)
    {
        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromText(text);
        Assert.IsTrue(imageNames.SetEquals(expectedImageNames));
    }
    [DataTestMethod,
        DataRow(new string[] { "img1" }, "![Mnesios:img1]", "", ""),
        DataRow(new string[] { "a", "b", "c", "d" }, "![Mnesios:d,size=small] `![Mnesios:x,size=small]`", "![Mnesios:d]![Mnesios:a]![Mnesios:b]", "![Mnesios:d,size=big]`![Mnesios:c,size=medium]`![Mnesios:c]"),
        ]
    public void ComplexCaseInCard(string[] expectedImageNames, string frontSide, string backSide, string additionalInfo)
    {
        var card = new Card() { FrontSide = frontSide, BackSide = backSide, AdditionalInfo = additionalInfo };
        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromSides(card.FrontSide, card.BackSide, card.AdditionalInfo);
        Assert.IsTrue(imageNames.SetEquals(expectedImageNames));
    }
}
