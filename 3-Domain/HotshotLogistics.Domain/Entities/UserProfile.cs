// <copyright file="UserProfile.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    /// <summary>
    /// Represents a user profile from Microsoft Graph API.
    /// </summary>
    public class UserProfile 
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the given name.
        /// </summary>
        public string? GivenName { get; set; }

        /// <summary>
        /// Gets or sets the surname.
        /// </summary>
        public string? Surname { get; set; }

        /// <summary>
        /// Gets or sets the user principal name.
        /// </summary>
        public string? UserPrincipalName { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? Mail { get; set; }

        /// <summary>
        /// Gets or sets the job title.
        /// </summary>
        public string? JobTitle { get; set; }

        /// <summary>
        /// Gets or sets the department.
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Gets or sets the office location.
        /// </summary>
        public string? OfficeLocation { get; set; }

        /// <summary>
        /// Gets or sets the mobile phone number.
        /// </summary>
        public string? MobilePhone { get; set; }

        /// <summary>
        /// Gets or sets the business phone number.
        /// </summary>
        public string? BusinessPhones { get; set; }

        /// <summary>
        /// Gets or sets the preferred language.
        /// </summary>
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTimeOffset? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the user roles from Azure AD.
        /// </summary>
        public List<string>? Roles { get; set; }
    }
}
