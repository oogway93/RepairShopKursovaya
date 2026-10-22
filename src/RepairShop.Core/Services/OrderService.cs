using System;
using System.Collections.Generic;
using System.Linq;
using RepairShop.Core.Infrastructure;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;
using RepairShop.Core.Storage;

namespace RepairShop.Core.Services
{
    /// <summary>Параметры поиска заказов.</summary>
    public class OrderFilter
    {
        public string Text { get; set; }
        public OrderStatus? Status { get; set; }
        public int MasterId { get; set; }          // 0 – любой мастер
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool ActiveOnly { get; set; }
    }

    /// <summary>Приём заказов, ведение ремонта, расчёт стоимости и выдача.</summary>
    public class OrderService
    {
        /// <summary>Гарантийный срок на выполненный ремонт, дней.</summary>
        public const int WarrantyDays = 30;

        private readonly ShopDatabase _db;

        public OrderService(ShopDatabase db)
        {
            _db = db;
        }

        // ---------- Создание ----------

        /// <summary>Оформляет приём устройства в ремонт.</summary>
        public RepairOrder CreateOrder(int clientId, Device device, string defect,
                                       int masterId, DateTime dueDate, int userId = 0)
        {
            if (!_db.Data.Clients.Any(c => c.Id == clientId))
                throw new BusinessException("Клиент не найден.");
            if (device == null)
                throw new BusinessException("Не указано устройство.");
            device.Brand = Validation.RequireText(device.Brand, "Производитель");
            device.Model = Validation.RequireText(device.Model, "Модель");
            device.SerialNumber = (device.SerialNumber ?? string.Empty).Trim();
            defect = Validation.RequireText(defect, "Описание неисправности");

            DateTime now = _db.Clock.Now;
            if (dueDate.Date < now.Date)
                throw new BusinessException("Срок готовности не может быть раньше сегодняшней даты.");
            if (masterId != 0)
                RequireMaster(masterId);

            device.Id = ShopDatabase.NextId(_db.Data.Devices.Select(d => d.Id));
            device.ClientId = clientId;

            var order = new RepairOrder
            {
                Id = ShopDatabase.NextId(_db.Data.Orders.Select(o => o.Id)),
                Number = NextNumber(now),
                ClientId = clientId,
                DeviceId = device.Id,
                Defect = defect,
                CreatedAt = now,
                DueDate = dueDate.Date,
                Status = OrderStatus.Accepted,
                MasterId = masterId,
                IsWarrantyCase = FindWarrantyBase(device.SerialNumber, now) != null
            };
            order.History.Add(new StatusChange
            {
                Date = now,
                Status = OrderStatus.Accepted,
                UserId = userId,
                Comment = order.IsWarrantyCase ? "Устройство принято по гарантии" : "Устройство принято"
            });

            _db.Data.Devices.Add(device);
            _db.Data.Orders.Add(order);
            _db.Save();
            return order;
        }

        private string NextNumber(DateTime now)
        {
            string prefix = "РМ-" + now.Year + "-";
            int count = _db.Data.Orders.Count(o => o.Number.StartsWith(prefix, StringComparison.Ordinal));
            return prefix + (count + 1).ToString("0000");
        }

        /// <summary>
        /// Ищет выданный заказ на то же устройство (по серийному номеру),
        /// гарантия на который ещё действует.
        /// </summary>
        public RepairOrder FindWarrantyBase(string serialNumber, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return null;
            string serial = serialNumber.Trim();
            foreach (var order in _db.Data.Orders)
            {
                if (order.Status != OrderStatus.Issued || order.IsWarrantyCase || !order.ClosedAt.HasValue)
                    continue;
                var device = _db.Data.Devices.FirstOrDefault(d => d.Id == order.DeviceId);
                if (device == null ||
                    !string.Equals(device.SerialNumber, serial, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (now <= order.ClosedAt.Value.AddDays(WarrantyDays))
                    return order;
            }
            return null;
        }

        // ---------- Чтение ----------

        public RepairOrder Get(int id)
        {
            var order = _db.Data.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
                throw new BusinessException("Заказ не найден.");
            return order;
        }

        public Client ClientOf(RepairOrder order)
        {
            return _db.Data.Clients.FirstOrDefault(c => c.Id == order.ClientId);
        }

        public Device DeviceOf(RepairOrder order)
        {
            return _db.Data.Devices.FirstOrDefault(d => d.Id == order.DeviceId);
        }

        public User MasterOf(RepairOrder order)
        {
            return _db.Data.Users.FirstOrDefault(u => u.Id == order.MasterId);
        }

        public List<RepairOrder> Search(OrderFilter filter)
        {
            IEnumerable<RepairOrder> query = _db.Data.Orders;
            if (filter != null)
            {
                if (filter.Status.HasValue)
                    query = query.Where(o => o.Status == filter.Status.Value);
                if (filter.MasterId != 0)
                    query = query.Where(o => o.MasterId == filter.MasterId);
                if (filter.From.HasValue)
                    query = query.Where(o => o.CreatedAt.Date >= filter.From.Value.Date);
                if (filter.To.HasValue)
                    query = query.Where(o => o.CreatedAt.Date <= filter.To.Value.Date);
                if (filter.ActiveOnly)
                    query = query.Where(o => !OrderWorkflow.IsFinal(o.Status));
                if (!string.IsNullOrWhiteSpace(filter.Text))
                {
                    string t = filter.Text.Trim();
                    query = query.Where(o => Matches(o, t));
                }
            }
            return query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToList();
        }

        private bool Matches(RepairOrder order, string text)
        {
            if (order.Number.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            var client = ClientOf(order);
            if (client != null)
            {
                string digits = PhoneDigits(text);
                if (client.FullName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (digits.Length > 0 && client.Phone.Contains(digits)))
                    return true;
            }
            var device = DeviceOf(order);
            if (device != null)
            {
                if ((device.Brand + " " + device.Model).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (device.SerialNumber ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>Цифры телефона, если запрос похож на номер (иначе пустая строка).</summary>
        public static string PhoneDigits(string text)
        {
            if (text.Any(ch => !(char.IsDigit(ch) || ch == '+' || ch == '-' || ch == '(' || ch == ')' || ch == ' ')))
                return string.Empty;
            return new string(text.Where(char.IsDigit).ToArray());
        }

        // ---------- Статусы ----------

        public void AssignMaster(int orderId, int masterId, int userId = 0)
        {
            var order = Get(orderId);
            if (OrderWorkflow.IsFinal(order.Status))
                throw new BusinessException("Заказ закрыт, мастера изменить нельзя.");
            RequireMaster(masterId);
            order.MasterId = masterId;
            _db.Save();
        }

        public void ChangeStatus(int orderId, OrderStatus newStatus, string comment, int userId = 0)
        {
            var order = Get(orderId);
            if (!OrderWorkflow.CanTransit(order.Status, newStatus))
                throw new BusinessException("Переход из статуса «" + Localization.Name(order.Status) +
                                            "» в статус «" + Localization.Name(newStatus) + "» невозможен.");

            if ((newStatus == OrderStatus.Diagnostics || newStatus == OrderStatus.InRepair) && order.MasterId == 0)
                throw new BusinessException("Сначала назначьте мастера на заказ.");
            if (newStatus == OrderStatus.Ready && order.Works.Count == 0)
                throw new BusinessException("Нельзя завершить ремонт: в заказе нет ни одной выполненной работы.");

            DateTime now = _db.Clock.Now;

            if (newStatus == OrderStatus.Cancelled)
            {
                ReturnPartsToStock(order);
                order.Works.Clear();
            }

            order.Status = newStatus;
            order.History.Add(new StatusChange
            {
                Date = now,
                Status = newStatus,
                UserId = userId,
                Comment = string.IsNullOrWhiteSpace(comment) ? string.Empty : comment.Trim()
            });

            if (newStatus == OrderStatus.Issued || newStatus == OrderStatus.Cancelled)
            {
                order.ClosedAt = now;
                order.FinalCost = CalculateCost(order).Total;
            }
            _db.Save();
        }

        // ---------- Работы и запчасти ----------

        public void AddWork(int orderId, int serviceId, int quantity = 1)
        {
            var order = RequireEditable(orderId);
            Validation.RequirePositive(quantity, "Количество");
            var service = _db.Data.Services.FirstOrDefault(s => s.Id == serviceId);
            if (service == null)
                throw new BusinessException("Услуга не найдена в прайс-листе.");
            order.Works.Add(new OrderWork
            {
                ServiceId = service.Id,
                Name = service.Name,
                Price = service.Price,
                Quantity = quantity
            });
            _db.Save();
        }

        public void AddCustomWork(int orderId, string name, decimal price, int quantity = 1)
        {
            var order = RequireEditable(orderId);
            name = Validation.RequireText(name, "Наименование работы");
            Validation.RequireNonNegative(price, "Цена");
            Validation.RequirePositive(quantity, "Количество");
            order.Works.Add(new OrderWork { ServiceId = 0, Name = name, Price = price, Quantity = quantity });
            _db.Save();
        }

        public void RemoveWork(int orderId, int index)
        {
            var order = RequireEditable(orderId);
            if (index < 0 || index >= order.Works.Count)
                throw new BusinessException("Работа не найдена.");
            order.Works.RemoveAt(index);
            _db.Save();
        }

        /// <summary>Списывает запчасть со склада и добавляет её в заказ.</summary>
        public void AddPart(int orderId, int partId, int quantity = 1)
        {
            var order = RequireEditable(orderId);
            Validation.RequirePositive(quantity, "Количество");
            var part = _db.Data.Parts.FirstOrDefault(p => p.Id == partId);
            if (part == null)
                throw new BusinessException("Запчасть не найдена.");
            if (part.Quantity < quantity)
                throw new BusinessException("Недостаточно запчастей на складе: «" + part.Name +
                                            "», доступно " + part.Quantity + " шт.");

            part.Quantity -= quantity;
            var existing = order.Parts.FirstOrDefault(p => p.PartId == partId && p.Price == part.Price);
            if (existing != null)
                existing.Quantity += quantity;
            else
                order.Parts.Add(new OrderPart
                {
                    PartId = part.Id,
                    Name = part.Name,
                    Price = part.Price,
                    Quantity = quantity
                });
            _db.Save();
        }

        /// <summary>Убирает запчасть из заказа и возвращает её на склад.</summary>
        public void RemovePart(int orderId, int index)
        {
            var order = RequireEditable(orderId);
            if (index < 0 || index >= order.Parts.Count)
                throw new BusinessException("Запчасть не найдена в заказе.");
            var line = order.Parts[index];
            var part = _db.Data.Parts.FirstOrDefault(p => p.Id == line.PartId);
            if (part != null)
                part.Quantity += line.Quantity;
            order.Parts.RemoveAt(index);
            _db.Save();
        }

        private void ReturnPartsToStock(RepairOrder order)
        {
            foreach (var line in order.Parts)
            {
                var part = _db.Data.Parts.FirstOrDefault(p => p.Id == line.PartId);
                if (part != null)
                    part.Quantity += line.Quantity;
            }
            order.Parts.Clear();
        }

        // ---------- Стоимость и гарантия ----------

        public CostBreakdown CalculateCost(RepairOrder order)
        {
            // Для закрытого заказа скидку считаем по числу выданных заказов клиента до него.
            int completedBefore = _db.Data.Orders.Count(o =>
                o.ClientId == order.ClientId && o.Status == OrderStatus.Issued &&
                o.Id != order.Id && o.CreatedAt < order.CreatedAt);
            return CostCalculator.Calculate(order, completedBefore);
        }

        public DateTime? WarrantyEnd(RepairOrder order)
        {
            if (order.Status != OrderStatus.Issued || !order.ClosedAt.HasValue || order.IsWarrantyCase)
                return null;
            return order.ClosedAt.Value.AddDays(WarrantyDays);
        }

        public bool IsOverdue(RepairOrder order)
        {
            return !OrderWorkflow.IsFinal(order.Status) && order.DueDate.Date < _db.Clock.Now.Date;
        }

        // ---------- Вспомогательные ----------

        private RepairOrder RequireEditable(int orderId)
        {
            var order = Get(orderId);
            if (!OrderWorkflow.IsEditable(order.Status))
                throw new BusinessException("В статусе «" + Localization.Name(order.Status) +
                                            "» состав работ и запчастей изменять нельзя.");
            return order;
        }

        private void RequireMaster(int masterId)
        {
            var master = _db.Data.Users.FirstOrDefault(u => u.Id == masterId);
            if (master == null || master.Role != UserRole.Master || !master.IsActive)
                throw new BusinessException("Указанный мастер не найден или недоступен.");
        }
    }
}
