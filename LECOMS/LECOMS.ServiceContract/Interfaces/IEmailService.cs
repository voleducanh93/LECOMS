using LECOMS.Data.DTOs.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IEmailService
    {
        public void SendEmail(EmailRequestDTO request);
        public void SendEmailConfirmation(string username, string confirmLink);
        Task SendEmailForgotPassword(string email, string resetLink);
        Task SendExpiryAlertsAsync(string adminEmail, List<string> expiringVaccines);

    }
}
