using System;
using System.Net;
using System.Runtime.Serialization;

namespace ADOApi.Exceptions
{
    public class AzureDevOpsApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? ErrorCode { get; }

        public AzureDevOpsApiException(string message) : base(message)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            ErrorCode = null;
        }

        public AzureDevOpsApiException(string message, Exception innerException) 
            : base(message, innerException)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            ErrorCode = null;
        }

        public AzureDevOpsApiException(string message, HttpStatusCode statusCode, string? errorCode = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}