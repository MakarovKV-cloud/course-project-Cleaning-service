using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.Interfaces
{
    public interface IRequestServicesRepository
    {
        int Add(RequestService requestService);
        bool Delete(int id);
        List<RequestService> GetAll();
        List<RequestService> GetByRequestId(int requestId);
        bool DeleteByRequestId(int requestId);
        bool Update(RequestService requestService);
    }
}