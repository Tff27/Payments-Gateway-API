using System;

namespace Domain.Exceptions
{
    public class CreditCardDataValidationException : ArgumentException
    {
        public CreditCardDataValidationException(string message, string paramName) : base(message, paramName)
        {
        }
    }
}
