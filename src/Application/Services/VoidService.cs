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
    public class VoidService : IVoidService
    {
        private readonly ILogger logger;
        private readonly IPaymentRepository paymentRepository;

        public VoidService(ILogger<VoidService> logger, IPaymentRepository transactionRepository)
        {
            this.logger = logger;
            this.paymentRepository = transactionRepository;
        }

        public async Task<ResponseDto> CancelAsync(VoidDto voidData)
        {
            var result = await this.paymentRepository.GetAsync(voidData.AuthorizationId);

            // This will generate a 404 error for security reasons we could opt for returning an ok status instead
            if (result == null)
            {
                throw new NotFoundException("Payment authorization not found.");
            }

            if (result.Status.StatusCode != PaymentStatusCode.Authorized)
            {
                logger.LogError($"Invalid payment status for authorization {voidData.AuthorizationId} be void.");
                throw new PaymentStatusValidationException("voided" , result.Status.StatusCode.ToString());
            }

            result.Status = new PaymentStatus {
                Updated = DateTime.UtcNow,
                StatusCode = PaymentStatusCode.Cancelled,
                ErrorMessage = string.Empty
            };

            await this.paymentRepository.SaveChangesAsync(result); 

            return new ResponseDto
            {
                Amount = result.AuthorizedAmount,
                Currency = result.Currency
            };
        }
    }
}
