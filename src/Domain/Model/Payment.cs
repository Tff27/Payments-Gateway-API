using System;
using System.Linq;
using Domain.Exceptions;
using LuhnNet;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Model
{
    public class Payment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string AuthorizationId { get; set; }

        public double AuthorizedAmount { get; set; }

        public double CapturedAmount { get; set; }

        public string Currency { get; set; }

        public string Number { get; set; }

        public int ExpirationMonth { get; set; }

        public int ExpirationYear { get; set; }

        public int Cvv { get; set; }

        public DateTime Created { get; set; }

        public PaymentStatus Status { get; set; }

        public void ValidateCard()
        {
            ValidateAmount();
            ValidateCurrency();
            ValidateExpirationDate();
            ValidateCvv();
            ValidateCardNumber();

            void ValidateAmount()
            {
                if (this.AuthorizedAmount <= 0)
                {
                    throw new CreditCardDataValidationException("The amount was to be superior to zero", nameof(AuthorizedAmount));
                }
            }

            void ValidateCurrency()
            {
                if (string.IsNullOrEmpty(this.Currency))
                {
                    throw new CreditCardDataValidationException("The currency must be suplied", nameof(Currency));
                }
            }

            void ValidateExpirationDate()
            {
                if (this.ExpirationMonth < 1 || this.ExpirationMonth > 12)
                {
                    throw new CreditCardDataValidationException("The card expiration date is invalid", "ExpirationDate");
                }

                //if the year is only represented by two digits this validation will need to be updated
                if (this.ExpirationYear < DateTime.UtcNow.Year || (this.ExpirationYear <= DateTime.UtcNow.Year && this.ExpirationMonth < DateTime.UtcNow.Month))
                {
                    throw new CreditCardDataValidationException("The card is expired", "ExpirationDate");
                }
            }

            void ValidateCvv()
            {
                //There could be cvv codes with 4 digits, lets assume that we only have 3 digits cvv for now
                if (this.Cvv < 100 || this.Cvv > 999)
                {
                    throw new CreditCardDataValidationException("The card cvv is invalid", nameof(Cvv));
                }
            }

            void ValidateCardNumber()
            {
                if (string.IsNullOrEmpty(this.Number) || !this.Number.All(char.IsDigit) || this.Number.Length != 16 || !Luhn.IsValid(this.Number))
                {
                    throw new CreditCardDataValidationException("The card number is invalid", nameof(Number));
                }
            }
        }

        public void ValidateCaptureAmount(double amount)
        {
            if (amount > AuthorizedAmount || CapturedAmount + amount > AuthorizedAmount)
            {
                throw new AmountViolationException("The captured amount cannot be higher than the maximum authorized amount");
            }
        }

        public void ValidateRefundAmount(double amount)
        {
            if (amount > CapturedAmount)
            {
                throw new AmountViolationException("The refund amount cannot be higher than the maximum captured amount");
            }
        }
    }
}
