using System;
using System.Drawing;
using System.Windows.Forms;
using RepairShop.Core;
using RepairShop.Core.Models;

namespace RepairShop.App
{
    /// <summary>Окно входа в систему.</summary>
    public class LoginForm : Form
    {
        private readonly ShopApp _app;
        private readonly TextBox _login, _password;
        private readonly Label _error;

        public User LoggedUser { get; private set; }

        public LoginForm(ShopApp app)
        {
            _app = app;
            Text = "Ремонтная мастерская — вход";
            Font = Ui.BaseFont;
            ClientSize = new Size(360, 270);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Ui.Label(this, "Ремонтная мастерская", 20, 18, 320, Ui.TitleFont);
            Ui.Label(this, "электронного оборудования", 22, 46, 320);
            Ui.Label(this, "Логин", 22, 85);
            _login = Ui.Text(this, 22, 106, 316);
            Ui.Label(this, "Пароль", 22, 138);
            _password = Ui.Text(this, 22, 159, 316);
            _password.UseSystemPasswordChar = true;
            _error = Ui.Label(this, "", 22, 190, 316);
            _error.ForeColor = Color.Firebrick;
            var ok = Ui.Button(this, "Войти", 22, 215, 150, (s, e) => DoLogin(), 32);
            Ui.Button(this, "Выход", 188, 215, 150, (s, e) => DialogResult = DialogResult.Cancel, 32);
            AcceptButton = ok;
        }

        private void DoLogin()
        {
            var user = _app.Auth.Login(_login.Text, _password.Text);
            if (user == null)
            {
                _error.Text = "Неверный логин или пароль";
                _password.Clear();
                _password.Focus();
                return;
            }
            LoggedUser = user;
            DialogResult = DialogResult.OK;
        }
    }
}
