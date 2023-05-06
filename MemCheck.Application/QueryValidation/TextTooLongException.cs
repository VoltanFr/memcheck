using System;

namespace MemCheck.Application.QueryValidation;

// This is an InvalidOperationException since this should never happen: the calling code must check the validity

public class TextTooLongException : InvalidOperationException
{
    public TextTooLongException(int receivedLength, int minAcceptedValue, int maxAcceptedValue) : base($"Text too long: {receivedLength} bytes (min: {minAcceptedValue}, max: {maxAcceptedValue})")
    {
    }
    public TextTooLongException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public TextTooLongException()
    {
    }
}
