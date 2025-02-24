using System.Collections.ObjectModel;

namespace OlymPOS
{
    public class ProductGroup
    {
        public int ProductGroupID { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public int? View { get; set; }
        public int? ViewOrder { get; set; }
        public int? Extra_ID { get; set; }
        public int? Icon_ID { get; set; }
        public bool ISSub { get; set; }
        public int? Sub_From_GroupID { get; set; }
        public bool Has_Sub { get; set; }
        //public string ImageSource => Has_Sub ? "subgroup.png" : "group.png";
        public ObservableCollection<ProductGroup> Subcategories { get; set; } = new ObservableCollection<ProductGroup>();
    }

}
