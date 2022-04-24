using System;

namespace MemCheck.Application.QueryValidation
{
    public sealed class PersoTagAllowedOnlyOnPrivateCardsException : RequestInputException
    {
        public PersoTagAllowedOnlyOnPrivateCardsException(string message) : base(message)
        {
        }
        public PersoTagAllowedOnlyOnPrivateCardsException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public PersoTagAllowedOnlyOnPrivateCardsException()
        {
        }
    }
}
