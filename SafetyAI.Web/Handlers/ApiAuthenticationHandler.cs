using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Web.App_Start
{
    public class ApiAuthenticationHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Skip authentication for OPTIONS requests (CORS preflight)
                if (request.Method == HttpMethod.Options)
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                // Check for API key in headers
                if (request.Headers.Contains("X-API-Key"))
                {
                    var apiKey = request.Headers.GetValues("X-API-Key").FirstOrDefault();
                    if (IsValidApiKey(apiKey))
                    {
                        return await base.SendAsync(request, cancellationToken);
                    }
                }

                // Check for basic authentication
                if (request.Headers.Authorization != null && request.Headers.Authorization.Scheme == "Basic")
                {
                    if (IsValidBasicAuth(request.Headers.Authorization.Parameter))
                    {
                        return await base.SendAsync(request, cancellationToken);
                    }
                }

                // For development/testing, allow requests without authentication
                // In production, this should be removed
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                // Return unauthorized response
                Logger.LogWarning($"Unauthorized API request: {request.Method} {request.RequestUri}", "API");
                return request.CreateResponse(HttpStatusCode.Unauthorized, new
                {
                    error = "Unauthorized",
                    message = "Valid API key or authentication required"
                });
            }
            catch (Exception ex)
            {
                Logger.LogError($"Authentication handler error: {ex.Message}", "API");
                return request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    error = "Authentication Error",
                    message = "An error occurred during authentication"
                });
            }
        }

        private bool IsValidApiKey(string apiKey)
        {
            // In a real implementation, this would validate against a database or configuration
            // For now, accept any non-empty API key
            return !string.IsNullOrWhiteSpace(apiKey);
        }

        private bool IsValidBasicAuth(string credentials)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(credentials))
                    return false;

                var decodedCredentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(credentials));
                var parts = decodedCredentials.Split(':');
                
                if (parts.Length != 2)
                    return false;

                var username = parts[0];
                var password = parts[1];

                // In a real implementation, validate against user store
                // For now, accept any non-empty credentials
                return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
            }
            catch
            {
                return false;
            }
        }
    }
}