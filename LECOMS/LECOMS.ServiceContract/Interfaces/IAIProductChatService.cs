using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IAIProductChatService
    {
        Task<string> GetProductAnswerAsync(Product product, string userMessage);
    }

}
