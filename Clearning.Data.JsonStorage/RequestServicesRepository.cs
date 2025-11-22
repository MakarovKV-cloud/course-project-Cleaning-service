using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cleaning.Data.JsonStorage
{
    public class RequestServicesRepository : IRequestServicesRepository
    {
        private readonly List<RequestService> _requestServices = ReadRequestServices();
        private static int _nextId = GetNextId();

        public int Add(RequestService requestService)
        {
            requestService.Id = _nextId++;
            _requestServices.Add(requestService);
            SaveRequestServices();
            return requestService.Id;
        }

        public bool Delete(int id)
        {
            var requestServiceToDelete = _requestServices.FirstOrDefault(rs => rs.Id == id);
            if (requestServiceToDelete != null)
            {
                _requestServices.Remove(requestServiceToDelete);
                SaveRequestServices();
                return true;
            }
            return false;
        }

        public bool DeleteByRequestId(int requestId)
        {
            var servicesToDelete = _requestServices.Where(rs => rs.RequestId == requestId).ToList();
            foreach (var service in servicesToDelete)
            {
                _requestServices.Remove(service);
            }
            if (servicesToDelete.Count > 0)
            {
                SaveRequestServices();
                return true;
            }
            return false;
        }

        public List<RequestService> GetAll()
        {
            return _requestServices.ToList();
        }

        public List<RequestService> GetByRequestId(int requestId)
        {
            return _requestServices.Where(rs => rs.RequestId == requestId).ToList();
        }

        public bool Update(RequestService requestService)
        {
            var existingRequestService = _requestServices.FirstOrDefault(rs => rs.Id == requestService.Id);
            if (existingRequestService != null)
            {
                existingRequestService.RequestId = requestService.RequestId;
                existingRequestService.ServiceId = requestService.ServiceId;
                SaveRequestServices();
                return true;
            }
            return false;
        }

        private static List<RequestService> ReadRequestServices()
        {
            try
            {
                var requestServicesJson = File.ReadAllText("database-requestservices.json");
                if (string.IsNullOrWhiteSpace(requestServicesJson))
                {
                    return new List<RequestService>();
                }
                return JsonSerializer.Deserialize<List<RequestService>>(requestServicesJson) ?? new List<RequestService>();
            }
            catch (FileNotFoundException)
            {
                File.WriteAllText("database-requestservices.json", "[]");
                return new List<RequestService>();
            }
            catch (Exception)
            {
                File.WriteAllText("database-requestservices.json", "[]");
                return new List<RequestService>();
            }
        }

        private void SaveRequestServices()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var requestServicesJson = JsonSerializer.Serialize(_requestServices, options);
                File.WriteAllText("database-requestservices.json", requestServicesJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении услуг заявок: {ex.Message}");
            }
        }

        private static int GetNextId()
        {
            try
            {
                var requestServices = ReadRequestServices();
                return requestServices.Count > 0 ? requestServices.Max(rs => rs.Id) + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}