using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ProgrammersGuide.Models
{
    public class ResetPasswordModel // here we have to declare 3 properties
    {
        [Required( AllowEmptyStrings = false,ErrorMessage ="This field is required!!")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }


        [DataType(DataType.Password)]
        [Compare("NewPassword",ErrorMessage ="The passwords doesn't match !!")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string ResetCode { get; set; }
    }
}