using System;
using System.Net;

namespace ADOApi.Exceptions
{
    public class AzureDevOpsException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ErrorCode { get; }

        public AzureDevOpsException(string message, HttpStatusCode statusCode, string errorCode)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

        public AzureDevOpsException(string message, HttpStatusCode statusCode, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
} 