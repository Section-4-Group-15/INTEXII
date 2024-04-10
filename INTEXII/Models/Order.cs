using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class Order
{
    [Key]
    public long Transaction_Id { get; set; }
    
    public long Customer_Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? Day_Of_Week { get; set; }

    public byte? Time { get; set; }

    public string? Entry_Mode { get; set; }

    public short? Amount { get; set; }

    public string? Type_Of_Transaction { get; set; }

    public string? Country_Of_Transaction { get; set; }

    public string? Shipping_Address { get; set; }

    public string? Bank { get; set; }

    public string? Type_Of_Card { get; set; }

    public byte? Fraud { get; set; }
}
