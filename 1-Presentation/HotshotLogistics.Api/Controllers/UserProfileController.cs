// <copyright file="UserProfileController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Controllers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Contracts.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// API controller for user profile management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService userProfileService;
        private readonly ILogger<UserProfileController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfileController"/> class.
        /// </summary>
        /// <param name="userProfileService">The user profile service.</param>
        /// <param name="logger">The logger.</param>
        public UserProfileController(
            IUserProfileService userProfileService,
            ILogger<UserProfileController> logger)
        {
            this.userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current user's profile information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user profile information.</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserProfile>> GetCurrentUserProfile(CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await this.userProfileService.GetCurrentUserProfileAsync(cancellationToken);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while retrieving current user profile");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates the current user's profile information.
        /// </summary>
        /// <param name="profile">The updated profile information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful.</returns>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCurrentUserProfile(
            [FromBody] UserProfile profile,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (profile == null)
                {
                    return BadRequest("Profile data is required");
                }

                await this.userProfileService.UpdateCurrentUserProfileAsync(profile, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while updating user profile");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Synchronizes the current user's profile with the local database.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful.</returns>
        [HttpPost("sync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SyncUserProfile(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the current user ID from claims
                var userId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                           ?? this.User.FindFirst("oid")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Unable to identify current user");
                }

                await this.userProfileService.SyncUserProfileAsync(userId, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while synchronizing user profile");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}
