using System;

namespace Domain.Model
{
    public class PaymentStatus
    {
        public PaymentStatusCode StatusCode { get; set; }

        public bool IsError => StatusCode == PaymentStatusCode.Rejected;

        public string ErrorMessage { get; set; }

        public DateTime Updated { get; set; }
    }

    public enum PaymentStatusCode
    {
        Authorized,
        Accepted,
        Rejected,
        Refunded,
        Cancelled
    }
}
