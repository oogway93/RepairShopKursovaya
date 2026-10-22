using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RepairShop.Core.Infrastructure;
using RepairShop.Core.Logic;
using RepairShop.Core.Models;
using RepairShop.Core.Storage;

namespace RepairShop.Core.Services
{
    /// <summary>Аутентификация и управление сотрудниками.</summary>
    public class AuthService
    {
        private readonly ShopDatabase _db;

        public AuthService(ShopDatabase db)
        {
            _db = db;
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
                return Convert.ToBase64String(bytes);
            }
        }

        private static string NewSalt()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        /// <summary>Возвращает сотрудника при верных логине и пароле, иначе null.</summary>
        public User Login(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || password == null)
                return null;
            var user = _db.Data.Users.FirstOrDefault(u =>
                string.Equals(u.Login, login.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == null || !user.IsActive)
                return null;
            return HashPassword(password, user.Salt) == user.PasswordHash ? user : null;
        }

        public User CreateUser(string login, string fullName, string password, UserRole role)
        {
            login = Validation.RequireText(login, "Логин");
            fullName = Validation.RequireText(fullName, "ФИО");
            if (password == null || password.Length < 6)
                throw new BusinessException("Пароль должен содержать не менее 6 символов.");
            if (_db.Data.Users.Any(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessException("Сотрудник с таким логином уже существует.");

            string salt = NewSalt();
            var user = new User
            {
                Id = ShopDatabase.NextId(_db.Data.Users.Select(u => u.Id)),
                Login = login,
                FullName = fullName,
                Salt = salt,
                PasswordHash = HashPassword(password, salt),
                Role = role,
                IsActive = true
            };
            _db.Data.Users.Add(user);
            _db.Save();
            return user;
        }

        public void ChangePassword(int userId, string oldPassword, string newPassword)
        {
            var user = _db.Data.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                throw new BusinessException("Сотрудник не найден.");
            if (HashPassword(oldPassword ?? string.Empty, user.Salt) != user.PasswordHash)
                throw new BusinessException("Текущий пароль указан неверно.");
            if (newPassword == null || newPassword.Length < 6)
                throw new BusinessException("Новый пароль должен содержать не менее 6 символов.");
            user.Salt = NewSalt();
            user.PasswordHash = HashPassword(newPassword, user.Salt);
            _db.Save();
        }

        public void SetActive(int userId, bool active)
        {
            var user = _db.Data.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                throw new BusinessException("Сотрудник не найден.");
            if (!active && user.Role == UserRole.Administrator &&
                _db.Data.Users.Count(u => u.Role == UserRole.Administrator && u.IsActive) <= 1)
                throw new BusinessException("Нельзя отключить последнего администратора.");
            user.IsActive = active;
            _db.Save();
        }

        public System.Collections.Generic.List<User> Masters()
        {
            return _db.Data.Users
                .Where(u => u.Role == UserRole.Master && u.IsActive)
                .OrderBy(u => u.FullName).ToList();
        }
    }
}
