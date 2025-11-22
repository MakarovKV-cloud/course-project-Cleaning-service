using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.Interfaces
{
    public interface IServicesRepository
    {
        int Add(Service service);
        bool Delete(int id);
        List<Service> GetAll();
        Service? GetById(int id);
        bool Update(Service service);
    }
}