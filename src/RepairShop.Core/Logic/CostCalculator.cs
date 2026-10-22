using System;
using System.Linq;
using RepairShop.Core.Models;

namespace RepairShop.Core.Logic
{
    /// <summary>Расшифровка стоимости заказа.</summary>
    public class CostBreakdown
    {
        public decimal WorksTotal { get; set; }
        public decimal PartsTotal { get; set; }
        public int DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiagnosticsFee { get; set; }
        public bool IsWarranty { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>Расчёт стоимости ремонта с учётом скидок, гарантии и отмены.</summary>
    public static class CostCalculator
    {
        /// <summary>Стоимость диагностики, оплачиваемая при отказе клиента от ремонта.</summary>
        public const decimal DefaultDiagnosticsFee = 500m;

        /// <summary>Скидка постоянного клиента по числу ранее выданных заказов.</summary>
        public static int LoyaltyDiscountPercent(int completedOrders)
        {
            if (completedOrders >= 6) return 10;
            if (completedOrders >= 3) return 5;
            return 0;
        }

        public static CostBreakdown Calculate(RepairOrder order, int completedBefore,
                                              decimal diagnosticsFee = DefaultDiagnosticsFee)
        {
            var result = new CostBreakdown();
            result.WorksTotal = order.Works.Sum(w => w.Total);
            result.PartsTotal = order.Parts.Sum(p => p.Total);
            result.IsWarranty = order.IsWarrantyCase;

            // Гарантийный ремонт для клиента бесплатный.
            if (order.IsWarrantyCase)
            {
                result.Total = 0m;
                return result;
            }

            // Отменённый заказ: оплачивается только диагностика, если она проводилась.
            if (order.Status == OrderStatus.Cancelled)
            {
                bool wasDiagnosed = order.History.Any(h => h.Status == OrderStatus.Diagnostics);
                result.DiagnosticsFee = wasDiagnosed ? diagnosticsFee : 0m;
                result.Total = result.DiagnosticsFee;
                return result;
            }

            result.DiscountPercent = LoyaltyDiscountPercent(completedBefore);
            result.DiscountAmount = Math.Round(result.WorksTotal * result.DiscountPercent / 100m, 2);
            result.Total = result.WorksTotal + result.PartsTotal - result.DiscountAmount;
            return result;
        }
    }
}
