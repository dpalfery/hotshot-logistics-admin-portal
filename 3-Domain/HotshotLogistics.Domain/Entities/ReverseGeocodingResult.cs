// <copyright file="ReverseGeocodingResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
   

    /// <summary>
    /// Represents the result of a reverse geocoding operation.
    /// </summary>
    public class ReverseGeocodingResult 
    {
        /// <inheritdoc/>
        public string Address { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string City { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string State { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string PostalCode { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string Country { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string FormattedAddress { get; set; } = string.Empty;

        /// <inheritdoc/>
        public bool IsValid { get; set; }

        /// <inheritdoc/>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
