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
    public class VoidServiceTests : MoqMeUp<VoidService>
    {
        private readonly IVoidService target;

        private readonly Mock<ILogger<VoidService>> loggerMock;

        public VoidServiceTests()
        {
            this.target = this.Build();
            this.loggerMock = new Mock<ILogger<VoidService>>();
        }

        [Fact]
        public async Task CancelAsync_ValidAuthorizationId_ReturnsServiceResponse_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();

            var voidData = Builder<VoidDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
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

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync(payment);

            // Act
            var act = await this.target.CancelAsync(voidData);

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<ResponseDto>(act);
            Assert.Equal(payment.Currency, act.Currency);
            Assert.Equal(payment.AuthorizedAmount, act.Amount);
            this.Get<IPaymentRepository>().Verify(x => x.SaveChangesAsync(It.IsAny<Payment>()), Times.Once);
        }


        [Fact]
        public async Task CancelAsync_InvalidAuthorizationId_ReturnsNotFound_Async()
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();

            var voidData = Builder<VoidDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
                .Build();

            this.Get<IPaymentRepository>().Setup(x => x.GetAsync(authorizationID))
                .ReturnsAsync((Payment)null);

            // Act
            var act = await Record.ExceptionAsync(async () => await this.target.CancelAsync(voidData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<NotFoundException>(act);
        }

        [Theory]
        [InlineData(PaymentStatusCode.Cancelled)]
        [InlineData(PaymentStatusCode.Refunded)]
        [InlineData(PaymentStatusCode.Rejected)]
        [InlineData(PaymentStatusCode.Accepted)]
        public async Task CancelAsync_InvalidPaymentState_ReturnsException_Async(PaymentStatusCode status)
        {
            // Arrange
            var authorizationID = ObjectId.GenerateNewId().ToString();

            var voidData = Builder<VoidDto>.CreateNew()
                .With(x => x.AuthorizationId = authorizationID)
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
            var act = await Record.ExceptionAsync(async () => await this.target.CancelAsync(voidData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<PaymentStatusValidationException>(act);
        }
    }
}
