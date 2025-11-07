using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using HotshotLogistics.Application.Validators;
using HotshotLogistics.Domain.DTOs;
using Xunit;

namespace HotshotLogistics.Tests.Driver
{
    public class DriverRegistrationValidatorTests
    {
        private readonly DriverRegistrationValidator validator = new DriverRegistrationValidator();

        [Fact]
        public async Task Validate_ValidDriverRegistration_ShouldPass()
        {
            var driver = new DriverDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-1234",
                LicenseNumber = "DL123456",
                LicenseExpiryDate = DateTime.UtcNow.AddYears(2)
            };

            var result = validator.TestValidate(driver);
            result.ShouldNotHaveAnyValidationErrors();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyFirstName_ShouldFail()
        {
            var driver = new DriverDto { FirstName = string.Empty };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidFirstNameCharacters_ShouldFail()
        {
            var driver = new DriverDto { FirstName = "John123" };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyLastName_ShouldFail()
        {
            var driver = new DriverDto { LastName = string.Empty };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyEmail_ShouldFail()
        {
            var driver = new DriverDto { Email = string.Empty };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidEmailFormat_ShouldFail()
        {
            var driver = new DriverDto { Email = "invalid-email" };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyPhoneNumber_ShouldFail()
        {
            var driver = new DriverDto { PhoneNumber = string.Empty };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidPhoneNumberFormat_ShouldFail()
        {
            var driver = new DriverDto { PhoneNumber = "123-456-789" };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyLicenseNumber_ShouldFail()
        {
            var driver = new DriverDto { LicenseNumber = string.Empty };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.LicenseNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidLicenseNumberCharacters_ShouldFail()
        {
            var driver = new DriverDto { LicenseNumber = "DL@123" };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.LicenseNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_MissingLicenseExpiryDate_ShouldFail()
        {
            var driver = new DriverDto { LicenseExpiryDate = default };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.LicenseExpiryDate);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_LicenseExpiresTooSoon_ShouldFail()
        {
            var driver = new DriverDto { LicenseExpiryDate = DateTime.UtcNow.AddDays(20) };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.LicenseExpiryDate);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_LicenseNotValidForRegistration_ShouldFail()
        {
            var driver = new DriverDto { LicenseExpiryDate = DateTime.UtcNow.AddDays(100) };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.LicenseExpiryDate);
            await Task.CompletedTask;
        }
    }

    public class DriverUpdateValidatorTests
    {
        private readonly DriverUpdateValidator validator = new DriverUpdateValidator();

        [Fact]
        public async Task Validate_ValidDriverUpdate_ShouldPass()
        {
            var driver = new DriverDto { Id = 1, FirstName = "John", LastName = "Doe", Email = "a@b.com", LicenseExpiryDate = DateTime.UtcNow.AddYears(1) };
            var result = validator.TestValidate(driver);
            result.ShouldNotHaveAnyValidationErrors();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_MissingIdForUpdate_ShouldFail()
        {
            var driver = new DriverDto { Id = 0 };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.Id);
            await Task.CompletedTask;
        }
    }
}
