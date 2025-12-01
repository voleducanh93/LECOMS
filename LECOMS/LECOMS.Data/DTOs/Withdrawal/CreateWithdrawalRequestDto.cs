using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Withdrawal
{
    public class CreateWithdrawalRequestDto
    {
        public int ShopId { get; set; }
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public string? Note { get; set; }
    }
}
