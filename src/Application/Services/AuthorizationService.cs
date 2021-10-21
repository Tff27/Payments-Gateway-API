using Application.Dto;
using Application.ServicesInterfaces;
using Domain.Core;
using Domain.Model;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger logger;
        private readonly IPaymentRepository paymentRepository;

        public AuthorizationService(ILogger<AuthorizationService> logger, IPaymentRepository transactionRepository)
        {
            this.logger = logger;
            this.paymentRepository = transactionRepository;
        }

        public async Task<AuthorizationResponseDto> AuthorizeAsync(AuthorizationDto authorization)
        {
            // Edge case - Fail authorization for card: 4000 0000 0000 0119
            if (authorization.Number.Equals("4000000000000119"))
            {
                logger.LogDebug($"Edge case - Fail authorization for card: {authorization.Number}");
                throw new Exception($"Edge case - Authorization Failed for card: {authorization.Number}");
            }

            var payment = new Payment()
            {
                AuthorizationId = ObjectId.GenerateNewId().ToString(),
                AuthorizedAmount = authorization.Amount,
                CapturedAmount = 0,
                Number = authorization.Number,
                Created = DateTime.UtcNow,
                Currency = authorization.Currency,
                Cvv = authorization.Cvv,
                ExpirationMonth = authorization.ExpirationMonth,
                ExpirationYear = authorization.ExpirationYear,
                Status = new PaymentStatus()
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Authorized
                }
            };

            payment.ValidateCard();

            var result = await this.paymentRepository.AddAsync(payment);

            return new AuthorizationResponseDto
            {
                Id = result.AuthorizationId,
                Amount = result.AuthorizedAmount,
                Currency = result.Currency
            };
        }
    }
}
