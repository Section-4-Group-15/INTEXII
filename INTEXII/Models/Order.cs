using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class Order
{
    [Key]
    public long transaction_ID { get; set; }
    
    public long customer_ID { get; set; }

    public DateOnly? date { get; set; }

    public string? day_of_week { get; set; }

    public byte? time { get; set; }

    public string? entry_mode { get; set; }

    public short? amount { get; set; }

    public string? type_of_transaction { get; set; }

    public string? country_of_transaction { get; set; }

    public string? shipping_address { get; set; }

    public string? bank { get; set; }

    public string? type_of_card { get; set; }

    public byte? fraud { get; set; }
}
