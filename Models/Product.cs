using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OlymPOS
{
    public class Product : INotifyPropertyChanged
    {
        public int ProductID { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public decimal Price { get; set; }
        public int ProductGroupID { get; set; }
        public string Printer { get; set; }
        public int? VAT { get; set; }
        public bool Build { get; set; }
        public int? Extra_ID { get; set; }
        public int Row_Print { get; set; }
        public bool Auto_Extra { get; set; }
        public bool Has_Options { get; set; }
        public int? Extra_ID_Key { get; set; }
        public int? Menu_Number { get; set; }
        public bool Include_Group { get; set; }
        public bool Favorite { get; set; }
        public string Drink_Or_Food { get; set; }
        public string CPrinter { get; set; } // Check the actual data type 
        public bool Sale_Lock { get; set; }
        public bool ToPrinter { get; set; }
        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }
        public int GroupID;
        public string ProductName;


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

