using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using OlymPOS;

namespace OlymPOS
{
    public interface IDataService
    {
        Task<ObservableCollection<ProductGroup>> LoadProductGroups();
        Task<ObservableCollection<Product>> LoadProducts();
        // Add methods for loading Order data as needed later 
    }
}

