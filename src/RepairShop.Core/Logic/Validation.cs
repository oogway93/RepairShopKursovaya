using System.Linq;
using System.Text.RegularExpressions;
using RepairShop.Core.Infrastructure;

namespace RepairShop.Core.Logic
{
    /// <summary>Проверка и нормализация вводимых данных.</summary>
    public static class Validation
    {
        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        /// <summary>Приводит телефон к виду +7XXXXXXXXXX или выбрасывает исключение.</summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new BusinessException("Укажите номер телефона клиента.");

            string digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && (digits[0] == '7' || digits[0] == '8'))
                return "+7" + digits.Substring(1);
            if (digits.Length == 10)
                return "+7" + digits;

            throw new BusinessException("Некорректный номер телефона. Ожидается 10 цифр после кода страны.");
        }

        /// <summary>Проверяет e-mail (поле необязательное).</summary>
        public static string NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;
            email = email.Trim();
            if (!EmailRegex.IsMatch(email))
                throw new BusinessException("Некорректный адрес электронной почты.");
            return email;
        }

        public static string RequireText(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new BusinessException("Поле «" + fieldName + "» не заполнено.");
            return value.Trim();
        }

        public static void RequireNonNegative(decimal value, string fieldName)
        {
            if (value < 0)
                throw new BusinessException("Поле «" + fieldName + "» не может быть отрицательным.");
        }

        public static void RequirePositive(int value, string fieldName)
        {
            if (value <= 0)
                throw new BusinessException("Поле «" + fieldName + "» должно быть больше нуля.");
        }
    }
}
