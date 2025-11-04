// <copyright file="DriverService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using System.Collections.Generic;
using System.Threading.Tasks;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;

namespace HotshotLogistics.Application.Services
{

    /// <summary>
    /// Service for managing drivers.
    /// </summary>
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository driverRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverService"/> class.
        /// </summary>
        /// <param name="driverRepository">The driver repository.</param>
        public DriverService(IDriverRepository driverRepository)
        {
            this.driverRepository = driverRepository;
        }

        /// <inheritdoc/>
        public Task<Driver> CreateDriverAsync(Driver driver)
        {

            return this.driverRepository.CreateDriverAsync(driver);

        }

        /// <inheritdoc/>
        public Task<Driver?> GetDriverByIdAsync(int id)
        {
            return this.driverRepository.GetDriverByIdAsync(id);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Driver>> GetDriversAsync()
        {

            return this.driverRepository.GetDriversAsync();

        }

        public Task<Driver> UpdateDriverAsync(Driver driver)
        {
            return this.driverRepository.UpdateDriverAsync(driver);
        }

        public Task<bool> DeleteDriverAsync(int id)
        {
            return this.driverRepository.DeleteDriverAsync(id);
        }
    }
}
