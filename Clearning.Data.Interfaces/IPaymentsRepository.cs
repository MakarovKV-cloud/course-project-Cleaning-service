using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.Interfaces
{
    public interface IPaymentsRepository
    {
        int Add(Payment payment);
        bool Delete(int id);
        List<Payment> GetAll();
        Payment? GetById(int id);
        Payment? GetByRequestId(int requestId);
        bool Update(Payment payment);
    }
}