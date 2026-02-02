using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace HEMS.Attributes
{
    /// <summary>
    /// Authorization attribute specifically for coordinator access
    /// </summary>
    public class CoordinatorAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Phase1Login", "Authentication", null);
                return;
            }

            // Check if user has coordinator role
            var userRoles = context.HttpContext.User.Claims
                .Where(c => c.Type == "Role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();

            if (!userRoles.Contains("Coordinator"))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Error", 
                    new { requiredRoles = "Coordinator", userRole = string.Join(",", userRoles) });
                return;
            }
        }
    }
}