using System;

namespace MemCheck.Application.QueryValidation;

// This is an InvalidOperationException since this should never happen: the calling code must check the validity

public class TextTooShortException : InvalidOperationException
{
    public TextTooShortException(int receivedLength, int minAcceptedValue, int maxAcceptedValue) : base($"Text too short: {receivedLength} bytes (min: {minAcceptedValue}, max: {maxAcceptedValue})")
    {
    }
    public TextTooShortException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public TextTooShortException()
    {
    }
}
