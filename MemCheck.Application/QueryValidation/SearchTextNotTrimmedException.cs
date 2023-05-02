using System;

namespace MemCheck.Application.QueryValidation;

// This is an InvalidOperationException since this should never happen: the calling code must trim

public class TextNotTrimmedException : InvalidOperationException
{
    public TextNotTrimmedException()
    {
    }
}
