using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class LineItem
{
    [Key]
    public int LineId { get; set; }
    public int TransactionId { get; set; }

    public byte ProductId { get; set; }

    public byte Qty { get; set; }

    public byte Rating { get; set; }
}
