// <copyright file="PaymentProcessorFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Application.Services;

/// <summary>
/// Factory for creating payment processor instances.
/// </summary>
public class PaymentProcessorFactory : IPaymentProcessorFactory
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly ILogger<PaymentProcessorFactory> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentProcessorFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public PaymentProcessorFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PaymentProcessorFactory> logger)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the default payment processor.
    /// </summary>
    /// <returns>The default payment processor.</returns>
    public IPaymentProcessor GetDefaultProcessor()
    {
        var defaultProcessor = configuration["Payment:DefaultProcessor"] ?? "Stripe";
        return GetProcessor(defaultProcessor);
    }

    /// <summary>
    /// Gets a payment processor by name.
    /// </summary>
    /// <param name="processorName">The name of the processor.</param>
    /// <returns>The payment processor instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the processor name is not supported.</exception>
    public IPaymentProcessor GetProcessor(string processorName)
    {
        logger.LogDebug("Getting payment processor: {ProcessorName}", processorName);

        return processorName.ToLowerInvariant() switch
        {
            "stripe" => serviceProvider.GetRequiredService<StripePaymentProcessor>(),
            "paypal" => serviceProvider.GetRequiredService<PayPalPaymentProcessor>(),
            _ => throw new ArgumentException($"Unsupported payment processor: {processorName}", nameof(processorName))
        };
    }

    /// <summary>
    /// Gets all available payment processors.
    /// </summary>
    /// <returns>A collection of all payment processors.</returns>
    public IEnumerable<IPaymentProcessor> GetAllProcessors()
    {
        return new List<IPaymentProcessor>
        {
            serviceProvider.GetRequiredService<StripePaymentProcessor>(),
            serviceProvider.GetRequiredService<PayPalPaymentProcessor>()
        };
    }

    /// <summary>
    /// Gets a payment processor based on the payment method type.
    /// </summary>
    /// <param name="paymentMethodType">The payment method type.</param>
    /// <returns>The appropriate payment processor.</returns>
    public IPaymentProcessor GetProcessorForPaymentMethod(PaymentMethodType paymentMethodType)
    {
        // For now, use default processor for all payment methods
        // In the future, this could route specific payment methods to specific processors
        logger.LogDebug("Getting processor for payment method type");
        return GetDefaultProcessor();
    }

    /// <summary>
    /// Gets the available processor names.
    /// </summary>
    /// <returns>A collection of processor names.</returns>
    public IEnumerable<string> GetAvailableProcessorNames()
    {
        return new[] { "Stripe", "PayPal" };
    }
}
