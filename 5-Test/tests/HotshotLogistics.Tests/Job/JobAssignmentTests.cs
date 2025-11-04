// <copyright file="JobAssignmentTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Job
{

    using System;
    using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Entities;
    using Xunit;

    /// <summary>
    /// Tests for the JobAssignment class.
    /// </summary>
    public class JobAssignmentTests
    {
        /// <summary>
        /// Tests that the default constructor sets appropriate default values.
        /// </summary>
        [Fact]
        public void Default_Constructor_Sets_Defaults()
        {
            var assignment = new JobAssignment();
            Assert.False(string.IsNullOrWhiteSpace(assignment.Id));
            Assert.Equal(string.Empty, assignment.JobId);
            Assert.Equal(0, assignment.DriverId);
            Assert.True((DateTime.UtcNow - assignment.AssignedAt).TotalSeconds < 5); // AssignedAt is now
            Assert.Equal(JobAssignmentStatus.Active, assignment.Status);
            Assert.Null(assignment.UpdatedAt);
        }

        /// <summary>
        /// Tests that properties can be assigned values.
        /// </summary>
        [Fact]
        public void Can_Assign_Properties()
        {
            var assignment = new JobAssignment
            {
                Id = "test-id",
                JobId = "job-123",
                DriverId = 42,
                AssignedAt = new DateTime(2024, 6, 16, 12, 0, 0, DateTimeKind.Utc),
                Status = JobAssignmentStatus.Completed,
                UpdatedAt = new DateTime(2024, 6, 17, 8, 0, 0, DateTimeKind.Utc)
            };
            Assert.Equal("test-id", assignment.Id);
            Assert.Equal("job-123", assignment.JobId);
            Assert.Equal(42, assignment.DriverId);
            Assert.Equal(new DateTime(2024, 6, 16, 12, 0, 0, DateTimeKind.Utc), assignment.AssignedAt);
            Assert.Equal(JobAssignmentStatus.Completed, assignment.Status);
            Assert.Equal(new DateTime(2024, 6, 17, 8, 0, 0, DateTimeKind.Utc), assignment.UpdatedAt);
        }

        /// <summary>
        /// Tests that the status can be transitioned.
        /// </summary>
        [Fact]
        public void Can_Transition_Status()
        {
            var assignment = new JobAssignment();
            Assert.Equal(JobAssignmentStatus.Active, assignment.Status);
            assignment.Status = JobAssignmentStatus.Completed;
            Assert.Equal(JobAssignmentStatus.Completed, assignment.Status);
        }

        /// <summary>
        /// Tests that navigation properties can be set.
        /// </summary>
        [Fact]
        public void Can_Set_Navigation_Properties()
        {
            var job = new Domain.Entities.Job { Id = "job-1", Title = "Test Job" };
            var driver = new Driver
            {
                Id = 1,
                PersonalInfo = new PersonalInfo { FirstName = "Alice", LastName = "Smith" }
            };
            var assignment = new JobAssignment
            {
                Job = job,
                Driver = driver
            };
            Assert.Equal(job, assignment.Job);
            Assert.Equal(driver, assignment.Driver);
        }
    }
}
