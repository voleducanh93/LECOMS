using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Jobs
{
    public class GroqAuthHandler : DelegatingHandler
    {
        private readonly IConfiguration _config;

        public GroqAuthHandler(IConfiguration config)
        {
            _config = config;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var apiKey = _config["Groq:ApiKey"];

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
