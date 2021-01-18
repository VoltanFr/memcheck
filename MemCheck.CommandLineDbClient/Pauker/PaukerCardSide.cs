using System;

namespace MemCheck.CommandLineDbClient.Pauker
{
    class PaukerCardSide
    {
        private readonly long? learnedTimestamp;
        private readonly string orientation;
        private readonly string repeatByTyping;
        private readonly string text;
        public PaukerCardSide(long? learnedTimestamp, string orientation, string repeatByTyping, string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            this.learnedTimestamp = learnedTimestamp;
            this.text = text.Trim();
            this.orientation = orientation ?? throw new ArgumentNullException(nameof(orientation));
            this.repeatByTyping = repeatByTyping ?? throw new ArgumentNullException(nameof(repeatByTyping));
        }
        public string Text
        {
            get
            {
                return text;
            }
        }
        public long? LearnedTimestamp
        {
            get
            {
                return learnedTimestamp;
            }
        }
        public string Orientation
        {
            get
            {
                return orientation;
            }
        }
        public string RepeatByTyping
        {
            get
            {
                return repeatByTyping;
            }
        }
    }
}
