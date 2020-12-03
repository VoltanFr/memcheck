using System;
using System.Runtime.Serialization;

namespace MemCheck.WebUI.Controllers
{
    [Serializable]
    internal class RequestInputException : Exception
    {
        public RequestInputException()
        {
        }

        public RequestInputException(string? message) : base(message)
        {
        }

        public RequestInputException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected RequestInputException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}