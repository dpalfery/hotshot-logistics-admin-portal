// <copyright file="SendGridEmailService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Data.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Contracts.Services;
    using HotshotLogistics.Domain.DTOs;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.ValueObjects;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using SendGrid;
    using SendGrid.Helpers.Mail;

    /// <summary>
    /// Service for sending emails via SendGrid.
    /// </summary>
    public class SendGridEmailService : ICommunicationService
    {
        private readonly SendGridSettings _settings;
        private readonly ILogger<SendGridEmailService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailService"/> class.
        /// </summary>
        /// <param name="settings">The SendGrid settings.</param>
        /// <param name="logger">The logger.</param>
        public SendGridEmailService(
            IOptions<SendGridSettings> settings,
            ILogger<SendGridEmailService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public string Type => "Email";

        /// <inheritdoc/>
        public async Task<bool> SendAsync(CommunicationMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.To))
            {
                throw new ArgumentException("Recipient email address cannot be empty", nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.Body))
            {
                throw new ArgumentException("Message body cannot be empty", nameof(message));
            }

            try
            {
                // Format message with template data
                var formattedBody = FormatMessage(message.Body, message.TemplateData);

                var client = new SendGridClient(_settings.ApiKey);
                var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
                var to = new EmailAddress(message.To);
                var subject = message.Subject ?? "Notification";
                var plainTextContent = formattedBody;
                var htmlContent = $"<p>{formattedBody.Replace("\n", "<br>")}</p>"; // Simple HTML conversion

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg, cancellationToken);

                _logger.LogInformation("Email sent successfully to {To}. Status: {Status}", message.To, response.StatusCode);

                return response.StatusCode == System.Net.HttpStatusCode.Accepted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", message.To);
                return false;
            }
        }

        /// <summary>
        /// Formats a message template with the provided data.
        /// </summary>
        /// <param name="template">The message template.</param>
        /// <param name="data">The template data.</param>
        /// <returns>The formatted message.</returns>
        private static string FormatMessage(string template, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(template) || data == null || !data.Any())
            {
                return template;
            }

            var result = template;
            foreach (var kvp in data)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            return result;
        }
    }
}
