using System;
using System.Collections.Generic;

namespace RepairShop.Core.Models
{
    /// <summary>Сотрудник мастерской (администратор или мастер).</summary>
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string FullName { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Клиент мастерской.</summary>
    public class Client
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    /// <summary>Устройство клиента, принятое в ремонт.</summary>
    public class Device
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public DeviceType Type { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }

        public override string ToString()
        {
            return (Brand + " " + Model).Trim();
        }
    }

    /// <summary>Позиция прайс-листа (вид работ).</summary>
    public class ServiceItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    /// <summary>Запчасть на складе.</summary>
    public class Part
    {
        public int Id { get; set; }
        public string Article { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    /// <summary>Выполненная работа в составе заказа.</summary>
    public class OrderWork
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public decimal Total
        {
            get { return Price * Quantity; }
        }
    }

    /// <summary>Использованная запчасть в составе заказа.</summary>
    public class OrderPart
    {
        public int PartId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public decimal Total
        {
            get { return Price * Quantity; }
        }
    }

    /// <summary>Запись в истории изменения статусов заказа.</summary>
    public class StatusChange
    {
        public DateTime Date { get; set; }
        public OrderStatus Status { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; }
    }

    /// <summary>Заказ на ремонт.</summary>
    public class RepairOrder
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public int ClientId { get; set; }
        public int DeviceId { get; set; }
        public string Defect { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ClosedAt { get; set; }
        public OrderStatus Status { get; set; }
        public int MasterId { get; set; }
        public bool IsWarrantyCase { get; set; }
        public decimal FinalCost { get; set; }
        public List<OrderWork> Works { get; set; } = new List<OrderWork>();
        public List<OrderPart> Parts { get; set; } = new List<OrderPart>();
        public List<StatusChange> History { get; set; } = new List<StatusChange>();
    }

    /// <summary>Корневой объект хранилища: все таблицы данных.</summary>
    public class ShopData
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Client> Clients { get; set; } = new List<Client>();
        public List<Device> Devices { get; set; } = new List<Device>();
        public List<ServiceItem> Services { get; set; } = new List<ServiceItem>();
        public List<Part> Parts { get; set; } = new List<Part>();
        public List<RepairOrder> Orders { get; set; } = new List<RepairOrder>();
    }
}
