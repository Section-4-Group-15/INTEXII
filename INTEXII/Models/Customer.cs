using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class Customer
{
    [Key]
    public short customer_ID { get; set; }

    public string? first_name { get; set; }

    public string? last_name { get; set; }

    public DateOnly? birth_date { get; set; }

    public string? country_of_residence { get; set; }

    public string? gender { get; set; }

    public double? age { get; set; }
}
