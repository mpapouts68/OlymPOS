using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface ITableRepository : IRepository<ServicingPoint>
    {
        Task<IEnumerable<ServicingPoint>> GetByAreaAsync(int areaId);
        Task<bool> SetTableStatusAsync(int tableId, bool isActive, int? orderId = null);
    }
}
