#if false
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

namespace HotshotLogistics.Tests.UserProfile

/// <summary>
/// Unit tests for UserProfileService
/// </summary>
public class UserProfileServiceTests
{
    private readonly Mock<GraphServiceClient> _graphClientMock;
    private readonly Mock<ILogger<UserProfileService>> _loggerMock;
    private readonly UserProfileService _service;

    public UserProfileServiceTests()
    {
        _graphClientMock = new Mock<GraphServiceClient>();
        _loggerMock = new Mock<ILogger<UserProfileService>>();

        _service = new UserProfileService(
            _graphClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_ShouldReturnUserProfile_WhenGraphCallSucceeds()
    {
        // Arrange
        var user = new User
        {
            Id = "user123",
            DisplayName = "John Doe",
            GivenName = "John",
            Surname = "Doe",
            UserPrincipalName = "john.doe@contoso.com",
            Mail = "john.doe@contoso.com",
            JobTitle = "Developer",
            Department = "IT",
            OfficeLocation = "Building A",
            MobilePhone = "+1234567890",
            BusinessPhones = new List<string> { "+0987654321" },
            PreferredLanguage = "en-US",
            LastModifiedDateTime = DateTimeOffset.Now
        };

        var requestBuilder = new Mock<IUserMeRequestBuilder>();
        var request = new Mock<IUserMeRequest>();

        _graphClientMock.Setup(c => c.Me).Returns(requestBuilder.Object);
        requestBuilder.Setup(r => r.Request()).Returns(request.Object);
        request.Setup(r => r.Select(It.IsAny<string>())).Returns(request.Object);
        request.Setup(r => r.GetAsync(default)).ReturnsAsync(user);

        // Mock app role assignments
        var appRoleAssignments = new List<AppRoleAssignment>
        {
            new AppRoleAssignment { AppRoleId = Guid.NewGuid(), ResourceId = Guid.NewGuid() }
        };
        var assignmentsRequestBuilder = new Mock<IUserAppRoleAssignmentsCollectionRequestBuilder>();
        var assignmentsRequest = new Mock<IUserAppRoleAssignmentsCollectionRequest>();
        _graphClientMock.Setup(c => c.Me.AppRoleAssignments).Returns(assignmentsRequestBuilder.Object);
        assignmentsRequestBuilder.Setup(r => r.Request()).Returns(assignmentsRequest.Object);
        assignmentsRequest.Setup(r => r.GetAsync(default)).ReturnsAsync(appRoleAssignments);

        // Mock app role definition
        var appRole = new AppRole { Value = "Admin" };
        var appRequestBuilder = new Mock<IApplicationAppRolesCollectionRequestBuilder>();
        var appRequest = new Mock<IApplicationAppRolesCollectionRequest>();
        _graphClientMock.Setup(c => c.Applications[It.IsAny<string>()].AppRoles[It.IsAny<string>()].Request()).Returns(appRequest.Object);
        appRequest.Setup(r => r.GetAsync(default)).ReturnsAsync(appRole);

        // Act
        var result = await _service.GetCurrentUserProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.DisplayName.Should().Be(user.DisplayName);
        result.GivenName.Should().Be(user.GivenName);
        result.Surname.Should().Be(user.Surname);
        result.UserPrincipalName.Should().Be(user.UserPrincipalName);
        result.Mail.Should().Be(user.Mail);
        result.JobTitle.Should().Be(user.JobTitle);
        result.Department.Should().Be(user.Department);
        result.OfficeLocation.Should().Be(user.OfficeLocation);
        result.MobilePhone.Should().Be(user.MobilePhone);
        result.BusinessPhones.Should().Be("+0987654321");
        result.PreferredLanguage.Should().Be(user.PreferredLanguage);
        result.LastModifiedDateTime.Should().Be(user.LastModifiedDateTime);
        result.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_ShouldThrowException_WhenGraphCallFails()
    {
        // Arrange
        var requestBuilder = new Mock<IUserMeRequestBuilder>();
        var request = new Mock<IUserMeRequest>();

        _graphClientMock.Setup(c => c.Me).Returns(requestBuilder.Object);
        requestBuilder.Setup(r => r.Request()).Returns(request.Object);
        request.Setup(r => r.Select(It.IsAny<string>())).Returns(request.Object);
        request.Setup(r => r.GetAsync(default)).ThrowsAsync(new ServiceException(new Error()));

        // Act
        var act = async () => await _service.GetCurrentUserProfileAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unable to retrieve user profile");
    }

    [Fact]
    public async Task UpdateCurrentUserProfileAsync_ShouldUpdateProfile_WhenGraphCallSucceeds()
    {
        // Arrange
        var profile = new UserProfile
        {
            DisplayName = "Jane Doe",
            JobTitle = "Senior Developer",
            Department = "Engineering"
        };

        var requestBuilder = new Mock<IUserMeRequestBuilder>();
        var request = new Mock<IUserMeRequest>();

        _graphClientMock.Setup(c => c.Me).Returns(requestBuilder.Object);
        requestBuilder.Setup(r => r.Request()).Returns(request.Object);
        request.Setup(r => r.UpdateAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);

        // Act
        await _service.UpdateCurrentUserProfileAsync(profile);

        // Assert
        request.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.DisplayName == profile.DisplayName &&
            u.JobTitle == profile.JobTitle &&
            u.Department == profile.Department), default), Times.Once);
    }

    [Fact]
    public async Task UpdateCurrentUserProfileAsync_ShouldThrowException_WhenGraphCallFails()
    {
        // Arrange
        var profile = new UserProfile { DisplayName = "Jane Doe" };

        var requestBuilder = new Mock<IUserMeRequestBuilder>();
        var request = new Mock<IUserMeRequest>();

        _graphClientMock.Setup(c => c.Me).Returns(requestBuilder.Object);
        requestBuilder.Setup(r => r.Request()).Returns(request.Object);
        request.Setup(r => r.UpdateAsync(It.IsAny<User>(), default)).ThrowsAsync(new ServiceException(new Error()));

        // Act
        var act = async () => await _service.UpdateCurrentUserProfileAsync(profile);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unable to update user profile");
    }

    [Fact]
    public async Task SyncUserProfileAsync_ShouldLogSyncOperation()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            DisplayName = "John Doe"
        };

        var requestBuilder = new Mock<IUserMeRequestBuilder>();
        var request = new Mock<IUserMeRequest>();

        _graphClientMock.Setup(c => c.Me).Returns(requestBuilder.Object);
        requestBuilder.Setup(r => r.Request()).Returns(request.Object);
        request.Setup(r => r.Select(It.IsAny<string>())).Returns(request.Object);
        request.Setup(r => r.GetAsync(default)).ReturnsAsync(user);

        // Act
        await _service.SyncUserProfileAsync(userId);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"User profile synchronized for user {userId}: John Doe")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Fact]
    public async Task SyncUserProfileAsync_ShouldThrowException_WhenGraphCallFails()
    {
        // Arrange
        var userId = "user123";

        var requestBuilder = new Mock<IUserMeRequestBuilder>();
        var request = new Mock<IUserMeRequest>();

        _graphClientMock.Setup(c => c.Me).Returns(requestBuilder.Object);
        requestBuilder.Setup(r => r.Request()).Returns(request.Object);
        request.Setup(r => r.Select(It.IsAny<string>())).Returns(request.Object);
        request.Setup(r => r.GetAsync(default)).ThrowsAsync(new ServiceException(new Error()));

        // Act
        var act = async () => await _service.SyncUserProfileAsync(userId);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Failed to sync user profile for user {userId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}
#endif
