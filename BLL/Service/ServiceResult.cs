using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string Token { get; set; }
        public DateTime Expire { get; set; }
        public string RefreshToken { get; set; }
        public object Data { get; set; }
    }
}
