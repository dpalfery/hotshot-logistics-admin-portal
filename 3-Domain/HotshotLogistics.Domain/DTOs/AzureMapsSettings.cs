// <copyright file="AzureMapsSettings.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    /// Configuration settings for Azure Maps service.
    /// </summary>
    public class AzureMapsSettings
    {
        /// <summary>
        /// Gets or sets the Azure Maps subscription key.
        /// </summary>
        public string SubscriptionKey { get; set; } = string.Empty;
    }
}
