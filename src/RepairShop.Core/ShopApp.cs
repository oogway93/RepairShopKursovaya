using RepairShop.Core.Services;
using RepairShop.Core.Storage;
using RepairShop.Core.Infrastructure;

namespace RepairShop.Core
{
    /// <summary>Корень композиции: связывает хранилище и все сервисы.</summary>
    public class ShopApp
    {
        public ShopDatabase Db { get; private set; }
        public AuthService Auth { get; private set; }
        public ClientService Clients { get; private set; }
        public CatalogService Catalog { get; private set; }
        public OrderService Orders { get; private set; }
        public ReportService Reports { get; private set; }

        public ShopApp(IDataStore store, IClock clock = null)
        {
            Db = new ShopDatabase(store, clock);
            Auth = new AuthService(Db);
            Clients = new ClientService(Db);
            Catalog = new CatalogService(Db);
            Orders = new OrderService(Db);
            Reports = new ReportService(Db);
        }
    }
}
