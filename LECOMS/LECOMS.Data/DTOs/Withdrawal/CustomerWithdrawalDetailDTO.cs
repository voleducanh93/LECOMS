using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Withdrawal
{
    public class CustomerWithdrawalDetailDTO
    {
        public string Id { get; set; }
        public decimal Amount { get; set; }
        public WithdrawalStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RejectionReason { get; set; }

        public string? Note { get; set; }
        public string? AdminNote { get; set; }

        public BankInfoDTO Bank { get; set; }
        public SimpleCustomerDTO Customer { get; set; }
    }

}
