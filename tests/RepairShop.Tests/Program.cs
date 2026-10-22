using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepairShop.Core;
using RepairShop.Core.Infrastructure;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;
using RepairShop.Core.Services;
using RepairShop.Core.Storage;

namespace RepairShop.Tests
{
    /// <summary>Минимальный самописный запуск модульных тестов (без внешних пакетов).</summary>
    public static class Program
    {
        private static int _passed, _failed;
        private static readonly FixedClock Clock = new FixedClock(new DateTime(2026, 10, 1, 10, 0, 0));

        public static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Модульные тесты RepairShop.Core");
            Console.WriteLine(new string('=', 72));

            // ----- Проверка данных -----
            Run("T01 Телефон 8(918)... приводится к +7", () =>
                Eq("+79181234567", Validation.NormalizePhone("8 (918) 123-45-67")));
            Run("T02 Телефон из 10 цифр получает код +7", () =>
                Eq("+79005551122", Validation.NormalizePhone("9005551122")));
            Run("T03 Короткий телефон отклоняется", () =>
                Throws(() => Validation.NormalizePhone("12345")));
            Run("T04 Некорректный e-mail отклоняется", () =>
                Throws(() => Validation.NormalizeEmail("abc@")));

            // ----- Авторизация -----
            Run("T05 Вход с верным паролем", () =>
            {
                var app = NewApp();
                True(app.Auth.Login("admin", "admin123") != null);
            });
            Run("T06 Вход с неверным паролем отклоняется", () =>
            {
                var app = NewApp();
                True(app.Auth.Login("admin", "wrong") == null);
            });
            Run("T07 Пароль не хранится в открытом виде", () =>
            {
                var app = NewApp();
                var u = app.Db.Data.Users.First(x => x.Login == "admin");
                True(u.PasswordHash != "admin123" && u.PasswordHash.Length > 20);
            });
            Run("T08 Короткий пароль сотрудника отклоняется", () =>
                Throws(() => NewApp().Auth.CreateUser("x", "Тест Тестов", "123", UserRole.Master)));
            Run("T09 Нельзя отключить последнего администратора", () =>
            {
                var app = NewApp();
                int id = app.Db.Data.Users.First(x => x.Login == "admin").Id;
                Throws(() => app.Auth.SetActive(id, false));
            });

            // ----- Клиенты -----
            Run("T10 Повторный телефон клиента отклоняется", () =>
            {
                var app = NewApp();
                app.Clients.Add("Новый Клиент", "+7 900 111 22 33", "");
                Throws(() => app.Clients.Add("Другой Клиент", "89001112233", ""));
            });
            Run("T11 Поиск клиента по части телефона", () =>
            {
                var app = NewApp();
                Eq(1, app.Clients.Search("1234567").Count);
            });
            Run("T12 Клиента с заказами удалить нельзя", () =>
            {
                var app = NewApp();
                int id = app.Db.Data.Orders.First().ClientId;
                Throws(() => app.Clients.Delete(id));
            });

            // ----- Заказы -----
            Run("T13 Номер заказа формируется по шаблону РМ-ГГГГ-NNNN", () =>
            {
                var app = NewApp();
                var o = NewOrder(app, "SN-NEW-1");
                True(o.Number.StartsWith("РМ-2026-"));
                Eq(12, o.Number.Length);
            });
            Run("T14 Заказ без описания неисправности отклоняется", () =>
            {
                var app = NewApp();
                int cid = app.Db.Data.Clients.First().Id;
                Throws(() => app.Orders.CreateOrder(cid,
                    new Device { Brand = "A", Model = "B" }, " ", 0, Clock.Now.AddDays(1)));
            });
            Run("T15 Срок готовности в прошлом отклоняется", () =>
            {
                var app = NewApp();
                int cid = app.Db.Data.Clients.First().Id;
                Throws(() => app.Orders.CreateOrder(cid,
                    new Device { Brand = "A", Model = "B" }, "не включается", 0, Clock.Now.AddDays(-1)));
            });
            Run("T16 Допустимый переход статуса: Принят → Диагностика", () =>
            {
                var app = NewApp();
                var o = NewOrder(app, "SN-T16", master: true);
                app.Orders.ChangeStatus(o.Id, OrderStatus.Diagnostics, "");
                Eq(OrderStatus.Diagnostics, o.Status);
            });
            Run("T17 Недопустимый переход: Принят → Выдан", () =>
            {
                var app = NewApp();
                var o = NewOrder(app, "SN-T17", master: true);
                Throws(() => app.Orders.ChangeStatus(o.Id, OrderStatus.Issued, ""));
            });
            Run("T18 Диагностика без мастера запрещена", () =>
            {
                var app = NewApp();
                var o = NewOrder(app, "SN-T18", master: false);
                Throws(() => app.Orders.ChangeStatus(o.Id, OrderStatus.Diagnostics, ""));
            });
            Run("T19 Завершить ремонт без работ нельзя", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T19");
                Throws(() => app.Orders.ChangeStatus(o.Id, OrderStatus.Ready, ""));
            });
            Run("T20 Каждая смена статуса попадает в историю", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T20");
                Eq(3, o.History.Count);
            });

            // ----- Работы, запчасти, склад -----
            Run("T21 Добавление запчасти списывает склад", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T21");
                var part = app.Catalog.Parts().First(p => p.Article == "USB-C-UNI");
                int before = part.Quantity;
                app.Orders.AddPart(o.Id, part.Id, 2);
                Eq(before - 2, part.Quantity);
            });
            Run("T22 Нехватка запчастей отклоняется, склад не меняется", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T22");
                var part = app.Catalog.Parts().First(p => p.Article == "LED-TV-43");
                Throws(() => app.Orders.AddPart(o.Id, part.Id, 5));
                Eq(1, part.Quantity);
            });
            Run("T23 Удаление запчасти из заказа возвращает её на склад", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T23");
                var part = app.Catalog.Parts().First(p => p.Article == "USB-C-UNI");
                int before = part.Quantity;
                app.Orders.AddPart(o.Id, part.Id, 3);
                app.Orders.RemovePart(o.Id, 0);
                Eq(before, part.Quantity);
            });
            Run("T24 Изменять состав работ в статусе «Принят» нельзя", () =>
            {
                var app = NewApp();
                var o = NewOrder(app, "SN-T24", master: true);
                Throws(() => app.Orders.AddWork(o.Id, app.Catalog.Services().First().Id));
            });
            Run("T25 Пополнение склада увеличивает остаток", () =>
            {
                var app = NewApp();
                var part = app.Catalog.Parts().First(p => p.Article == "LED-TV-43");
                app.Catalog.Restock(part.Id, 4);
                Eq(5, part.Quantity);
            });
            Run("T26 Список заканчивающихся запчастей", () =>
            {
                var app = NewApp();
                True(app.Catalog.LowStock().Any(p => p.Article == "LED-TV-43"));
            });

            // ----- Стоимость -----
            Run("T27 Итог = работы + запчасти", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T27");
                app.Orders.AddCustomWork(o.Id, "Пайка", 1000m, 2);
                app.Orders.AddPart(o.Id, app.Catalog.Parts().First(p => p.Article == "USB-C-UNI").Id, 2);
                var cost = app.Orders.CalculateCost(o);
                Eq(2300m, cost.Total);
            });
            Run("T28 Скидка 5% у клиента с тремя выданными заказами", () =>
            {
                Eq(0, CostCalculator.LoyaltyDiscountPercent(2));
                Eq(5, CostCalculator.LoyaltyDiscountPercent(3));
                Eq(10, CostCalculator.LoyaltyDiscountPercent(6));
            });
            Run("T29 Скидка применяется только к работам", () =>
            {
                var o = new RepairOrder();
                o.Works.Add(new OrderWork { Name = "w", Price = 1000m, Quantity = 1 });
                o.Parts.Add(new OrderPart { Name = "p", Price = 500m, Quantity = 1 });
                var c = CostCalculator.Calculate(o, 3);
                Eq(50m, c.DiscountAmount);
                Eq(1450m, c.Total);
            });
            Run("T30 Отмена после диагностики: оплачивается только диагностика", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T30");
                app.Orders.AddCustomWork(o.Id, "Работа", 3000m);
                app.Orders.ChangeStatus(o.Id, OrderStatus.Cancelled, "Клиент отказался");
                Eq(500m, o.FinalCost);
            });
            Run("T31 Отмена возвращает запчасти на склад", () =>
            {
                var app = NewApp();
                var o = ToRepair(app, "SN-T31");
                var part = app.Catalog.Parts().First(p => p.Article == "USB-C-UNI");
                int before = part.Quantity;
                app.Orders.AddPart(o.Id, part.Id, 4);
                app.Orders.ChangeStatus(o.Id, OrderStatus.Cancelled, "");
                Eq(before, part.Quantity);
            });

            // ----- Выдача и гарантия -----
            Run("T32 Выдача фиксирует итоговую стоимость и дату закрытия", () =>
            {
                var app = NewApp();
                var o = Issue(app, "SN-T32");
                True(o.ClosedAt.HasValue);
                Eq(1000m, o.FinalCost);
            });
            Run("T33 Повторное обращение в гарантийный срок бесплатно", () =>
            {
                var app = NewApp();
                Issue(app, "SN-T33");
                Clock.Current = Clock.Current.AddDays(10);
                var again = NewOrder(app, "SN-T33", master: true);
                True(again.IsWarrantyCase);
                Eq(0m, app.Orders.CalculateCost(again).Total);
            });
            Run("T34 После окончания гарантии ремонт платный", () =>
            {
                var app = NewApp();
                Issue(app, "SN-T34");
                Clock.Current = Clock.Current.AddDays(OrderService.WarrantyDays + 1);
                var again = NewOrder(app, "SN-T34", master: true);
                True(!again.IsWarrantyCase);
            });
            Run("T35 Просроченный заказ определяется по сроку", () =>
            {
                var app = NewApp();
                var o = NewOrder(app, "SN-T35", master: true);
                Clock.Current = Clock.Current.AddDays(5);
                True(app.Orders.IsOverdue(o));
            });

            // ----- Поиск и отчёты -----
            Run("T36 Поиск заказа по серийному номеру", () =>
            {
                var app = NewApp();
                NewOrder(app, "UNIQ-777");
                Eq(1, app.Orders.Search(new OrderFilter { Text = "uniq-777" }).Count);
            });
            Run("T37 Фильтр по статусу", () =>
            {
                var app = NewApp();
                int ready = app.Orders.Search(new OrderFilter { Status = OrderStatus.Ready }).Count;
                Eq(1, ready);
            });
            Run("T38 Выручка за период считает только закрытые заказы", () =>
            {
                var app = NewApp();
                Issue(app, "SN-T38");
                Eq(1000m, app.Reports.Revenue(Clock.Now.Date, Clock.Now.Date));
                Eq(0m, app.Reports.Revenue(Clock.Now.Date.AddDays(-3), Clock.Now.Date.AddDays(-1)));
            });
            Run("T39 CSV-выгрузка содержит заголовок и строки заказов", () =>
            {
                var app = NewApp();
                string csv = app.Reports.ToCsv(app.Orders.Search(null));
                True(csv.StartsWith("Номер;Дата;Клиент"));
                Eq(app.Db.Data.Orders.Count + 1, csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length);
            });

            // ----- Хранилище -----
            Run("T40 Данные сохраняются в XML и читаются обратно", () =>
            {
                string path = Path.Combine(Path.GetTempPath(), "repairshop_test_" + Guid.NewGuid() + ".xml");
                try
                {
                    var app1 = new ShopApp(new XmlDataStore(path), Clock);
                    DemoData.EnsureSeed(app1);
                    var app2 = new ShopApp(new XmlDataStore(path), Clock);
                    Eq(app1.Db.Data.Orders.Count, app2.Db.Data.Orders.Count);
                    Eq(app1.Db.Data.Parts.Count, app2.Db.Data.Parts.Count);
                    True(app2.Auth.Login("ivanov", "master123") != null);
                }
                finally { if (File.Exists(path)) File.Delete(path); }
            });

            Console.WriteLine(new string('=', 72));
            Console.WriteLine("Пройдено: " + _passed + ", провалено: " + _failed + ", всего: " + (_passed + _failed));
            return _failed == 0 ? 0 : 1;
        }

        // ---------- вспомогательные методы ----------

        private static ShopApp NewApp()
        {
            Clock.Current = new DateTime(2026, 10, 1, 10, 0, 0);
            var app = new ShopApp(new MemoryDataStore(), Clock);
            DemoData.EnsureSeed(app);
            return app;
        }

        private static RepairOrder NewOrder(ShopApp app, string serial, bool master = false)
        {
            // клиент без скидки постоянного покупателя
            int cid = app.Db.Data.Clients.First(c => c.FullName.StartsWith("Попов")).Id;
            int mid = master ? app.Auth.Masters().First().Id : 0;
            return app.Orders.CreateOrder(cid,
                new Device { Type = DeviceType.Smartphone, Brand = "Xiaomi", Model = "Redmi 9", SerialNumber = serial },
                "Не включается", mid, Clock.Now.AddDays(3));
        }

        private static RepairOrder ToRepair(ShopApp app, string serial)
        {
            var o = NewOrder(app, serial, master: true);
            app.Orders.ChangeStatus(o.Id, OrderStatus.Diagnostics, "");
            app.Orders.ChangeStatus(o.Id, OrderStatus.InRepair, "");
            return o;
        }

        private static RepairOrder Issue(ShopApp app, string serial)
        {
            var o = ToRepair(app, serial);
            app.Orders.AddCustomWork(o.Id, "Ремонт", 1000m);
            app.Orders.ChangeStatus(o.Id, OrderStatus.Ready, "");
            app.Orders.ChangeStatus(o.Id, OrderStatus.Issued, "");
            return o;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine("[ OK ] " + name);
            }
            catch (Exception ex)
            {
                _failed++;
                Console.WriteLine("[FAIL] " + name + " -> " + ex.Message);
            }
        }

        private static void Eq<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("ожидалось " + expected + ", получено " + actual);
        }

        private static void True(bool condition)
        {
            if (!condition) throw new Exception("условие не выполнено");
        }

        private static void Throws(Action action)
        {
            try { action(); }
            catch (BusinessException) { return; }
            throw new Exception("ожидалось исключение BusinessException");
        }
    }
}
