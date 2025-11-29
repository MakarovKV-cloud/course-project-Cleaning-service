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
            _requestsRepository = requestsRepository ?? throw new ArgumentNullException(nameof(requestsRepository));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            _citiesRepository = citiesRepository ?? throw new ArgumentNullException(nameof(citiesRepository));
        }

        // Статистика по клинерам (количество выполненных заказов)
        public List<CleanerStatisticItem> GetCleanersCompletedOrdersStatistics(int? year = null)
        {
            try
            {
                var filter = new RequestFilter
                {
                    Status = "Завершена",
                    StartDate = year.HasValue ? new DateOnly(year.Value, 1, 1) : null,
                    EndDate = year.HasValue ? new DateOnly(year.Value, 12, 31) : null
                };

                var completedRequests = _requestsRepository?.GetAll(filter) ?? new List<Request>();
                var cleaners = _usersRepository?.GetAll(new UserFilter { Role = "Cleaner" }) ?? new List<User>();

                // Фильтруем запросы с назначенными клинерами и проверяем на null
                var result = from cleaner in cleaners
                             where cleaner != null
                             join request in completedRequests.Where(r => r != null && r.CleanerId.HasValue)
                             on cleaner.Id equals request.CleanerId.Value
                             group request by new
                             {
                                 cleaner.Id,
                                 CleanerName = $"{cleaner.LastName ?? ""} {cleaner.FirstName ?? ""} {cleaner.MiddleName ?? ""}".Trim()
                             } into g
                             select new CleanerStatisticItem
                             {
                                 CleanerName = g.Key.CleanerName,
                                 Count = g.Count()
                             };

                return result.OrderByDescending(x => x.Count).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCleanersCompletedOrdersStatistics: {ex.Message}");
                return new List<CleanerStatisticItem>();
            }
        }

        // Статистика выполненных заказов по месяцам
        public List<CompletedOrdersStatisticItem> GetCompletedOrdersByMonth(int? year = null)
        {
            try
            {
                var filter = new RequestFilter
                {
                    Status = "Завершена",
                    StartDate = year.HasValue ? new DateOnly(year.Value, 1, 1) : null,
                    EndDate = year.HasValue ? new DateOnly(year.Value, 12, 31) : null
                };

                var items = _requestsRepository?.GetAll(filter) ?? new List<Request>();

                return items
                    .Where(x => x != null && x.CleaningDate != default(DateTime))
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCompletedOrdersByMonth: {ex.Message}");
                return new List<CompletedOrdersStatisticItem>();
            }
        }

        // Статистика заработка по месяцам
        public List<EarningsStatisticItem> GetEarningsByMonth(int? year = null)
        {
            try
            {
                var filter = new RequestFilter
                {
                    Status = "Завершена",
                    StartDate = year.HasValue ? new DateOnly(year.Value, 1, 1) : null,
                    EndDate = year.HasValue ? new DateOnly(year.Value, 12, 31) : null
                };

                var items = _requestsRepository?.GetAll(filter) ?? new List<Request>();

                return items
                    .Where(x => x != null && x.CleaningDate != default(DateTime))
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetEarningsByMonth: {ex.Message}");
                return new List<EarningsStatisticItem>();
            }
        }
    }

    // Расширения для безопасной работы со статистикой
    public static class StatisticsExtensions
    {
        public static string GetMonthName(this CompletedOrdersStatisticItem item)
        {
            if (item == null) return "Неизвестно";

            try
            {
                return new DateTime(item.Year, item.Month, 1).ToString("MMM yyyy");
            }
            catch
            {
                return "Неверная дата";
            }
        }

        public static string GetMonthName(this EarningsStatisticItem item)
        {
            if (item == null) return "Неизвестно";

            try
            {
                return new DateTime(item.Year, item.Month, 1).ToString("MMM yyyy");
            }
            catch
            {
                return "Неверная дата";
            }
        }
    }
}