using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.Interfaces
{
    public interface IUsersRepository
    {
        int Add(User user);
        bool Delete(int id);
        List<User> GetAll(UserFilter? filter = null);
        User? GetById(int id);
        User? GetByLogin(string login);
        bool Update(User user);
    }
}