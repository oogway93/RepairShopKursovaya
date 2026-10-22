using RepairShop.Core.Models;

namespace RepairShop.Core.Logic
{
    /// <summary>Русские названия перечислений для интерфейса и отчётов.</summary>
    public static class Localization
    {
        public static string Name(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Accepted: return "Принят";
                case OrderStatus.Diagnostics: return "Диагностика";
                case OrderStatus.WaitingParts: return "Ожидание запчастей";
                case OrderStatus.InRepair: return "В ремонте";
                case OrderStatus.Ready: return "Готов к выдаче";
                case OrderStatus.Issued: return "Выдан";
                case OrderStatus.Cancelled: return "Отменён";
                default: return status.ToString();
            }
        }

        public static string Name(DeviceType type)
        {
            switch (type)
            {
                case DeviceType.Smartphone: return "Смартфон";
                case DeviceType.Laptop: return "Ноутбук";
                case DeviceType.Tablet: return "Планшет";
                case DeviceType.Tv: return "Телевизор";
                case DeviceType.GameConsole: return "Игровая консоль";
                case DeviceType.Other: return "Другое";
                default: return type.ToString();
            }
        }

        public static string Name(UserRole role)
        {
            return role == UserRole.Administrator ? "Администратор" : "Мастер";
        }
    }
}
