// <copyright file="ExceptionHandlingMiddlewareTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HotshotLogistics.Api.Middleware;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Core.Exceptions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    using HotshotLogistics.Domain.DTOs;

    /// <summary>
    /// Integration tests for the ExceptionHandlingMiddleware.
    /// </summary>
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddlewareTests"/> class.
        /// </summary>
        public ExceptionHandlingMiddlewareTests()
        {
            mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        }

        /// <summary>
        /// Tests that ValidationException returns BadRequest with field errors.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_ValidationException_ReturnsBadRequestWithFieldErrors()
        {
            // Arrange
            var fieldErrors = new Dictionary<string, string[]>
            {
                { "Title", new[] { "Title is required" } },
                { "Amount", new[] { "Amount must be greater than 0" } }
            };

            var exception = new ValidationException("Validation failed", fieldErrors);

            var response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Validation failed");
            errorResponse.ErrorCode.Should().Be("VALIDATION_ERROR");
            errorResponse.FieldErrors.Should().BeEquivalentTo(fieldErrors);
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
            errorResponse.Path.Should().Be("/test");
        }

        /// <summary>
        /// Tests that BusinessRuleException returns UnprocessableEntity.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_BusinessRuleException_ReturnsUnprocessableEntity()
        {
            // Arrange
            var exception = new BusinessRuleException("Business rule violated");

            var response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Business rule violated");
            errorResponse.ErrorCode.Should().Be("BUSINESS_RULE_VIOLATION");
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that PaymentProcessingException returns PaymentRequired.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_PaymentProcessingException_ReturnsPaymentRequired()
        {
            // Arrange
            var exception = new PaymentProcessingException("Payment failed", "txn123", "CARD_DECLINED");

            var response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);

            var errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Payment failed");
            errorResponse.ErrorCode.Should().Be("PAYMENT_PROCESSING_ERROR");
            errorResponse.Details.Should().NotBeNull();

            var detailsJson = errorResponse.Details.ToString();
            var detailsElement = JsonSerializer.Deserialize<JsonElement>(detailsJson!);

            detailsElement.GetProperty("transactionId").GetString().Should().Be("txn123");
            detailsElement.GetProperty("providerErrorCode").GetString().Should().Be("CARD_DECLINED");
        }

        /// <summary>
        /// Tests that ExternalServiceException returns BadGateway.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_ExternalServiceException_ReturnsBadGateway()
        {
            // Arrange
            var exception = new ExternalServiceException("MapsAPI", "Service unavailable", 503, "/geocode");

            var response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

            var errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Service unavailable");
            errorResponse.ErrorCode.Should().Be("EXTERNAL_SERVICE_ERROR");
            errorResponse.Details.Should().NotBeNull();

            var detailsJson = errorResponse.Details.ToString();
            var detailsElement = JsonSerializer.Deserialize<JsonElement>(detailsJson!);

            detailsElement.GetProperty("serviceName").GetString().Should().Be("MapsAPI");
            detailsElement.GetProperty("statusCode").GetInt32().Should().Be(503);
            detailsElement.GetProperty("endpoint").GetString().Should().Be("/geocode");
        }

        /// <summary>
        /// Tests that generic Exception returns InternalServerError.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var exception = new Exception("Unexpected error");

            var response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("An unexpected error occurred. Please try again later.");
            errorResponse.ErrorCode.Should().Be("INTERNAL_SERVER_ERROR");
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that correlation ID is preserved from request header.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_WithCorrelationIdInHeader_PreservesCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-id";
            var exception = new ValidationException("Validation failed");

            var response = await ExecuteMiddlewareTestAsync(exception, correlationId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.CorrelationId.Should().Be(correlationId);

            // Check response header
            response.Headers.Should().ContainKey("X-Correlation-ID");
            response.Headers.GetValues("X-Correlation-ID").FirstOrDefault().Should().Be(correlationId);
        }

        /// <summary>
        /// Tests that correlation ID is generated when not provided.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_WithoutCorrelationId_GeneratesNewCorrelationId()
        {
            // Arrange
            var exception = new BusinessRuleException("Rule violated");

            var response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            var errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();

            response.Headers.Should().ContainKey("X-Correlation-ID");
            response.Headers.GetValues("X-Correlation-ID").FirstOrDefault().Should().Be(errorResponse.CorrelationId);
        }

        private async Task<HttpResponseMessage> ExecuteMiddlewareTestAsync(Exception exception, string? correlationId = null)
        {
            var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(mockLogger.Object);
                })
                .Configure(app =>
                {
                    app.UseExceptionHandling();
                    app.Run(context =>
                    {
                        throw exception;
                    });
                }));

            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "/test");
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add("X-Correlation-ID", correlationId);
            }

            return await client.SendAsync(request);
        }

        private async Task<ErrorResponse> DeserializeErrorResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<ErrorResponse>(content, options)!;
        }
    }
}
