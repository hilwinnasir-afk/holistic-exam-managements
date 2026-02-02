using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace HEMS.Attributes
{
    /// <summary>
    /// Custom authorization attribute for role-based access control
    /// </summary>
    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string? Roles { get; set; }

        public RoleAuthorizeAttribute()
        {
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Phase1Login", "Authentication", null);
                return;
            }

            // If no specific roles required, just check authentication
            if (string.IsNullOrEmpty(Roles))
            {
                return;
            }

            // Check if user has required role
            var requiredRoles = Roles.Split(',').Select(r => r.Trim()).ToArray();
            var userRoles = context.HttpContext.User.Claims
                .Where(c => c.Type == "Role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();

            if (!requiredRoles.Any(role => userRoles.Contains(role)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Error", 
                    new { requiredRoles = Roles, userRole = string.Join(",", userRoles) });
                return;
            }
        }
    }
}