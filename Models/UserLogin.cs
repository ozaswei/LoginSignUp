using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace ProgrammersGuide.Models
{
    public class UserLogin
    {
        [Display(Name ="Email ID")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email ID is required !!")]
        public string EmailID { get; set; }

        [Required(AllowEmptyStrings =false,ErrorMessage ="This field is required !!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

    }
}
//As this is custom model class, so we can add the validation directly
//Addd:: System.ComponentModel.DataAnnotation; to write the validation thingy