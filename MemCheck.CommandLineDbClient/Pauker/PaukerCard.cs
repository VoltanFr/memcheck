using System;

namespace MemCheck.CommandLineDbClient.Pauker;

internal class PaukerCard
{
    public PaukerCard(PaukerCardSide front, PaukerCardSide reverse)
    {
        Front = front ?? throw new ArgumentNullException(nameof(front));
        Reverse = reverse ?? throw new ArgumentNullException(nameof(reverse));
    }
    public PaukerCardSide Front { get; }
    public PaukerCardSide Reverse { get; }
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
        if (Reverse.LearnedTimestamp != null)
            throw new InvalidProgramException("Non null reverse.LearnedTimestamp found");
        if (Front.LearnedTimestamp == null)
            throw new InvalidProgramException("null front.LearnedTimestamp found");
        DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddMilliseconds(Front.LearnedTimestamp.Value).ToLocalTime();
        return dtDateTime;
    }
}
