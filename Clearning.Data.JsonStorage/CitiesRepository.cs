using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cleaning.Data.JsonStorage
{
    public class CitiesRepository : ICitiesRepository
    {
        private readonly List<City> _cities = ReadCities();
        private static int _nextId = GetNextId();

        public int Add(City city)
        {
            city.Id = _nextId++;
            _cities.Add(city);
            SaveCities();
            return city.Id;
        }

        public bool Delete(int id)
        {
            var cityToDelete = _cities.FirstOrDefault(c => c.Id == id);
            if (cityToDelete != null)
            {
                _cities.Remove(cityToDelete);
                SaveCities();
                return true;
            }
            return false;
        }

        public List<City> GetAll()
        {
            return _cities.ToList();
        }

        public City? GetById(int id)
        {
            return _cities.FirstOrDefault(c => c.Id == id);
        }

        public bool Update(City city)
        {
            var existingCity = _cities.FirstOrDefault(c => c.Id == city.Id);
            if (existingCity != null)
            {
                existingCity.Name = city.Name;
                SaveCities();
                return true;
            }
            return false;
        }

        private static List<City> ReadCities()
        {
            try
            {
                var citiesJson = File.ReadAllText("database-cities.json");
                if (string.IsNullOrWhiteSpace(citiesJson))
                {
                    return new List<City>();
                }
                return JsonSerializer.Deserialize<List<City>>(citiesJson) ?? new List<City>();
            }
            catch (FileNotFoundException)
            {
                File.WriteAllText("database-cities.json", "[]");
                return new List<City>();
            }
            catch (Exception)
            {
                File.WriteAllText("database-cities.json", "[]");
                return new List<City>();
            }
        }

        private void SaveCities()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var citiesJson = JsonSerializer.Serialize(_cities, options);
                File.WriteAllText("database-cities.json", citiesJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении городов: {ex.Message}");
            }
        }

        private static int GetNextId()
        {
            try
            {
                var cities = ReadCities();
                return cities.Count > 0 ? cities.Max(c => c.Id) + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}