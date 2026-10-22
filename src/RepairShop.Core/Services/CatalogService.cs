using System;
using System.Collections.Generic;
using System.Linq;
using RepairShop.Core.Infrastructure;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;
using RepairShop.Core.Storage;

namespace RepairShop.Core.Services
{
    /// <summary>Прайс-лист услуг и складской учёт запчастей.</summary>
    public class CatalogService
    {
        /// <summary>Остаток, при котором запчасть считается заканчивающейся.</summary>
        public const int LowStockThreshold = 3;

        private readonly ShopDatabase _db;

        public CatalogService(ShopDatabase db)
        {
            _db = db;
        }

        // ---------- Услуги ----------

        public ServiceItem AddService(string name, decimal price)
        {
            name = Validation.RequireText(name, "Наименование");
            Validation.RequireNonNegative(price, "Цена");
            if (_db.Data.Services.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessException("Такая услуга уже есть в прайс-листе.");
            var item = new ServiceItem
            {
                Id = ShopDatabase.NextId(_db.Data.Services.Select(s => s.Id)),
                Name = name,
                Price = price
            };
            _db.Data.Services.Add(item);
            _db.Save();
            return item;
        }

        public void UpdateService(int id, string name, decimal price)
        {
            var item = GetService(id);
            name = Validation.RequireText(name, "Наименование");
            Validation.RequireNonNegative(price, "Цена");
            item.Name = name;
            item.Price = price;
            _db.Save();
        }

        public void DeleteService(int id)
        {
            _db.Data.Services.Remove(GetService(id));
            _db.Save();
        }

        public ServiceItem GetService(int id)
        {
            var item = _db.Data.Services.FirstOrDefault(s => s.Id == id);
            if (item == null)
                throw new BusinessException("Услуга не найдена.");
            return item;
        }

        public List<ServiceItem> Services()
        {
            return _db.Data.Services.OrderBy(s => s.Name).ToList();
        }

        // ---------- Запчасти ----------

        public Part AddPart(string article, string name, int quantity, decimal price)
        {
            article = Validation.RequireText(article, "Артикул");
            name = Validation.RequireText(name, "Наименование");
            if (quantity < 0)
                throw new BusinessException("Количество не может быть отрицательным.");
            Validation.RequireNonNegative(price, "Цена");
            if (_db.Data.Parts.Any(p => string.Equals(p.Article, article, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessException("Запчасть с таким артикулом уже есть на складе.");
            var part = new Part
            {
                Id = ShopDatabase.NextId(_db.Data.Parts.Select(p => p.Id)),
                Article = article,
                Name = name,
                Quantity = quantity,
                Price = price
            };
            _db.Data.Parts.Add(part);
            _db.Save();
            return part;
        }

        public void UpdatePart(int id, string name, decimal price)
        {
            var part = GetPart(id);
            name = Validation.RequireText(name, "Наименование");
            Validation.RequireNonNegative(price, "Цена");
            part.Name = name;
            part.Price = price;
            _db.Save();
        }

        /// <summary>Пополнение склада (приход запчастей).</summary>
        public void Restock(int id, int quantity)
        {
            Validation.RequirePositive(quantity, "Количество");
            GetPart(id).Quantity += quantity;
            _db.Save();
        }

        public void DeletePart(int id)
        {
            var part = GetPart(id);
            if (_db.Data.Orders.Any(o => o.Parts.Any(p => p.PartId == id)))
                throw new BusinessException("Запчасть используется в заказах и не может быть удалена.");
            _db.Data.Parts.Remove(part);
            _db.Save();
        }

        public Part GetPart(int id)
        {
            var part = _db.Data.Parts.FirstOrDefault(p => p.Id == id);
            if (part == null)
                throw new BusinessException("Запчасть не найдена.");
            return part;
        }

        public List<Part> Parts()
        {
            return _db.Data.Parts.OrderBy(p => p.Name).ToList();
        }

        public List<Part> LowStock()
        {
            return _db.Data.Parts.Where(p => p.Quantity < LowStockThreshold)
                .OrderBy(p => p.Quantity).ThenBy(p => p.Name).ToList();
        }
    }
}
