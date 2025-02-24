using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS;

namespace OlymPOS
{
    public class ExtraViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Extra> _extras;
        public ObservableCollection<Extra> Extras { get; } = new ObservableCollection<Extra>();
        public ObservableCollection<Course> _displayedcourse;
        public ObservableCollection<Course> DisplayedCourse
        { 
            get => _displayedcourse;
            set
            {
                Console.WriteLine("_displayedCourse");

                    _displayedcourse = new ObservableCollection<Course>(DataService.Instance.Courses);

                OnPropertyChanged();
            }
        }
        public ExtraViewModel()
        {
            _displayedcourse = new ObservableCollection<Course>(DataService.Instance.Courses); // Assuming this is your data source
            //public List<string> CourseOptions { get; } = new List<string>() { "Starters", "Main", "Drinks", "Dessert" };
        }

            public async Task LoadExtras()
        {
            Extras.Clear();
            Console.WriteLine("Extra Cleared");
            DisplayedCourse = new ObservableCollection<Course>(DataService.Instance.Courses);

            //var categories = new ObservableCollection<ProductGroup>();
            using (var connection = new MySqlConnection(GlobalConString.ConnStr))
            {
                await connection.OpenAsync();
                var query = "SELECT * from hypergrp_relation_extra"; // Where HyperGroupID = 0";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Check if the reader has any rows (records) before processing
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            var extra = new Extra
                            {
                                Description = reader.GetString("Description"),
                                Price = reader.GetDecimalOrDefault("Price"),
                                ExtraId = reader.GetInt32("Extra_ID"),
                            };
                            Extras.Add(extra);
                            Console.WriteLine("Added Categories from Hyper");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No records found Hyper.");
                    }
                }
                var query2 = $"SELECT * from qry_extra_group Where ProductGroup_ID = {ProgSettings.ActGrpid}";

                using (var command = new MySqlCommand(query2, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Check if the reader has any rows (records) before processing
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            var extra = new Extra
                            {
                                Description = reader.GetString("Description"),
                                Price = reader.GetDecimalOrDefault("Price"),
                                ExtraId = reader.GetInt32("Extra_ID"),
                            };
                            Extras.Add(extra);
                            Console.WriteLine("Added Categories from Group");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No records found in qry_extra_group.");
                    }
                }

                var query3 = $"SELECT * from Qry_Extra Where Product_ID = {ProgSettings.Actprodrid}";

                using (var command = new MySqlCommand(query3, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Check if the reader has any rows (records) before processing
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            var extra = new Extra
                            {
                                Description = reader.GetString("Description"),
                                Price = reader.GetDecimalOrDefault("Price"),
                                ExtraId = reader.GetInt32("Extra_ID"),
                            };
                            Extras.Add(extra);
                            Console.WriteLine("Added Categories from Products");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No records found in Qry_Extra.");
                    }
                }
            }
            return;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
