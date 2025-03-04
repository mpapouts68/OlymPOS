using System.Collections.Generic;
using System.Threading.Tasks;
using OlymPOS.Models;

namespace OlymPOS.Repositories.Interfaces
{
    public interface ITableRepository
    {
        Task<List<ServicingPoint>> GetTablesAsync();
        Task UpdateTableStatusAsync(int tableId, bool occupied, bool reserved, int? activeOrderId);



    }
}

