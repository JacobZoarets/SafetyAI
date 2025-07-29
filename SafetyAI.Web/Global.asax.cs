using System;
using System.Web;
using SafetyAI.Web.App_Start;
using SafetyAI.Data.Services;
using SafetyAI.Services.Configuration;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            try
            {
                // Validate configuration
                ConfigurationValidator.ValidateConfiguration();
                Logger.LogInfo("Configuration validation passed", "Startup");
                
                // Initialize database
                DatabaseInitializer.Initialize();
                Logger.LogInfo("Database initialization completed", "Startup");
                
                // Configure Web API (commented out - not needed for Web Forms)
                // GlobalConfiguration.Configure(WebApiConfig.Register);
                // Logger.LogInfo("Web API configuration completed", "Startup");
                
                // Initialize dependency injection
                DependencyConfig.RegisterDependencies();
                Logger.LogInfo("Dependency injection configured", "Startup");
                
                // Cleanup old logs
                Logger.CleanupOldLogs();
                
                Logger.LogInfo("Application startup completed successfully", "Startup");
            }
            catch (Exception ex)
            {
                // Log the error and handle gracefully
                Logger.LogError(ex, "Startup");
                System.Diagnostics.Debug.WriteLine($"Application startup error: {ex.Message}");
                throw;
            }
        }

        protected void Application_End(object sender, EventArgs e)
        {
            // Clean up resources
            DependencyConfig.DisposeServices();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            
            // Log the error
            Logger.LogError(exception, "Application");
            
            // Clear the error
            Server.ClearError();
            
            // Redirect to error page based on error type
            if (exception is HttpException httpEx)
            {
                switch (httpEx.GetHttpCode())
                {
                    case 404:
                        Response.Redirect("~/NotFound.aspx", false);
                        break;
                    default:
                        Response.Redirect("~/Error.aspx", false);
                        break;
                }
            }
            else
            {
                Response.Redirect("~/Error.aspx", false);
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // Initialize session variables if needed
            Session["UserId"] = System.Web.HttpContext.Current.User?.Identity?.Name ?? "Anonymous";
            Session["SessionStartTime"] = DateTime.UtcNow;
        }

        protected void Session_End(object sender, EventArgs e)
        {
            // Clean up session resources
            // Log session end if needed
        }
    }
}