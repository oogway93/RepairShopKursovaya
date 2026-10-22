using System;

namespace RepairShop.Core.Infrastructure
{
    /// <summary>Нарушение бизнес-правила (сообщение показывается пользователю).</summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message)
        {
        }
    }

    /// <summary>Источник текущего времени (подменяется в тестах).</summary>
    public interface IClock
    {
        DateTime Now { get; }
    }

    public class SystemClock : IClock
    {
        public DateTime Now
        {
            get { return DateTime.Now; }
        }
    }

    public class FixedClock : IClock
    {
        public DateTime Current { get; set; }

        public FixedClock(DateTime now)
        {
            Current = now;
        }

        public DateTime Now
        {
            get { return Current; }
        }
    }
}
