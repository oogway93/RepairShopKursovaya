using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RepairShop.Core;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;

namespace RepairShop.App
{
    /// <summary>Оформление приёма устройства в ремонт.</summary>
    public class OrderCreateForm : Form
    {
        private readonly ShopApp _app;
        private readonly User _user;
        private readonly ComboBox _client, _type, _master;
        private readonly TextBox _brand, _model, _serial, _defect;
        private readonly DateTimePicker _due;
        private readonly Label _warranty;

        public RepairOrder CreatedOrder { get; private set; }

        public OrderCreateForm(ShopApp app, User user)
        {
            _app = app;
            _user = user;
            Text = "Новый заказ на ремонт";
            Font = Ui.BaseFont;
            ClientSize = new Size(500, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Ui.Label(this, "Клиент", 16, 14);
            _client = Ui.Combo(this, 16, 34, 350);
            Ui.Button(this, "Новый клиент…", 376, 32, 108, (s, e) => NewClient());

            Ui.Label(this, "Тип устройства", 16, 72);
            _type = Ui.Combo(this, 16, 92, 220);
            foreach (DeviceType t in Enum.GetValues(typeof(DeviceType)))
                _type.Items.Add(new Item<DeviceType>(Localization.Name(t), t));
            _type.SelectedIndex = 0;

            Ui.Label(this, "Производитель", 16, 126);
            _brand = Ui.Text(this, 16, 146, 220);
            Ui.Label(this, "Модель", 256, 126);
            _model = Ui.Text(this, 256, 146, 228);

            Ui.Label(this, "Серийный номер / IMEI", 16, 180);
            _serial = Ui.Text(this, 16, 200, 220);
            _serial.TextChanged += (s, e) => CheckWarranty();
            _warranty = Ui.Label(this, "", 256, 203, 230);
            _warranty.ForeColor = Color.DarkGreen;

            Ui.Label(this, "Описание неисправности", 16, 236);
            _defect = Ui.Text(this, 16, 256, 468);
            _defect.Multiline = true;
            _defect.Height = 80;

            Ui.Label(this, "Мастер", 16, 350);
            _master = Ui.Combo(this, 16, 370, 220);
            Ui.Label(this, "Срок готовности", 256, 350);
            _due = new DateTimePicker { Left = 256, Top = 370, Width = 228, Format = DateTimePickerFormat.Short,
                                        Value = DateTime.Today.AddDays(3) };
            Controls.Add(_due);

            var ok = Ui.Button(this, "Принять в ремонт", 220, 450, 150, (s, e) => Create(), 34);
            Ui.Button(this, "Отмена", 384, 450, 100, (s, e) => DialogResult = DialogResult.Cancel, 34);
            AcceptButton = null;
            FillClients(0);
            _master.Items.Add(new Item<int>("(не назначен)", 0));
            foreach (var m in _app.Auth.Masters())
                _master.Items.Add(new Item<int>(m.FullName, m.Id));
            _master.SelectedIndex = 0;
        }

        private void FillClients(int selectId)
        {
            _client.Items.Clear();
            foreach (var c in _app.Clients.Search(null))
            {
                int i = _client.Items.Add(new Item<int>(c.FullName + "  " + c.Phone, c.Id));
                if (c.Id == selectId) _client.SelectedIndex = i;
            }
            if (_client.SelectedIndex < 0 && _client.Items.Count > 0) _client.SelectedIndex = 0;
        }

        private void NewClient()
        {
            Client created = null;
            var fields = new[] { new Field("ФИО клиента"), new Field("Телефон"), new Field("E-mail (необязательно)") };
            FieldsDialog.Show("Новый клиент", fields, v =>
                Ui.Guard(() => created = _app.Clients.Add(v[0], v[1], v[2])));
            if (created != null) FillClients(created.Id);
        }

        private void CheckWarranty()
        {
            var basis = _app.Orders.FindWarrantyBase(_serial.Text, _app.Db.Clock.Now);
            _warranty.Text = basis != null ? "Гарантийный случай (заказ " + basis.Number + ")" : "";
        }

        private void Create()
        {
            var client = _client.SelectedItem as Item<int>;
            if (client == null) { Ui.Error("Выберите клиента или создайте нового."); return; }
            var device = new Device
            {
                Type = ((Item<DeviceType>)_type.SelectedItem).Value,
                Brand = _brand.Text,
                Model = _model.Text,
                SerialNumber = _serial.Text
            };
            int masterId = ((Item<int>)_master.SelectedItem).Value;
            RepairOrder order = null;
            if (Ui.Guard(() => order = _app.Orders.CreateOrder(client.Value, device, _defect.Text,
                                                               masterId, _due.Value, _user.Id)))
            {
                CreatedOrder = order;
                DialogResult = DialogResult.OK;
            }
        }
    }
}
