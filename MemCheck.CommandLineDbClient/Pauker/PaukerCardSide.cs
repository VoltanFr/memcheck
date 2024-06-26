﻿using System;

namespace MemCheck.CommandLineDbClient.Pauker;

internal sealed class PaukerCardSide
{
    public PaukerCardSide(long? learnedTimestamp, string orientation, string repeatByTyping, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        LearnedTimestamp = learnedTimestamp;
        Text = text.Trim();
        Orientation = orientation ?? throw new ArgumentNullException(nameof(orientation));
        RepeatByTyping = repeatByTyping ?? throw new ArgumentNullException(nameof(repeatByTyping));
    }
    public string Text { get; }
    public long? LearnedTimestamp { get; }
    public string Orientation { get; }
    public string RepeatByTyping { get; }
}
