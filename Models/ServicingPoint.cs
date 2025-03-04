namespace OlymPOS.Models
{
    public class ServicingPoint
    {
        public int PostID { get; set; }
        public string Description { get; set; }
        public int PostNumber { get; set; }
        public bool Active { get; set; }
        public bool Reserved { get; set; }
        public int ActiveOrderID { get; set; }
    }
}
