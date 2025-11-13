// <copyright file="LogSanitizer.cs" company="Hotshot Logistics">
// Copyright (c) Hotshot Logistics. All rights reserved.
// </copyright>

namespace HotshotLogistics.Core.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides lightweight sanitization methods for logging to prevent log injection
    /// and protect sensitive data without significant performance overhead.
    /// </summary>
    public static class LogSanitizer
    {
        private const int MaxExceptionMessageLength = 200;
        private const int MaxValidationErrorsToLog = 3;

        // Regex patterns for sensitive data detection (compiled for performance)
        private static readonly Regex EmailPattern = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
        private static readonly Regex PhonePattern = new Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);

        /// <summary>
        /// Sanitizes exception messages by truncating length and removing newlines
        /// to prevent log injection attacks.
        /// </summary>
        /// <param name="message">The exception message to sanitize.</param>
        /// <param name="maxLength">Maximum length of the sanitized message.</param>
        /// <returns>Sanitized exception message safe for logging.</returns>
        public static string SanitizeExceptionMessage(string message, int maxLength = MaxExceptionMessageLength)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "No message provided";
            }

            // Remove newlines and control characters to prevent log injection
            var sanitized = Regex.Replace(message, @"[\r\n\t]", " ");

            // Truncate to max length
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength) + "...";
            }

            return sanitized;
        }

        /// <summary>
        /// Fully masks a phone number for privacy protection.
        /// </summary>
        /// <param name="phoneNumber">The phone number to mask.</param>
        /// <returns>Fully masked phone number (***-***-****).</returns>
        public static string MaskPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return "***-***-****";
            }

            // Always return fully masked for maximum privacy
            return "***-***-****";
        }

        /// <summary>
        /// Masks an email address for privacy protection.
        /// </summary>
        /// <param name="email">The email address to mask.</param>
        /// <returns>Masked email address (***@***).</returns>
        public static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "***@***";
            }

            return "***@***";
        }

        /// <summary>
        /// Masks a device token showing only first and last 4 characters.
        /// </summary>
        /// <param name="token">The device token to mask.</param>
        /// <returns>Masked device token.</returns>
        public static string MaskDeviceToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "****";
            }

            if (token.Length <= 8)
            {
                return "****";
            }

            return $"{token.Substring(0, 4)}...{token.Substring(token.Length - 4)}";
        }

        /// <summary>
        /// Creates a safe hash of an ID for traceability without exposing the actual value.
        /// Uses SHA256 for consistent hashing.
        /// </summary>
        /// <param name="id">The ID to hash.</param>
        /// <returns>Hashed ID as hex string (first 12 characters for brevity).</returns>
        public static string SafeId(object? id)
        {
            if (id == null)
            {
                return "null";
            }

            var idString = id.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(idString))
            {
                return "empty";
            }

            // Use SHA256 for consistent hashing
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(idString));
            var hashHex = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();

            // Return first 12 chars for brevity while maintaining uniqueness
            return hashHex.Substring(0, Math.Min(12, hashHex.Length));
        }

        /// <summary>
        /// Sanitizes validation error messages by limiting count and truncating content.
        /// </summary>
        /// <param name="errors">Collection of error messages.</param>
        /// <returns>Sanitized error summary safe for logging.</returns>
        public static string SanitizeValidationErrors(IEnumerable<string> errors)
        {
            if (errors == null || !errors.Any())
            {
                return "No validation errors";
            }

            var errorList = errors.Take(MaxValidationErrorsToLog).ToList();
            var sanitizedErrors = errorList
                .Select(e => SanitizeExceptionMessage(e, 100))
                .ToList();

            var totalCount = errors.Count();
            var result = string.Join("; ", sanitizedErrors);

            if (totalCount > MaxValidationErrorsToLog)
            {
                result += $" (and {totalCount - MaxValidationErrorsToLog} more)";
            }

            return result;
        }

        /// <summary>
        /// Removes sensitive data patterns from a text string.
        /// </summary>
        /// <param name="text">Text to sanitize.</param>
        /// <returns>Text with sensitive data patterns removed.</returns>
        public static string RemoveSensitiveData(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Remove email addresses
            text = EmailPattern.Replace(text, "***@***");

            // Remove phone numbers
            text = PhonePattern.Replace(text, "***-***-****");

            return text;
        }

        /// <summary>
        /// Sanitizes a subject line for email logging by removing sensitive patterns.
        /// </summary>
        /// <param name="subject">Email subject to sanitize.</param>
        /// <returns>Sanitized subject line.</returns>
        public static string SanitizeEmailSubject(string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                return "[No Subject]";
            }

            // Remove sensitive data patterns and truncate
            var sanitized = RemoveSensitiveData(subject);
            sanitized = Regex.Replace(sanitized, @"[\r\n\t]", " ");

            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100) + "...";
            }

            return sanitized;
        }

        /// <summary>
        /// Creates a generic payment reference for logging without exposing amounts.
        /// </summary>
        /// <param name="invoiceId">Invoice identifier.</param>
        /// <returns>Safe payment reference for logging.</returns>
        public static string SafePaymentReference(string invoiceId)
        {
            return $"Payment for invoice ending in {SafeId(invoiceId)}";
        }
    }
}
