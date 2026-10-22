using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RepairShop.App
{
    /// <summary>Описание поля универсального диалога ввода.</summary>
    public class Field
    {
        public string Label;
        public string Value = "";
        public bool Password;
        public string[] Options;   // если задано – выпадающий список
        public bool Multiline;

        public Field(string label, string value = "")
        {
            Label = label;
            Value = value ?? "";
        }
    }

    /// <summary>Универсальный диалог ввода нескольких значений.</summary>
    public static class FieldsDialog
    {
        /// <summary>
        /// Показывает диалог. Проверка введённых данных выполняется в onOk:
        /// если делегат вернул false – диалог остаётся открытым.
        /// </summary>
        public static bool Show(string title, Field[] fields, Func<string[], bool> onOk)
        {
            int height = 90;
            foreach (var f in fields) height += f.Multiline ? 100 : 52;
            var dlg = Ui.NewDialog(title, 400, height + 36);
            var inputs = new Control[fields.Length];
            int y = 14;
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                Ui.Label(dlg, f.Label, 16, y);
                if (f.Options != null)
                {
                    var combo = Ui.Combo(dlg, 16, y + 20, 352);
                    foreach (var o in f.Options) combo.Items.Add(o);
                    combo.SelectedItem = f.Value;
                    if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0;
                    inputs[i] = combo;
                    y += 52;
                }
                else
                {
                    var tb = Ui.Text(dlg, 16, y + 20, 352, f.Value);
                    tb.UseSystemPasswordChar = f.Password;
                    if (f.Multiline)
                    {
                        tb.Multiline = true;
                        tb.Height = 74;
                        y += 100;
                    }
                    else y += 52;
                    inputs[i] = tb;
                }
            }
            bool accepted = false;
            var ok = Ui.Button(dlg, "OK", 190, y + 4, 85, (s, e) =>
            {
                var values = new string[inputs.Length];
                for (int i = 0; i < inputs.Length; i++) values[i] = inputs[i].Text;
                if (onOk(values))
                {
                    accepted = true;
                    dlg.DialogResult = DialogResult.OK;
                }
            });
            Ui.Button(dlg, "Отмена", 283, y + 4, 85, (s, e) => dlg.DialogResult = DialogResult.Cancel);
            dlg.AcceptButton = ok;
            dlg.ShowDialog();
            return accepted;
        }
    }

    /// <summary>Окно просмотра текстового документа (акт, отчёт).</summary>
    public class TextViewForm : Form
    {
        public TextViewForm(string title, string text, string defaultFileName)
        {
            Text = title;
            Font = Ui.BaseFont;
            ClientSize = new Size(640, 520);
            StartPosition = FormStartPosition.CenterParent;
            var box = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Font = new Font("Courier New", 10F), Dock = DockStyle.Fill,
                Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n")
            };
            var panel = new Panel { Dock = DockStyle.Bottom, Height = 44 };
            Controls.Add(box);
            Controls.Add(panel);
            Ui.Button(panel, "Сохранить в файл…", 10, 8, 150, (s, e) =>
            {
                using (var dlg = new SaveFileDialog { FileName = defaultFileName, Filter = "Текстовый файл|*.txt" })
                    if (dlg.ShowDialog() == DialogResult.OK)
                        System.IO.File.WriteAllText(dlg.FileName, text, System.Text.Encoding.UTF8);
            });
            Ui.Button(panel, "Закрыть", 530, 8, 100, (s, e) => Close());
        }
    }
}
