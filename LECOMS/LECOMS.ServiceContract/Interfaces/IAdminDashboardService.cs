using LECOMS.Data.DTOs.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardDTO> GetAdminDashboardAsync(
            DateTime from,
            DateTime to);
    }
}
