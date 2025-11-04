// <copyright file="JobRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Core.Repositories;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Data.Repositories
#pragma warning disable SA1202 // False positive - public members are correctly ordered before private members
{
    /// <summary>
    /// Repository for managing Job entities using native ADO.NET.
    /// </summary>
internal class JobRepository : BaseRepository<Job>, IJobRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public JobRepository(IConfiguration configuration)
            : base(configuration)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Job>> GetJobsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllAsync();
        }

        /// <inheritdoc/>
        public async Task<Job?> GetJobByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await GetByIdAsync(id);
        }

        /// <inheritdoc/>
        public async Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default)
        {
            job.CreatedAt = DateTime.UtcNow;
            return await AddAsync(job);
        }

        /// <inheritdoc/>
        public async Task<Job?> UpdateJobAsync(string id, Job jobDetails, CancellationToken cancellationToken = default)
        {
            var existingJob = await GetByIdAsync(id);
            if (existingJob == null)
            {
                return null;
            }

            existingJob.Title = jobDetails.Title;
            existingJob.CustomerId = jobDetails.CustomerId;
            existingJob.PickupLocation = jobDetails.PickupLocation;
            existingJob.DeliveryLocation = jobDetails.DeliveryLocation;
            existingJob.Status = jobDetails.Status;
            existingJob.Priority = jobDetails.Priority;
            existingJob.Pricing = jobDetails.Pricing;
            existingJob.EstimatedDeliveryTime = jobDetails.EstimatedDeliveryTime;
            existingJob.ScheduledPickupTime = jobDetails.ScheduledPickupTime;
            existingJob.AssignedDriverId = jobDetails.AssignedDriverId;
            existingJob.SpecialInstructions = jobDetails.SpecialInstructions;
            existingJob.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(existingJob);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteJobAsync(string id, CancellationToken cancellationToken = default)
        {
            return await DeleteAsync(id);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Job>> GetJobsAsync(
            JobFilterDto? filter = null,
            PaginationParameters? pagination = null,
            SortParameters? sort = null,
            CancellationToken cancellationToken = default)
        {
            pagination ??= new PaginationParameters();
            sort ??= new SortParameters();

            var (whereClause, parameters) = BuildWhereClause(filter);
            var orderByClause = BuildOrderByClause(sort);

            // Log the query details for debugging
            Console.WriteLine($"[DEBUG] JobRepository.GetJobsAsync - Filter: {filter}, Pagination: {pagination}, Sort: {sort}");
            Console.WriteLine($"[DEBUG] JobRepository.GetJobsAsync - WhereClause: {whereClause}");
            Console.WriteLine($"[DEBUG] JobRepository.GetJobsAsync - OrderByClause: {orderByClause}");

            var countQuery = $"SELECT COUNT(*) FROM {GetTableName()}{whereClause}";
            var dataQuery = $@"
                SELECT * FROM {GetTableName()}
                {whereClause}
                {orderByClause}
                OFFSET @Skip ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var jobs = new List<Job>();
            int totalCount = 0;

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Get total count
            using (var countCommand = new SqlCommand(countQuery, connection))
            {
                AddParametersToCommand(countCommand, parameters);
                totalCount = (int)await countCommand.ExecuteScalarAsync(cancellationToken);
            }

            // Get paged data
            using (var dataCommand = new SqlCommand(dataQuery, connection))
            {
                AddParametersToCommand(dataCommand, parameters);
                dataCommand.Parameters.AddWithValue("@Skip", pagination.Skip);
                dataCommand.Parameters.AddWithValue("@PageSize", pagination.PageSize);

                await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    jobs.Add(MapReaderToEntity(reader));
                }
            }

            var result = new PagedResult<Job>
            {
                Items = jobs,
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize,
            };

            // Log the results for debugging
            Console.WriteLine($"[DEBUG] JobRepository.GetJobsAsync - TotalCount: {totalCount}, Items returned: {jobs.Count}");
            Console.WriteLine($"[DEBUG] JobRepository.GetJobsAsync - PageNumber: {pagination.PageNumber}, PageSize: {pagination.PageSize}");

            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status, CancellationToken cancellationToken = default)
        {
            var query = $"SELECT * FROM {GetTableName()} WHERE Status = @Status ORDER BY CreatedAt DESC";
            var jobs = new List<Job>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Status", (int)status);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                jobs.Add(MapReaderToEntity(reader));
            }

            return jobs;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Job>> GetJobsByDriverAsync(int driverId, CancellationToken cancellationToken = default)
        {
            var query = $"SELECT * FROM {GetTableName()} WHERE AssignedDriverId = @DriverId ORDER BY ScheduledPickupTime ASC";
            var jobs = new List<Job>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DriverId", driverId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                jobs.Add(MapReaderToEntity(reader));
            }

            return jobs;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Job>> GetJobsByCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            var query = $"SELECT * FROM {GetTableName()} WHERE CustomerId = @CustomerId ORDER BY CreatedAt DESC";
            var jobs = new List<Job>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CustomerId", customerId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                jobs.Add(MapReaderToEntity(reader));
            }

            return jobs;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Job>> GetOverdueJobsAsync(CancellationToken cancellationToken = default)
        {
            var query = $@"
                SELECT * FROM {GetTableName()}
                WHERE EstimatedDeliveryTime < @CurrentTime
                AND Status NOT IN (@DeliveredStatus, @CancelledStatus)
                ORDER BY EstimatedDeliveryTime ASC";
            var jobs = new List<Job>();

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CurrentTime", DateTime.UtcNow);
            command.Parameters.AddWithValue("@DeliveredStatus", (int)JobStatus.Received);
            command.Parameters.AddWithValue("@CancelledStatus", (int)JobStatus.Pending);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                jobs.Add(MapReaderToEntity(reader));
            }

            return jobs;
        }

        /// <inheritdoc/>
        public async Task<int> GetJobCountAsync(JobFilterDto? filter = null, CancellationToken cancellationToken = default)
        {
            var (whereClause, parameters) = BuildWhereClause(filter);
            var query = $"SELECT COUNT(*) FROM {GetTableName()}{whereClause}";

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            AddParametersToCommand(command, parameters);

            return (int)await command.ExecuteScalarAsync(cancellationToken);
        }

        public async Task<IEnumerable<Job>> GetByDriverIdAsync(int driverId, CancellationToken cancellationToken = default)
        {
            return await GetJobsByDriverAsync(driverId, cancellationToken);
        }

        /// <inheritdoc/>
        protected override string GetTableName() => "Jobs";

        /// <inheritdoc/>
        protected override string GetPrimaryKeyColumnName() => "Id";

        /// <inheritdoc/>
        protected override Job MapReaderToEntity(SqlDataReader reader)
        {
            return new Job
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("CustomerId")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                PickupLocation = new Location
                {
                    Address = reader.GetString(reader.GetOrdinal("PickupAddress"))
                },
                DeliveryLocation = new Location
                {
                    Address = reader.GetString(reader.GetOrdinal("DeliveryAddress"))
                },
                Status = (JobStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Priority = (JobPriority)reader.GetInt32(reader.GetOrdinal("Priority")),
                Pricing = new PricingDetails { TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")) },
                EstimatedDeliveryTime = reader.GetDateTime(reader.GetOrdinal("EstimatedDeliveryTime")),
                ScheduledPickupTime = reader.IsDBNull(reader.GetOrdinal("ScheduledPickupTime")) ? DateTime.UtcNow : reader.GetDateTime(reader.GetOrdinal("ScheduledPickupTime")),
                AssignedDriverId = reader.IsDBNull(reader.GetOrdinal("AssignedDriverId")) ? null : reader.GetInt32(reader.GetOrdinal("AssignedDriverId")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                SpecialInstructions = reader.IsDBNull(reader.GetOrdinal("SpecialInstructions")) ? string.Empty : reader.GetString(reader.GetOrdinal("SpecialInstructions")),
            };
        }

        /// <inheritdoc/>
        protected override SqlParameter[] GetInsertParameters(Job entity)
        {
            return new[]
            {
                new SqlParameter("@Id", SqlDbType.NVarChar) { Value = entity.Id },
                new SqlParameter("@CustomerId", SqlDbType.NVarChar) { Value = entity.CustomerId },
                new SqlParameter("@Title", SqlDbType.NVarChar) { Value = entity.Title },
                new SqlParameter("@PickupAddress", SqlDbType.NVarChar) { Value = entity.PickupLocation.Address },
                new SqlParameter("@DeliveryAddress", SqlDbType.NVarChar) { Value = entity.DeliveryLocation.Address },
                new SqlParameter("@Status", SqlDbType.Int) { Value = (int)entity.Status },
                new SqlParameter("@Priority", SqlDbType.Int) { Value = (int)entity.Priority },
                new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Value = entity.Pricing.TotalAmount },
                new SqlParameter("@EstimatedDeliveryTime", SqlDbType.DateTime2) { Value = entity.EstimatedDeliveryTime },
                new SqlParameter("@ScheduledPickupTime", SqlDbType.DateTime2) { Value = entity.ScheduledPickupTime },
                new SqlParameter("@AssignedDriverId", SqlDbType.Int) { Value = (object?)entity.AssignedDriverId ?? DBNull.Value },
                new SqlParameter("@SpecialInstructions", SqlDbType.NVarChar) { Value = (object?)entity.SpecialInstructions ?? DBNull.Value },
            };
        }

        /// <inheritdoc/>
        protected override SqlParameter[] GetUpdateParameters(Job entity)
        {
            return new[]
            {
                new SqlParameter("@Id", SqlDbType.NVarChar) { Value = entity.Id },
                new SqlParameter("@CustomerId", SqlDbType.NVarChar) { Value = entity.CustomerId },
                new SqlParameter("@Title", SqlDbType.NVarChar) { Value = entity.Title },
                new SqlParameter("@PickupAddress", SqlDbType.NVarChar) { Value = entity.PickupLocation.Address },
                new SqlParameter("@DeliveryAddress", SqlDbType.NVarChar) { Value = entity.DeliveryLocation.Address },
                new SqlParameter("@Status", SqlDbType.Int) { Value = (int)entity.Status },
                new SqlParameter("@Priority", SqlDbType.Int) { Value = (int)entity.Priority },
                new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Value = entity.Pricing.TotalAmount },
                new SqlParameter("@EstimatedDeliveryTime", SqlDbType.DateTime2) { Value = entity.EstimatedDeliveryTime },
                new SqlParameter("@ScheduledPickupTime", SqlDbType.DateTime2) { Value = entity.ScheduledPickupTime },
                new SqlParameter("@AssignedDriverId", SqlDbType.Int) { Value = (object?)entity.AssignedDriverId ?? DBNull.Value },
                new SqlParameter("@SpecialInstructions", SqlDbType.NVarChar) { Value = (object?)entity.SpecialInstructions ?? DBNull.Value },
            };
        }

        /// <summary>
        /// Builds the WHERE clause and parameters for filtering jobs.
        /// </summary>
        /// <param name="filter">The job filter criteria.</param>
        /// <returns>A tuple containing the WHERE clause and SQL parameters.</returns>
        private static (string WhereClause, IEnumerable<SqlParameter> Parameters) BuildWhereClause(JobFilterDto? filter)
        {
            if (filter == null)
            {
                return (string.Empty, new List<SqlParameter>());
            }

            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.Status.HasValue)
            {
                conditions.Add("Status = @Status");
                parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = (int)filter.Status.Value });
            }

            if (!string.IsNullOrEmpty(filter.CustomerId))
            {
                conditions.Add("CustomerId = @CustomerId");
                parameters.Add(new SqlParameter("@CustomerId", SqlDbType.NVarChar) { Value = filter.CustomerId });
            }

            if (filter.DriverId.HasValue)
            {
                conditions.Add("AssignedDriverId = @DriverId");
                parameters.Add(new SqlParameter("@DriverId", SqlDbType.Int) { Value = filter.DriverId.Value });
            }

            if (filter.AssignedDriverId.HasValue)
            {
                conditions.Add("AssignedDriverId = @AssignedDriverId");
                parameters.Add(new SqlParameter("@AssignedDriverId", SqlDbType.Int) { Value = filter.AssignedDriverId.Value });
            }

            if (filter.HasAssignedDriver.HasValue)
            {
                if (filter.HasAssignedDriver.Value)
                {
                    conditions.Add("AssignedDriverId IS NOT NULL");
                }
                else
                {
                    conditions.Add("AssignedDriverId IS NULL");
                }
            }

            if (filter.Priority.HasValue)
            {
                conditions.Add("Priority = @Priority");
                parameters.Add(new SqlParameter("@Priority", SqlDbType.Int) { Value = (int)filter.Priority.Value });
            }

            if (filter.CreatedAfter.HasValue)
            {
                conditions.Add("CreatedAt >= @CreatedAfter");
                parameters.Add(new SqlParameter("@CreatedAfter", SqlDbType.DateTime2) { Value = filter.CreatedAfter.Value });
            }

            if (filter.CreatedBefore.HasValue)
            {
                conditions.Add("CreatedAt <= @CreatedBefore");
                parameters.Add(new SqlParameter("@CreatedBefore", SqlDbType.DateTime2) { Value = filter.CreatedBefore.Value });
            }

            if (filter.ScheduledPickupAfter.HasValue)
            {
                conditions.Add("ScheduledPickupTime >= @ScheduledPickupAfter");
                parameters.Add(new SqlParameter("@ScheduledPickupAfter", SqlDbType.DateTime2) { Value = filter.ScheduledPickupAfter.Value });
            }

            if (filter.ScheduledPickupBefore.HasValue)
            {
                conditions.Add("ScheduledPickupTime <= @ScheduledPickupBefore");
                parameters.Add(new SqlParameter("@ScheduledPickupBefore", SqlDbType.DateTime2) { Value = filter.ScheduledPickupBefore.Value });
            }

            if (filter.ScheduledAfter.HasValue)
            {
                conditions.Add("ScheduledPickupTime >= @ScheduledAfter");
                parameters.Add(new SqlParameter("@ScheduledAfter", SqlDbType.DateTime2) { Value = filter.ScheduledAfter.Value });
            }

            if (filter.ScheduledBefore.HasValue)
            {
                conditions.Add("ScheduledPickupTime <= @ScheduledBefore");
                parameters.Add(new SqlParameter("@ScheduledBefore", SqlDbType.DateTime2) { Value = filter.ScheduledBefore.Value });
            }

            if (filter.StatusList != null && filter.StatusList.Any())
            {
                var statusPlaceholders = string.Join(", ", filter.StatusList.Select((_, i) => $"@Status{i}"));
                conditions.Add($"Status IN ({statusPlaceholders})");
                for (var i = 0; i < filter.StatusList.Count; i++)
                {
                    parameters.Add(new SqlParameter($"@Status{i}", SqlDbType.Int) { Value = (int)filter.StatusList[i] });
                }
            }

            if (filter.EstimatedDeliveryAfter.HasValue)
            {
                conditions.Add("EstimatedDeliveryTime >= @EstimatedDeliveryAfter");
                parameters.Add(new SqlParameter("@EstimatedDeliveryAfter", SqlDbType.DateTime2) { Value = filter.EstimatedDeliveryAfter.Value });
            }

            if (filter.EstimatedDeliveryBefore.HasValue)
            {
                conditions.Add("EstimatedDeliveryTime <= @EstimatedDeliveryBefore");
                parameters.Add(new SqlParameter("@EstimatedDeliveryBefore", SqlDbType.DateTime2) { Value = filter.EstimatedDeliveryBefore.Value });
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                conditions.Add("(Title LIKE @SearchTerm OR SpecialInstructions LIKE @SearchTerm)");
                parameters.Add(new SqlParameter("@SearchTerm", SqlDbType.NVarChar) { Value = $"%{filter.SearchTerm}%" });
            }

            if (filter.MinAmount.HasValue)
            {
                conditions.Add("TotalAmount >= @MinAmount");
                parameters.Add(new SqlParameter("@MinAmount", SqlDbType.Decimal) { Value = filter.MinAmount.Value });
            }

            if (filter.MaxAmount.HasValue)
            {
                conditions.Add("TotalAmount <= @MaxAmount");
                parameters.Add(new SqlParameter("@MaxAmount", SqlDbType.Decimal) { Value = filter.MaxAmount.Value });
            }

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value)
                {
                    conditions.Add("Status NOT IN (@ReceivedStatus)");
                    parameters.Add(new SqlParameter("@ReceivedStatus", SqlDbType.Int) { Value = (int)JobStatus.Received });
                }
                else
                {
                    conditions.Add("Status IN (@ReceivedStatus)");
                    parameters.Add(new SqlParameter("@ReceivedStatus", SqlDbType.Int) { Value = (int)JobStatus.Received });
                }
            }

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
            {
                conditions.Add("EstimatedDeliveryTime < @CurrentTime AND Status NOT IN (@DeliveredStatus)");
                parameters.Add(new SqlParameter("@CurrentTime", SqlDbType.DateTime2) { Value = DateTime.UtcNow });
                parameters.Add(new SqlParameter("@DeliveredStatus", SqlDbType.Int) { Value = (int)JobStatus.Received });
            }

            var whereClause = conditions.Any() ? $" WHERE {string.Join(" AND ", conditions)}" : string.Empty;
            return (whereClause, parameters);
        }

        /// <summary>
        /// Builds the ORDER BY clause for sorting jobs.
        /// </summary>
        /// <param name="sort">The sort parameters.</param>
        /// <returns>The ORDER BY clause string.</returns>
        private static string BuildOrderByClause(SortParameters sort)
        {
            var validSortFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", "Id" },
                { "Title", "Title" },
                { "Status", "Status" },
                { "Priority", "Priority" },
                { "Amount", "TotalAmount" },
                { "TotalAmount", "TotalAmount" },
                { "CreatedAt", "CreatedAt" },
                { "UpdatedAt", "UpdatedAt" },
                { "ScheduledPickupTime", "ScheduledPickupTime" },
                { "EstimatedDeliveryTime", "EstimatedDeliveryTime" },
                { "CustomerId", "CustomerId" },
                { "AssignedDriverId", "AssignedDriverId" },
            };

            var sortField = validSortFields.ContainsKey(sort.SortBy) ? validSortFields[sort.SortBy] : "CreatedAt";
            var sortDirection = sort.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";

            return $" ORDER BY {sortField} {sortDirection}";
        }

        /// <summary>
        /// Adds parameters to a SQL command.
        /// </summary>
        /// <param name="command">The SQL command.</param>
        /// <param name="parameters">The parameters to add.</param>
        private static void AddParametersToCommand(SqlCommand command, IEnumerable<SqlParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                // Create a new parameter to avoid reuse issues
                var newParam = new SqlParameter(parameter.ParameterName, parameter.SqlDbType)
                {
                    Value = parameter.Value,
                };
                command.Parameters.Add(newParam);
            }
        }

        // Explicit interface implementations to bridge concrete/interface types
        async Task<Job?> IJobRepository.GetByIdAsync(object id)
        {
            return await GetByIdAsync(id);
        }

        async Task<IEnumerable<Job>> IJobRepository.GetAllAsync()
        {
            return await GetAllAsync();
        }

        async Task<Job> IJobRepository.AddAsync(Job entity)
        {
            return await AddAsync(entity);
        }

        async Task<Job> IJobRepository.UpdateAsync(Job entity)
        {
            return await UpdateAsync(entity);
        }

        async Task<bool> IJobRepository.DeleteAsync(object id)
        {
            return await DeleteAsync(id);
        }

        async Task<bool> IJobRepository.ExistsAsync(object id)
        {
            return await ExistsAsync(id);
        }
    }
}
