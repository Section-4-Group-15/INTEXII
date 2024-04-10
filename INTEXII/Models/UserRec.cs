using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models
{
    public class UserRec
    {
        [Key]
        public int Reccomendation_ID { get; set; }
        public int? Person_ID { get; set; } // Assuming this is a unique identifier for a person
        public string? Email { get; set; }
        public int? Product_ID { get; set; } // Assuming this uniquely identifies a recommended product
        public string? RecommendationName { get; set; }
        public string? RecommendationUrl { get; set; }
    }

}
