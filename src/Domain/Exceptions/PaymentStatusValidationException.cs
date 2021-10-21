using System;

namespace Domain.Exceptions
{
    public class PaymentStatusValidationException : Exception
    {
        public PaymentStatusValidationException(string paymentActionType, string paymentStatus) 
            : base($"A payment cannot be {paymentActionType} because is at {paymentStatus} status.")
        {
        }
    }
}
