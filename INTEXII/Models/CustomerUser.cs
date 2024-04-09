using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class CustomerUser
{
    [Key]
    public byte Id { get; set; }
    public string? UserId { get; set; }
    public string? CustomerId { get; set; }


}
