using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OlymPOS;

namespace OlymPOS.Services.Interfaces
{
    public interface IDataService
    {
        Task<ObservableCollection<ProductGroup>> LoadProductGroups();
        Task<ObservableCollection<Product>> LoadProducts();
        Task LoadAllDataAsync();

        ObservableCollection<ProductGroup> ProductCategories { get; }
        ObservableCollection<Product> AllProducts { get; }
        ObservableCollection<Course> Courses { get; }
    }
}
