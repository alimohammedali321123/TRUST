using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DbContext
{
    public class ApplicationUser:IdentityUser
    {
        public bool IsDeleted { get; set; } = false;
        public bool GoogleLogin { get; set; } = false;
        public string FullName { get; set; }

    }
}
