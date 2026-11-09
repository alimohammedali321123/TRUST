using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Authentication
{
    public class Verify
    {
        [EmailAddress]
        [Required]
        public string Email { get; set; }
        [Required]
        [MaxLength(6,ErrorMessage ="اقصى عدد هو 6 فقط")]
        public string Code { get; set; }
    }
}