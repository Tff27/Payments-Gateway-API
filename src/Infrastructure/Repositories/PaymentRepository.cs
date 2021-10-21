using Domain.Core;
using Domain.Model;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IMongoCollection<Payment> payments;

        public PaymentRepository(
            IMongoDatabase database)
        {
            this.payments = database.GetCollection<Payment>("Payments");
        }

        public async Task<Payment> AddAsync(Payment payment)
        {
            await payments.InsertOneAsync(payment);

            return payment;
        }

        public async Task<Payment> GetAsync(string id)
        {
            var payment = await payments.Find(p => p.AuthorizationId == id).FirstOrDefaultAsync();

            return payment;
        }

        public async Task SaveChangesAsync(Payment payment)
        {
            await payments.ReplaceOneAsync(p => p.AuthorizationId == payment.AuthorizationId, payment);
        }
    }
}
