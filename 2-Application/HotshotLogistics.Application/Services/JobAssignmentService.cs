// <copyright file="JobAssignmentService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    /// Service implementation for job assignment operations.
    /// </summary>
    public class JobAssignmentService : IJobAssignmentService
    {
        private readonly IJobAssignmentRepository assignmentRepository;
        private readonly IJobRepository jobRepository;
        private readonly IDriverRepository driverRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobAssignmentService"/> class.
        /// </summary>
        /// <param name="assignmentRepository">The job assignment repository.</param>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="driverRepository">The driver repository.</param>
        public JobAssignmentService(
            IJobAssignmentRepository assignmentRepository,
            IJobRepository jobRepository,
            IDriverRepository driverRepository)
        {
            this.assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
            this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            this.driverRepository = driverRepository ?? throw new ArgumentNullException(nameof(driverRepository));
        }

        /// <inheritdoc/>
        public async Task<JobAssignmentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Assignment ID cannot be empty.", nameof(id));
            }

            return await this.assignmentRepository.GetByIdAsync(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<JobAssignmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await this.assignmentRepository.GetAllAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<JobAssignmentDto>> GetByDriverIdAsync(int driverId, CancellationToken cancellationToken = default)
        {
            if (driverId <= 0)
            {
                throw new ArgumentException("Driver ID must be greater than zero.", nameof(driverId));
            }

            return await this.assignmentRepository.GetByDriverIdAsync(driverId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<JobAssignmentDto>> GetByJobIdAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be empty.", nameof(jobId));
            }

            return await this.assignmentRepository.GetByJobIdAsync(jobId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<JobAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            return await this.assignmentRepository.GetActiveAssignmentsAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<JobAssignmentDto> AssignJobAsync(string jobId, int driverId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job ID cannot be empty.", nameof(jobId));
            }

            if (driverId <= 0)
            {
                throw new ArgumentException("Driver ID must be greater than zero.", nameof(driverId));
            }

            // Verify job exists
            var job = await this.jobRepository.GetJobByIdAsync(jobId, cancellationToken);

            if (job == null)
            {
                throw new KeyNotFoundException($"Job with ID {jobId} not found.");
            }

            // Verify driver exists
            var driver = await this.driverRepository.GetDriverByIdAsync(driverId, cancellationToken);
            if (driver == null)
            {
                throw new KeyNotFoundException($"Driver with ID {driverId} not found.");
            }

            // Check if assignment already exists
            var existingAssignments = await this.assignmentRepository.GetByJobIdAsync(jobId, cancellationToken);
            var activeAssignment = existingAssignments.FirstOrDefault(a => a.Status == JobAssignmentStatus.Active);

            if (activeAssignment != null)
            {
                throw new InvalidOperationException($"Job {jobId} is already assigned to driver {activeAssignment.DriverId}.");
            }

            var assignment = new JobAssignmentDto
            {
                JobId = jobId,
                DriverId = driverId,
                AssignedAt = DateTime.UtcNow,
                Status = JobAssignmentStatus.Active
            };

            return await this.assignmentRepository.CreateAsync(assignment, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<JobAssignmentDto> UpdateAssignmentStatusAsync(string id, JobAssignmentStatus status, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Assignment ID cannot be empty.", nameof(id));
            }

            var assignment = await this.assignmentRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Assignment with ID {id} not found.");

            assignment.Status = status;
            return await this.assignmentRepository.UpdateAsync(assignment, cancellationToken);

        }

        /// <inheritdoc/>
        public async Task<bool> UnassignJobAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Assignment ID cannot be empty.", nameof(id));
            }

            return await this.assignmentRepository.DeleteAsync(id, cancellationToken);

        }
    }
}
