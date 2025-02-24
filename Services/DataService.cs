using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS;

namespace OlymPOS
{
    public class DataService
    {
        private static DataService instance;
        public static DataService Instance => instance ?? (instance = new DataService());

        public ObservableCollection<ProductGroup> ProductCategories { get; private set; }
        public ObservableCollection<Product> AllProducts { get; private set; }
        public ObservableCollection<Course> Courses { get; private set; } 
        public DataService()
        {
            ProductCategories = new ObservableCollection<ProductGroup>();
            AllProducts = new ObservableCollection<Product>();
            Courses = new ObservableCollection<Course>();

        }

        public async Task LoadAllDataAsync()
        {
            try
            {
                ProductCategories = await LoadProductCategoriesAsync();
                AllProducts = await LoadAllProductsAsync();
                Courses = await LoadAllCourseAsync();

                //LinkProductsToCategories();
                OrganizeProductGroups();


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private async Task<ObservableCollection<ProductGroup>> LoadProductCategoriesAsync()
        {
            var categories = new ObservableCollection<ProductGroup>();
            using (var connection = new MySqlConnection(GlobalConString.ConnStr))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM ProductGroups";
                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var category = new ProductGroup
                        {
                            ProductGroupID = reader.GetInt32("ProductGroup_ID"),
                            Description = reader.GetString("Description"),
                            Description2 = reader.IsDBNull(reader.GetOrdinal("Description2")) ? null : reader.GetString("Description2"),
                            View = reader.IsDBNull(reader.GetOrdinal("View")) ? (int?)null : reader.GetInt32("View"),
                            ViewOrder = reader.IsDBNull(reader.GetOrdinal("ViewOrder")) ? (int?)null : reader.GetInt32("ViewOrder"),
                            Extra_ID = reader.IsDBNull(reader.GetOrdinal("Extra_ID")) ? (int?)null : reader.GetInt32("Extra_ID"),
                            Icon_ID = reader.IsDBNull(reader.GetOrdinal("Icon_ID")) ? (int?)null : reader.GetInt32("Icon_ID"),
                            ISSub = reader.GetBoolean("ISSub"),
                            Sub_From_GroupID = reader.IsDBNull(reader.GetOrdinal("Sub_From_GroupID")) ? (int?)null : reader.GetInt32("Sub_From_GroupID"),
                            Has_Sub = reader.GetBoolean("Has_Sub"),
                        };
                        categories.Add(category);
                        Console.WriteLine("Added Categories");
                    }
                }
            }
            return categories;
        }

        private async Task<ObservableCollection<Product>> LoadAllProductsAsync()
        {
            var products = new ObservableCollection<Product>();
            using (var connection = new MySqlConnection(GlobalConString.ConnStr))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM Products";
                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var product = new Product
                        {
                            ProductID = reader.GetInt32("Product_ID"),
                            Description = reader.GetString("Description"),
                            Description2 = reader.IsDBNull(reader.GetOrdinal("Description2")) ? null : reader.GetString("Description2"),
                            Price = reader.GetDecimal("Price"),
                            ProductGroupID = reader.GetInt32("ProductGroup_ID"),
                            Printer = reader.IsDBNull(reader.GetOrdinal("Printer")) ? null : reader.GetString("Printer"),
                            VAT = reader.IsDBNull(reader.GetOrdinal("VAT")) ? (int?)null : reader.GetInt32("VAT"),
                            Build = reader.GetBoolean("Build"),
                            Extra_ID = reader.IsDBNull(reader.GetOrdinal("Extra_ID")) ? (int?)null : reader.GetInt32("Extra_ID"),
                            Row_Print = reader.GetInt32("Row_Print"),
                            Auto_Extra = reader.GetBoolean("Auto_Extra"),
                            Has_Options = reader.GetBoolean("Has_Options"),
                            Extra_ID_Key = reader.IsDBNull(reader.GetOrdinal("Extra_ID_Key")) ? (int?)null : reader.GetInt32("Extra_ID_Key"),
                            Menu_Number = reader.IsDBNull(reader.GetOrdinal("Menu_Number")) ? (int?)null : reader.GetInt32("Menu_Number"),
                            Include_Group = reader.GetBoolean("Include_Group"),
                            Favorite = reader.GetBoolean("Favorite"),
                            Drink_Or_Food = reader.IsDBNull(reader.GetOrdinal("Drink_Or_Food")) ? null : reader.GetString("Drink_Or_Food"),
                            CPrinter = reader.IsDBNull(reader.GetOrdinal("CPrinter")) ? null : reader.GetString("CPrinter"),
                            Sale_Lock = reader.GetBoolean("Sale_Lock"),
                            Quantity = 0
                        };
                        products.Add(product);
                        Console.WriteLine(reader.GetString("Description"));
                    }
                }
            }
            return products;
        }

        private async Task<ObservableCollection<Course>> LoadAllCourseAsync()
        {
            var courses = new ObservableCollection<Course>();
            using (var connection = new MySqlConnection(GlobalConString.ConnStr))
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM Grouping_Basis";
                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var course = new Course
                        {
                            CourseId= reader.GetInt32("Serving_Row"),
                            Description = reader.GetString("Description"),
                        };
                        courses.Add(course);
                        Console.WriteLine(reader.GetString("Description"));
                    }
                }
            }
            return courses;
        }


        private void LinkProductsToCategories()
        {
            foreach (var category in ProductCategories)
            {
                if (category.ISSub)
                {
                    var parentCategory = ProductCategories.FirstOrDefault(pc => pc.ProductGroupID == category.Sub_From_GroupID);
                    if (parentCategory != null)
                    {
                        if (parentCategory.Subcategories == null)
                        {
                            parentCategory.Subcategories = new ObservableCollection<ProductGroup>();
                        }
                        parentCategory.Subcategories.Add(category);
                    }
                }
            }
        }

        private void OrganizeCategories()
        {
            // Filter root categories
            var rootCategories = Instance.ProductCategories
                                    .Where(pc => !pc.ISSub).ToList();

            // Iterate over root categories to find and assign their subcategories
            Console.WriteLine("Start Organizing");
            foreach (var rootCategory in rootCategories)
            {
                var subCategories = Instance.ProductCategories
                                        .Where(pc => pc.ISSub && pc.Sub_From_GroupID == rootCategory.ProductGroupID)
                                        .ToList();

                rootCategory.Subcategories = new ObservableCollection<ProductGroup>(subCategories);

                // Console output for debugging
                Console.WriteLine($"Root: {rootCategory.Description}, Subcategories Count: {rootCategory.Subcategories.Count}");
                foreach (var subCategory in rootCategory.Subcategories)
                {
                    Console.WriteLine($"\tSubcategory: {subCategory.Description}");
                }
            }

            ProductCategories = new ObservableCollection<ProductGroup>(rootCategories);
        }
        private void OrganizeProductGroups()
        {
            var rootCategories = ProductCategories.Where(pc => !pc.ISSub).ToList();

            foreach (var rootCategory in rootCategories)
            {
                var subCategories = ProductCategories
                                    .Where(pc => pc.ISSub && pc.Sub_From_GroupID == rootCategory.ProductGroupID)
                                    .ToList();

                rootCategory.Subcategories = new ObservableCollection<ProductGroup>(subCategories);
            }

            // Replace the original list with only the root categories, which now include their subcategories
            ProductCategories = new ObservableCollection<ProductGroup>(rootCategories);
            Console.WriteLine("Finished Organizing");
        }
        
    }
    }