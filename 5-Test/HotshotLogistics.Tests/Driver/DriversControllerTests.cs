namespace HotshotLogistics.Tests.Driver
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HotshotLogistics.Api.Controllers;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.DTOs;
    using HotshotLogistics.Domain.ValueObjects;
    using HotshotLogistics.Contracts.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    /// <summary>
    /// Tests for the DriversController focusing on XSS prevention.
    /// </summary>
    public class DriversControllerTests
    {
        private readonly Mock<IDriverService> mockDriverService;
        private readonly Mock<ILogger<DriversController>> mockLogger;
        private readonly DriversController controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriversControllerTests"/> class.
        /// </summary>
        public DriversControllerTests()
        {
            mockDriverService = new Mock<IDriverService>();
            mockLogger = new Mock<ILogger<DriversController>>();
            controller = new DriversController(mockDriverService.Object, mockLogger.Object);
        }

        /// <summary>
        /// Tests that driver text fields are sanitized when XSS payloads are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetDriverById_WithXSSPayloadInTextFields_ReturnsSanitizedFields()
        {
            var driverId = 1;
            var xssPayload = "<script>alert('xss')</script>";
            var imgXssPayload = "<img src=x onerror=\"alert('xss')\">";
            
            var driverWithXss = new Driver
            {
                Id = driverId,
                PersonalInfo = new PersonalInfo
                {
                    FirstName = xssPayload,
                    LastName = imgXssPayload,
                    Email = "test<b>@</b>example.com",
                    PhoneNumber = "123-456-7890"
                },
                License = new LicenseInfo
                {
                    LicenseNumber = "DL123456",
                    LicenseExpiryDate = DateTime.UtcNow.AddYears(1)
                },
                IsActive = true
            };

            mockDriverService.Setup(s => s.GetDriverByIdAsync(driverId))
                .ReturnsAsync(driverWithXss);

            var result = await controller.GetDriverById(driverId);

            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDriver = okResult.Value.Should().BeOfType<DriverDto>().Subject;
            
            returnedDriver.FirstName.Should().NotContain("<script>");
            returnedDriver.FirstName.Should().NotContain("alert");
            
            returnedDriver.LastName.Should().NotContain("<img");
            returnedDriver.LastName.Should().NotContain("onerror");
            
            returnedDriver.Email.Should().NotContain("<b>");
            returnedDriver.Email.Should().Contain("test");
            returnedDriver.Email.Should().Contain("example");
            returnedDriver.Email.Should().Contain("com");
        }
    }
}
