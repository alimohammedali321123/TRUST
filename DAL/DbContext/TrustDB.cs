using DAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
namespace DAL.DbContext
{
    public class TrustDB : IdentityDbContext<ApplicationUser>
    {
        public TrustDB(DbContextOptions options) : base(options)
        {
        }
        public DbSet<RefreshToken> RefreshToken { get; set; }
        public DbSet<AccountVerification> AccountVerification { get; set; }
    }
}