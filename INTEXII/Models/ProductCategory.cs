using System;
using System.Collections.Generic;

namespace INTEXII.Models;

public partial class ProductCategory
{
    public byte Id { get; set; }
    public byte ProductId { get; set; }

    public byte CategoryId { get; set; }
}
