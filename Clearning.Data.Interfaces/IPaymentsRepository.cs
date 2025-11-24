using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.Interfaces
{
    public interface IPaymentsRepository
    {
        int Add(Payment payment);
        bool Delete(int id);
        List<Payment> GetAll(PaymentFilter? filter = null);
        Payment? GetById(int id);
        Payment? GetByRequestId(int requestId);
        bool Update(Payment payment);
    }
}