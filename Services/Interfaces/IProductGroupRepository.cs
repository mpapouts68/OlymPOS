using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface IProductGroupRepository : IRepository<ProductGroup>
    {
        Task<IEnumerable<ProductGroup>> GetRootGroupsAsync();
        Task<IEnumerable<ProductGroup>> GetSubgroupsAsync(int parentGroupId);
    }
}
