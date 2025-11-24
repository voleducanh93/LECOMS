using LECOMS.Data.DTOs.Seller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ISellerDashboardService
    {
        Task<SellerDashboardDTO> GetSellerDashboardAsync(
            string sellerUserId,
            DateTime from,
            DateTime to);
    }
}
