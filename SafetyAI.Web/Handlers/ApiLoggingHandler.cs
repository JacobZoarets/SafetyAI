using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Web.App_Start
{
    public class ApiLoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Log request
                Logger.LogInfo($"API Request: {request.Method} {request.RequestUri}", "API");

                // Process request
                var response = await base.SendAsync(request, cancellationToken);

                // Log response
                var duration = DateTime.UtcNow - startTime;
                Logger.LogInfo($"API Response: {(int)response.StatusCode} {response.StatusCode} - Duration: {duration.TotalMilliseconds}ms", "API");

                return response;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Logger.LogError($"API Error: {request.Method} {request.RequestUri} - Duration: {duration.TotalMilliseconds}ms - Error: {ex.Message}", "API");
                throw;
            }
        }
    }
}