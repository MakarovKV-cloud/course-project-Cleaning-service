using Cleaning.Data.Interfaces;
using Domain.Cleaning;
using Domain.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cleaning.Services
{
    public class StatisticsService
    {
        private readonly IRequestsRepository _requestsRepository;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly IUsersRepository _usersRepository;
        private readonly ICitiesRepository _citiesRepository;

        public StatisticsService(
            IRequestsRepository requestsRepository,
            IPaymentsRepository paymentsRepository,
            IUsersRepository usersRepository,
            ICitiesRepository citiesRepository)
        {
            _requestsRepository = requestsRepository;
            _paymentsRepository = paymentsRepository;
            _usersRepository = usersRepository;
            _citiesRepository = citiesRepository;
        }

        // Статистика по клинерам (количество выполненных заказов)
        public List<CleanerStatisticItem> GetCleanersCompletedOrdersStatistics(int? year = null)
        {
            var filter = new RequestFilter
            {
                Status = "Завершена",
                StartDate = year.HasValue ? new DateOnly(year.Value, 1, 1) : null,
                EndDate = year.HasValue ? new DateOnly(year.Value, 12, 31) : null
            };

            var completedRequests = _requestsRepository.GetAll(filter);
            var cleaners = _usersRepository.GetAll(new UserFilter { Role = "Cleaner" });

            var result = from cleaner in cleaners
                         join request in completedRequests on cleaner.Id equals request.CleanerId
                         group request by new
                         {
                             cleaner.Id,
                             CleanerName = $"{cleaner.LastName} {cleaner.FirstName} {cleaner.MiddleName ?? ""}".Trim()
                         } into g
                         select new CleanerStatisticItem
                         {
                             CleanerName = g.Key.CleanerName,
                             Count = g.Count()
                         };

            return result.OrderByDescending(x => x.Count).ToList();
        }

        // Статистика выполненных заказов по месяцам
        public List<CompletedOrdersStatisticItem> GetCompletedOrdersByMonth(int? year = null)
        {
            var filter = new RequestFilter
            {
                Status = "Завершена",
                StartDate = year.HasValue ? new DateOnly(year.Value, 1, 1) : null,
                EndDate = year.HasValue ? new DateOnly(year.Value, 12, 31) : null
            };

            var items = _requestsRepository.GetAll(filter);

            return items
                .GroupBy(x => new { x.CleaningDate.Year, x.CleaningDate.Month })
                .Select(g => new CompletedOrdersStatisticItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    CompletedOrdersCount = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();
        }

        //  Статистика заработка по месяцам
        public List<EarningsStatisticItem> GetEarningsByMonth(int? year = null)
        {
            var filter = new RequestFilter
            {
                Status = "Завершена",
                StartDate = year.HasValue ? new DateOnly(year.Value, 1, 1) : null,
                EndDate = year.HasValue ? new DateOnly(year.Value, 12, 31) : null
            };

            var items = _requestsRepository.GetAll(filter);

            return items
                .GroupBy(x => new { x.CleaningDate.Year, x.CleaningDate.Month })
                .Select(g => new EarningsStatisticItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalEarnings = g.Sum(x => x.TotalCost)
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();
        }
    }
}