using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ADOApi.Exceptions
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = new ErrorResponse();

            switch (exception)
            {
                case AzureDevOpsException adoEx:
                    context.Response.StatusCode = (int)adoEx.StatusCode;
                    response = new ErrorResponse
                    {
                        StatusCode = (int)adoEx.StatusCode,
                        Message = adoEx.Message,
                        ErrorCode = adoEx.ErrorCode,
                        RequestId = context.TraceIdentifier
                    };
                    break;
                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new ErrorResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = argEx.Message,
                        ErrorCode = "INVALID_ARGUMENT",
                        RequestId = context.TraceIdentifier
                    };
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new ErrorResponse
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError,
                        Message = "An unexpected error occurred",
                        ErrorCode = "INTERNAL_ERROR",
                        RequestId = context.TraceIdentifier
                    };
                    break;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
    }
} 