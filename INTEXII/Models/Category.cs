using System;
using System.Collections.Generic;

namespace INTEXII.Models;

public partial class Category
{
    public byte CategoryId { get; set; }

    public string CategoryDescription { get; set; } = null!;
}
