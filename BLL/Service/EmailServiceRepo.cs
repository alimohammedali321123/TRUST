using BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Repositories
{
    public class EmailServiceRepo : EmailService
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var mail = "axyvorn.trast@gmail.com";
            var pass = "ticbhwunibxhhtpq";
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl=true,
                Credentials=new NetworkCredential(mail,pass)
            };
            return client.SendMailAsync(
               new MailMessage( from:mail,
                to:email,
                subject,
                message
                ));
        }

    }
}