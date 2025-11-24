using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Cleaning.Data.JsonStorage
{
    public class UsersRepository : IUsersRepository
    {
        private readonly List<User> _users = ReadUsers();
        private static int _nextId = GetNextId();

        public List<User> GetAll(UserFilter? filter = null)
        {
            var result = _users.AsEnumerable();

            if (filter == null)
                return result.ToList();

            if (!string.IsNullOrEmpty(filter.Role))
                result = result.Where(x => x.Role == filter.Role);

            if (filter.CreatedFrom.HasValue)
                result = result.Where(x => DateOnly.FromDateTime(x.CreatedAt) >= filter.CreatedFrom.Value);

            if (filter.CreatedTo.HasValue)
                result = result.Where(x => DateOnly.FromDateTime(x.CreatedAt) <= filter.CreatedTo.Value);

            return result.ToList();
        }

        public int Add(User user)
        {
            user.Id = _nextId++;
            _users.Add(user);
            SaveUsers();
            return user.Id;
        }

        public bool Delete(int id)
        {
            var userToDelete = _users.FirstOrDefault(u => u.Id == id);
            if (userToDelete != null)
            {
                _users.Remove(userToDelete);
                SaveUsers();
                return true;
            }
            return false;
        }

        public User? GetById(int id)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }

        public User? GetByLogin(string login)
        {
            return _users.FirstOrDefault(u => u.Login == login);
        }

        public bool Update(User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.LastName = user.LastName;
                existingUser.FirstName = user.FirstName;
                existingUser.MiddleName = user.MiddleName;
                existingUser.Login = user.Login;
                existingUser.Password = user.Password;
                existingUser.Role = user.Role;
                SaveUsers();
                return true;
            }
            return false;
        }

        private static List<User> ReadUsers()
        {
            try
            {
                var usersJson = File.ReadAllText("database-users.json");
                if (string.IsNullOrWhiteSpace(usersJson))
                {
                    return new List<User>();
                }
                return JsonSerializer.Deserialize<List<User>>(usersJson) ?? new List<User>();
            }
            catch (FileNotFoundException)
            {
                File.WriteAllText("database-users.json", "[]");
                return new List<User>();
            }
            catch (Exception)
            {
                File.WriteAllText("database-users.json", "[]");
                return new List<User>();
            }
        }

        private void SaveUsers()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var usersJson = JsonSerializer.Serialize(_users, options);
                File.WriteAllText("database-users.json", usersJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении пользователей: {ex.Message}");
            }
        }

        private static int GetNextId()
        {
            try
            {
                var users = ReadUsers();
                return users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}