// <copyright file="UserProfileService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Contracts.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Domain.Entities;

    /// <summary>
    /// Interface for user profile management using Microsoft Graph API.
    /// </summary>
    public interface IUserProfileService
    {
        /// <summary>
        /// Gets the current user's profile information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user profile information.</returns>
        Task<UserProfile> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the current user's profile information.
        /// </summary>
        /// <param name="profile">The updated profile information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateCurrentUserProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes user profile data with the local database.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SyncUserProfileAsync(string userId, CancellationToken cancellationToken = default);
    }
}
