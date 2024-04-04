using System;
using System.Runtime.Serialization;

namespace ADOApi.Exceptions
{
    [Serializable]
    internal class AzureDevOpsApiException : Exception
    {
        public AzureDevOpsApiException()
        {
        }

        public AzureDevOpsApiException(string? message) : base(message)
        {
        }

        public AzureDevOpsApiException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected AzureDevOpsApiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}