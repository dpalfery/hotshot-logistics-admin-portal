// <copyright file="CargoDetails.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.ValueObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
   

    /// <summary>
    /// Represents details about cargo being transported.
    /// </summary>
    public class CargoDetails 
    {
        /// <summary>
        /// Gets or sets the description of the cargo.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the weight of the cargo in pounds.
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// Gets or sets the dimensions of the cargo (length x width x height).
        /// </summary>
        public string Dimensions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity or number of pieces.
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Gets or sets the value of the cargo for insurance purposes.
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cargo is fragile.
        /// </summary>
        public bool IsFragile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cargo is hazardous.
        /// </summary>
        public bool IsHazardous { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cargo requires temperature control.
        /// </summary>
        public bool RequiresTemperatureControl { get; set; }

        /// <summary>
        /// Gets or sets the required temperature range if temperature control is needed.
        /// </summary>
        public string TemperatureRange { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets any special handling instructions.
        /// </summary>
        public string SpecialInstructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the packaging type.
        /// </summary>
        public string PackagingType { get; set; } = string.Empty;

        /// <summary>
        /// Validates the cargo details.
        /// </summary>
        /// <returns>True if the cargo details are valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Description) &&
                   Weight > 0 &&
                   Quantity > 0 &&
                   Value >= 0 &&
                   (!RequiresTemperatureControl || !string.IsNullOrWhiteSpace(TemperatureRange));
        }

        /// <summary>
        /// Gets a summary of special requirements.
        /// </summary>
        /// <returns>A string describing special requirements.</returns>
        public string GetSpecialRequirements()
        {
            var requirements = new List<string>();

            if (IsFragile)
                requirements.Add("Fragile");

            if (IsHazardous)
                requirements.Add("Hazardous");

            if (RequiresTemperatureControl)
                requirements.Add($"Temperature Control ({TemperatureRange})");

            return requirements.Any() ? string.Join(", ", requirements) : "None";
        }
    }
}
