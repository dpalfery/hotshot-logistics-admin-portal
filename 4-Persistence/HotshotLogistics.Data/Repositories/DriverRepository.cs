// <copyright file="DriverRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
{
    /// <summary>
    /// Repository for managing Driver entities using native ADO.NET.
    /// </summary>
internal class DriverRepository : BaseRepository<Driver>, IDriverRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DriverRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public DriverRepository(IConfiguration configuration)
            : base(configuration)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Driver>> GetDriversAsync(CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT * FROM Drivers WHERE IsActive = 1 ORDER BY LastName, FirstName";
            var drivers = await ExecuteQueryAsync(sql);
            return drivers.Cast<Driver>();
        }

        /// <inheritdoc/>
        public async Task<Driver?> GetDriverByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT * FROM Drivers WHERE Id = @Id";
            var parameters = new[] { new SqlParameter("@Id", SqlDbType.Int) { Value = id } };
            var drivers = await ExecuteQueryAsync(sql, parameters);
            return drivers.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<Driver> CreateDriverAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            var domainDriver = (Driver)driver;
            domainDriver.CreatedAt = DateTime.UtcNow;
            return await AddAsync(domainDriver);
        }

        /// <inheritdoc/>
        public async Task<Driver> UpdateDriverAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            var domainDriver = (Driver)driver;
            domainDriver.UpdatedAt = DateTime.UtcNow;
            return await UpdateAsync(domainDriver);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDriverAsync(int id, CancellationToken cancellationToken = default)
        {
            // Soft delete - mark as inactive
            const string sql = "UPDATE Drivers SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            var parameters = new[]
            {
                new SqlParameter("@Id", SqlDbType.Int) { Value = id },
                new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
            };
            var rowsAffected = await ExecuteNonQueryAsync(sql, parameters);
            return rowsAffected > 0;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Driver>> GetActiveDriversAsync(CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT * FROM Drivers WHERE IsActive = 1 ORDER BY LastName, FirstName";
            var drivers = await ExecuteQueryAsync(sql);
            return drivers.Cast<Driver>();
        }

        /// <inheritdoc/>
        public async Task<Driver?> GetDriverByLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT * FROM Drivers WHERE LicenseNumber = @LicenseNumber";
            var parameters = new[] { new SqlParameter("@LicenseNumber", SqlDbType.NVarChar) { Value = licenseNumber } };
            var drivers = await ExecuteQueryAsync(sql, parameters);
            return drivers.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Driver>> GetDriversByStatusAsync(DriverStatus status, CancellationToken cancellationToken = default)
        {
            // Note: CurrentStatus is not stored in the database, returning all active drivers
            const string sql = "SELECT * FROM Drivers WHERE IsActive = 1 ORDER BY LastName, FirstName";
            var drivers = await ExecuteQueryAsync(sql);
            return drivers.Cast<Driver>();
        }

        /// <inheritdoc/>
        protected override string GetTableName() => "Drivers";

        /// <inheritdoc/>
        protected override string GetPrimaryKeyColumnName() => "Id";

        /// <inheritdoc/>
        protected override Driver MapReaderToEntity(SqlDataReader reader)
        {
            return new Driver
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PersonalInfo = new PersonalInfo
                {
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                },
                License = new LicenseInfo
                {
                    LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber")),
                    LicenseExpiryDate = reader.IsDBNull(reader.GetOrdinal("LicenseExpiryDate"))
                        ? DateTime.UtcNow.AddYears(1) // Default to 1 year from now if NULL
                        : reader.GetDateTime(reader.GetOrdinal("LicenseExpiryDate")),
                },
                CurrentStatus = (DriverStatus)reader.GetInt32(reader.GetOrdinal("CurrentStatus")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            };
        }

        /// <inheritdoc/>
        protected override SqlParameter[] GetInsertParameters(Driver entity)
        {
            return new[]
            {
                new SqlParameter("@FirstName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.FirstName },
                new SqlParameter("@LastName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.LastName },
                new SqlParameter("@Email", SqlDbType.NVarChar) { Value = entity.PersonalInfo.Email },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar) { Value = (object?)entity.PersonalInfo.PhoneNumber ?? DBNull.Value },
                new SqlParameter("@LicenseNumber", SqlDbType.NVarChar) { Value = entity.License.LicenseNumber },
                new SqlParameter("@LicenseState", SqlDbType.NVarChar) { Value = (object?)entity.License.LicenseState ?? DBNull.Value },
                new SqlParameter("@LicenseExpiryDate", SqlDbType.Date) { Value = (object?)entity.License.LicenseExpiryDate },
                new SqlParameter("@InsuranceExpiryDate", SqlDbType.Date) { Value = entity.Vehicle?.InsuranceExpiryDate is DateTime date && date != DateTime.MinValue ? (object)date : DBNull.Value },
                new SqlParameter("@CurrentStatus", SqlDbType.Int) { Value = (int)entity.CurrentStatus },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = entity.IsActive },
            };
        }

        /// <inheritdoc/>
        protected override SqlParameter[] GetUpdateParameters(Driver entity)
        {
            return new[]
            {
                new SqlParameter("@Id", SqlDbType.Int) { Value = entity.Id },
                new SqlParameter("@FirstName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.FirstName },
                new SqlParameter("@LastName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.LastName },
                new SqlParameter("@Email", SqlDbType.NVarChar) { Value = entity.PersonalInfo.Email },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar) { Value = (object?)entity.PersonalInfo.PhoneNumber ?? DBNull.Value },
                new SqlParameter("@LicenseNumber", SqlDbType.NVarChar) { Value = entity.License.LicenseNumber },
                new SqlParameter("@LicenseState", SqlDbType.NVarChar) { Value = (object?)entity.License.LicenseState ?? DBNull.Value },
                new SqlParameter("@LicenseExpiryDate", SqlDbType.Date) { Value = (object?)entity.License.LicenseExpiryDate },
                new SqlParameter("@InsuranceExpiryDate", SqlDbType.Date) { Value = entity.Vehicle?.InsuranceExpiryDate is DateTime date && date != DateTime.MinValue ? (object)date : DBNull.Value },
                new SqlParameter("@CurrentStatus", SqlDbType.Int) { Value = (int)entity.CurrentStatus },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = entity.IsActive },
            };
        }
    }
}
