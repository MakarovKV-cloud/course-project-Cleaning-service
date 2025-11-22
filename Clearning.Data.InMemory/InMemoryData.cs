using Domain.Cleaning;
using System.Collections.Generic;

namespace Cleaning.Data.InMemory
{
    public static class InMemoryData
    {
        public static List<User> Users { get; } = new List<User>();
        public static List<Request> Requests { get; } = new List<Request>();
        public static List<City> Cities { get; } = new List<City>();
        public static List<Service> Services { get; } = new List<Service>();
        public static List<RequestService> RequestServices { get; } = new List<RequestService>();
        public static List<Payment> Payments { get; } = new List<Payment>();
    }
}