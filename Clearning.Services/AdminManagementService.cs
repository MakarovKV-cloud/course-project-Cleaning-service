using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using Domain.Clearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clearning.Services
{
    public class AdminManagementService
    {
        private readonly User _currentUser;
        private readonly IUsersRepository _usersRepository;

        public AdminManagementService(User currentUser, IUsersRepository usersRepository)
        {
            _currentUser = currentUser;
            _usersRepository = usersRepository;
        }

        public void ApplyRole(User user, string newRole)
        {
            ArgumentNullException.ThrowIfNull(user);
            Role.ThrowIfInvalid(newRole);

            if (user.Role == newRole)
            {
                return;
            }

            if (user.Id == _currentUser.Id)
            {
                throw new LogicException("Нельзя изменить свою собственную роль");
            }

            if (user.Role == Role.Admin && newRole != Role.Admin)
            {
                var adminUsers = _usersRepository.GetAll().Where(u => u.Role == Role.Admin).ToList();
                if (adminUsers.Count <= 1)
                {
                    throw new LogicException("Нельзя изменить роль последнего администратора");
                }
            }

            user.Role = newRole;
            _usersRepository.Update(user);
        }
    }
}
