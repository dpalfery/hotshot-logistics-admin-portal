// <copyright file="DriversController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Core.Logging;
using HotshotLogistics.Core.Security;
using HotshotLogistics.Domain.ValueObjects;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    /// API controller for driver management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DriversController : ControllerBase
    {
        private readonly IDriverService driverService;
        private readonly ILogger<DriversController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriversController"/> class.
        /// </summary>
        /// <param name="driverService">The driver service.</param>
        /// <param name="logger">The logger.</param>
        public DriversController(
            IDriverService driverService,
            ILogger<DriversController> logger)
        {
            this.driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all drivers.
        /// </summary>
        /// <returns>A list of drivers.</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<DriverDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<DriverDto>>> GetDrivers()
        {
            try
            {
                var drivers = await driverService.GetDriversAsync();
                var driverDtos = drivers.Select(d => new DriverDto
                {
                    Id = d.Id,
                    FirstName = HtmlSanitizer.HtmlEncode(d.PersonalInfo.FirstName),
                    LastName = HtmlSanitizer.HtmlEncode(d.PersonalInfo.LastName),
                    Email = HtmlSanitizer.HtmlEncode(d.PersonalInfo.Email),
                    PhoneNumber = HtmlSanitizer.HtmlEncode(d.PersonalInfo.PhoneNumber),
                    LicenseNumber = d.License.LicenseNumber,
                    LicenseExpiryDate = d.License.LicenseExpiryDate,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                });
                return Ok(driverDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving drivers");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets a driver by ID.
        /// </summary>
        /// <param name="id">The driver ID.</param>
        /// <returns>The driver if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DriverDto>> GetDriverById(int id)
        {
            try
            {
                var driver = await driverService.GetDriverByIdAsync(id);
                if (driver == null)
                {
                    return NotFound($"Driver with ID {HtmlSanitizer.HtmlEncode(id.ToString())} not found");
                }
                var driverDto = new DriverDto
                {
                    Id = driver.Id,
                    FirstName = HtmlSanitizer.HtmlEncode(driver.PersonalInfo.FirstName),
                    LastName = HtmlSanitizer.HtmlEncode(driver.PersonalInfo.LastName),
                    Email = HtmlSanitizer.HtmlEncode(driver.PersonalInfo.Email),
                    PhoneNumber = HtmlSanitizer.HtmlEncode(driver.PersonalInfo.PhoneNumber),
                    LicenseNumber = driver.License.LicenseNumber,
                    LicenseExpiryDate = driver.License.LicenseExpiryDate,
                    IsActive = driver.IsActive,
                    CreatedAt = driver.CreatedAt,
                    UpdatedAt = driver.UpdatedAt
                };
                return Ok(driverDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving driver");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Creates a new driver.
        /// </summary>
        /// <param name="driverDto">The driver data.</param>
        /// <returns>The created driver.</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(DriverDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DriverDto>> CreateDriver([FromBody] DriverDto driverDto)
        {
            try
            {
                if (driverDto == null)
                {
                    return BadRequest("Driver data is required");
                }

                var driver = new HotshotLogistics.Domain.Entities.Driver
                {
                    PersonalInfo = new PersonalInfo
                    {
                        FirstName = driverDto.FirstName ?? string.Empty,
                        LastName = driverDto.LastName ?? string.Empty,
                        Email = driverDto.Email ?? string.Empty,
                        PhoneNumber = driverDto.PhoneNumber ?? string.Empty,
                    },
                    License = new LicenseInfo
                    {
                        LicenseNumber = driverDto.LicenseNumber ?? string.Empty,
                        LicenseExpiryDate = driverDto.LicenseExpiryDate,
                    },
                    IsActive = driverDto.IsActive,
                };


                var createdDriver = await driverService.CreateDriverAsync(driver);

                var createdDriverDto = new DriverDto
                {
                    Id = createdDriver.Id,
                    FirstName = HtmlSanitizer.HtmlEncode(createdDriver.PersonalInfo.FirstName),
                    LastName = HtmlSanitizer.HtmlEncode(createdDriver.PersonalInfo.LastName),
                    Email = HtmlSanitizer.HtmlEncode(createdDriver.PersonalInfo.Email),
                    PhoneNumber = HtmlSanitizer.HtmlEncode(createdDriver.PersonalInfo.PhoneNumber),
                    LicenseNumber = createdDriver.License.LicenseNumber,
                    LicenseExpiryDate = createdDriver.License.LicenseExpiryDate,
                    IsActive = createdDriver.IsActive,
                    CreatedAt = createdDriver.CreatedAt,
                    UpdatedAt = createdDriver.UpdatedAt
                };


                return CreatedAtAction(
                    nameof(GetDriverById),
                    new { id = createdDriver.Id },
                    createdDriverDto);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid driver data provided: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating driver");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates an existing driver.
        /// </summary>
        /// <param name="id">The ID of the driver to update.</param>
        /// <param name="driverDto">The updated driver data.</param>
        /// <returns>The updated driver.</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DriverDto>> UpdateDriver(int id, [FromBody] DriverDto driverDto)
        {
            if (id != driverDto.Id)
            {
                return BadRequest("Driver ID mismatch");
            }

            try
            {
                var driver = new HotshotLogistics.Domain.Entities.Driver
                {
                    Id = driverDto.Id,
                    PersonalInfo = new PersonalInfo
                    {
                        FirstName = driverDto.FirstName ?? string.Empty,
                        LastName = driverDto.LastName ?? string.Empty,
                        Email = driverDto.Email ?? string.Empty,
                        PhoneNumber = driverDto.PhoneNumber ?? string.Empty,
                    },
                    License = new LicenseInfo
                    {
                        LicenseNumber = driverDto.LicenseNumber ?? string.Empty,
                        LicenseExpiryDate = driverDto.LicenseExpiryDate,
                    },
                    IsActive = driverDto.IsActive,
                };

                var updatedDriver = await driverService.UpdateDriverAsync(driver);
                if (updatedDriver == null)
                {
                    return NotFound();
                }

                var updatedDriverDto = new DriverDto
                {
                    Id = updatedDriver.Id,
                    FirstName = HtmlSanitizer.HtmlEncode(updatedDriver.PersonalInfo.FirstName),
                    LastName = HtmlSanitizer.HtmlEncode(updatedDriver.PersonalInfo.LastName),
                    Email = HtmlSanitizer.HtmlEncode(updatedDriver.PersonalInfo.Email),
                    PhoneNumber = HtmlSanitizer.HtmlEncode(updatedDriver.PersonalInfo.PhoneNumber),
                    LicenseNumber = updatedDriver.License.LicenseNumber,
                    LicenseExpiryDate = updatedDriver.License.LicenseExpiryDate,
                    IsActive = updatedDriver.IsActive,
                    CreatedAt = updatedDriver.CreatedAt,
                    UpdatedAt = updatedDriver.UpdatedAt
                };

                return Ok(updatedDriverDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating driver {DriverId}", LogSanitizer.SafeId(id));
                return StatusCode(500, "An internal error occurred.");
            }
        }

        /// <summary>
        /// Deletes a driver.
        /// </summary>
        /// <param name="id">The ID of the driver to delete.</param>
        /// <returns>A response indicating success or failure.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            try
            {
                var success = await driverService.DeleteDriverAsync(id);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting driver {DriverId}", LogSanitizer.SafeId(id));
                return StatusCode(500, "An internal error occurred.");
            }
        }

    }
}
