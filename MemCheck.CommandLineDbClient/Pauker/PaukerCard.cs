using System;

namespace MemCheck.CommandLineDbClient.Pauker
{
    class PaukerCard
    {
        private readonly PaukerCardSide front;
        private readonly PaukerCardSide reverse;
        public PaukerCard(PaukerCardSide front, PaukerCardSide reverse)
        {
            if (front == null) throw new ArgumentNullException(nameof(front));
            if (reverse == null) throw new ArgumentNullException(nameof(reverse));
            this.front = front;
            this.reverse = reverse;
        }
        public PaukerCardSide Front
        {
            get
            {
                return front;
            }
        }
        public PaukerCardSide Reverse
        {
            get
            {
                return reverse;
            }
        }
        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(obj, this))
                return true;
            if (obj.GetType() != GetType())
                return false;
            var other = (PaukerCard)obj;
            return other.Front.Text.Equals(Front.Text, StringComparison.OrdinalIgnoreCase) && other.Reverse.Text.Equals(Reverse.Text, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode()
        {
            return Front.Text.GetHashCode() ^ Reverse.Text.GetHashCode();
        }
        public DateTime LearnedDate()
        {
            if (reverse.LearnedTimestamp != null)
                throw new InvalidProgramException("Non null reverse.LearnedTimestamp found");
            if (front.LearnedTimestamp == null)
                throw new InvalidProgramException("null front.LearnedTimestamp found");
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(front.LearnedTimestamp.Value).ToLocalTime();
            return dtDateTime;
        }
    }
}
