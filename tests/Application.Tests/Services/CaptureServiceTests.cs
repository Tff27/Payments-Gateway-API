using Application.Dto;
using Application.Services;
using Application.ServicesInterfaces;
using Domain.Core;
using Domain.Exceptions;
using Domain.Model;
using FizzWare.NBuilder;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using MoqMeUp;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class CaptureServiceTests : MoqMeUp<CaptureService>
    {
        private readonly ICaptureService target;

        private readonly Mock<ILogger<CaptureService>> loggerMock;

        public CaptureServiceTests()
        {
            this.target = this.Build();
            this.loggerMock = new Mock<ILogger<CaptureService>>();
        }

        [Fact]
        public async Task CaptureAsync_ValidAuthorizationId_ReturnsServiceResponse_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;

            var captureData = Builder<CaptureDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = "4000000000002115")
                .And(x => x.AuthorizedAmount = 20)
                .And(x => x.CapturedAmount = 0)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Authorized
                })
                .Build();

            var bankResponse = Builder<BankResponse>.CreateNew()
                .With(x => x.ErrorMessage = null)
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);
            this.Get<IBankGateway>().Setup(x => x.Capture(amount))
                .Returns(bankResponse);

            // Act
            var act = await this.target.CaptureAsync(captureData);

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<ResponseDto>(act);
            Assert.Equal(payment.Currency, act.Currency);
            Assert.Equal(payment.AuthorizedAmount - amount, act.Amount);
            this.Get<IPaymentRepository>().Verify(x => x.SaveChangesAsync(It.IsAny<Payment>()), Times.Once);
        }

        [Fact]
        public async Task CaptureAsync_InvalidAuthorizationId_ReturnsNotFound_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;

            var captureData = Builder<CaptureDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync((Payment)null);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.CaptureAsync(captureData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<NotFoundException>(act);
        }

        [Fact]
        public async Task CaptureAsync_ValidAuthorizationId_ReturnsErrorEdgeCase_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;
            var edgeCaseCardNumber = "4000000000000259";
            var errorMessage = $"Edge case - Capture Failed for card: {edgeCaseCardNumber}";

            var captureData = Builder<CaptureDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = edgeCaseCardNumber)
                .And(x => x.AuthorizedAmount = 20)
                .And(x => x.CapturedAmount = 0)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Authorized
                })
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.CaptureAsync(captureData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<Exception>(act);
            Assert.Equal(errorMessage, act.Message);
        }

        [Theory]
        [InlineData(PaymentStatusCode.Cancelled)]
        [InlineData(PaymentStatusCode.Refunded)]
        public async Task CaptureAsync_InvalidPaymentStatus_ReturnsServiceResponse_Async(PaymentStatusCode status)
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;

            var captureData = Builder<CaptureDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = "4000000000002115")
                .And(x => x.AuthorizedAmount = 20)
                .And(x => x.CapturedAmount = 0)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = status
                })
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.CaptureAsync(captureData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<PaymentStatusValidationException>(act);
        }

        [Theory]
        [InlineData(30, 20, 0)]
        [InlineData(11, 20, 10)]
        public async Task CaptureAsync_InvalidCaptureAmount_ReturnsAmountViolationException_Async(double amount, double authorizedAmount, double capturedAmount)
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();

            var captureData = Builder<CaptureDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = "4000000000002115")
                .And(x => x.AuthorizedAmount = authorizedAmount)
                .And(x => x.CapturedAmount = capturedAmount)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Authorized
                })
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.CaptureAsync(captureData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<AmountViolationException>(act);
        }

        [Fact]
        public async Task CaptureAsync_OverTheLimitCaptureAmount_ReturnsAmountViolationException_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 150;

            var captureData = Builder<CaptureDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = "4000000000002115")
                .And(x => x.AuthorizedAmount = 200)
                .And(x => x.CapturedAmount = 0)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Authorized
                })
                .Build();

            var bankResponse = Builder<BankResponse>.CreateNew()
                 .With(x => x.ErrorMessage = "ERROR!")
                 .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);
            this.Get<IBankGateway>().Setup(x => x.Capture(amount))
                .Returns(bankResponse);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.CaptureAsync(captureData));

            // Assert
            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<AmountViolationException>(act);
        }
    }
}
