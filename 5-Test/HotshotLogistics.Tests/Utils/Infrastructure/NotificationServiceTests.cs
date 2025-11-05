using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using Xunit;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    public class NotificationServiceTests
    {
        [Fact]
        public async Task SendSmsAsync_CallsCommunicationService()
        {
            var cacheMock = new Mock<IDistributedCache>();
            var loggerMock = new Mock<ILogger<NotificationService>>();
            var factoryMock = new Mock<ICommunicationServiceFactory>();
            var commServiceMock = new Mock<ICommunicationService>();

            commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Sms")).Returns(commServiceMock.Object);

            var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            var result = await service.SendSmsAsync("+1234567890", "Test message");

            Assert.True(result);
            factoryMock.Verify(f => f.GetService("Sms"), Times.Once);
            commServiceMock.Verify(s => s.SendAsync(It.Is<CommunicationMessage>(m => m.To == "+1234567890" && m.Body == "Test message"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_CallsCommunicationService()
        {
            var cacheMock = new Mock<IDistributedCache>();
            var loggerMock = new Mock<ILogger<NotificationService>>();
            var factoryMock = new Mock<ICommunicationServiceFactory>();
            var commServiceMock = new Mock<ICommunicationService>();

            commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Email")).Returns(commServiceMock.Object);

            var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            var result = await service.SendEmailAsync("test@example.com", "Subject", "Test message");

            Assert.True(result);
            factoryMock.Verify(f => f.GetService("Email"), Times.Once);
            commServiceMock.Verify(s => s.SendAsync(It.Is<CommunicationMessage>(m => m.To == "test@example.com" && m.Subject == "Subject" && m.Body == "Test message"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendPushNotificationAsync_CallsCommunicationService()
        {
            var cacheMock = new Mock<IDistributedCache>();
            var loggerMock = new Mock<ILogger<NotificationService>>();
            var factoryMock = new Mock<ICommunicationServiceFactory>();
            var commServiceMock = new Mock<ICommunicationService>();

            commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Push")).Returns(commServiceMock.Object);

            var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            var result = await service.SendPushNotificationAsync("device123", "Title", "Test message");

            Assert.True(result);
            factoryMock.Verify(f => f.GetService("Push"), Times.Once);
            commServiceMock.Verify(s => s.SendAsync(It.Is<CommunicationMessage>(m => m.To == "device123" && m.Title == "Title" && m.Body == "Test message"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendSmsAsync_RetryOnFailure()
        {
            var cacheMock = new Mock<IDistributedCache>();
            var loggerMock = new Mock<ILogger<NotificationService>>();
            var factoryMock = new Mock<ICommunicationServiceFactory>();
            var commServiceMock = new Mock<ICommunicationService>();

            // Fail first two times, succeed on third
            commServiceMock.SetupSequence(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Sms")).Returns(commServiceMock.Object);

            var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            var result = await service.SendSmsAsync("+1234567890", "Test message");

            Assert.True(result);
            commServiceMock.Verify(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
    }
}
