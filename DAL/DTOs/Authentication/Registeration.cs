using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.Authentication
{
    public class Registeration
    {
        [Required(ErrorMessage = "حقل الاسم الكامل مطلوب.")]
        [MinLength(3, ErrorMessage = "يجب ألا يقل الاسم عن 3 أحرف.")]
        [MaxLength(50, ErrorMessage = "يجب ألا يزيد الاسم عن 50 حرفاً.")]
        [RegularExpression(@"^[\u0600-\u06FFa-zA-Z\s]+$", ErrorMessage = "يسمح فقط بالحروف العربية والإنجليزية وبدون أرقام.")]
        public string FullName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Compare(nameof(Password),ErrorMessage ="كلمات المرور غير متطابقة حاول مجددا")]
        public string ConfirmPassword { get; set; }
    }
}
