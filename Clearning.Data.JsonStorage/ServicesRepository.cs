using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cleaning.Data.JsonStorage
{
    public class ServicesRepository : IServicesRepository
    {
        private readonly List<Service> _services = ReadServices();
        private static int _nextId = GetNextId();

        public int Add(Service service)
        {
            service.Id = _nextId++;
            _services.Add(service);
            SaveServices();
            return service.Id;
        }

        public bool Delete(int id)
        {
            var serviceToDelete = _services.FirstOrDefault(s => s.Id == id);
            if (serviceToDelete != null)
            {
                _services.Remove(serviceToDelete);
                SaveServices();
                return true;
            }
            return false;
        }

        public List<Service> GetAll()
        {
            return _services.ToList();
        }

        public Service? GetById(int id)
        {
            return _services.FirstOrDefault(s => s.Id == id);
        }

        public bool Update(Service service)
        {
            var existingService = _services.FirstOrDefault(s => s.Id == service.Id);
            if (existingService != null)
            {
                existingService.Name = service.Name;
                existingService.PricePerSquareMeter = service.PricePerSquareMeter;
                existingService.RequiresArea = service.RequiresArea;
                SaveServices();
                return true;
            }
            return false;
        }

        private static List<Service> ReadServices()
        {
            try
            {
                var servicesJson = File.ReadAllText("database-services.json");
                if (string.IsNullOrWhiteSpace(servicesJson))
                {
                    // Создаем начальные услуги
                    var defaultServices = new List<Service>
                    {
                        new Service { Id = 1, Name = "Сухая уборка", PricePerSquareMeter = 50, RequiresArea = true },
                        new Service { Id = 2, Name = "Влажная уборка", PricePerSquareMeter = 80, RequiresArea = true },
                        new Service { Id = 3, Name = "Мытье окон", PricePerSquareMeter = 100, RequiresArea = false },
                        new Service { Id = 4, Name = "Химчистка ковров", PricePerSquareMeter = 120, RequiresArea = true }
                    };
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText("database-services.json", JsonSerializer.Serialize(defaultServices, options));
                    return defaultServices;
                }
                return JsonSerializer.Deserialize<List<Service>>(servicesJson) ?? new List<Service>();
            }
            catch (FileNotFoundException)
            {
                File.WriteAllText("database-services.json", "[]");
                return new List<Service>();
            }
            catch (Exception)
            {
                File.WriteAllText("database-services.json", "[]");
                return new List<Service>();
            }
        }

        private void SaveServices()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var servicesJson = JsonSerializer.Serialize(_services, options);
                File.WriteAllText("database-services.json", servicesJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении услуг: {ex.Message}");
            }
        }

        private static int GetNextId()
        {
            try
            {
                var services = ReadServices();
                return services.Count > 0 ? services.Max(s => s.Id) + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}