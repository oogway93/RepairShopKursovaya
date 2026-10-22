using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;
using RepairShop.Core.Storage;

namespace RepairShop.Core.Services
{
    public class MasterStat
    {
        public User Master { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DeviceTypeStat
    {
        public DeviceType Type { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Отчёты и печатные документы.</summary>
    public class ReportService
    {
        private readonly ShopDatabase _db;

        public ReportService(ShopDatabase db)
        {
            _db = db;
        }

        /// <summary>Выручка по заказам, закрытым в периоде (выдача устройства или оплата диагностики).</summary>
        public decimal Revenue(DateTime from, DateTime to)
        {
            return _db.Data.Orders
                .Where(o => o.ClosedAt.HasValue &&
                            o.ClosedAt.Value.Date >= from.Date && o.ClosedAt.Value.Date <= to.Date)
                .Sum(o => o.FinalCost);
        }

        public Dictionary<OrderStatus, int> OrdersByStatus()
        {
            var result = new Dictionary<OrderStatus, int>();
            foreach (OrderStatus s in Enum.GetValues(typeof(OrderStatus)))
                result[s] = _db.Data.Orders.Count(o => o.Status == s);
            return result;
        }

        public List<MasterStat> MasterStats(DateTime from, DateTime to)
        {
            var list = new List<MasterStat>();
            foreach (var master in _db.Data.Users.Where(u => u.Role == UserRole.Master))
            {
                var mine = _db.Data.Orders.Where(o => o.MasterId == master.Id).ToList();
                list.Add(new MasterStat
                {
                    Master = master,
                    Active = mine.Count(o => !OrderWorkflow.IsFinal(o.Status)),
                    Completed = mine.Count(o => o.Status == OrderStatus.Issued && o.ClosedAt.HasValue &&
                                                o.ClosedAt.Value.Date >= from.Date && o.ClosedAt.Value.Date <= to.Date),
                    Revenue = mine.Where(o => o.Status == OrderStatus.Issued && o.ClosedAt.HasValue &&
                                              o.ClosedAt.Value.Date >= from.Date && o.ClosedAt.Value.Date <= to.Date)
                                  .Sum(o => o.FinalCost)
                });
            }
            return list.OrderByDescending(m => m.Revenue).ThenBy(m => m.Master.FullName).ToList();
        }

        public List<DeviceTypeStat> DeviceTypeStats()
        {
            var types = _db.Data.Orders
                .Select(o => _db.Data.Devices.FirstOrDefault(d => d.Id == o.DeviceId))
                .Where(d => d != null)
                .GroupBy(d => d.Type)
                .Select(g => new DeviceTypeStat { Type = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count).ToList();
            return types;
        }

        /// <summary>Средний срок ремонта (в днях) по выданным заказам.</summary>
        public double AverageRepairDays()
        {
            var done = _db.Data.Orders
                .Where(o => o.Status == OrderStatus.Issued && o.ClosedAt.HasValue).ToList();
            if (done.Count == 0)
                return 0;
            return Math.Round(done.Average(o => (o.ClosedAt.Value - o.CreatedAt).TotalDays), 1);
        }

        public List<RepairOrder> OverdueOrders()
        {
            DateTime today = _db.Clock.Now.Date;
            return _db.Data.Orders
                .Where(o => !OrderWorkflow.IsFinal(o.Status) && o.DueDate.Date < today)
                .OrderBy(o => o.DueDate).ToList();
        }

        /// <summary>Сводный текстовый отчёт за период.</summary>
        public string BuildSummary(DateTime from, DateTime to)
        {
            var ru = new CultureInfo("ru-RU");
            var sb = new StringBuilder();
            sb.AppendLine("ОТЧЁТ О РАБОТЕ МАСТЕРСКОЙ");
            sb.AppendLine("Период: " + from.ToString("dd.MM.yyyy") + " – " + to.ToString("dd.MM.yyyy"));
            sb.AppendLine(new string('=', 52));
            sb.AppendLine("Выручка за период: " + Revenue(from, to).ToString("N2", ru) + " руб.");
            sb.AppendLine("Средний срок ремонта: " + AverageRepairDays().ToString("0.0", ru) + " дн.");
            sb.AppendLine();
            sb.AppendLine("Заказы по статусам:");
            foreach (var pair in OrdersByStatus())
                sb.AppendLine("  " + Localization.Name(pair.Key).PadRight(22) + pair.Value);
            sb.AppendLine();
            sb.AppendLine("Загрузка мастеров:");
            foreach (var m in MasterStats(from, to))
                sb.AppendLine("  " + m.Master.FullName.PadRight(22) + "в работе: " + m.Active +
                              ", выдано: " + m.Completed + ", выручка: " + m.Revenue.ToString("N2", ru));
            sb.AppendLine();
            sb.AppendLine("Популярные типы устройств:");
            foreach (var s in DeviceTypeStats())
                sb.AppendLine("  " + Localization.Name(s.Type).PadRight(22) + s.Count);
            sb.AppendLine();
            var overdue = OverdueOrders();
            sb.AppendLine("Просроченные заказы: " + overdue.Count);
            foreach (var o in overdue)
                sb.AppendLine("  " + o.Number + " (срок " + o.DueDate.ToString("dd.MM.yyyy") + ")");
            return sb.ToString();
        }

        /// <summary>Выгрузка заказов в CSV (разделитель «;»).</summary>
        public string ToCsv(IEnumerable<RepairOrder> orders)
        {
            var ru = new CultureInfo("ru-RU");
            var sb = new StringBuilder();
            sb.AppendLine("Номер;Дата;Клиент;Телефон;Устройство;Статус;Мастер;Срок;Стоимость");
            foreach (var o in orders)
            {
                var client = _db.Data.Clients.FirstOrDefault(c => c.Id == o.ClientId);
                var device = _db.Data.Devices.FirstOrDefault(d => d.Id == o.DeviceId);
                var master = _db.Data.Users.FirstOrDefault(u => u.Id == o.MasterId);
                decimal cost = OrderWorkflow.IsFinal(o.Status) ? o.FinalCost : CostCalculator.Calculate(o, 0).Total;
                sb.AppendLine(string.Join(";", new[]
                {
                    o.Number,
                    o.CreatedAt.ToString("dd.MM.yyyy"),
                    Escape(client != null ? client.FullName : ""),
                    client != null ? client.Phone : "",
                    Escape(device != null ? device.ToString() : ""),
                    Localization.Name(o.Status),
                    Escape(master != null ? master.FullName : ""),
                    o.DueDate.ToString("dd.MM.yyyy"),
                    cost.ToString("0.00", ru)
                }));
            }
            return sb.ToString();
        }

        private static string Escape(string value)
        {
            if (value.Contains(";") || value.Contains("\""))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        /// <summary>Акт приёма устройства в ремонт.</summary>
        public string BuildAcceptanceAct(RepairOrder order)
        {
            var client = _db.Data.Clients.FirstOrDefault(c => c.Id == order.ClientId);
            var device = _db.Data.Devices.FirstOrDefault(d => d.Id == order.DeviceId);
            var sb = new StringBuilder();
            sb.AppendLine("АКТ ПРИЁМА УСТРОЙСТВА В РЕМОНТ № " + order.Number);
            sb.AppendLine("Дата приёма: " + order.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
            sb.AppendLine("Срок готовности: " + order.DueDate.ToString("dd.MM.yyyy"));
            sb.AppendLine(new string('-', 52));
            sb.AppendLine("Клиент:   " + (client != null ? client.FullName : "—"));
            sb.AppendLine("Телефон:  " + (client != null ? client.Phone : "—"));
            sb.AppendLine("Устройство: " + (device != null ? Localization.Name(device.Type) + ", " + device : "—"));
            sb.AppendLine("Серийный номер: " + (device != null && device.SerialNumber != "" ? device.SerialNumber : "не указан"));
            sb.AppendLine("Заявленная неисправность: " + order.Defect);
            if (order.IsWarrantyCase)
                sb.AppendLine("Гарантийный случай: ремонт выполняется бесплатно.");
            sb.AppendLine(new string('-', 52));
            sb.AppendLine("Гарантия на выполненный ремонт – " + OrderService.WarrantyDays + " дней со дня выдачи.");
            sb.AppendLine("Диагностика при отказе от ремонта – " +
                          CostCalculator.DefaultDiagnosticsFee.ToString("0") + " руб.");
            sb.AppendLine();
            sb.AppendLine("Подпись клиента: ____________    Приёмщик: ____________");
            return sb.ToString();
        }

        /// <summary>Акт выполненных работ (выдача устройства).</summary>
        public string BuildCompletionAct(RepairOrder order, CostBreakdown cost)
        {
            var ru = new CultureInfo("ru-RU");
            var client = _db.Data.Clients.FirstOrDefault(c => c.Id == order.ClientId);
            var device = _db.Data.Devices.FirstOrDefault(d => d.Id == order.DeviceId);
            var sb = new StringBuilder();
            sb.AppendLine("АКТ ВЫПОЛНЕННЫХ РАБОТ К ЗАКАЗУ № " + order.Number);
            sb.AppendLine("Клиент: " + (client != null ? client.FullName : "—") +
                          "; устройство: " + (device != null ? device.ToString() : "—"));
            sb.AppendLine(new string('-', 52));
            sb.AppendLine("Работы:");
            foreach (var w in order.Works)
                sb.AppendLine("  " + w.Name + " — " + w.Quantity + " x " + w.Price.ToString("N2", ru) +
                              " = " + w.Total.ToString("N2", ru));
            sb.AppendLine("Запчасти:");
            if (order.Parts.Count == 0)
                sb.AppendLine("  не использовались");
            foreach (var p in order.Parts)
                sb.AppendLine("  " + p.Name + " — " + p.Quantity + " x " + p.Price.ToString("N2", ru) +
                              " = " + p.Total.ToString("N2", ru));
            sb.AppendLine(new string('-', 52));
            sb.AppendLine("Работы: " + cost.WorksTotal.ToString("N2", ru) + " руб.");
            sb.AppendLine("Запчасти: " + cost.PartsTotal.ToString("N2", ru) + " руб.");
            if (cost.DiscountPercent > 0)
                sb.AppendLine("Скидка постоянного клиента " + cost.DiscountPercent + "%: -" +
                              cost.DiscountAmount.ToString("N2", ru) + " руб.");
            if (cost.IsWarranty)
                sb.AppendLine("Гарантийный ремонт: к оплате 0,00 руб.");
            sb.AppendLine("ИТОГО К ОПЛАТЕ: " + cost.Total.ToString("N2", ru) + " руб.");
            if (order.Status == OrderStatus.Issued && order.ClosedAt.HasValue && !order.IsWarrantyCase)
                sb.AppendLine("Гарантия до: " + order.ClosedAt.Value.AddDays(OrderService.WarrantyDays).ToString("dd.MM.yyyy"));
            return sb.ToString();
        }
    }
}
