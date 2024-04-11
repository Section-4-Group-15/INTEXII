using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INTEXII.Models;

public partial class Product
{

    [Key]
    public int product_ID { get; set; }

    public string Name { get; set; } = null!;

    public short? Year { get; set; }

    public short? Num_Parts { get; set; }

    public short? Price { get; set; }

    public string? Img_Link { get; set; }

    public string? Primary_Color { get; set; }

    public string? Secondary_Color { get; set; }

    public string? Description { get; set; }
    public string? Category_1 { get; set; }
    public string? Category_2 { get; set; }

}
