using Domain.Model;
using System.Threading.Tasks;

namespace Domain.Core
{
    public interface IPaymentRepository
    {
        public Task<Payment> AddAsync(Payment payment);

        public Task<Payment> GetAsync(string id);

        public Task SaveChangesAsync(Payment payment);
    }
}
