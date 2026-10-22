using System;
using System.Collections.Generic;
using System.Linq;
using RepairShop.Core.Infrastructure;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;
using RepairShop.Core.Storage;

namespace RepairShop.Core.Services
{
    /// <summary>Учёт клиентов мастерской.</summary>
    public class ClientService
    {
        private readonly ShopDatabase _db;

        public ClientService(ShopDatabase db)
        {
            _db = db;
        }

        public Client Add(string fullName, string phone, string email)
        {
            fullName = Validation.RequireText(fullName, "ФИО");
            phone = Validation.NormalizePhone(phone);
            email = Validation.NormalizeEmail(email);
            if (_db.Data.Clients.Any(c => c.Phone == phone))
                throw new BusinessException("Клиент с таким телефоном уже зарегистрирован.");

            var client = new Client
            {
                Id = ShopDatabase.NextId(_db.Data.Clients.Select(c => c.Id)),
                FullName = fullName,
                Phone = phone,
                Email = email
            };
            _db.Data.Clients.Add(client);
            _db.Save();
            return client;
        }

        public void Update(int id, string fullName, string phone, string email)
        {
            var client = Get(id);
            fullName = Validation.RequireText(fullName, "ФИО");
            phone = Validation.NormalizePhone(phone);
            email = Validation.NormalizeEmail(email);
            if (_db.Data.Clients.Any(c => c.Phone == phone && c.Id != id))
                throw new BusinessException("Клиент с таким телефоном уже зарегистрирован.");
            client.FullName = fullName;
            client.Phone = phone;
            client.Email = email;
            _db.Save();
        }

        public void Delete(int id)
        {
            var client = Get(id);
            if (_db.Data.Orders.Any(o => o.ClientId == id))
                throw new BusinessException("Нельзя удалить клиента, у которого есть заказы.");
            _db.Data.Devices.RemoveAll(d => d.ClientId == id);
            _db.Data.Clients.Remove(client);
            _db.Save();
        }

        public Client Get(int id)
        {
            var client = _db.Data.Clients.FirstOrDefault(c => c.Id == id);
            if (client == null)
                throw new BusinessException("Клиент не найден.");
            return client;
        }

        /// <summary>Поиск по ФИО, телефону или e-mail (без учёта регистра).</summary>
        public List<Client> Search(string text)
        {
            IEnumerable<Client> query = _db.Data.Clients;
            if (!string.IsNullOrWhiteSpace(text))
            {
                string t = text.Trim();
                string digits = OrderService.PhoneDigits(t);
                query = query.Where(c =>
                    c.FullName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (c.Email ?? string.Empty).IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (digits.Length > 0 && c.Phone.Contains(digits)));
            }
            return query.OrderBy(c => c.FullName).ToList();
        }

        /// <summary>Сколько заказов клиента уже выдано (для скидки постоянного клиента).</summary>
        public int CompletedOrders(int clientId)
        {
            return _db.Data.Orders.Count(o => o.ClientId == clientId && o.Status == OrderStatus.Issued);
        }

        public int OrdersCount(int clientId)
        {
            return _db.Data.Orders.Count(o => o.ClientId == clientId);
        }
    }
}
