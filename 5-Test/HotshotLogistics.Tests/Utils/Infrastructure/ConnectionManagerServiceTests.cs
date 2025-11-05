using Xunit;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Contracts.Hubs;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
/// Integration tests for ConnectionManagerService
/// </summary>
public class ConnectionManagerServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<ConnectionManagerService>> _loggerMock;
    private readonly Mock<ISignalRClientWrapper> _signalRClientMock;
    private readonly IConnectionManagerService _service;

    public ConnectionManagerServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<ConnectionManagerService>>();
        _signalRClientMock = new Mock<ISignalRClientWrapper>();

        _service = new ConnectionManagerService(
            _cacheMock.Object,
            _loggerMock.Object,
            _signalRClientMock.Object);
    }

    [Fact]
    public async Task AddConnectionAsync_ShouldStoreConnection_WhenConnectionDoesNotExist()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "conn1";
        var expectedKey = $"user_connections:{userId}";
        var expectedConnectionKey = $"connection_user:{connectionId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        await _service.AddConnectionAsync(userId, connectionId);

        // Assert
        _cacheMock.Verify(c => c.SetAsync(expectedKey, It.Is<byte[]>(b =>
            Encoding.UTF8.GetString(b).Contains(connectionId)), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(expectedConnectionKey, It.Is<byte[]>(b =>
            Encoding.UTF8.GetString(b) == userId), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddConnectionAsync_ShouldNotAddDuplicateConnection()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "conn1";
        var existingConnections = new List<string> { connectionId };
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(existingConnections)));

        // Act
        await _service.AddConnectionAsync(userId, connectionId);

        // Assert
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveConnectionAsync_ShouldRemoveConnection_WhenExists()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "conn1";
        var existingConnections = new List<string> { connectionId, "conn2" };
        var expectedKey = $"user_connections:{userId}";
        var expectedConnectionKey = $"connection_user:{connectionId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(existingConnections)));
        _cacheMock.Setup(c => c.GetAsync(expectedConnectionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(userId));

        // Act
        await _service.RemoveConnectionAsync(connectionId);

        // Assert
        _cacheMock.Verify(c => c.SetAsync(expectedKey, It.Is<byte[]>(b =>
            !Encoding.UTF8.GetString(b).Contains(connectionId)), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(expectedConnectionKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserConnectionsAsync_ShouldReturnConnections_WhenExist()
    {
        // Arrange
        var userId = "user1";
        var connections = new List<string> { "conn1", "conn2" };
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(connections)));

        // Act
        var result = await _service.GetUserConnectionsAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(connections);
    }

    [Fact]
    public async Task GetUserConnectionsAsync_ShouldReturnEmptyList_WhenNoConnections()
    {
        // Arrange
        var userId = "user1";
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[])null);

        // Act
        var result = await _service.GetUserConnectionsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task IsUserConnectedAsync_ShouldReturnTrue_WhenConnectionsExist()
    {
        // Arrange
        var userId = "user1";
        var connections = new List<string> { "conn1" };
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(connections)));

        // Act
        var result = await _service.IsUserConnectedAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendToUserAsync_ShouldCallSignalRClient()
    {
        // Arrange
        var userId = "user1";
        var methodName = "TestMethod";
        var args = new object[] { "arg1", 123 };

        // Act
        await _service.SendToUserAsync(userId, methodName, args);

        // Assert
        _signalRClientMock.Verify(c => c.SendToUserAsync(userId, methodName, args), Times.Once);
    }

    [Fact]
    public async Task SendToUsersAsync_ShouldCallSignalRClientForEachUser()
    {
        // Arrange
        var userIds = new List<string> { "user1", "user2" };
        var methodName = "TestMethod";
        var args = new object[] { "arg1" };

        // Act
        await _service.SendToUsersAsync(userIds, methodName, args);

        // Assert
        _signalRClientMock.Verify(c => c.SendToUserAsync("user1", methodName, args), Times.Once);
        _signalRClientMock.Verify(c => c.SendToUserAsync("user2", methodName, args), Times.Once);
    }

    [Fact]
    public async Task RemoveUserConnectionsAsync_ShouldRemoveAllUserConnections()
    {
        // Arrange
        var userId = "user1";
        var connections = new List<string> { "conn1", "conn2" };
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.SetupSequence(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(connections))) // initial call in RemoveUserConnectionsAsync
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(connections))) // first call inside RemoveConnectionAsync (conn1)
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new List<string> { "conn2" }))); // second call inside RemoveConnectionAsync (conn2)

        // Act
        await _service.RemoveUserConnectionsAsync(userId);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReconnectionAsync_ShouldReturnTrue_WhenConnectionStillActive()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "conn1";
        var connections = new List<string> { connectionId };
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(connections)));

        // Act
        var result = await _service.HandleReconnectionAsync(userId, connectionId);

        // Assert
        result.Should().BeTrue();
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleReconnectionAsync_ShouldReconnect_WhenConnectionNotActive()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "conn1";
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.HandleReconnectionAsync(userId, connectionId);

        // Assert
        result.Should().BeTrue();
        _cacheMock.Verify(c => c.SetAsync(expectedKey, It.Is<byte[]>(b =>
            Encoding.UTF8.GetString(b).Contains(connectionId)), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReconnectionAsync_ShouldReturnFalse_AfterMaxRetries()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "conn1";
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis connection failed"));

        // Act
        var result = await _service.HandleReconnectionAsync(userId, connectionId, 5);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserIdAsync_ShouldReturnUserId_WhenConnectionExists()
    {
        // Arrange
        var connectionId = "conn1";
        var userId = "user1";
        var expectedKey = $"connection_user:{connectionId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(userId));

        // Act
        var result = await _service.GetUserIdAsync(connectionId);

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public async Task ConnectionExistsAsync_ShouldReturnTrue_WhenConnectionExists()
    {
        // Arrange
        var connectionId = "conn1";
        var userId = "user1";
        var expectedKey = $"connection_user:{connectionId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(userId));

        // Act
        var result = await _service.ConnectionExistsAsync(connectionId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserConnectionCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var userId = "user1";
        var connections = new List<string> { "conn1", "conn2", "conn3" };
        var expectedKey = $"user_connections:{userId}";

        _cacheMock.Setup(c => c.GetAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(connections)));

        // Act
        var result = await _service.GetUserConnectionCountAsync(userId);

        // Assert
        result.Should().Be(3);
    }
}
}
