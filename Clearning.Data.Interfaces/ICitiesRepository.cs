using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.Interfaces
{
    public interface ICitiesRepository
    {
        int Add(City city);
        bool Delete(int id);
        List<City> GetAll();
        City? GetById(int id);
        bool Update(City city);
    }
}