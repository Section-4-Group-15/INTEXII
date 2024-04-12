using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class WishlistProduct
{
    [Key]
    public int wishlist_Id { get; set; }
    public string user_Id { get; set; }
    public int product_Id { get; set; }
    public int quantity { get; set; }
    public decimal cost { get; set; }
}
