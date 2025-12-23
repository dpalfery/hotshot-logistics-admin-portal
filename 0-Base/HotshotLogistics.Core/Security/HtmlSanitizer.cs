// <copyright file="HtmlSanitizer.cs" company="Hotshot Logistics">
// Copyright (c) Hotshot Logistics. All rights reserved.
// </copyright>

namespace HotshotLogistics.Core.Security
{
    using System;
    using System.Web;
    using Ganss.Xss;

    /// <summary>
    /// Provides HTML sanitization and encoding for output sanitization
    /// to prevent XSS vulnerabilities in API responses.
    /// </summary>
    public static class HtmlSanitizer
    {
        private static readonly HtmlSanitizerHelper _sanitizerHelper = new HtmlSanitizerHelper();

        /// <summary>
        /// Sanitizes a string to remove any HTML, JavaScript, or other potentially malicious content.
        /// Uses strict HTML sanitization to prevent XSS attacks.
        /// </summary>
        /// <param name="input">The string to sanitize.</param>
        /// <returns>Sanitized string safe for any context.</returns>
        public static string HtmlEncode(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }

            return _sanitizerHelper.Sanitize(input);
        }

        /// <summary>
        /// Internal helper class for HTML sanitization using the mganss/HtmlSanitizer library.
        /// </summary>
        private class HtmlSanitizerHelper
        {
            private readonly Ganss.Xss.HtmlSanitizer _sanitizer;

            public HtmlSanitizerHelper()
            {
                _sanitizer = new Ganss.Xss.HtmlSanitizer();
                
                _sanitizer.AllowedTags.Clear();
                _sanitizer.AllowedAttributes.Clear();
                _sanitizer.AllowedSchemes.Clear();
            }

            public string Sanitize(string input)
            {
                return _sanitizer.Sanitize(input);
            }
        }

        /// <summary>
        /// Encodes a string for safe URL output by converting URL special characters.
        /// </summary>
        /// <param name="input">The string to encode for URL context.</param>
        /// <returns>URL-encoded string safe for URL context.</returns>
        public static string UrlEncode(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }

            return System.Uri.EscapeDataString(input);
        }

        /// <summary>
        /// Encodes a string for safe JavaScript output.
        /// </summary>
        /// <param name="input">The string to encode for JavaScript context.</param>
        /// <returns>JavaScript-encoded string safe for JavaScript context.</returns>
        public static string JavaScriptEncode(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }

            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("<", "\\x3c")
                .Replace(">", "\\x3e")
                .Replace("&", "\\x26");
        }
    }
}
