using Application.Services;
using Application.Dto;
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
    public class AuthorizationServiceTests : MoqMeUp<AuthorizationService>
    {
        private readonly IAuthorizationService target;

        private readonly Mock<ILogger<AuthorizationService>> loggerMock;

        public AuthorizationServiceTests()
        {
            target = Build();
            loggerMock = new Mock<ILogger<AuthorizationService>>();
        }

        [Fact]
        public async Task AuthorizeAsync_ValidCard_ReturnsServiceResponse_Async()
        {
            // Arrange
            var authorizationData = Builder<AuthorizationDto>.CreateNew()
                .With(x => x.Number = "4000000000002115")
                .And(x => x.Amount = 20)
                .And(x => x.Currency = "€")
                .And(x => x.Cvv = 100)
                .And(x => x.ExpirationMonth = 11)
                .And(x => x.ExpirationYear = 2021)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = ObjectId.GenerateNewId().ToString())
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

            Get<IPaymentRepository>().Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync(payment);

            // Act
            var act = await target.AuthorizeAsync(authorizationData);

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<AuthorizationResponseDto>(act);
            Assert.NotNull(act.Id);
            Assert.Equal(authorizationData.Currency, act.Currency);
            Assert.Equal(authorizationData.Amount, act.Amount);
        }


        [Fact]
        public async Task AuthorizeAsync_ValidCard_ReturnsErrorEdgeCase_Async()
        {
            // Arrange
            var edgeCaseCardNumber = "4000000000000119";

            var authorizationData = Builder<AuthorizationDto>.CreateNew()
                .With(x => x.Number = edgeCaseCardNumber)
                .Build();

            var errorMessage = $"Edge case - Authorization Failed for card: {edgeCaseCardNumber}";

            // Act
            var act = await Record.ExceptionAsync(async () => await target.AuthorizeAsync(authorizationData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<Exception>(act);
            Assert.Equal(errorMessage, act.Message);
        }

        [Theory]
        [InlineData("400000000000211", 20, "€", 100, 11, 2021)]
        [InlineData("400000000000211A", 20, "€", 100, 11, 2021)]
        [InlineData("", 20, "€", 100, 11, 2021)]
        [InlineData("4000000000002115", 0, "€", 100, 11, 2021)]
        [InlineData("4000000000002115", -1, "€", 100, 11, 2021)]
        [InlineData("4000000000002115", 20, "", 100, 11, 2021)]
        [InlineData("4000000000002115", 20, "€", 1000, 11, 2021)]
        [InlineData("4000000000002115", 20, "€", 99, 11, 2021)]
        [InlineData("4000000000002115", 20, "€", 100, -1, 2021)]
        [InlineData("4000000000002115", 20, "€", 100, 13, 2021)]
        [InlineData("4000000000002115", 20, "€", 100, 11, 2020)]
        [InlineData("4000000000002115", 20, "€", 100, 9, 2021)]
        [InlineData("4000000000002116", 20, "€", 100, 12, 2021)]
        public async Task AuthorizeAsync_InvalidCard_ReturnsServiceResponse_Async(string cardNumber, double amount,
                                                                                  string currency, int cvv,
                                                                                  int expirationMonth,
                                                                                  int expirationYear)
        {
            // Arrange
            var authorizationData = Builder<AuthorizationDto>.CreateNew()
                .With(x => x.Number = cardNumber)
                .And(x => x.Amount = amount)
                .And(x => x.Currency = currency)
                .And(x => x.Cvv = cvv)
                .And(x => x.ExpirationMonth = expirationMonth)
                .And(x => x.ExpirationYear = expirationYear)
                .Build();

            var payment = Builder<Payment>.CreateNew()
                .With(x => x.AuthorizationId = ObjectId.GenerateNewId().ToString())
                .And(x => x.Number = cardNumber)
                .And(x => x.AuthorizedAmount = amount)
                .And(x => x.CapturedAmount = 0)
                .And(x => x.Currency = currency)
                .And(x => x.Cvv = cvv)
                .And(x => x.ExpirationMonth = expirationMonth)
                .And(x => x.ExpirationYear = expirationYear)
                .And(x => x.Created = DateTime.UtcNow)
                .And(x => x.Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Authorized
                })
                .Build();

            // Act
            var act = await Record.ExceptionAsync(async () => await target.AuthorizeAsync(authorizationData));

            // Assert
            Assert.NotNull(act);
            Assert.IsAssignableFrom<CreditCardDataValidationException>(act);
        }
    }
}
