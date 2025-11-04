using System.Collections.Generic;
using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Contracts.Services
{
    /// <summary>
    /// Factory abstraction for resolving payment processor implementations.
    /// </summary>
    public interface IPaymentProcessorFactory
    {
        /// <summary>
        /// Gets the default payment processor as configured in the system.
        /// </summary>
        /// <returns>The default <see cref="IPaymentProcessor"/>.</returns>
        IPaymentProcessor GetDefaultProcessor();

        /// <summary>
        /// Gets a payment processor by its provider name.
        /// </summary>
        /// <param name="processorName">The provider name (e.g., "Stripe", "PayPal").</param>
        /// <returns>The <see cref="IPaymentProcessor"/> implementation.</returns>
        IPaymentProcessor GetProcessor(string processorName);

        /// <summary>
        /// Returns all available payment processors registered in the container.
        /// </summary>
        /// <returns>An enumerable of all available processors.</returns>
        IEnumerable<IPaymentProcessor> GetAllProcessors();

        /// <summary>
        /// Resolves a payment processor for a specific payment method type.
        /// </summary>
        /// <param name="paymentMethodType">The payment method type.</param>
        /// <returns>The <see cref="IPaymentProcessor"/> best suited for the method.</returns>
        IPaymentProcessor GetProcessorForPaymentMethod(PaymentMethodType paymentMethodType);

        /// <summary>
        /// Gets the list of available processor names.
        /// </summary>
        /// <returns>Enumerable of processor names.</returns>
        IEnumerable<string> GetAvailableProcessorNames();
    }
}
