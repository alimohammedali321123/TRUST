using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DAL.AccountVerificationCode
{
    public class VerificationCode
    {
        public string GenerateverificationCode(int length=6)
        {
            var chars = "AbCDEfGhiJKlMnoPQrstUVWxyZ0123645879";
            char[] code=new char[length];
            for (int i = 0; i < length; i++)
            {
                code[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
            return new string(code);
        }
    }
}
