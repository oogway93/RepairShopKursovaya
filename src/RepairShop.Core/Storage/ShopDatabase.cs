using System.Collections.Generic;
using System.Linq;
using RepairShop.Core.Infrastructure;
using RepairShop.Core.Models;

namespace RepairShop.Core.Storage
{
    /// <summary>Контекст данных: хранит все таблицы в памяти и сохраняет их через IDataStore.</summary>
    public class ShopDatabase
    {
        private readonly IDataStore _store;

        public ShopData Data { get; private set; }
        public IClock Clock { get; private set; }

        public ShopDatabase(IDataStore store, IClock clock = null)
        {
            _store = store;
            Clock = clock ?? new SystemClock();
            Data = store.Load();
        }

        public void Save()
        {
            _store.Save(Data);
        }

        /// <summary>Следующий свободный идентификатор для набора сущностей.</summary>
        public static int NextId(IEnumerable<int> existingIds)
        {
            int max = 0;
            foreach (int id in existingIds)
                if (id > max) max = id;
            return max + 1;
        }
    }
}
