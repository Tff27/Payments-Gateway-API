using Domain.Model;

namespace Domain.Core
{
    public interface IBankGateway
    {
        public BankResponse Capture(double amount);

        public BankResponse Refund(double amount);
    }
}
