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
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    PhoneNumber = "(555) 123-4567"
                },
                License = new HotshotLogistics.Domain.ValueObjects.LicenseInfo
                {
                    LicenseNumber = "DL123456",
                    LicenseExpiryDate = DateTime.UtcNow.AddYears(3)
                }
            };

            var result = validator.TestValidate(driver);
            result.ShouldNotHaveAnyValidationErrors();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyFirstName_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    FirstName = string.Empty
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PersonalInfo.FirstName);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidFirstNameCharacters_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    FirstName = "John123"
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PersonalInfo.FirstName);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyLastName_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    LastName = string.Empty
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PersonalInfo.LastName);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyEmail_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    Email = string.Empty
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PersonalInfo.Email);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidEmailFormat_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    Email = "invalid-email"
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PersonalInfo.Email);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyPhoneNumber_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    PhoneNumber = string.Empty
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PersonalInfo.PhoneNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidPhoneNumberFormat_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    PhoneNumber = "123-456-789"
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.PersonalInfo.PhoneNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_EmptyLicenseNumber_ShouldFail()
        {
            var driver = new DriverDto
            {
                License = new HotshotLogistics.Domain.ValueObjects.LicenseInfo
                {
                    LicenseNumber = string.Empty
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.License.LicenseNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_InvalidLicenseNumberCharacters_ShouldFail()
        {
            var driver = new DriverDto
            {
                License = new HotshotLogistics.Domain.ValueObjects.LicenseInfo
                {
                    LicenseNumber = "DL@123"
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.License.LicenseNumber);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_MissingLicenseExpiryDate_ShouldFail()
        {
            var driver = new DriverDto
            {
                License = new HotshotLogistics.Domain.ValueObjects.LicenseInfo
                {
                    LicenseExpiryDate = default
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x.License.LicenseExpiryDate);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_LicenseExpiresTooSoon_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    PhoneNumber = "(555) 123-4567"
                },
                License = new HotshotLogistics.Domain.ValueObjects.LicenseInfo
                {
                    LicenseNumber = "DL123456",
                    LicenseExpiryDate = DateTime.UtcNow.AddDays(20) // Too soon - should be at least 30 days
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x); // Validation is on the whole object
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Validate_LicenseNotValidForRegistration_ShouldFail()
        {
            var driver = new DriverDto
            {
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    PhoneNumber = "(555) 123-4567"
                },
                License = new HotshotLogistics.Domain.ValueObjects.LicenseInfo
                {
                    LicenseNumber = "DL123456",
                    LicenseExpiryDate = DateTime.UtcNow.AddDays(100) // Less than 6 months - should fail registration requirement
                }
            };
            var result = validator.TestValidate(driver);
            result.ShouldHaveValidationErrorFor(x => x);
            await Task.CompletedTask;
        }
    }

    public class DriverUpdateValidatorTests
    {
        private readonly DriverUpdateValidator validator = new DriverUpdateValidator();

        [Fact]
        public async Task Validate_ValidDriverUpdate_ShouldPass()
        {
            var driver = new DriverDto
            {
                Id = 1,
                PersonalInfo = new HotshotLogistics.Domain.ValueObjects.PersonalInfo
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "a@b.com"
                },
                License = new HotshotLogistics.Domain.ValueObjects.LicenseInfo
                {
                    LicenseExpiryDate = DateTime.UtcNow.AddYears(1)
                }
            };
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
