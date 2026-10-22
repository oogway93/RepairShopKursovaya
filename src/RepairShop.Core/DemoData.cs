using System;
using System.Linq;
using RepairShop.Core.Models;
using RepairShop.Core.Services;
using RepairShop.Core.Storage;

namespace RepairShop.Core
{
    /// <summary>Начальное наполнение базы при первом запуске.</summary>
    public static class DemoData
    {
        public static void EnsureSeed(ShopApp app)
        {
            if (app.Db.Data.Users.Count > 0)
                return;

            app.Auth.CreateUser("admin", "Лемешинский Марк", "admin123", UserRole.Administrator);
            var m1 = app.Auth.CreateUser("ivanov", "Иванов Сергей", "master123", UserRole.Master);
            var m2 = app.Auth.CreateUser("petrova", "Петрова Ольга", "master123", UserRole.Master);

            string[][] services =
            {
                new[] { "Диагностика устройства", "500" },
                new[] { "Замена дисплея смартфона", "2500" },
                new[] { "Замена аккумулятора", "1200" },
                new[] { "Замена разъёма зарядки", "900" },
                new[] { "Чистка ноутбука от пыли и замена термопасты", "1500" },
                new[] { "Замена клавиатуры ноутбука", "1800" },
                new[] { "Ремонт материнской платы", "4500" },
                new[] { "Замена подсветки телевизора", "3500" },
                new[] { "Установка и настройка ОС", "1000" }
            };
            foreach (var s in services)
                app.Catalog.AddService(s[0], decimal.Parse(s[1]));

            app.Catalog.AddPart("DSP-SM-A52", "Дисплейный модуль Samsung A52", 5, 3200m);
            app.Catalog.AddPart("BAT-IP11", "Аккумулятор iPhone 11", 8, 1800m);
            app.Catalog.AddPart("USB-C-UNI", "Разъём USB Type-C", 20, 150m);
            app.Catalog.AddPart("KBD-LEN-L5", "Клавиатура Lenovo IdeaPad L5", 2, 1400m);
            app.Catalog.AddPart("PST-GD-3", "Термопаста GD900 (3 г)", 15, 250m);
            app.Catalog.AddPart("LED-TV-43", "Планка подсветки 43\"", 1, 2100m);

            var c1 = app.Clients.Add("Смирнов Алексей Павлович", "8 (918) 123-45-67", "smirnov@mail.ru");
            var c2 = app.Clients.Add("Кузнецова Мария Андреевна", "+7 900 555 11 22", "kuznetsova@gmail.com");
            var c3 = app.Clients.Add("Попов Дмитрий Игоревич", "89995554433", "");
            var c4 = app.Clients.Add("Волкова Екатерина Сергеевна", "+7 (961) 777-88-99", "volkova@yandex.ru");

            DateTime today = app.Db.Clock.Now.Date;

            // Архив: ранее выданные и отменённый заказы (даты сдвигаются в прошлое).
            SeedClosed(app, c1, DeviceType.Smartphone, "Xiaomi", "Redmi 9", "XM9-70011", "Не заряжается", m1,
                       40, 2, "Замена разъёма зарядки", "USB-C-UNI");
            SeedClosed(app, c1, DeviceType.Laptop, "HP", "Pavilion 15", "HP15-4471", "Перегрев, шум кулера", m2,
                       30, 2, "Чистка ноутбука", "PST-GD-3");
            SeedClosed(app, c1, DeviceType.Tablet, "Huawei", "MatePad T10", "HW-T10-318", "Медленно работает", m1,
                       20, 1, "Установка и настройка ОС", null);
            SeedClosed(app, c2, DeviceType.GameConsole, "Sony", "PlayStation 4", "PS4-CUH-2208", "Не включается", m2,
                       15, 5, "Ремонт материнской платы", null);
            SeedClosed(app, c4, DeviceType.Smartphone, "Samsung", "Galaxy A52", "R58N30XYZ98", "Разбит дисплей", m1,
                       8, 2, "Замена дисплея смартфона", "DSP-SM-A52");
            SeedCancelled(app, c3, DeviceType.Tablet, "Lenovo", "Tab M10", "LTM10-5521", "Не включается", m2, 6);

            var o1 = app.Orders.CreateOrder(c1.Id,
                new Device { Type = DeviceType.Smartphone, Brand = "Samsung", Model = "Galaxy A52", SerialNumber = "R58N30ABC12" },
                "Разбит экран, сенсор не реагирует", m1.Id, today.AddDays(3));
            app.Orders.ChangeStatus(o1.Id, OrderStatus.Diagnostics, "Выполняется диагностика");
            app.Orders.ChangeStatus(o1.Id, OrderStatus.InRepair, "Требуется замена дисплея");
            app.Orders.AddWork(o1.Id, app.Catalog.Services().First(s => s.Name.StartsWith("Замена дисплея")).Id);
            app.Orders.AddPart(o1.Id, app.Catalog.Parts().First(p => p.Article == "DSP-SM-A52").Id);

            var o2 = app.Orders.CreateOrder(c2.Id,
                new Device { Type = DeviceType.Laptop, Brand = "Lenovo", Model = "IdeaPad L5", SerialNumber = "PF2XK9L1" },
                "Шумит вентилятор, ноутбук перегревается", m2.Id, today.AddDays(2));
            app.Orders.ChangeStatus(o2.Id, OrderStatus.Diagnostics, "");
            app.Orders.ChangeStatus(o2.Id, OrderStatus.InRepair, "Нужна чистка и замена термопасты");
            app.Orders.AddWork(o2.Id, app.Catalog.Services().First(s => s.Name.StartsWith("Чистка")).Id);
            app.Orders.AddPart(o2.Id, app.Catalog.Parts().First(p => p.Article == "PST-GD-3").Id);
            app.Orders.ChangeStatus(o2.Id, OrderStatus.Ready, "Ремонт завершён");

            app.Orders.CreateOrder(c3.Id,
                new Device { Type = DeviceType.Tv, Brand = "LG", Model = "43UP7500", SerialNumber = "" },
                "Включается и сразу гаснет изображение", 0, today.AddDays(5));

            var o4 = app.Orders.CreateOrder(c4.Id,
                new Device { Type = DeviceType.Smartphone, Brand = "Apple", Model = "iPhone 11", SerialNumber = "F17DX0QJN73" },
                "Быстро разряжается аккумулятор", m1.Id, today.AddDays(1));
            app.Orders.ChangeStatus(o4.Id, OrderStatus.Diagnostics, "");
            app.Orders.ChangeStatus(o4.Id, OrderStatus.WaitingParts, "Ожидается поставка аккумулятора");
        }

        private static RepairOrder SeedOrder(ShopApp app, Client client, DeviceType type, string brand, string model,
                                             string serial, string defect, User master)
        {
            var order = app.Orders.CreateOrder(client.Id,
                new Device { Type = type, Brand = brand, Model = model, SerialNumber = serial },
                defect, master.Id, app.Db.Clock.Now.Date.AddDays(3));
            app.Orders.ChangeStatus(order.Id, OrderStatus.Diagnostics, "");
            app.Orders.ChangeStatus(order.Id, OrderStatus.InRepair, "");
            return order;
        }

        private static void SeedClosed(ShopApp app, Client client, DeviceType type, string brand, string model,
                                       string serial, string defect, User master, int daysAgo, int repairDays,
                                       string serviceStart, string partArticle)
        {
            var order = SeedOrder(app, client, type, brand, model, serial, defect, master);
            app.Orders.AddWork(order.Id, app.Catalog.Services().First(s => s.Name.StartsWith(serviceStart)).Id);
            if (partArticle != null)
                app.Orders.AddPart(order.Id, app.Catalog.Parts().First(p => p.Article == partArticle).Id);
            app.Orders.ChangeStatus(order.Id, OrderStatus.Ready, "Ремонт завершён");
            app.Orders.ChangeStatus(order.Id, OrderStatus.Issued, "Выдан клиенту");
            Backdate(order, daysAgo, repairDays);
        }

        private static void SeedCancelled(ShopApp app, Client client, DeviceType type, string brand, string model,
                                          string serial, string defect, User master, int daysAgo)
        {
            var order = app.Orders.CreateOrder(client.Id,
                new Device { Type = type, Brand = brand, Model = model, SerialNumber = serial },
                defect, master.Id, app.Db.Clock.Now.Date.AddDays(3));
            app.Orders.ChangeStatus(order.Id, OrderStatus.Diagnostics, "");
            app.Orders.ChangeStatus(order.Id, OrderStatus.Cancelled, "Клиент отказался от ремонта");
            Backdate(order, daysAgo, 1);
        }

        /// <summary>Сдвигает даты заказа в прошлое (только для демонстрационных данных).</summary>
        private static void Backdate(RepairOrder order, int daysAgo, int repairDays)
        {
            TimeSpan shift = TimeSpan.FromDays(daysAgo);
            order.CreatedAt = order.CreatedAt - shift;
            order.DueDate = order.CreatedAt.Date.AddDays(3);
            foreach (var h in order.History)
                h.Date = h.Date - shift;
            order.ClosedAt = order.CreatedAt.AddDays(repairDays);
            order.History[order.History.Count - 1].Date = order.ClosedAt.Value;
        }
    }
}
