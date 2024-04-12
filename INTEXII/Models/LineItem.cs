using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class LineItem
{
    [Key]
    public int line_id { get; set; }
    public int transaction_id { get; set; }

    public byte product_ID { get; set; }

    public byte qty { get; set; }

    public byte rating { get; set; }
}
