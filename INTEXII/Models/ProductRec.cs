using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models
{
    public class ProductRec
    {
        [Key]
        public int product_rec_ID { get; set; }
        public int? product_ID { get; set; }
        public string? product_name { get; set; }
        public int? recommended_product_ID { get; set; }
        public string? recommended_product_name { get; set; }
        public string? product_url { get; set; }

    }
}
