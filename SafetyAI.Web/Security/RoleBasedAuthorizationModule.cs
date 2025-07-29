using System;
using System.Security.Principal;
using System.Web;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Services.Security;

namespace SafetyAI.Web.Security
{
    public class RoleBasedAuthorizationModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.AuthorizeRequest += OnAuthorizeRequest;
        }

        private void OnAuthorizeRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;

            try
            {
                // Skip authorization for certain paths
                var path = context.Request.Path.ToLowerInvariant();
                if (ShouldSkipAuthorization(path))
                {
                    return;
                }

                // Get current user
                var user = context.User;
                if (user == null || !user.Identity.IsAuthenticated)
                {
                    // Redirect to login or return unauthorized
                    if (IsApiRequest(path))
                    {
                        context.Response.StatusCode = 401;
                        context.Response.End();
                        return;
                    }
                    else
                    {
                        context.Response.Redirect("~/Login.aspx");
                        return;
                    }
                }

                // Check role-based permissions
                if (!HasRequiredPermissions(user, path))
                {
                    Logger.LogWarning($"Access denied for user {user.Identity.Name} to path {path}", "Authorization");
                    
                    if (IsApiRequest(path))
                    {
                        context.Response.StatusCode = 403;
                        context.Response.End();
                    }
                    else
                    {
                        context.Response.Redirect("~/AccessDenied.aspx");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Authorization module error: {ex.Message}", "Authorization");
            }
        }

        private bool ShouldSkipAuthorization(string path)
        {
            var skipPaths = new[]
            {
                "/login.aspx",
                "/error.aspx",
                "/accessdenied.aspx",
                "/styles/",
                "/scripts/",
                "/images/",
                "/favicon.ico"
            };

            foreach (var skipPath in skipPaths)
            {
                if (path.StartsWith(skipPath))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsApiRequest(string path)
        {
            return path.StartsWith("/api/");
        }

        private bool HasRequiredPermissions(IPrincipal user, string path)
        {
            try
            {
                // Define role-based access rules
                var userRoles = GetUserRoles(user.Identity.Name);

                // Admin users have access to everything
                if (userRoles.Contains("Administrator"))
                {
                    return true;
                }

                // Safety Manager permissions
                if (userRoles.Contains("SafetyManager"))
                {
                    // Safety managers can access all safety-related functions
                    return true;
                }

                // Supervisor permissions
                if (userRoles.Contains("Supervisor"))
                {
                    // Supervisors can access most functions except admin areas
                    if (path.Contains("/admin/"))
                    {
                        return false;
                    }
                    return true;
                }

                // Employee permissions (default)
                if (userRoles.Contains("Employee"))
                {
                    // Employees can access basic functions
                    var restrictedPaths = new[]
                    {
                        "/admin/",
                        "/api/v1/safety/analytics/",
                        "/management/"
                    };

                    foreach (var restrictedPath in restrictedPaths)
                    {
                        if (path.Contains(restrictedPath))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                // Default deny
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking permissions for user {user.Identity.Name}: {ex.Message}", "Authorization");
                return false;
            }
        }

        private string[] GetUserRoles(string username)
        {
            try
            {
                // In a real implementation, this would query Active Directory or a database
                // For now, return default roles based on username patterns
                
                if (username.ToLowerInvariant().Contains("admin"))
                {
                    return new[] { "Administrator", "SafetyManager", "Supervisor", "Employee" };
                }
                
                if (username.ToLowerInvariant().Contains("safety") || username.ToLowerInvariant().Contains("manager"))
                {
                    return new[] { "SafetyManager", "Supervisor", "Employee" };
                }
                
                if (username.ToLowerInvariant().Contains("supervisor") || username.ToLowerInvariant().Contains("lead"))
                {
                    return new[] { "Supervisor", "Employee" };
                }
                
                // Default role
                return new[] { "Employee" };
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting roles for user {username}: {ex.Message}", "Authorization");
                return new[] { "Employee" }; // Default to least privileged role
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }

    public static class SecurityHelper
    {
        public static bool IsInRole(string username, string role)
        {
            try
            {
                // This would typically check against Active Directory or database
                var userRoles = GetUserRoles(username);
                return Array.Exists(userRoles, r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking role {role} for user {username}: {ex.Message}", "Security");
                return false;
            }
        }

        public static string[] GetUserRoles(string username)
        {
            // Same logic as in the module above
            if (username.ToLowerInvariant().Contains("admin"))
            {
                return new[] { "Administrator", "SafetyManager", "Supervisor", "Employee" };
            }
            
            if (username.ToLowerInvariant().Contains("safety") || username.ToLowerInvariant().Contains("manager"))
            {
                return new[] { "SafetyManager", "Supervisor", "Employee" };
            }
            
            if (username.ToLowerInvariant().Contains("supervisor") || username.ToLowerInvariant().Contains("lead"))
            {
                return new[] { "Supervisor", "Employee" };
            }
            
            return new[] { "Employee" };
        }

        public static void LogSecurityEvent(string eventType, string description, string username = null)
        {
            try
            {
                Logger.LogWarning($"SECURITY EVENT: {eventType} - {description} - User: {username}", "Security");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to log security event: {ex.Message}", "Security");
            }
        }
    }
}