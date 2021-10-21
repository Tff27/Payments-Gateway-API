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
    public class CaptureService : ICaptureService
    {
        private readonly ILogger logger;
        private readonly IPaymentRepository paymentRepository;
        private readonly IBankGateway bankGateway;

        public CaptureService(ILogger<CaptureService> logger, IPaymentRepository transactionRepository, IBankGateway bankGateway)
        {
            this.logger = logger;
            this.paymentRepository = transactionRepository;
            this.bankGateway = bankGateway;
        }

        public async Task<ResponseDto> CaptureAsync(CaptureDto captureData)
        {
            var result = await this.paymentRepository.GetAsync(captureData.AuthorizationId);

            // This will generate a 404 error for security reasons we could opt for returning an ok status instead
            if (result == null)
            {
                throw new NotFoundException("Payment authorization not found.");
            }

            // Edge case - Fail capture for card: 4000 0000 0000 0259
            if (result.Number.Equals("4000000000000259"))
            {
                logger.LogDebug($"Edge case - Fail capture for card: {result.Number}");
                throw new Exception($"Edge case - Capture Failed for card: {result.Number}");
            }

            if (result.Status.StatusCode != PaymentStatusCode.Authorized 
                && result.Status.StatusCode != PaymentStatusCode.Accepted
                && result.Status.StatusCode != PaymentStatusCode.Rejected)
            {
                logger.LogError($"Invalid payment status for authorization {captureData.AuthorizationId} be captured.");
                throw new PaymentStatusValidationException("capture", result.Status.StatusCode.ToString());
            }
        
            result.ValidateCaptureAmount(captureData.Amount);

            var bankCaptureResult = bankGateway.Capture(captureData.Amount);

            if (!bankCaptureResult.IsSuccess)
            {
                result.Status = new PaymentStatus
                {
                    Updated = DateTime.UtcNow,
                    StatusCode = PaymentStatusCode.Rejected,
                    ErrorMessage = bankCaptureResult.ErrorMessage
                };

                await this.paymentRepository.SaveChangesAsync(result);

                throw new AmountViolationException(bankCaptureResult.ErrorMessage);
            }

            result.CapturedAmount += captureData.Amount;
            result.Status = new PaymentStatus
            {
                Updated = DateTime.UtcNow,
                StatusCode = PaymentStatusCode.Accepted,
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
