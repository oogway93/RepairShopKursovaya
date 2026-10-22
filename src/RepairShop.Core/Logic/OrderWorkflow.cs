using System.Collections.Generic;
using System.Linq;
using RepairShop.Core.Models;

namespace RepairShop.Core.Logic
{
    /// <summary>Правила жизненного цикла заказа (конечный автомат статусов).</summary>
    public static class OrderWorkflow
    {
        private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions =
            new Dictionary<OrderStatus, OrderStatus[]>
            {
                { OrderStatus.Accepted,     new[] { OrderStatus.Diagnostics, OrderStatus.Cancelled } },
                { OrderStatus.Diagnostics,  new[] { OrderStatus.WaitingParts, OrderStatus.InRepair, OrderStatus.Cancelled } },
                { OrderStatus.WaitingParts, new[] { OrderStatus.InRepair, OrderStatus.Cancelled } },
                { OrderStatus.InRepair,     new[] { OrderStatus.WaitingParts, OrderStatus.Ready, OrderStatus.Cancelled } },
                { OrderStatus.Ready,        new[] { OrderStatus.Issued, OrderStatus.InRepair } },
                { OrderStatus.Issued,       new OrderStatus[0] },
                { OrderStatus.Cancelled,    new OrderStatus[0] }
            };

        public static IEnumerable<OrderStatus> NextStatuses(OrderStatus from)
        {
            return Transitions[from];
        }

        public static bool CanTransit(OrderStatus from, OrderStatus to)
        {
            return Transitions[from].Contains(to);
        }

        public static bool IsFinal(OrderStatus status)
        {
            return status == OrderStatus.Issued || status == OrderStatus.Cancelled;
        }

        /// <summary>Работы и запчасти можно менять только пока идёт диагностика или ремонт.</summary>
        public static bool IsEditable(OrderStatus status)
        {
            return status == OrderStatus.Diagnostics
                || status == OrderStatus.WaitingParts
                || status == OrderStatus.InRepair;
        }
    }
}
