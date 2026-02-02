using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HEMS.Attributes;

namespace HEMS.Controllers
{
    /// <summary>
    /// Controller for testing functionality
    /// Restricted to coordinators only for testing purposes
    /// </summary>
    [Authorize]
    [CoordinatorAuthorize]
    public class TestController : Controller
    {
        /// <summary>
        /// Network handling test page
        /// </summary>
        /// <returns>Test view for network handling</returns>
        public ActionResult NetworkHandling()
        {
            return View();
        }
    }
}