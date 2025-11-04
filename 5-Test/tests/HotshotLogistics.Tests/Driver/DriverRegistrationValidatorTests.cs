using FluentValidation.TestHelper;
using HotshotLogistics.Application.Validators;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Tests.Driver
{
    /// <summary>
/// Unit tests for DriverRegistrationValidator.
/// </summary>
public class DriverRegistrationValidatorTests
{
    private readonly DriverRegistrationValidator validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DriverRegistrationValidatorTests"/> class.
    /// </summary>
    public DriverRegistrationValidatorTests()
    {
        this.validator = new DriverRegistrationValidator();
    }

    /// <summary>
    /// Tests that validation passes for a valid driver registration.
    /// </summary>
    [Fact]
    public void Validate_ValidDriverRegistration_ShouldPass()
    {
        // Arrange
        var driver = new DriverDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+1-555-123-4567",
            LicenseNumber = "DL123456789",
            LicenseExpiryDate = DateTime.UtcNow.AddYears(3), // Valid for more than 2 years (current age validation requirement)
            IsActive = true
        };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that validation fails when first name is empty.
    /// </summary>
    [Fact]
    public void Validate_EmptyFirstName_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { FirstName = string.Empty };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name is required.");
    }

    /// <summary>
    /// Tests that validation fails when first name contains invalid characters.
    /// </summary>
    [Fact]
    public void Validate_InvalidFirstNameCharacters_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { FirstName = "John123" };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name can only contain letters, spaces, hyphens, and apostrophes.");
    }

    /// <summary>
    /// Tests that validation fails when last name is empty.
    /// </summary>
    [Fact]
    public void Validate_EmptyLastName_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { LastName = string.Empty };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Last name is required.");
    }

    /// <summary>
    /// Tests that validation fails when email is empty.
    /// </summary>
    [Fact]
    public void Validate_EmptyEmail_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { Email = string.Empty };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email address is required.");
    }

    /// <summary>
    /// Tests that validation fails when email format is invalid.
    /// </summary>
    [Fact]
    public void Validate_InvalidEmailFormat_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { Email = "invalid-email" };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("A valid email address is required.");
    }

    /// <summary>
    /// Tests that validation fails when phone number is empty.
    /// </summary>
    [Fact]
    public void Validate_EmptyPhoneNumber_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { PhoneNumber = string.Empty };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
              .WithErrorMessage("Phone number is required.");
    }

    /// <summary>
    /// Tests that validation fails when phone number format is invalid.
    /// </summary>
    [Fact]
    public void Validate_InvalidPhoneNumberFormat_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { PhoneNumber = "123-456-789" };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
              .WithErrorMessage("A valid US phone number is required.");
    }

    /// <summary>
    /// Tests that validation fails when license number is empty.
    /// </summary>
    [Fact]
    public void Validate_EmptyLicenseNumber_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { LicenseNumber = string.Empty };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LicenseNumber)
              .WithErrorMessage("Driver's license number is required.");
    }

    /// <summary>
    /// Tests that validation fails when license number contains invalid characters.
    /// </summary>
    [Fact]
    public void Validate_InvalidLicenseNumberCharacters_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { LicenseNumber = "DL@123" };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LicenseNumber)
              .WithErrorMessage("License number can only contain letters, numbers, and hyphens.");
    }

    /// <summary>
    /// Tests that validation fails when license expiry date is not set.
    /// </summary>
    [Fact]
    public void Validate_MissingLicenseExpiryDate_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { LicenseExpiryDate = default };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LicenseExpiryDate)
              .WithErrorMessage("License expiry date is required.");
    }

    /// <summary>
    /// Tests that validation fails when license expires too soon.
    /// </summary>
    [Fact]
    public void Validate_LicenseExpiresTooSoon_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { LicenseExpiryDate = DateTime.UtcNow.AddDays(20) };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("License must be valid for at least 30 days.");
    }

    /// <summary>
    /// Tests that validation fails when license is not valid for registration.
    /// </summary>
    [Fact]
    public void Validate_LicenseNotValidForRegistration_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { LicenseExpiryDate = DateTime.UtcNow.AddDays(100) }; // Less than 1 year from now

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("License must be valid and meet minimum requirements for registration.");
    }
}

/// <summary>
/// Unit tests for DriverUpdateValidator.
/// </summary>
public class DriverUpdateValidatorTests
{
    private readonly DriverUpdateValidator validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DriverUpdateValidatorTests"/> class.
    /// </summary>
    public DriverUpdateValidatorTests()
    {
        this.validator = new DriverUpdateValidator();
    }

    /// <summary>
    /// Tests that validation passes for a valid driver update.
    /// </summary>
    [Fact]
    public void Validate_ValidDriverUpdate_ShouldPass()
    {
        // Arrange
        var driver = new DriverDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+1-555-123-4567",
            LicenseNumber = "DL123456789",
            LicenseExpiryDate = DateTime.UtcNow.AddMonths(8)
        };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that validation fails when driver ID is missing for update.
    /// </summary>
    [Fact]
    public void Validate_MissingIdForUpdate_ShouldFail()
    {
        // Arrange
        var driver = new DriverDto { Id = 0 };

        // Act
        var result = this.validator.TestValidate(driver);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Driver ID is required for updates.");
    }
}
}
