// <copyright file="MappingServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Data.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

public class MappingServiceTests
{
    #region MockMappingService Tests

    [Fact]
    public async Task MockMappingService_GeocodeAddressAsync_ValidAddress_ReturnsGeocodingResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        // Act
        var result = await service.GeocodeAddressAsync("123 Main St, City, State", CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotEqual(0m, result.Latitude);
        Assert.NotEqual(0m, result.Longitude);
        Assert.Equal("123 Main St, City, State", result.FormattedAddress);
        Assert.Equal(0.95, result.Confidence);
    }

    [Fact]
    public async Task MockMappingService_GeocodeAddressAsync_SameAddress_ReturnsSameCoordinates()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);
        var address = "456 Oak Ave, Town, State";

        // Act
        var result1 = await service.GeocodeAddressAsync(address, CancellationToken.None);
        var result2 = await service.GeocodeAddressAsync(address, CancellationToken.None);

        // Assert - Mock service should return consistent results for same address
        Assert.Equal(result1.Latitude, result2.Latitude);
        Assert.Equal(result1.Longitude, result2.Longitude);
    }

    [Fact]
    public async Task MockMappingService_ReverseGeocodeAsync_ValidCoordinates_ReturnsReverseGeocodingResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        // Act
        var result = await service.ReverseGeocodeAsync(40.7128m, -74.0060m, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("123 Mock Street", result.Address);
        Assert.Equal("Mock City", result.City);
        Assert.Equal("TX", result.State);
        Assert.Equal("12345", result.PostalCode);
        Assert.Equal("US", result.Country);
    }

    [Fact]
    public async Task MockMappingService_ValidateAddressAsync_ValidAddress_ReturnsTrue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        // Act
        var result = await service.ValidateAddressAsync("123 Main St", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MockMappingService_ValidateAddressAsync_EmptyAddress_ReturnsFalse()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        // Act
        var result = await service.ValidateAddressAsync("", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MockMappingService_CalculateRouteAsync_ValidLocations_ReturnsRouteResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        var origin = new Location { Latitude = 40.7128m, Longitude = -74.0060m };
        var destination = new Location { Latitude = 40.7589m, Longitude = -73.9851m };

        // Act
        var result = await service.CalculateRouteAsync(origin, destination, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.Distance > 0);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.Equal(2, result.Waypoints.Count);
        Assert.Equal("mock_polyline_encoded_string", result.Polyline);
    }

    [Fact]
    public async Task MockMappingService_CalculateRouteAsync_MissingCoordinates_ReturnsInvalidResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        var origin = new Location { Latitude = 40.7128m, Longitude = null };
        var destination = new Location { Latitude = 40.7589m, Longitude = -73.9851m };

        // Act
        var result = await service.CalculateRouteAsync(origin, destination, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Both origin and destination must have coordinates", result.ErrorMessage);
    }

    [Fact]
    public async Task MockMappingService_OptimizeRouteAsync_ValidWaypoints_ReturnsOptimizedResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        var waypoints = new List<Location>
        {
            new Location { Latitude = 40.7128m, Longitude = -74.0060m },
            new Location { Latitude = 40.7589m, Longitude = -73.9851m },
            new Location { Latitude = 40.7489m, Longitude = -73.9680m }
        };

        // Act
        var result = await service.OptimizeRouteAsync(waypoints, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.OptimizedWaypoints.Count);
        Assert.True(result.TotalDistance > 0);
        Assert.True(result.TotalDuration > TimeSpan.Zero);
        Assert.Equal(2, result.RouteSegments.Count); // n-1 segments for n waypoints
    }

    [Fact]
    public async Task MockMappingService_OptimizeRouteAsync_LessThanTwoWaypoints_ReturnsInvalidResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        var waypoints = new List<Location>
        {
            new Location { Latitude = 40.7128m, Longitude = -74.0060m }
        };

        // Act
        var result = await service.OptimizeRouteAsync(waypoints, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("At least 2 waypoints required", result.ErrorMessage);
    }

    [Fact]
    public async Task MockMappingService_CalculateDistanceAsync_ValidLocations_ReturnsDistanceResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MockMappingService>>();
        var service = new MockMappingService(loggerMock.Object);

        var origin = new Location { Latitude = 40.7128m, Longitude = -74.0060m };
        var destination = new Location { Latitude = 40.7589m, Longitude = -73.9851m };

        // Act
        var result = await service.CalculateDistanceAsync(origin, destination, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.Distance > 0);
        Assert.True(result.Duration > TimeSpan.Zero);
    }

    #endregion

    #region MappingServiceFactory Tests

    [Fact]
    public void MappingServiceFactory_MockProvider_CreatesMockMappingService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mapping:Provider", "Mock" }
            })
            .Build();

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger<MockMappingService>>().Object);

        var httpClientFactory = new Mock<IHttpClientFactory>();

        var factory = new MappingServiceFactory(configuration, loggerFactory.Object, httpClientFactory.Object);

        // Act
        var service = factory.CreateMappingService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<MockMappingService>(service);
    }

    [Fact]
    public void MappingServiceFactory_AzureMapsProvider_WithValidKey_CreatesAzureMapsService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mapping:Provider", "AzureMaps" },
                { "Mapping:AzureMaps:SubscriptionKey", "valid-key-123" }
            })
            .Build();

        var logger = new Mock<ILogger<MappingServiceFactory>>();
        var services = new List<IMappingService> { new AzureMapsService(new HttpClient(), logger.Object, "test-key") };

        var factory = new MappingServiceFactory(services, logger.Object);

        // Act
        var service = factory.CreateMappingService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<AzureMapsService>(service);
    }

    [Fact]
    public void MappingServiceFactory_AzureMapsProvider_WithoutKey_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mapping:Provider", "AzureMaps" }
            })
            .Build();

        var loggerFactory = new Mock<ILoggerFactory>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        var factory = new MappingServiceFactory(configuration, loggerFactory.Object, httpClientFactory.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateMappingService());
        Assert.Contains("Azure Maps subscription key is not configured", exception.Message);
    }

    [Fact]
    public void MappingServiceFactory_AzureMapsProvider_WithPlaceholderKey_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mapping:Provider", "AzureMaps" },
                { "Mapping:AzureMaps:SubscriptionKey", "YOUR_AZURE_MAPS_KEY_HERE" }
            })
            .Build();

        var loggerFactory = new Mock<ILoggerFactory>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        var factory = new MappingServiceFactory(configuration, loggerFactory.Object, httpClientFactory.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateMappingService());
        Assert.Contains("Azure Maps subscription key is not configured", exception.Message);
    }

    [Fact]
    public void MappingServiceFactory_GoogleMapsProvider_WithValidKey_CreatesGoogleMapsService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mapping:Provider", "GoogleMaps" },
                { "Mapping:GoogleMaps:ApiKey", "valid-key-123" }
            })
            .Build();

        var logger = new Mock<ILogger<MappingServiceFactory>>();
        var services = new List<IMappingService> { new GoogleMapsService(new HttpClient(), logger.Object, "test-key") };

        var factory = new IMappingServiceFactory(services, logger.Object);

        // Act
        var service = factory.CreateMappingService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<GoogleMapsService>(service);
    }

    [Fact]
    public void MappingServiceFactory_GoogleMapsProvider_WithoutKey_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mapping:Provider", "GoogleMaps" }
            })
            .Build();

        var loggerFactory = new Mock<ILoggerFactory>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        var factory = new MappingServiceFactory(configuration, loggerFactory.Object, httpClientFactory.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateMappingService());
        Assert.Contains("Google Maps API key is not configured", exception.Message);
    }

    [Fact]
    public void MappingServiceFactory_UnsupportedProvider_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mapping:Provider", "UnsupportedProvider" }
            })
            .Build();

        var loggerFactory = new Mock<ILoggerFactory>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        var factory = new MappingServiceFactory(configuration, loggerFactory.Object, httpClientFactory.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateMappingService());
        Assert.Contains("Unsupported mapping provider", exception.Message);
    }

    [Fact]
    public void MappingServiceFactory_NoProviderConfigured_UsesMockByDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var logger = new Mock<ILogger<MappingServiceFactory>>();
        var services = new List<IMappingService> { new MockMappingService(logger.Object) };

        var factory = new IMappingServiceFactory(services, logger.Object);

        // Act
        var service = factory.CreateMappingService();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<MockMappingService>(service);
    }

    #endregion

    #region Existing Azure Maps Tests

    [Fact]
    public async Task AzureMapsService_GeocodeAddressAsync_ValidAddress_ReturnsGeocodingResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""results"": [
                    {
                        ""position"": { ""lat"": 40.7128, ""lon"": -74.0060 },
                        ""address"": { ""freeformAddress"": ""New York, NY, USA"" },
                        ""confidence"": 0.9
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<AzureMapsService>>();
        var service = new AzureMapsService(httpClient, loggerMock.Object, "test-key");

        // Act
        var result = await service.GeocodeAddressAsync("New York, NY", CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(40.7128m, result.Latitude);
        Assert.Equal(-74.0060m, result.Longitude);
        Assert.Equal("New York, NY, USA", result.FormattedAddress);
        Assert.Equal(0.9, result.Confidence);
    }

    [Fact]
    public async Task AzureMapsService_GeocodeAddressAsync_InvalidAddress_ReturnsInvalidResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"{ ""results"": [] }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<AzureMapsService>>();
        var service = new AzureMapsService(httpClient, loggerMock.Object, "test-key");

        // Act
        var result = await service.GeocodeAddressAsync("Invalid Address", CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("No geocoding results found", result.ErrorMessage);
    }

    [Fact]
    public async Task AzureMapsService_ReverseGeocodeAsync_ValidCoordinates_ReturnsReverseGeocodingResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""addresses"": [
                    {
                        ""address"": {
                            ""streetName"": ""Broadway"",
                            ""municipality"": ""New York"",
                            ""countrySubdivision"": ""NY"",
                            ""postalCode"": ""10001"",
                            ""countryCode"": ""US"",
                            ""freeformAddress"": ""Broadway, New York, NY 10001, USA""
                        }
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<AzureMapsService>>();
        var service = new AzureMapsService(httpClient, loggerMock.Object, "test-key");

        // Act
        var result = await service.ReverseGeocodeAsync(40.7128m, -74.0060m, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Broadway", result.Address);
        Assert.Equal("New York", result.City);
        Assert.Equal("NY", result.State);
        Assert.Equal("10001", result.PostalCode);
        Assert.Equal("US", result.Country);
    }

    [Fact]
    public async Task AzureMapsService_CalculateRouteAsync_ValidLocations_ReturnsRouteResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""routes"": [
                    {
                        ""summary"": {
                            ""lengthInMeters"": 10000,
                            ""travelTimeInSeconds"": 900
                        }
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<AzureMapsService>>();
        var service = new AzureMapsService(httpClient, loggerMock.Object, "test-key");

        var origin = new Location { Latitude = 40.7128m, Longitude = -74.0060m };
        var destination = new Location { Latitude = 40.7589m, Longitude = -73.9851m };

        // Act
        var result = await service.CalculateRouteAsync(origin, destination, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(6.21371, result.Distance, 2); // 10000 meters ≈ 6.21 miles
        Assert.Equal(TimeSpan.FromSeconds(900), result.Duration);
        Assert.Equal(DateTime.UtcNow.AddSeconds(900).Date, result.EstimatedArrival.Date); // Approximate time check
    }

    [Fact]
    public async Task GoogleMapsService_GeocodeAddressAsync_ValidAddress_ReturnsGeocodingResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""status"": ""OK"",
                ""results"": [
                    {
                        ""formatted_address"": ""New York, NY, USA"",
                        ""geometry"": {
                            ""location"": { ""lat"": 40.7128, ""lng"": -74.0060 }
                        }
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<GoogleMapsService>>();
        var service = new GoogleMapsService(httpClient, loggerMock.Object, "test-key");

        // Act
        var result = await service.GeocodeAddressAsync("New York, NY", CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(40.7128m, result.Latitude);
        Assert.Equal(-74.0060m, result.Longitude);
        Assert.Equal("New York, NY, USA", result.FormattedAddress);
        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public async Task GoogleMapsService_CalculateRouteAsync_ValidLocations_ReturnsRouteResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""status"": ""OK"",
                ""routes"": [
                    {
                        ""legs"": [
                            {
                                ""distance"": { ""value"": 10000 },
                                ""duration"": { ""value"": 900 }
                            }
                        ],
                        ""overview_polyline"": { ""points"": ""test_polyline"" }
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<GoogleMapsService>>();
        var service = new GoogleMapsService(httpClient, loggerMock.Object, "test-key");

        var origin = new Location { Latitude = 40.7128m, Longitude = -74.0060m };
        var destination = new Location { Latitude = 40.7589m, Longitude = -73.9851m };

        // Act
        var result = await service.CalculateRouteAsync(origin, destination, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(6.21371, result.Distance, 2); // 10000 meters ≈ 6.21 miles
        Assert.Equal(TimeSpan.FromSeconds(900), result.Duration);
        Assert.Equal("test_polyline", result.Polyline);
    }

    [Fact]
    public async Task MappingService_OptimizeRouteAsync_ValidWaypoints_ReturnsOptimizedResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""routes"": [
                    {
                        ""summary"": {
                            ""lengthInMeters"": 10000,
                            ""travelTimeInSeconds"": 900
                        }
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<AzureMapsService>>();
        var service = new AzureMapsService(httpClient, loggerMock.Object, "test-key");

        var waypoints = new List<Location>
        {
            new Location { Latitude = 40.7128m, Longitude = -74.0060m },
            new Location { Latitude = 40.7589m, Longitude = -73.9851m }
        };

        // Act
        var result = await service.OptimizeRouteAsync(waypoints, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(2, result.OptimizedWaypoints.Count);
        Assert.Equal(6.21371, result.TotalDistance, 2);
        Assert.Equal(TimeSpan.FromSeconds(900), result.TotalDuration);
        Assert.Single(result.RouteSegments);
    }

    [Fact]
    public async Task MappingService_CalculateDistanceAsync_ValidLocations_ReturnsDistanceResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""routes"": [
                    {
                        ""summary"": {
                            ""lengthInMeters"": 10000,
                            ""travelTimeInSeconds"": 900
                        }
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<AzureMapsService>>();
        var service = new AzureMapsService(httpClient, loggerMock.Object, "test-key");

        var origin = new Location { Latitude = 40.7128m, Longitude = -74.0060m };
        var destination = new Location { Latitude = 40.7589m, Longitude = -73.9851m };

        // Act
        var result = await service.CalculateDistanceAsync(origin, destination, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(6.21371, result.Distance, 2);
        Assert.Equal(TimeSpan.FromSeconds(900), result.Duration);
    }

    [Fact]
    public async Task MappingService_ValidateAddressAsync_ValidAddress_ReturnsTrue()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"
            {
                ""status"": ""OK"",
                ""results"": [
                    {
                        ""formatted_address"": ""New York, NY, USA"",
                        ""geometry"": {
                            ""location"": { ""lat"": 40.7128, ""lng"": -74.0060 }
                        }
                    }
                ]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var loggerMock = new Mock<ILogger<GoogleMapsService>>();
        var service = new GoogleMapsService(httpClient, loggerMock.Object, "test-key");

        // Act
        var result = await service.ValidateAddressAsync("New York, NY", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    #endregion
}
}
