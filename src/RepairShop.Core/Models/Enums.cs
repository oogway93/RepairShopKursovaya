namespace RepairShop.Core.Models
{
    /// <summary>Роль сотрудника в системе.</summary>
    public enum UserRole
    {
        Administrator = 0,
        Master = 1
    }

    /// <summary>Статус заказа на ремонт.</summary>
    public enum OrderStatus
    {
        Accepted = 0,      // Принят
        Diagnostics = 1,   // Диагностика
        WaitingParts = 2,  // Ожидание запчастей
        InRepair = 3,      // В ремонте
        Ready = 4,         // Готов к выдаче
        Issued = 5,        // Выдан клиенту
        Cancelled = 6      // Отменён
    }

    /// <summary>Тип устройства.</summary>
    public enum DeviceType
    {
        Smartphone = 0,
        Laptop = 1,
        Tablet = 2,
        Tv = 3,
        GameConsole = 4,
        Other = 5
    }
}
