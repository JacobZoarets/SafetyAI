using System;
using System.Web.UI;

namespace SafetyAI.Web
{
    public partial class Error : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Log the error if available in session or query string
            var errorMessage = Request.QueryString["error"];
            if (!string.IsNullOrEmpty(errorMessage))
            {
                SafetyAI.Services.Infrastructure.Logger.LogError($"Error page accessed with message: {errorMessage}", "ErrorPage");
            }
        }
    }
}