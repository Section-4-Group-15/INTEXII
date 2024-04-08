using System;
using System.Collections.Generic;

namespace INTEXII.Models;

public partial class Order
{
    public long TransactionId { get; set; }

    public long CustomerId { get; set; }

    public DateOnly? Date { get; set; }

    public string? DayOfWeek { get; set; }

    public byte? Time { get; set; }

    public string? EntryMode { get; set; }

    public short? Amount { get; set; }

    public string? TypeOfTransaction { get; set; }

    public string? CountryOfTransaction { get; set; }

    public string? ShippingAddress { get; set; }

    public string? Bank { get; set; }

    public string? TypeOfCard { get; set; }

    public byte? Fraud { get; set; }
}
