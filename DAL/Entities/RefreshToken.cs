using DAL.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class RefreshToken:BaseModel
    {
        public string Token { get; set; }
        public string UserId { get; set; }
        public DateTime ExpiredDate { get; set; }
        public bool IsRevoked { get; set; }
    }
}
