using Application.Dto;
using Application.ServicesInterfaces;
using Domain.Core;
using Domain.Exceptions;
using Domain.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RefundService : IRefundService
    {
        private readonly ILogger logger;
        private readonly IPaymentRepository paymentRepository;
        private readonly IBankGateway bankGateway;

        public RefundService(ILogger<RefundService> logger, IPaymentRepository transactionRepository, IBankGateway bankGateway)
        {
            this.logger = logger;
            this.paymentRepository = transactionRepository;
            this.bankGateway = bankGateway;
        }

        public async Task<ResponseDto> RefundAsync(RefundDto captureData)
        {
            var result = await this.paymentRepository.GetAsync(captureData.AuthorizationId);

            // This will generate a 404 error for security reasons we could opt for returning an ok status instead
            if (result == null)
            {
                throw new NotFoundException("Payment authorization not found.");
            }

            // Edge case - Fail capture for card: 4000 0000 0000 3238
            if (result.Number.Equals("4000000000003238"))
            {
                logger.LogDebug($"Edge case - Fail refund for card: {result.Number}");
                throw new Exception($"Edge case - Refund Failed for card: {result.Number}");
            }

            if (result.Status.StatusCode != PaymentStatusCode.Accepted && result.Status.StatusCode != PaymentStatusCode.Refunded)
            {
                logger.LogError($"Invalid payment status for authorization {captureData.AuthorizationId} be refunded.");
                throw new PaymentStatusValidationException("refund", result.Status.StatusCode.ToString());
            }

            result.ValidateRefundAmount(captureData.Amount);

            bankGateway.Refund(captureData.Amount);

            result.CapturedAmount -= captureData.Amount;
            result.Status = new PaymentStatus
            {
                Updated = DateTime.UtcNow,
                StatusCode = PaymentStatusCode.Refunded,
                ErrorMessage = string.Empty
            };

            await this.paymentRepository.SaveChangesAsync(result);

            return new ResponseDto
            {
                Amount = result.AuthorizedAmount - result.CapturedAmount,
                Currency = result.Currency
            };
        }
    }
}
