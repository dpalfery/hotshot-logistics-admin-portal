// <copyright file="ErrorResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.DTOs
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a structured error response.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracking the request.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the field-level validation errors.
        /// </summary>
        public IReadOnlyDictionary<string, string[]>? FieldErrors { get; set; }

        /// <summary>
        /// Gets or sets additional details about the error.
        /// </summary>
        public object? Details { get; set; }

        /// <summary>
        /// Gets or sets the path of the request that caused the error.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponse"/> class.
        /// </summary>
        public ErrorResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponse"/> class with a message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ErrorResponse(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponse"/> class with a message and error code.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        public ErrorResponse(string message, string errorCode)
        {
            Message = message;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponse"/> class with a message, error code, and field errors.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="fieldErrors">The field-level validation errors.</param>
        public ErrorResponse(string message, string errorCode, IReadOnlyDictionary<string, string[]> fieldErrors)
        {
            Message = message;
            ErrorCode = errorCode;
            FieldErrors = fieldErrors;
        }
    }
}
