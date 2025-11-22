using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Clearning
{
    public static class Role
    {
        public const string Admin = "Admin";
        public const string Cleaner = "Cleaner";
        public const string Client = "Client";

        public static bool IsValid(string role)
        {
            return role == Admin
                || role == Cleaner
                || role == Client;
        }

        public static void ThrowIfInvalid(string role)
        {
            if (!IsValid(role))
            {
                throw new InvalidOperationException($"Role {role} in not valid");
            }
        }
    }
}
