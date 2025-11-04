// <copyright file="ExceptionHandlingMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Middleware
{
    using System;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Core.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using HotshotLogistics.Domain.DTOs;

    /// <summary>
    /// Middleware for handling exceptions globally and returning structured error responses.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionHandlingMiddleware> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = GetOrCreateCorrelationId(context);
            var errorResponse = CreateErrorResponse(exception, correlationId, context.Request.Path);

            var statusCode = GetStatusCode(exception);
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            // Add correlation ID to response headers
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            // Log the error with correlation ID
            LogException(exception, correlationId, statusCode);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
        }

        private string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                return correlationId.ToString();
            }

            return Guid.NewGuid().ToString();
        }

        private ErrorResponse CreateErrorResponse(Exception exception, string correlationId, string path)
        {
            return exception switch
            {
                ValidationException validationEx => new ErrorResponse(
                    validationEx.Message,
                    "VALIDATION_ERROR",
                    validationEx.Errors)
                {
                    CorrelationId = correlationId,
                    Path = path,
                    Details = validationEx.Errors
                },

                BusinessRuleException businessEx => new ErrorResponse(
                    businessEx.Message,
                    "BUSINESS_RULE_VIOLATION")
                {
                    CorrelationId = correlationId,
                    Path = path
                },

                PaymentProcessingException paymentEx => new ErrorResponse(
                    paymentEx.Message,
                    "PAYMENT_PROCESSING_ERROR")
                {
                    CorrelationId = correlationId,
                    Path = path,
                    Details = new
                    {
                        TransactionId = paymentEx.TransactionId,
                        ProviderErrorCode = paymentEx.ProviderErrorCode
                    }
                },

                ExternalServiceException externalEx => new ErrorResponse(
                    externalEx.Message,
                    "EXTERNAL_SERVICE_ERROR")
                {
                    CorrelationId = correlationId,
                    Path = path,
                    Details = new
                    {
                        ServiceName = externalEx.ServiceName,
                        StatusCode = externalEx.StatusCode,
                        Endpoint = externalEx.Endpoint
                    }
                },

                _ => new ErrorResponse(
                    "An unexpected error occurred. Please try again later.",
                    "INTERNAL_SERVER_ERROR")
                {
                    CorrelationId = correlationId,
                    Path = path,
                    // Include actual exception details for debugging
                    Details = new
                    {
                        ExceptionType = exception.GetType().Name,
                        Message = exception.Message,
                        StackTrace = exception.StackTrace,
                        InnerException = exception.InnerException?.Message
                    }
                }
            };
        }

        private HttpStatusCode GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ValidationException => HttpStatusCode.BadRequest,
                BusinessRuleException => HttpStatusCode.UnprocessableEntity,
                PaymentProcessingException => HttpStatusCode.PaymentRequired,
                ExternalServiceException => HttpStatusCode.BadGateway,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private void LogException(Exception exception, string correlationId, HttpStatusCode statusCode)
        {
            var logLevel = statusCode >= HttpStatusCode.InternalServerError ? LogLevel.Error : LogLevel.Warning;

            logger.Log(logLevel, exception, "Exception handled by middleware. CorrelationId: {CorrelationId}, StatusCode: {StatusCode}", correlationId, (int)statusCode);
        }
    }
}
