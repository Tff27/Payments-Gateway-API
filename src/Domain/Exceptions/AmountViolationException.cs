using System;

namespace Domain.Exceptions
{
    public class AmountViolationException : ArgumentException
    {
        public AmountViolationException(string message) : base(message)
        {
        }
    }
}
