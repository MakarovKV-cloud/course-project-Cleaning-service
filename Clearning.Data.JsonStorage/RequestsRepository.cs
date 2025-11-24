using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cleaning.Data.JsonStorage
{
    public class RequestsRepository : IRequestsRepository
    {
        private readonly List<Request> _requests = ReadRequests();
        private static int _nextId = GetNextId();

        public List<Request> GetAll(RequestFilter? filter = null)
        {
            var result = _requests.AsEnumerable();

            if (filter == null)
                return result.ToList();

            if (filter.StartDate.HasValue)
                result = result.Where(x => DateOnly.FromDateTime(x.CleaningDate) >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                result = result.Where(x => DateOnly.FromDateTime(x.CleaningDate) <= filter.EndDate.Value);

            if (filter.CleanerId.HasValue)
                result = result.Where(x => x.CleanerId == filter.CleanerId.Value);

            if (filter.ClientId.HasValue)
                result = result.Where(x => x.UserId == filter.ClientId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                result = result.Where(x => x.Status == filter.Status);

            if (filter.CityId.HasValue)
                result = result.Where(x => x.CityId == filter.CityId.Value);

            return result.ToList();
        }

        public int Add(Request request)
        {
            request.Id = _nextId++;
            _requests.Add(request);
            SaveRequests();
            return request.Id;
        }

        public bool Delete(int id)
        {
            var requestToDelete = _requests.FirstOrDefault(r => r.Id == id);
            if (requestToDelete != null)
            {
                _requests.Remove(requestToDelete);
                SaveRequests();
                return true;
            }
            return false;
        }

        public Request? GetById(int id)
        {
            return _requests.FirstOrDefault(r => r.Id == id);
        }

        public List<Request> GetByUserId(int userId)
        {
            return _requests.Where(r => r.UserId == userId).ToList();
        }

        public List<Request> GetByCleanerId(int cleanerId)
        {
            return _requests.Where(r => r.CleanerId == cleanerId).ToList();
        }

        public bool Update(Request request)
        {
            var existingRequest = _requests.FirstOrDefault(r => r.Id == request.Id);
            if (existingRequest != null)
            {
                existingRequest.UserId = request.UserId;
                existingRequest.Area = request.Area;
                existingRequest.CleaningDate = request.CleaningDate;
                existingRequest.CityId = request.CityId;
                existingRequest.District = request.District;
                existingRequest.Address = request.Address;
                existingRequest.TotalCost = request.TotalCost;
                existingRequest.Status = request.Status;
                existingRequest.CleanerId = request.CleanerId;
                existingRequest.PaymentId = request.PaymentId;
                SaveRequests();
                return true;
            }
            return false;
        }

        private static List<Request> ReadRequests()
        {
            try
            {
                var requestsJson = File.ReadAllText("database-requests.json");
                if (string.IsNullOrWhiteSpace(requestsJson))
                {
                    return new List<Request>();
                }
                return JsonSerializer.Deserialize<List<Request>>(requestsJson) ?? new List<Request>();
            }
            catch (FileNotFoundException)
            {
                File.WriteAllText("database-requests.json", "[]");
                return new List<Request>();
            }
            catch (Exception)
            {
                File.WriteAllText("database-requests.json", "[]");
                return new List<Request>();
            }
        }

        private void SaveRequests()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var requestsJson = JsonSerializer.Serialize(_requests, options);
                File.WriteAllText("database-requests.json", requestsJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении заявок: {ex.Message}");
            }
        }

        private static int GetNextId()
        {
            try
            {
                var requests = ReadRequests();
                return requests.Count > 0 ? requests.Max(r => r.Id) + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}