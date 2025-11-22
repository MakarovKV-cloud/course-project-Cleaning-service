using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.Interfaces
{
    public interface IRequestsRepository
    {
        int Add(Request request);
        bool Delete(int id);
        List<Request> GetAll();
        Request? GetById(int id);
        List<Request> GetByUserId(int userId);
        List<Request> GetByCleanerId(int cleanerId);
        bool Update(Request request);
    }
}