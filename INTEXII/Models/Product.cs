using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class Product
{
    [Key]
    public byte ProductId { get; set; }

    public string Name { get; set; } = null!;

    public short? Year { get; set; }

    public short? NumParts { get; set; }

    public short? Price { get; set; }

    public string? ImgLink { get; set; }

    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public string? Description { get; set; }
    public string? Category1 { get; set; }
    public string? Category2 { get; set; }

}
