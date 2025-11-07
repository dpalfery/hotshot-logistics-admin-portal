using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using System.Collections.Generic;
using Xunit;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    public class DriverServiceTests
    {
        [Fact]
        public async Task GetDriversAsync_ReturnsDriversFromRepository()
        {
            var expected = new List<HotshotLogistics.Domain.Entities.Driver> { new HotshotLogistics.Domain.Entities.Driver { Id = 1 }, new HotshotLogistics.Domain.Entities.Driver { Id = 2 } };
            var repoMock = new Mock<IDriverRepository>();
            repoMock.Setup(r => r.GetDriversAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            var service = new DriverService(repoMock.Object);
            var result = await service.GetDriversAsync();
            Assert.Equal(expected, result);
            repoMock.Verify(r => r.GetDriversAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDriverByIdAsync_UsesRepository()
        {
            var driver = new HotshotLogistics.Domain.Entities.Driver { Id = 1 };
            var repoMock = new Mock<IDriverRepository>();
            repoMock.Setup(r => r.GetDriverByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
            var service = new DriverService(repoMock.Object);

            var result = await service.GetDriverByIdAsync(1);

            Assert.Equal(driver, result);
            repoMock.Verify(r => r.GetDriverByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateDriverAsync_CallsRepository()
        {
            var newDriver = new HotshotLogistics.Domain.Entities.Driver { Id = 1 };
            var repoMock = new Mock<IDriverRepository>();
            repoMock.Setup(r => r.CreateDriverAsync(newDriver, It.IsAny<CancellationToken>())).ReturnsAsync(newDriver);
            var service = new DriverService(repoMock.Object);
            var result = await service.CreateDriverAsync(newDriver);

            Assert.Equal(newDriver, result);
            repoMock.Verify(r => r.CreateDriverAsync(newDriver, It.IsAny<CancellationToken>()), Times.Once);

        }
    }
}
