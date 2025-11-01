using LECOMS.Data.DTOs.LandingPage;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ILandingPageService
    {
        Task<LandingPageDTO> GetLandingPageDataAsync();
    }
}
