// <copyright file="AuthorizationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HotshotLogistics.Application.Authorization;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    /// <summary>
    /// Integration tests for authorization policies and handlers.
    /// </summary>
    public class AuthorizationTests
    {
        private readonly Mock<ILogger<ResourceOwnerAuthorizationHandler>> loggerMock;

        public AuthorizationTests()
        {
            this.loggerMock = new Mock<ILogger<ResourceOwnerAuthorizationHandler>>();
        }

        /// <summary>
        /// Tests that Admin role grants access to all resources.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_AdminRole_GrantsAccess()
        {
            // Arrange
            var handler = new ResourceOwnerAuthorizationHandler(this.loggerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("roles", "Admin")
            }));
            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement("Customer") },
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
        }

        /// <summary>
        /// Tests that Manager role grants access to non-system resources.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_ManagerRole_GrantsAccessToNonSystemResources()
        {
            // Arrange
            var handler = new ResourceOwnerAuthorizationHandler(this.loggerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("roles", "Manager")
            }));
            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement("Customer") },
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
        }

        /// <summary>
        /// Tests that Manager role denies access to system resources.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_ManagerRole_DeniesAccessToSystemResources()
        {
            // Arrange
            var handler = new ResourceOwnerAuthorizationHandler(this.loggerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("roles", "Manager")
            }));
            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement("System") },
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        /// <summary>
        /// Tests that users without appropriate roles are denied access.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_NoAppropriateRole_DeniesAccess()
        {
            // Arrange
            var handler = new ResourceOwnerAuthorizationHandler(this.loggerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("roles", "Driver")
            }));
            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement("Customer") },
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        /// <summary>
        /// Tests that null user denies access.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_NullUser_DeniesAccess()
        {
            // Arrange
            var handler = new ResourceOwnerAuthorizationHandler(this.loggerMock.Object);
            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement("Customer") },
                null!,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        /// <summary>
        /// Tests authorization policies configuration.
        /// </summary>
        [Fact]
        public void AuthorizationPolicies_Constants_AreDefined()
        {
            // Assert
            AuthorizationPolicies.Admin.Should().Be("Admin");
            AuthorizationPolicies.Manager.Should().Be("Manager");
            AuthorizationPolicies.Driver.Should().Be("Driver");
            AuthorizationPolicies.Customer.Should().Be("Customer");
            AuthorizationPolicies.ManagerOrAdmin.Should().Be("ManagerOrAdmin");
            AuthorizationPolicies.ManagerOrDriver.Should().Be("ManagerOrDriver");
            AuthorizationPolicies.OwnResource.Should().Be("OwnResource");
            AuthorizationPolicies.CustomerResource.Should().Be("CustomerResource");
        }
    }
}
