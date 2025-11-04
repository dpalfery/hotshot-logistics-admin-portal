// <copyright file="JobAssignmentsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Controllers
{
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.ValueObjects;
    using HotshotLogistics.Core.Enums;
    using HotshotLogistics.Domain.DTOs;
    using HotshotLogistics.Contracts.Services;
    using Microsoft.AspNetCore.Authorization;
    using HotshotLogistics.Application.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// API controller for managing job assignments.
	/// </summary>
 	[Authorize]
 	[ApiController]
 	[Route("api/[controller]")]
    public class JobAssignmentsController : ControllerBase
    {
        private readonly IJobAssignmentService assignmentService;
        private readonly ILogger<JobAssignmentsController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobAssignmentsController"/> class.
        /// </summary>
        /// <param name="assignmentService">The job assignment service.</param>
        /// <param name="logger">The logger.</param>
        public JobAssignmentsController(
            IJobAssignmentService assignmentService,
            ILogger<JobAssignmentsController> logger)
        {
            this.assignmentService = assignmentService ?? throw new ArgumentNullException(nameof(assignmentService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a job assignment by ID.
        /// </summary>
        /// <param name="id">The assignment ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The job assignment if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(JobAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JobAssignmentDto>> GetById(string id, CancellationToken cancellationToken = default)
        {

            var assignment = await assignmentService.GetByIdAsync(id, cancellationToken);
            if (assignment == null)
            {
                return NotFound();
            }

            return Ok(assignment);

        }

        /// <summary>
        /// Gets all job assignments.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments.</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetAll(CancellationToken cancellationToken = default)
        {
            var assignments = await assignmentService.GetAllAsync(cancellationToken);
            return Ok(assignments);
        }

        /// <summary>
        /// Gets job assignments by driver ID.
        /// </summary>
        /// <param name="driverId">The driver ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments for the driver.</returns>
        [HttpGet("driver/{driverId}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrDriver)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetByDriverId(int driverId, CancellationToken cancellationToken = default)
        {
            var assignments = await this.assignmentService.GetByDriverIdAsync(driverId, cancellationToken);
            return this.Ok(assignments);
        }

        /// <summary>
        /// Gets job assignments by job ID.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments for the job.</returns>
        [HttpGet("job/{jobId}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetByJobId(string jobId, CancellationToken cancellationToken = default)
        {
            var assignments = await this.assignmentService.GetByJobIdAsync(jobId, cancellationToken);
            return this.Ok(assignments);
        }

        /// <summary>
        /// Gets active job assignments.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of active job assignments.</returns>
        [HttpGet("active")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetActive(CancellationToken cancellationToken = default)
        {

            var assignments = await this.assignmentService.GetActiveAssignmentsAsync(cancellationToken);
            return this.Ok(assignments);
        }

        /// <summary>
        /// Assigns a job to a driver.
        /// </summary>
        /// <param name="request">The assignment request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created job assignment.</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(JobAssignmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<JobAssignmentDto>> AssignJob(
            [FromBody] AssignJobRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var assignment = await this.assignmentService.AssignJobAsync(
                    request.JobId,
                    request.DriverId,
                    cancellationToken);

                return this.CreatedAtAction(
                    nameof(this.GetById),
                    new { id = assignment.Id },
                    assignment);
            }
            catch (KeyNotFoundException ex)
            {

                this.logger.LogWarning(ex, "Failed to assign job: {Message}", ex.Message);
                return this.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                this.logger.LogWarning(ex, "Failed to assign job: {Message}", ex.Message);
                return this.Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while assigning job");
                return this.StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates a job assignment status.
        /// </summary>
        /// <param name="id">The assignment ID.</param>
        /// <param name="status">The new status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated job assignment.</returns>
        [HttpPut("{id}/status")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrDriver)]
        [ProducesResponseType(typeof(JobAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JobAssignmentDto>> UpdateStatus(
            string id,
            [FromBody] JobAssignmentStatus status,
            CancellationToken cancellationToken = default)
        {
            try
            {

                var assignment = await this.assignmentService.UpdateAssignmentStatusAsync(id, status, cancellationToken);
                return this.Ok(assignment);
            }
            catch (KeyNotFoundException ex)
            {
                this.logger.LogWarning(ex, "Failed to update assignment status: {Message}", ex.Message);
                return this.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while updating assignment status");
                return this.StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Unassigns a job from a driver.
        /// </summary>
        /// <param name="id">The assignment ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, 404 Not Found.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnassignJob(string id, CancellationToken cancellationToken = default)
        {
            var result = await this.assignmentService.UnassignJobAsync(id, cancellationToken);
            if (!result)
            {
                return this.NotFound();
            }

            return this.NoContent();
        }
    }

    /// <summary>
    /// Request model for assigning a job to a driver.
    /// </summary>
    public class AssignJobRequest
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the driver ID.
        /// </summary>
        public int DriverId { get; set; }
    }
}
