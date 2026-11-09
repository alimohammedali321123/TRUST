using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.GenerateRefreshToken
{
    public class GenerateRefreshToken
    {
        public string RefreshToken()
        {
            var randombytes=new byte[64];
            using (var RT = RandomNumberGenerator.Create())
            {
                RT.GetBytes(randombytes);
                return Convert.ToBase64String(randombytes);
            }
        }
    }
}
