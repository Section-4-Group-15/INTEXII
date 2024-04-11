using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INTEXII.Models;

public partial class Prediction
{
    [Key]
    public int Prediction_Id { get; set; }
    public int Order_Id { get; set; }
    public int Prediction_Outcome { get; set; }
}
