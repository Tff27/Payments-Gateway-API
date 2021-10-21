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
    public class RefundServiceTests : MoqMeUp<RefundService>
    {
        private readonly IRefundService target;

        private readonly Mock<ILogger<RefundService>> loggerMock;

        public RefundServiceTests()
        {
            this.target = this.Build();
            this.loggerMock = new Mock<ILogger<RefundService>>();
        }

        [Fact]
        public async Task RefundAsync_ValidAuthorizationId_ReturnsServiceResponse_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;

            var refundData = Builder<RefundDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = "4000000000002115")
                .And(x => x.AuthorizedAmount = 20)
                .And(x => x.CapturedAmount = 20)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Accepted
                })
                .Build();

            var bankResponse = Builder<BankResponse>.CreateNew()
                .With(x => x.ErrorMessage = null)
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);
            this.Get<IBankGateway>().Setup(x => x.Refund(amount))
                .Returns(bankResponse);

            // Act
            var act = await this.target.RefundAsync(refundData);

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<ResponseDto>(act);
            Assert.Equal(payment.Currency, act.Currency);
            Assert.Equal(payment.AuthorizedAmount - amount, act.Amount);
            this.Get<IPaymentRepository>().Verify(x => x.SaveChangesAsync(It.IsAny<Payment>()), Times.Once);
        }

        [Fact]
        public async Task RefundAsync_InvalidAuthorizationId_ReturnsNotFound_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;

            var refundData = Builder<RefundDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync((Payment)null);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.RefundAsync(refundData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<NotFoundException>(act);
        }

        [Fact]
        public async Task RefundAsync_ValidAuthorizationId_ReturnsErrorEdgeCase_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;
            var edgeCaseCardNumber = "4000000000003238";
            var errorMessage = $"Edge case - Refund Failed for card: {edgeCaseCardNumber}";

            var refundData = Builder<RefundDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = edgeCaseCardNumber)
                .And(x => x.AuthorizedAmount = 20)
                .And(x => x.CapturedAmount = 20)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Accepted
                })
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.RefundAsync(refundData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<Exception>(act);
            Assert.Equal(errorMessage, act.Message);
        }

        [Theory]
        [InlineData(PaymentStatusCode.Cancelled)]
        [InlineData(PaymentStatusCode.Authorized)]
        [InlineData(PaymentStatusCode.Rejected)]
        public async Task RefundAsync_InvalidPaymentStatus_ReturnsServiceResponse_Async(PaymentStatusCode status)
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();
            var amount = 10;

            var refundData = Builder<RefundDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = amount)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = "4000000000002115")
                .And(x => x.AuthorizedAmount = 20)
                .And(x => x.CapturedAmount = 20)
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
            var act = await Record.ExceptionAsync(async () => await this.target.RefundAsync(refundData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<PaymentStatusValidationException>(act);
        }

        [Fact]
        public async Task RefundAsync_InvalidRefundAmount_ReturnsAmountViolationException_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();

            var refundData = Builder<RefundDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Amount = 20)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .And(x => x.Number = "4000000000002115")
                .And(x => x.AuthorizedAmount = 20)
                .And(x => x.CapturedAmount = 10)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Accepted
                })
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.RefundAsync(refundData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<AmountViolationException>(act);
        }
    }
}
