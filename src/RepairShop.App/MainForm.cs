using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RepairShop.Core;
using RepairShop.Core.Infrastructure;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;
using RepairShop.Core.Services;

namespace RepairShop.App
{
    /// <summary>Главное окно приложения.</summary>
    public class MainForm : Form
    {
        private static readonly CultureInfo Ru = new CultureInfo("ru-RU");

        private readonly ShopApp _app;
        private readonly User _user;
        private TabControl _tabs;

        // Заказы
        private TextBox _search;
        private ComboBox _statusFilter, _masterFilter;
        private CheckBox _activeOnly;
        private DataGridView _orders;
        // Клиенты, склад, прайс, сотрудники
        private TextBox _clientSearch;
        private DataGridView _clients, _parts, _services, _users;
        // Отчёты
        private DateTimePicker _from, _to;
        private TextBox _report;

        public bool LogoutRequested { get; private set; }

        public MainForm(ShopApp app, User user)
        {
            _app = app;
            _user = user;
            Text = "Ремонтная мастерская электронного оборудования";
            Font = Ui.BaseFont;
            ClientSize = new Size(1040, 640);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 520);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(_tabs);
            var header = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(38, 70, 110) };
            Controls.Add(header);
            var who = Ui.Label(header, "Сотрудник: " + user.FullName + "  (" + Localization.Name(user.Role) + ")", 12, 11);
            who.ForeColor = Color.White;
            who.Font = Ui.BoldFont;
            var logout = Ui.Button(header, "Выйти", 940, 6, 88, (s, e) => { LogoutRequested = true; Close(); }, 28);
            logout.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            BuildOrdersTab();
            if (IsAdmin) BuildClientsTab();
            BuildStockTab();
            BuildPriceTab();
            if (IsAdmin) BuildReportsTab();
            if (IsAdmin) BuildUsersTab();

            RefreshAll();
        }

        private bool IsAdmin { get { return _user.Role == UserRole.Administrator; } }

        private TabPage AddTab(string title)
        {
            var page = new TabPage(title) { Padding = new Padding(4) };
            _tabs.TabPages.Add(page);
            return page;
        }

        private void RefreshAll()
        {
            LoadOrders();
            if (_clients != null) LoadClients();
            LoadParts();
            LoadServices();
            if (_users != null) LoadUsers();
        }

        // ================= Заказы =================

        private void BuildOrdersTab()
        {
            var page = AddTab("Заказы");
            _orders = Ui.Grid(page, "№ заказа", "Принят", "Клиент", "Устройство", "Статус", "Мастер", "Срок", "Стоимость, руб.");
            _orders.Dock = DockStyle.Fill;
            _orders.CellDoubleClick += (s, e) => OpenOrder();

            var bar = new Panel { Dock = DockStyle.Top, Height = 78 };
            page.Controls.Add(bar);
            Ui.Label(bar, "Поиск (номер, клиент, телефон, устройство, S/N):", 6, 6);
            _search = Ui.Text(bar, 6, 26, 300);
            _search.TextChanged += (s, e) => LoadOrders();
            Ui.Label(bar, "Статус", 320, 6);
            _statusFilter = Ui.Combo(bar, 320, 26, 160);
            _statusFilter.Items.Add(new Item<OrderStatus?>("Все статусы", null));
            foreach (OrderStatus st in Enum.GetValues(typeof(OrderStatus)))
                _statusFilter.Items.Add(new Item<OrderStatus?>(Localization.Name(st), st));
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += (s, e) => LoadOrders();
            Ui.Label(bar, "Мастер", 492, 6);
            _masterFilter = Ui.Combo(bar, 492, 26, 160);
            _masterFilter.Items.Add(new Item<int>("Все мастера", 0));
            foreach (var m in _app.Auth.Masters())
                _masterFilter.Items.Add(new Item<int>(m.FullName, m.Id));
            _masterFilter.SelectedIndex = 0;
            if (!IsAdmin)
            {
                // мастер видит только свои заказы
                _masterFilter.SelectedIndex = _masterFilter.Items.Cast<Item<int>>().ToList()
                    .FindIndex(i => i.Value == _user.Id);
                _masterFilter.Enabled = false;
            }
            _masterFilter.SelectedIndexChanged += (s, e) => LoadOrders();
            _activeOnly = new CheckBox { Text = "Только активные", Left = 664, Top = 27, Width = 140 };
            _activeOnly.CheckedChanged += (s, e) => LoadOrders();
            bar.Controls.Add(_activeOnly);

            var newBtn = Ui.Button(bar, "Новый заказ", 6, 50, 120, (s, e) => NewOrder(), 24);
            newBtn.Enabled = IsAdmin;
            Ui.Button(bar, "Открыть заказ", 134, 50, 120, (s, e) => OpenOrder(), 24);
            var csv = Ui.Button(bar, "Экспорт в CSV…", 262, 50, 130, (s, e) => ExportCsv(), 24);
            csv.Enabled = IsAdmin;
        }

        private void LoadOrders()
        {
            if (_orders == null || _masterFilter == null || _statusFilter == null) return;
            var filter = new OrderFilter
            {
                Text = _search.Text,
                Status = ((Item<OrderStatus?>)_statusFilter.SelectedItem).Value,
                MasterId = ((Item<int>)_masterFilter.SelectedItem).Value,
                ActiveOnly = _activeOnly.Checked
            };
            _orders.Rows.Clear();
            foreach (var o in _app.Orders.Search(filter))
            {
                var client = _app.Orders.ClientOf(o);
                var device = _app.Orders.DeviceOf(o);
                var master = _app.Orders.MasterOf(o);
                decimal cost = OrderWorkflow.IsFinal(o.Status) ? o.FinalCost : _app.Orders.CalculateCost(o).Total;
                var row = Ui.AddRow(_orders, o, o.Number, o.CreatedAt.ToString("dd.MM.yyyy"), client.FullName,
                                    device.ToString(), Localization.Name(o.Status),
                                    master != null ? master.FullName : "—", o.DueDate.ToString("dd.MM.yyyy"),
                                    cost.ToString("N2", Ru));
                if (_app.Orders.IsOverdue(o)) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
                else if (o.Status == OrderStatus.Ready) row.DefaultCellStyle.BackColor = Color.FromArgb(220, 245, 220);
                else if (OrderWorkflow.IsFinal(o.Status)) row.DefaultCellStyle.ForeColor = Color.Gray;
            }
            _orders.ClearSelection();
            if (_orders.Rows.Count > 0) _orders.Rows[0].Selected = true;
        }

        private void NewOrder()
        {
            using (var f = new OrderCreateForm(_app, _user))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    LoadOrders();
                    LoadClients();
                    new TextViewForm("Акт приёма", _app.Reports.BuildAcceptanceAct(f.CreatedOrder),
                                     "akt_priema_" + f.CreatedOrder.Number + ".txt").ShowDialog(this);
                }
            }
        }

        private void OpenOrder()
        {
            var o = Ui.Selected<RepairOrder>(_orders);
            if (o == null) return;
            using (var f = new OrderDetailsForm(_app, _user, o.Id))
                f.ShowDialog(this);
            RefreshAll();
        }

        private void ExportCsv()
        {
            using (var dlg = new SaveFileDialog { FileName = "orders.csv", Filter = "CSV|*.csv" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var orders = _orders.Rows.Cast<DataGridViewRow>().Select(r => (RepairOrder)r.Tag).ToList();
                File.WriteAllText(dlg.FileName, _app.Reports.ToCsv(orders), new UTF8Encoding(true));
                Ui.Info("Выгружено заказов: " + orders.Count);
            }
        }

        // ================= Клиенты =================

        private void BuildClientsTab()
        {
            var page = AddTab("Клиенты");
            _clients = Ui.Grid(page, "ФИО", "Телефон", "E-mail", "Заказов", "Скидка");
            _clients.Dock = DockStyle.Fill;
            var bar = new Panel { Dock = DockStyle.Top, Height = 62 };
            page.Controls.Add(bar);
            Ui.Label(bar, "Поиск (ФИО, телефон, e-mail):", 6, 6);
            _clientSearch = Ui.Text(bar, 6, 26, 300);
            _clientSearch.TextChanged += (s, e) => LoadClients();
            Ui.Button(bar, "Добавить", 330, 24, 100, (s, e) => EditClient(null), 24);
            Ui.Button(bar, "Изменить", 438, 24, 100, (s, e) => EditClient(Ui.Selected<Client>(_clients)), 24);
            Ui.Button(bar, "Удалить", 546, 24, 100, (s, e) => DeleteClient(), 24);
        }

        private void LoadClients()
        {
            if (_clients == null) return;
            _clients.Rows.Clear();
            foreach (var c in _app.Clients.Search(_clientSearch.Text))
            {
                int discount = CostCalculator.LoyaltyDiscountPercent(_app.Clients.CompletedOrders(c.Id));
                Ui.AddRow(_clients, c, c.FullName, c.Phone, c.Email, _app.Clients.OrdersCount(c.Id),
                          discount > 0 ? discount + "%" : "—");
            }
        }

        private void EditClient(Client c)
        {
            var fields = new[] { new Field("ФИО клиента", c != null ? c.FullName : ""),
                                 new Field("Телефон", c != null ? c.Phone : ""),
                                 new Field("E-mail (необязательно)", c != null ? c.Email : "") };
            if (FieldsDialog.Show(c == null ? "Новый клиент" : "Клиент", fields, v => Ui.Guard(() =>
            {
                if (c == null) _app.Clients.Add(v[0], v[1], v[2]);
                else _app.Clients.Update(c.Id, v[0], v[1], v[2]);
            })))
                LoadClients();
        }

        private void DeleteClient()
        {
            var c = Ui.Selected<Client>(_clients);
            if (c != null && Ui.Confirm("Удалить клиента «" + c.FullName + "»?") &&
                Ui.Guard(() => _app.Clients.Delete(c.Id)))
                LoadClients();
        }

        // ================= Склад =================

        private void BuildStockTab()
        {
            var page = AddTab("Склад запчастей");
            _parts = Ui.Grid(page, "Артикул", "Наименование", "Остаток, шт.", "Цена, руб.");
            _parts.Dock = DockStyle.Fill;
            var bar = new Panel { Dock = DockStyle.Top, Height = 44 };
            page.Controls.Add(bar);
            var add = Ui.Button(bar, "Добавить", 6, 8, 100, (s, e) => EditPart(null), 26);
            var edit = Ui.Button(bar, "Изменить", 114, 8, 100, (s, e) => EditPart(Ui.Selected<Part>(_parts)), 26);
            var restock = Ui.Button(bar, "Пополнить", 222, 8, 100, (s, e) => Restock(), 26);
            var del = Ui.Button(bar, "Удалить", 330, 8, 100, (s, e) => DeletePart(), 26);
            Ui.Label(bar, "Красным выделены позиции с остатком менее " + CatalogService.LowStockThreshold + " шт.", 450, 14);
            add.Enabled = edit.Enabled = restock.Enabled = del.Enabled = IsAdmin;
        }

        private void LoadParts()
        {
            if (_parts == null) return;
            _parts.Rows.Clear();
            foreach (var p in _app.Catalog.Parts())
            {
                var row = Ui.AddRow(_parts, p, p.Article, p.Name, p.Quantity, p.Price.ToString("N2", Ru));
                if (p.Quantity < CatalogService.LowStockThreshold)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
            }
        }

        private void EditPart(Part p)
        {
            var fields = p == null
                ? new[] { new Field("Артикул"), new Field("Наименование"), new Field("Количество", "0"), new Field("Цена, руб.") }
                : new[] { new Field("Наименование", p.Name), new Field("Цена, руб.", p.Price.ToString(Ru)) };
            if (FieldsDialog.Show(p == null ? "Новая запчасть" : "Запчасть", fields, v => Ui.Guard(() =>
            {
                if (p == null)
                    _app.Catalog.AddPart(v[0], v[1], ParseInt(v[2]), ParseMoney(v[3]));
                else
                    _app.Catalog.UpdatePart(p.Id, v[0], ParseMoney(v[1]));
            })))
                LoadParts();
        }

        private void Restock()
        {
            var p = Ui.Selected<Part>(_parts);
            if (p == null) return;
            if (FieldsDialog.Show("Пополнение: " + p.Name, new[] { new Field("Количество, шт.", "1") },
                v => Ui.Guard(() => _app.Catalog.Restock(p.Id, ParseInt(v[0])))))
                LoadParts();
        }

        private void DeletePart()
        {
            var p = Ui.Selected<Part>(_parts);
            if (p != null && Ui.Confirm("Удалить запчасть «" + p.Name + "»?") && Ui.Guard(() => _app.Catalog.DeletePart(p.Id)))
                LoadParts();
        }

        // ================= Прайс-лист =================

        private void BuildPriceTab()
        {
            var page = AddTab("Прайс-лист");
            _services = Ui.Grid(page, "Вид работ", "Цена, руб.");
            _services.Dock = DockStyle.Fill;
            var bar = new Panel { Dock = DockStyle.Top, Height = 44 };
            page.Controls.Add(bar);
            var add = Ui.Button(bar, "Добавить", 6, 8, 100, (s, e) => EditService(null), 26);
            var edit = Ui.Button(bar, "Изменить", 114, 8, 100, (s, e) => EditService(Ui.Selected<ServiceItem>(_services)), 26);
            var del = Ui.Button(bar, "Удалить", 222, 8, 100, (s, e) => DeleteService(), 26);
            add.Enabled = edit.Enabled = del.Enabled = IsAdmin;
        }

        private void LoadServices()
        {
            if (_services == null) return;
            _services.Rows.Clear();
            foreach (var s in _app.Catalog.Services())
                Ui.AddRow(_services, s, s.Name, s.Price.ToString("N2", Ru));
        }

        private void EditService(ServiceItem item)
        {
            var fields = new[] { new Field("Наименование", item != null ? item.Name : ""),
                                 new Field("Цена, руб.", item != null ? item.Price.ToString(Ru) : "") };
            if (FieldsDialog.Show(item == null ? "Новая услуга" : "Услуга", fields, v => Ui.Guard(() =>
            {
                if (item == null) _app.Catalog.AddService(v[0], ParseMoney(v[1]));
                else _app.Catalog.UpdateService(item.Id, v[0], ParseMoney(v[1]));
            })))
                LoadServices();
        }

        private void DeleteService()
        {
            var s = Ui.Selected<ServiceItem>(_services);
            if (s != null && Ui.Confirm("Удалить услугу «" + s.Name + "»?") && Ui.Guard(() => _app.Catalog.DeleteService(s.Id)))
                LoadServices();
        }

        // ================= Отчёты =================

        private void BuildReportsTab()
        {
            var page = AddTab("Отчёты");
            _report = new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Font = new Font("Courier New", 10F), BackColor = Color.White
            };
            page.Controls.Add(_report);
            var bar = new Panel { Dock = DockStyle.Top, Height = 50 };
            page.Controls.Add(bar);
            Ui.Label(bar, "Период с", 6, 16);
            _from = new DateTimePicker { Left = 70, Top = 12, Width = 110, Format = DateTimePickerFormat.Short,
                                         Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            bar.Controls.Add(_from);
            Ui.Label(bar, "по", 190, 16);
            _to = new DateTimePicker { Left = 214, Top = 12, Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            bar.Controls.Add(_to);
            Ui.Button(bar, "Сформировать отчёт", 340, 10, 170, (s, e) => BuildReport(), 28);
            BuildReport();
        }

        private void BuildReport()
        {
            _report.Text = _app.Reports.BuildSummary(_from.Value, _to.Value).Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        // ================= Сотрудники =================

        private void BuildUsersTab()
        {
            var page = AddTab("Сотрудники");
            _users = Ui.Grid(page, "ФИО", "Логин", "Роль", "Активен");
            _users.Dock = DockStyle.Fill;
            var bar = new Panel { Dock = DockStyle.Top, Height = 44 };
            page.Controls.Add(bar);
            Ui.Button(bar, "Добавить сотрудника", 6, 8, 160, (s, e) => AddUser(), 26);
            Ui.Button(bar, "Вкл/выкл доступ", 174, 8, 140, (s, e) => ToggleUser(), 26);
            Ui.Button(bar, "Сменить свой пароль", 322, 8, 160, (s, e) => ChangeOwnPassword(), 26);
        }

        private void LoadUsers()
        {
            if (_users == null) return;
            _users.Rows.Clear();
            foreach (var u in _app.Db.Data.Users.OrderBy(x => x.FullName))
                Ui.AddRow(_users, u, u.FullName, u.Login, Localization.Name(u.Role), u.IsActive ? "да" : "нет");
        }

        private void AddUser()
        {
            var fields = new[] { new Field("ФИО"), new Field("Логин"),
                                 new Field("Пароль (не менее 6 символов)") { Password = true },
                                 new Field("Роль") { Options = new[] { "Мастер", "Администратор" }, Value = "Мастер" } };
            if (FieldsDialog.Show("Новый сотрудник", fields, v => Ui.Guard(() =>
                _app.Auth.CreateUser(v[1], v[0], v[2], v[3] == "Администратор" ? UserRole.Administrator : UserRole.Master))))
                LoadUsers();
        }

        private void ToggleUser()
        {
            var u = Ui.Selected<User>(_users);
            if (u != null && Ui.Guard(() => _app.Auth.SetActive(u.Id, !u.IsActive)))
                LoadUsers();
        }

        private void ChangeOwnPassword()
        {
            var fields = new[] { new Field("Текущий пароль") { Password = true },
                                 new Field("Новый пароль") { Password = true } };
            if (FieldsDialog.Show("Смена пароля", fields, v => Ui.Guard(() => _app.Auth.ChangePassword(_user.Id, v[0], v[1]))))
                Ui.Info("Пароль изменён.");
        }

        // ================= Разбор чисел =================

        private static int ParseInt(string text)
        {
            int n;
            if (!int.TryParse(text.Trim(), out n))
                throw new BusinessException("Количество должно быть целым числом.");
            return n;
        }

        private static decimal ParseMoney(string text)
        {
            decimal d;
            if (!decimal.TryParse(text.Trim().Replace('.', ','), NumberStyles.Number, Ru, out d))
                throw new BusinessException("Цена указана неверно.");
            return d;
        }
    }
}
