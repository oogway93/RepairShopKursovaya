using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using RepairShop.Core;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;

namespace RepairShop.App
{
    /// <summary>Карточка заказа: статусы, работы, запчасти, стоимость, история.</summary>
    public class OrderDetailsForm : Form
    {
        private static readonly CultureInfo Ru = new CultureInfo("ru-RU");

        private readonly ShopApp _app;
        private readonly User _user;
        private readonly int _orderId;
        private readonly Label _title, _client, _device, _defect, _dates, _status, _warranty, _cost;
        private readonly ComboBox _next, _master, _service, _part;
        private readonly TextBox _comment;
        private readonly NumericUpDown _serviceQty, _partQty;
        private readonly DataGridView _works, _parts, _history;
        private readonly Button _changeStatus, _assign, _addWork, _addCustom, _delWork, _addPart, _delPart;

        public OrderDetailsForm(ShopApp app, User user, int orderId)
        {
            _app = app;
            _user = user;
            _orderId = orderId;
            Font = Ui.BaseFont;
            ClientSize = new Size(930, 690);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _title = Ui.Label(this, "", 12, 10, 600, Ui.TitleFont);
            _warranty = Ui.Label(this, "", 640, 14, 280);
            _warranty.ForeColor = Color.DarkGreen;
            _client = Ui.Label(this, "", 14, 46, 440);
            _device = Ui.Label(this, "", 14, 68, 440);
            _defect = Ui.Label(this, "", 14, 90, 900);
            _dates = Ui.Label(this, "", 470, 46, 440);

            // --- Статус и мастер ---
            var gStatus = new GroupBox { Text = "Статус и мастер", Left = 10, Top = 118, Width = 910, Height = 92 };
            Controls.Add(gStatus);
            Ui.Label(gStatus, "Текущий статус:", 12, 24);
            _status = Ui.Label(gStatus, "", 118, 24, 200, Ui.BoldFont);
            Ui.Label(gStatus, "Новый статус", 330, 24);
            _next = Ui.Combo(gStatus, 330, 44, 170);
            Ui.Label(gStatus, "Комментарий", 510, 24);
            _comment = Ui.Text(gStatus, 510, 44, 220);
            _changeStatus = Ui.Button(gStatus, "Сменить статус", 740, 41, 156, (s, e) => ChangeStatus());
            Ui.Label(gStatus, "Мастер", 12, 44);
            _master = Ui.Combo(gStatus, 12, 66 - 4, 150);
            _master.Top = 64;
            _assign = Ui.Button(gStatus, "Назначить", 170, 62, 100, (s, e) => AssignMaster(), 24);

            // --- Работы ---
            var gWorks = new GroupBox { Text = "Выполненные работы", Left = 10, Top = 216, Width = 450, Height = 240 };
            Controls.Add(gWorks);
            _works = Ui.Grid(gWorks, "Работа", "Цена", "Кол.", "Сумма");
            _works.SetBounds(8, 20, 434, 120);
            _service = Ui.Combo(gWorks, 8, 148, 300);
            _serviceQty = new NumericUpDown { Left = 316, Top = 148, Width = 50, Minimum = 1, Maximum = 99, Value = 1 };
            gWorks.Controls.Add(_serviceQty);
            _addWork = Ui.Button(gWorks, "Добавить", 374, 146, 68, (s, e) => AddWork(), 26);
            _addCustom = Ui.Button(gWorks, "Своя работа…", 8, 182, 130, (s, e) => AddCustomWork());
            _delWork = Ui.Button(gWorks, "Удалить выбранную", 146, 182, 150, (s, e) => RemoveWork());

            // --- Запчасти ---
            var gParts = new GroupBox { Text = "Использованные запчасти", Left = 470, Top = 216, Width = 450, Height = 240 };
            Controls.Add(gParts);
            _parts = Ui.Grid(gParts, "Запчасть", "Цена", "Кол.", "Сумма");
            _parts.SetBounds(8, 20, 434, 120);
            _part = Ui.Combo(gParts, 8, 148, 300);
            _partQty = new NumericUpDown { Left = 316, Top = 148, Width = 50, Minimum = 1, Maximum = 99, Value = 1 };
            gParts.Controls.Add(_partQty);
            _addPart = Ui.Button(gParts, "Списать", 374, 146, 68, (s, e) => AddPart(), 26);
            _delPart = Ui.Button(gParts, "Вернуть на склад", 8, 182, 150, (s, e) => RemovePart());

            // --- Стоимость и история ---
            var gCost = new GroupBox { Text = "Стоимость", Left = 10, Top = 462, Width = 450, Height = 170 };
            Controls.Add(gCost);
            _cost = Ui.Label(gCost, "", 12, 24, 426);
            _cost.Height = 136;
            var gHist = new GroupBox { Text = "История статусов", Left = 470, Top = 462, Width = 450, Height = 170 };
            Controls.Add(gHist);
            _history = Ui.Grid(gHist, "Дата", "Статус", "Комментарий");
            _history.SetBounds(8, 20, 434, 142);

            Ui.Button(this, "Акт приёма", 10, 646, 130, (s, e) => ShowAct(true), 32);
            Ui.Button(this, "Акт выполненных работ", 148, 646, 190, (s, e) => ShowAct(false), 32);
            Ui.Button(this, "Закрыть", 820, 646, 100, (s, e) => Close(), 32);

            foreach (var sv in _app.Catalog.Services())
                _service.Items.Add(new Item<int>(sv.Name + " — " + sv.Price.ToString("N0", Ru) + " р.", sv.Id));
            if (_service.Items.Count > 0) _service.SelectedIndex = 0;
            Reload();
        }

        private bool IsAdmin { get { return _user.Role == UserRole.Administrator; } }

        private void Reload()
        {
            var o = _app.Orders.Get(_orderId);
            var client = _app.Orders.ClientOf(o);
            var device = _app.Orders.DeviceOf(o);
            Text = "Заказ " + o.Number;
            _title.Text = "Заказ № " + o.Number;
            _warranty.Text = o.IsWarrantyCase ? "ГАРАНТИЙНЫЙ СЛУЧАЙ — ремонт бесплатный" : "";
            _client.Text = "Клиент: " + client.FullName + ", тел. " + client.Phone;
            _device.Text = "Устройство: " + Localization.Name(device.Type) + " " + device +
                           (string.IsNullOrEmpty(device.SerialNumber) ? "" : ", S/N " + device.SerialNumber);
            _defect.Text = "Неисправность: " + o.Defect;
            string due = "Принят: " + o.CreatedAt.ToString("dd.MM.yyyy") + "    Срок: " + o.DueDate.ToString("dd.MM.yyyy");
            if (_app.Orders.IsOverdue(o)) due += "  (ПРОСРОЧЕН)";
            var wend = _app.Orders.WarrantyEnd(o);
            if (wend.HasValue) due += "    Гарантия до: " + wend.Value.ToString("dd.MM.yyyy");
            _dates.Text = due;
            _status.Text = Localization.Name(o.Status);

            // доступные переходы
            _next.Items.Clear();
            foreach (var st in OrderWorkflow.NextStatuses(o.Status))
            {
                bool adminOnly = st == OrderStatus.Issued || st == OrderStatus.Cancelled;
                if (adminOnly && !IsAdmin) continue;
                _next.Items.Add(new Item<OrderStatus>(Localization.Name(st), st));
            }
            if (_next.Items.Count > 0) _next.SelectedIndex = 0;
            _changeStatus.Enabled = _next.Items.Count > 0;

            // мастер
            _master.Items.Clear();
            foreach (var m in _app.Auth.Masters())
            {
                int i = _master.Items.Add(new Item<int>(m.FullName, m.Id));
                if (m.Id == o.MasterId) _master.SelectedIndex = i;
            }
            _master.Enabled = _assign.Enabled = IsAdmin && !OrderWorkflow.IsFinal(o.Status);

            // запчасти для списания
            _part.Items.Clear();
            foreach (var p in _app.Catalog.Parts())
                _part.Items.Add(new Item<int>(p.Name + " (ост. " + p.Quantity + ")", p.Id));
            if (_part.Items.Count > 0) _part.SelectedIndex = 0;

            bool editable = OrderWorkflow.IsEditable(o.Status);
            _addWork.Enabled = _addCustom.Enabled = _delWork.Enabled = editable;
            _addPart.Enabled = _delPart.Enabled = editable;

            _works.Rows.Clear();
            for (int i = 0; i < o.Works.Count; i++)
                Ui.AddRow(_works, i, o.Works[i].Name, o.Works[i].Price.ToString("N2", Ru),
                          o.Works[i].Quantity, o.Works[i].Total.ToString("N2", Ru));
            _parts.Rows.Clear();
            for (int i = 0; i < o.Parts.Count; i++)
                Ui.AddRow(_parts, i, o.Parts[i].Name, o.Parts[i].Price.ToString("N2", Ru),
                          o.Parts[i].Quantity, o.Parts[i].Total.ToString("N2", Ru));
            _history.Rows.Clear();
            foreach (var h in o.History)
                Ui.AddRow(_history, h, h.Date.ToString("dd.MM.yy HH:mm"), Localization.Name(h.Status), h.Comment);

            var c = _app.Orders.CalculateCost(o);
            string text = "Работы: " + c.WorksTotal.ToString("N2", Ru) + " руб.\r\n" +
                          "Запчасти: " + c.PartsTotal.ToString("N2", Ru) + " руб.\r\n";
            if (c.DiscountPercent > 0)
                text += "Скидка постоянного клиента " + c.DiscountPercent + "%: -" + c.DiscountAmount.ToString("N2", Ru) + " руб.\r\n";
            if (c.DiagnosticsFee > 0)
                text += "Оплата диагностики: " + c.DiagnosticsFee.ToString("N2", Ru) + " руб.\r\n";
            if (c.IsWarranty)
                text += "Гарантийный ремонт: оплата не требуется.\r\n";
            text += "\r\nИТОГО К ОПЛАТЕ: " + c.Total.ToString("N2", Ru) + " руб.";
            _cost.Text = text;
        }

        private void ChangeStatus()
        {
            var item = _next.SelectedItem as Item<OrderStatus>;
            if (item == null) return;
            if (item.Value == OrderStatus.Cancelled && !Ui.Confirm("Отменить заказ? Запчасти будут возвращены на склад."))
                return;
            if (Ui.Guard(() => _app.Orders.ChangeStatus(_orderId, item.Value, _comment.Text, _user.Id)))
            {
                _comment.Clear();
                Reload();
                if (item.Value == OrderStatus.Issued)
                    ShowAct(false);
            }
        }

        private void AssignMaster()
        {
            var item = _master.SelectedItem as Item<int>;
            if (item != null && Ui.Guard(() => _app.Orders.AssignMaster(_orderId, item.Value, _user.Id)))
                Ui.Info("Мастер назначен.");
        }

        private void AddWork()
        {
            var item = _service.SelectedItem as Item<int>;
            if (item != null && Ui.Guard(() => _app.Orders.AddWork(_orderId, item.Value, (int)_serviceQty.Value)))
                Reload();
        }

        private void AddCustomWork()
        {
            var fields = new[] { new Field("Наименование работы"), new Field("Цена, руб.") };
            if (FieldsDialog.Show("Своя работа", fields, v => Ui.Guard(() =>
            {
                decimal price;
                if (!decimal.TryParse(v[1].Replace('.', ','), NumberStyles.Number, Ru, out price))
                    throw new RepairShop.Core.Infrastructure.BusinessException("Цена указана неверно.");
                _app.Orders.AddCustomWork(_orderId, v[0], price);
            })))
                Reload();
        }

        private void RemoveWork()
        {
            if (_works.SelectedRows.Count == 0) return;
            if (Ui.Guard(() => _app.Orders.RemoveWork(_orderId, (int)_works.SelectedRows[0].Tag)))
                Reload();
        }

        private void AddPart()
        {
            var item = _part.SelectedItem as Item<int>;
            if (item != null && Ui.Guard(() => _app.Orders.AddPart(_orderId, item.Value, (int)_partQty.Value)))
                Reload();
        }

        private void RemovePart()
        {
            if (_parts.SelectedRows.Count == 0) return;
            if (Ui.Guard(() => _app.Orders.RemovePart(_orderId, (int)_parts.SelectedRows[0].Tag)))
                Reload();
        }

        private void ShowAct(bool acceptance)
        {
            var o = _app.Orders.Get(_orderId);
            string text = acceptance
                ? _app.Reports.BuildAcceptanceAct(o)
                : _app.Reports.BuildCompletionAct(o, _app.Orders.CalculateCost(o));
            new TextViewForm(acceptance ? "Акт приёма" : "Акт выполненных работ", text,
                             (acceptance ? "akt_priema_" : "akt_rabot_") + o.Number + ".txt").ShowDialog(this);
        }
    }
}
