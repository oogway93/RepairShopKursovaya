using System;
using System.Drawing;
using System.Windows.Forms;
using RepairShop.Core.Infrastructure;

namespace RepairShop.App
{
    /// <summary>Элемент выпадающего списка: текст + значение.</summary>
    public class Item<T>
    {
        public string Text { get; private set; }
        public T Value { get; private set; }

        public Item(string text, T value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>Вспомогательные методы создания элементов управления.</summary>
    public static class Ui
    {
        public static readonly Font BaseFont = new Font("Segoe UI", 9F);
        public static readonly Font BoldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI", 13F, FontStyle.Bold);

        public static Label Label(Control parent, string text, int x, int y, int width = 0, Font font = null)
        {
            var l = new Label { Text = text, Left = x, Top = y, AutoSize = width == 0 };
            if (width > 0) l.Width = width;
            if (font != null) l.Font = font;
            parent.Controls.Add(l);
            return l;
        }

        public static TextBox Text(Control parent, int x, int y, int width, string text = "")
        {
            var t = new TextBox { Left = x, Top = y, Width = width, Text = text };
            parent.Controls.Add(t);
            return t;
        }

        public static Button Button(Control parent, string text, int x, int y, int width, EventHandler click, int height = 28)
        {
            var b = new Button { Text = text, Left = x, Top = y, Width = width, Height = height, UseVisualStyleBackColor = true };
            if (click != null) b.Click += click;
            parent.Controls.Add(b);
            return b;
        }

        public static ComboBox Combo(Control parent, int x, int y, int width)
        {
            var c = new ComboBox { Left = x, Top = y, Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
            parent.Controls.Add(c);
            return c;
        }

        public static DataGridView Grid(Control parent, params string[] headers)
        {
            var g = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 26
            };
            for (int i = 0; i < headers.Length; i++)
                g.Columns.Add("c" + i, headers[i]);
            parent.Controls.Add(g);
            return g;
        }

        /// <summary>Добавляет строку в таблицу и запоминает связанный объект.</summary>
        public static DataGridViewRow AddRow(DataGridView grid, object tag, params object[] cells)
        {
            int idx = grid.Rows.Add(cells);
            grid.Rows[idx].Tag = tag;
            return grid.Rows[idx];
        }

        public static T Selected<T>(DataGridView grid) where T : class
        {
            if (grid.SelectedRows.Count == 0) return null;
            return grid.SelectedRows[0].Tag as T;
        }

        public static void Error(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Info(string message)
        {
            MessageBox.Show(message, "Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(message, "Подтверждение", MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>Выполняет действие; бизнес-ошибки показывает пользователю.</summary>
        public static bool Guard(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (BusinessException ex)
            {
                Error(ex.Message);
                return false;
            }
        }

        public static Form NewDialog(string title, int width, int height)
        {
            return new Form
            {
                Text = title,
                Width = width,
                Height = height,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = BaseFont
            };
        }
    }
}
