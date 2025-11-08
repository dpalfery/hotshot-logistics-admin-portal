// <copyright file="LocationTrackingRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Core.Repositories;
using HotshotLogistics.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Data.Repositories
#pragma warning disable SA1202 // False positive - public members are correctly ordered before protected members
{
    /// <summary>
    /// Repository implementation for location tracking operations using native ADO.NET.
    /// </summary>
internal class LocationTrackingRepository : BaseRepository<LocationTracking>, ILocationTrackingRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationTrackingRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public LocationTrackingRepository(IConfiguration configuration)
            : base(configuration)
        {
        }

        /// <inheritdoc/>
        public new async Task<LocationTracking> AddAsync(LocationTracking locationTracking)
        {
            if (locationTracking is not LocationTracking locationTrackingEntity)
            {
                throw new ArgumentException("LocationTracking must be of type LocationTracking", nameof(locationTracking));
            }

            return await base.AddAsync(locationTrackingEntity);
        }

        /// <inheritdoc/>
        public async Task<LocationTracking?> GetByIdAsync(long id)
        {
            const string sql = "SELECT * FROM LocationTracking WHERE Id = @Id";
            var parameters = new[] { new SqlParameter("@Id", id) };
            var results = await ExecuteQueryAsync(sql, parameters);
            return results.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LocationTracking>> GetByJobIdAsync(string jobId)
        {
            const string sql = @"
                SELECT * FROM LocationTracking
                WHERE JobId = @JobId
                ORDER BY Timestamp ASC";

            var parameters = new[] { new SqlParameter("@JobId", jobId) };
            var results = await ExecuteQueryAsync(sql, parameters);
            return results.Cast<LocationTracking>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LocationTracking>> GetByDriverIdAsync(int driverId)
        {
            const string sql = @"
                SELECT * FROM LocationTracking
                WHERE DriverId = @DriverId
                ORDER BY Timestamp DESC";

            var parameters = new[] { new SqlParameter("@DriverId", driverId) };
            var results = await ExecuteQueryAsync(sql, parameters);
            return results.Cast<LocationTracking>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LocationTracking>> GetByJobIdAndTimeRangeAsync(string jobId, DateTime startTime, DateTime endTime)
        {
            const string sql = @"
                SELECT * FROM LocationTracking
                WHERE JobId = @JobId
                AND Timestamp BETWEEN @StartTime AND @EndTime
                ORDER BY Timestamp ASC";

            var parameters = new[]
            {
                new SqlParameter("@JobId", jobId),
                new SqlParameter("@StartTime", startTime),
                new SqlParameter("@EndTime", endTime),
            };

            var results = await ExecuteQueryAsync(sql, parameters);
            return results.Cast<LocationTracking>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LocationTracking>> GetByDriverIdAndTimeRangeAsync(int driverId, DateTime startTime, DateTime endTime)
        {
            const string sql = @"
                SELECT * FROM LocationTracking
                WHERE DriverId = @DriverId
                AND Timestamp BETWEEN @StartTime AND @EndTime
                ORDER BY Timestamp ASC";

            var parameters = new[]
            {
                new SqlParameter("@DriverId", driverId),
                new SqlParameter("@StartTime", startTime),
                new SqlParameter("@EndTime", endTime),
            };

            var results = await ExecuteQueryAsync(sql, parameters);
            return results.Cast<LocationTracking>();
        }

        /// <inheritdoc/>
        public async Task<LocationTracking?> GetLatestByJobIdAsync(string jobId)
        {
            const string sql = @"
                SELECT TOP 1 * FROM LocationTracking
                WHERE JobId = @JobId
                ORDER BY Timestamp DESC";

            var parameters = new[] { new SqlParameter("@JobId", jobId) };
            var results = await ExecuteQueryAsync(sql, parameters);
            return results.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<LocationTracking?> GetLatestByDriverIdAsync(int driverId)
        {
            const string sql = @"
                SELECT TOP 1 * FROM LocationTracking
                WHERE DriverId = @DriverId
                ORDER BY Timestamp DESC";

            var parameters = new[] { new SqlParameter("@DriverId", driverId) };
            var results = await ExecuteQueryAsync(sql, parameters);
            return results.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LocationTracking>> GetLatestByJobIdAsync(string jobId, int count)
        {
            var sql = $@"
                SELECT TOP {count} * FROM LocationTracking
                WHERE JobId = @JobId
                ORDER BY Timestamp DESC";

            var parameters = new[] { new SqlParameter("@JobId", jobId) };
            var results = await ExecuteQueryAsync(sql, parameters);
            return results.Cast<LocationTracking>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LocationTracking>> GetByGeographicAreaAsync(
            decimal centerLatitude,
            decimal centerLongitude,
            double radiusMiles,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            var whereClause = new StringBuilder();
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@CenterLat", centerLatitude),
                new SqlParameter("@CenterLon", centerLongitude),
                new SqlParameter("@RadiusMiles", radiusMiles),
            };

            // Use the Haversine formula to calculate distance
            whereClause.Append(@"
                WHERE (3959 * ACOS(
                    COS(RADIANS(@CenterLat)) *
                    COS(RADIANS(Latitude)) *
                    COS(RADIANS(Longitude) - RADIANS(@CenterLon)) +
                    SIN(RADIANS(@CenterLat)) *
                    SIN(RADIANS(Latitude))
                )) <= @RadiusMiles");

            if (startTime.HasValue)
            {
                whereClause.Append(" AND Timestamp >= @StartTime");
                parameters.Add(new SqlParameter("@StartTime", startTime.Value));
            }

            if (endTime.HasValue)
            {
                whereClause.Append(" AND Timestamp <= @EndTime");
                parameters.Add(new SqlParameter("@EndTime", endTime.Value));
            }

            var sql = $@"
                SELECT * FROM LocationTracking
                {whereClause}
                ORDER BY Timestamp DESC";

            var results = await ExecuteQueryAsync(sql, parameters.ToArray());
            return results.Cast<LocationTracking>();
        }

        /// <inheritdoc/>
        public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate)
        {
            const string sql = "DELETE FROM LocationTracking WHERE Timestamp < @CutoffDate";
            var parameters = new[] { new SqlParameter("@CutoffDate", cutoffDate) };
            return await ExecuteNonQueryAsync(sql, parameters);
        }

        /// <inheritdoc/>
        public async Task<double> GetTotalDistanceByJobIdAsync(string jobId)
        {
            const string sql = @"
                WITH OrderedLocations AS (
                    SELECT
                        Latitude,
                        Longitude,
                        LAG(Latitude) OVER (ORDER BY Timestamp) AS PrevLatitude,
                        LAG(Longitude) OVER (ORDER BY Timestamp) AS PrevLongitude
                    FROM LocationTracking
                    WHERE JobId = @JobId
                )
                SELECT ISNULL(SUM(
                    3959 * ACOS(
                        COS(RADIANS(PrevLatitude)) *
                        COS(RADIANS(Latitude)) *
                        COS(RADIANS(Longitude) - RADIANS(PrevLongitude)) +
                        SIN(RADIANS(PrevLatitude)) *
                        SIN(RADIANS(Latitude))
                    )
                ), 0) AS TotalDistance
                FROM OrderedLocations
                WHERE PrevLatitude IS NOT NULL AND PrevLongitude IS NOT NULL";

            var parameters = new[] { new SqlParameter("@JobId", jobId) };
            return await ExecuteScalarAsync<double>(sql, parameters);
        }

        /// <inheritdoc/>
        public async Task<double> GetTotalDistanceByDriverIdAsync(int driverId, DateTime startTime, DateTime endTime)
        {
            const string sql = @"
                WITH OrderedLocations AS (
                    SELECT
                        Latitude,
                        Longitude,
                        LAG(Latitude) OVER (ORDER BY Timestamp) AS PrevLatitude,
                        LAG(Longitude) OVER (ORDER BY Timestamp) AS PrevLongitude
                    FROM LocationTracking
                    WHERE DriverId = @DriverId
                    AND Timestamp BETWEEN @StartTime AND @EndTime
                )
                SELECT ISNULL(SUM(
                    3959 * ACOS(
                        COS(RADIANS(PrevLatitude)) *
                        COS(RADIANS(Latitude)) *
                        COS(RADIANS(Longitude) - RADIANS(PrevLongitude)) +
                        SIN(RADIANS(PrevLatitude)) *
                        SIN(RADIANS(Latitude))
                    )
                ), 0) AS TotalDistance
                FROM OrderedLocations
                WHERE PrevLatitude IS NOT NULL AND PrevLongitude IS NOT NULL";

            var parameters = new[]
            {
                new SqlParameter("@DriverId", driverId),
                new SqlParameter("@StartTime", startTime),
                new SqlParameter("@EndTime", endTime),
            };

            return await ExecuteScalarAsync<double>(sql, parameters);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(long id)
        {
            const string sql = "SELECT COUNT(1) FROM LocationTracking WHERE Id = @Id";
            var parameters = new[] { new SqlParameter("@Id", id) };
            var count = await ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }

        /// <inheritdoc/>
        public async Task<int> GetCountByJobIdAsync(string jobId)
        {
            const string sql = "SELECT COUNT(*) FROM LocationTracking WHERE JobId = @JobId";
            var parameters = new[] { new SqlParameter("@JobId", jobId) };
            return await ExecuteScalarAsync<int>(sql, parameters);
        }

        /// <inheritdoc/>
        public async Task<int> AddBatchAsync(IEnumerable<LocationTracking> locationTrackingRecords)
        {
            var records = locationTrackingRecords.ToList();
            if (!records.Any())
            {
                return 0;
            }

            const string sql = @"
                INSERT INTO LocationTracking (JobId, DriverId, Latitude, Longitude, Speed, Heading, Accuracy, Timestamp)
                VALUES (@JobId, @DriverId, @Latitude, @Longitude, @Speed, @Heading, @Accuracy, @Timestamp)";

            var insertedCount = 0;

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var record in records)
                {
                    if (record is not LocationTracking locationTracking)
                    {
                        continue;
                    }

                    await using var command = new SqlCommand(sql, connection, transaction);
                    command.Parameters.AddRange(GetInsertParameters(locationTracking));

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        insertedCount++;
                    }
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return insertedCount;
        }

        /// <inheritdoc/>
        protected override string GetTableName() => "LocationTracking";

        /// <inheritdoc/>
        protected override string GetPrimaryKeyColumnName() => "Id";

        /// <inheritdoc/>
        protected override LocationTracking MapReaderToEntity(SqlDataReader reader)
        {
            return new LocationTracking
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                JobId = reader.GetString(reader.GetOrdinal("JobId")),
                DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
                Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
                Speed = reader.IsDBNull(reader.GetOrdinal("Speed")) ? null : reader.GetDecimal(reader.GetOrdinal("Speed")),
                Heading = reader.IsDBNull(reader.GetOrdinal("Heading")) ? null : reader.GetInt32(reader.GetOrdinal("Heading")),
                Accuracy = reader.IsDBNull(reader.GetOrdinal("Accuracy")) ? null : reader.GetDecimal(reader.GetOrdinal("Accuracy")),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
            };
        }

        /// <inheritdoc/>
        protected override SqlParameter[] GetInsertParameters(LocationTracking entity)
        {
            return new[]
            {
                new SqlParameter("@JobId", entity.JobId),
                new SqlParameter("@DriverId", entity.DriverId),
                new SqlParameter("@Latitude", entity.Latitude),
                new SqlParameter("@Longitude", entity.Longitude),
                new SqlParameter("@Speed", (object?)entity.Speed ?? DBNull.Value),
                new SqlParameter("@Heading", (object?)entity.Heading ?? DBNull.Value),
                new SqlParameter("@Accuracy", (object?)entity.Accuracy ?? DBNull.Value),
                new SqlParameter("@Timestamp", entity.Timestamp),
            };
        }

        /// <inheritdoc/>
        protected override SqlParameter[] GetUpdateParameters(LocationTracking entity)
        {
            return new[]
            {
                new SqlParameter("@Id", entity.Id),
                new SqlParameter("@JobId", entity.JobId),
                new SqlParameter("@DriverId", entity.DriverId),
                new SqlParameter("@Latitude", entity.Latitude),
                new SqlParameter("@Longitude", entity.Longitude),
                new SqlParameter("@Speed", (object?)entity.Speed ?? DBNull.Value),
                new SqlParameter("@Heading", (object?)entity.Heading ?? DBNull.Value),
                new SqlParameter("@Accuracy", (object?)entity.Accuracy ?? DBNull.Value),
                new SqlParameter("@Timestamp", entity.Timestamp),
            };
        }
#pragma warning restore SA1202
    }
}
