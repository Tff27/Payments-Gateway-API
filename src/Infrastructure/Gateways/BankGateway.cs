using Domain.Core;
using Domain.Model;

namespace Infrastructure.Gateways
{
    // This will be a fake implementation of a call to the customers Bank
    public class BankGateway : IBankGateway
    {
        // Lets assume that customers was always a balance of 100
        private double BankBalance;

        public BankGateway()
        {
            BankBalance = 100;
        }

        public BankResponse Capture(double amount)
        {
            // Will be assuming that the currency is always the same that was requested at authorization phase to avoid conversion problems
            if (BankBalance < amount)
            {
                return new BankResponse()
                {
                    ErrorMessage = "Insuficient balance for this transaction.",
                };
            }

            BankBalance -= amount;

            // should contain a call to do the transaction to merchant account
            // fees could be applied

            return new BankResponse();
        }

        public BankResponse Refund(double amount)
        {
            // Will be assuming that the currency is always the same that was requested at authorization phase to avoid conversion problems
            BankBalance += amount;

            // should contain a call to do the transaction to retrieve money from merchant account

            return new BankResponse();
        }
    }
}
