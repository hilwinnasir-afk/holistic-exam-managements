using HEMS.Models;
using HEMS.Models.ViewModels;
using HEMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HEMS.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly HEMS.Services.IAuthenticationService _authService;
        private readonly ISessionService _sessionService;

        public AuthenticationController(HEMS.Services.IAuthenticationService authService, ISessionService sessionService)
        {
            _authService = authService;
            _sessionService = sessionService;
        }

        /// <summary>
        /// GET: Phase 1 Login page
        /// </summary>
        public ActionResult Phase1Login()
        {
            // If user is already authenticated, redirect appropriately
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new Phase1LoginViewModel());
        }

        /// <summary>
        /// GET: Coordinator Login page (simple single-phase login)
        /// </summary>
        public ActionResult CoordinatorLogin()
        {
            // Check if user is already authenticated as coordinator
            if (User.Identity.IsAuthenticated)
            {
                var userRoles = User.Claims
                    .Where(c => c.Type == "Role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToArray();

                if (userRoles.Contains("Coordinator"))
                {
                    // User is already logged in as coordinator, show option to continue or logout
                    ViewBag.AlreadyLoggedIn = true;
                    ViewBag.UserEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                }
            }

            return View(new CoordinatorLoginViewModel());
        }

        /// <summary>
        /// POST: Coordinator Login - Simple authentication for coordinators
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CoordinatorLogin(CoordinatorLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get user by email
            var user = _authService.GetUserByEmail(model.Email);
            
            if (user == null)
            {
                model.ErrorMessage = "Invalid email or password. Please check your credentials and try again.";
                return View(model);
            }
            
            if (user.Role == null || user.Role.RoleName != "Coordinator")
            {
                model.ErrorMessage = "Access denied. This login is for coordinators only.";
                return View(model);
            }
            
            // Verify password using BCrypt
            bool passwordValid = false;
            try
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            }
            catch (Exception)
            {
                // Fallback to plain text comparison for testing
                passwordValid = (user.PasswordHash == model.Password);
            }
            
            if (!passwordValid)
            {
                model.ErrorMessage = "Invalid email or password. Please check your credentials and try again.";
                return View(model);
            }

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Username),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim("Role", user.Role.RoleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            // Sign in the user
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), authProperties);

            // Create session for coordinator
            _sessionService.CreateUserSession(user, phase1Completed: true, phase2Completed: true);
            
            TempData["SuccessMessage"] = "Welcome to HEMS Coordinator Dashboard!";
            return RedirectToAction("Index", "Coordinator");
        }

        /// <summary>
        /// POST: Phase 1 Login - Identity verification using university email and calculated password
        /// Enhanced with comprehensive error handling and detailed result information
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Phase1Login(Phase1LoginViewModel model)
        {
            try
            {
                // Debug: Log the incoming request
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase1Login POST - Email: {model.Email}, Password Length: {model.Password?.Length ?? 0}");
                
                if (!ModelState.IsValid)
                {
                    // Debug: Log validation errors
                    var validationErrors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ModelState Invalid: {string.Join(", ", validationErrors.Select(v => $"{v.Field}: {string.Join("; ", v.Errors)}"))}");
                    
                    // Add debug info to view for development
                    ViewBag.DebugInfo = $"Validation Errors: {string.Join(", ", validationErrors.Select(v => $"{v.Field}: {string.Join("; ", v.Errors)}"))}";
                    return View(model);
                }

                // Get client information for detailed error handling
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Calling ValidatePhase1LoginWithDetails - IP: {ipAddress}");

                // Validate Phase 1 login credentials with comprehensive error handling
                var result = _authService.ValidatePhase1LoginWithDetails(model.Email, model.Password, ipAddress, userAgent);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Authentication Result - Success: {result.IsSuccess}, ErrorType: {result.ErrorType}, ErrorMessage: {result.ErrorMessage}");
                
                if (result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Authentication successful for user: {result.User?.Username}, Role: {result.User?.Role?.RoleName}");
                    
                    // Create claims for the user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                        new Claim(ClaimTypes.Name, result.User.Username),
                        new Claim(ClaimTypes.Email, result.User.Username),
                        new Claim(ClaimTypes.Role, result.User.Role?.RoleName ?? "Student"),
                        new Claim("Role", result.User.Role?.RoleName ?? "Student"),
                        new Claim("Phase1Completed", "true")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    // Sign in the user
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    // Store session token for validation
                    HttpContext.Session.SetString("Phase1SessionToken", result.SessionToken ?? "");
                    
                    // Create user session with Phase 1 completion
                    _sessionService.CreateUserSession(result.User, phase1Completed: true);

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Session created, redirecting based on role: {result.User.Role?.RoleName}");

                    // Check user role and redirect appropriately
                    if (result.User.Role?.RoleName == "Student")
                    {
                        // Check if Phase 1 was already completed and it's exam time
                        if (result.User.LoginPhaseCompleted && _authService.IsExamTimeNow())
                        {
                            TempData["SuccessMessage"] = "Phase 1 verification completed. Please proceed to Phase 2 login.";
                            return RedirectToAction("Phase2Login");
                        }
                        
                        // Students go directly to their dashboard after Phase 1 login
                        TempData["SuccessMessage"] = "Welcome to HEMS Student Dashboard!";
                        return RedirectToAction("StudentDashboard", "Home");
                    }
                    else
                    {
                        // Other users proceed to Phase 2 login
                        TempData["SuccessMessage"] = "Phase 1 login successful. Please proceed to Phase 2 login on exam day.";
                        return RedirectToAction("Phase2Login");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Authentication failed - ErrorType: {result.ErrorType}, Message: {result.ErrorMessage}, DetailedMessage: {result.DetailedErrorMessage}");
                    
                    // Add debug information to the view
                    ViewBag.DebugInfo = $"Error Type: {result.ErrorType}, Message: {result.ErrorMessage}, Detailed: {result.DetailedErrorMessage}";
                    
                    // Handle different error types with appropriate user messages
                    switch (result.ErrorType)
                    {
                        case AuthenticationErrorType.InvalidEmailFormat:
                            model.ErrorMessage = "Please enter a valid university email address from an approved educational institution.";
                            break;
                        case AuthenticationErrorType.UserNotFound:
                            model.ErrorMessage = "Invalid email or password. Please check your credentials and try again.";
                            break;
                        case AuthenticationErrorType.AccountLocked:
                            model.ErrorMessage = result.ErrorMessage;
                            ViewBag.LockoutEndTime = result.LockoutEndTime;
                            break;
                        case AuthenticationErrorType.Phase1AlreadyCompleted:
                            model.ErrorMessage = "Identity verification already completed. Please login on the exam date.";
                            break;
                        case AuthenticationErrorType.IncorrectPassword:
                        case AuthenticationErrorType.MultipleFailedAttempts:
                            model.ErrorMessage = result.ErrorMessage;
                            ViewBag.FailedAttemptCount = result.FailedAttemptCount;
                            break;
                        case AuthenticationErrorType.StudentRecordNotFound:
                            model.ErrorMessage = "Student record not found. Please contact your coordinator for assistance.";
                            break;
                        case AuthenticationErrorType.SystemError:
                        default:
                            model.ErrorMessage = "An error occurred during login. Please try again or contact support if the problem persists.";
                            break;
                    }
                    
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in Phase1Login: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                
                model.ErrorMessage = "A system error occurred during login. Please try again.";
                ViewBag.DebugInfo = $"Exception: {ex.Message}";
                return View(model);
            }
        }

        /// <summary>
        /// GET: Phase 2 Login page
        /// </summary>
        public ActionResult Phase2Login()
        {
            var model = new Phase2LoginViewModel();
            
            // Show success message from Phase 1 if available
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            return View(model);
        }

        /// <summary>
        /// POST: Phase 2 Login - Exam-day login using student ID and coordinator-generated password
        /// Enhanced with comprehensive error handling and detailed result information
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Phase2Login(Phase2LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get client information for detailed error handling
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase2Login POST - IdNumber: {model.IdNumber}, Password Length: {model.Password?.Length ?? 0}");

                // Validate Phase 2 login credentials with comprehensive error handling
                var result = _authService.ValidatePhase2LoginWithDetails(model.IdNumber, model.Password ?? "", ipAddress, userAgent);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase2 Authentication Result - Success: {result.IsSuccess}, ErrorType: {result.ErrorType}, ErrorMessage: {result.ErrorMessage}");
                
                if (result.IsSuccess && result.User != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase2 authentication successful for user: {result.User.Username}, Role: {result.User.Role?.RoleName}");
                    
                    // Create claims for the user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                        new Claim(ClaimTypes.Name, result.User.Username),
                        new Claim(ClaimTypes.Email, result.User.Username),
                        new Claim(ClaimTypes.Role, result.User.Role?.RoleName ?? "Student"),
                        new Claim("Role", result.User.Role?.RoleName ?? "Student"),
                        new Claim("Phase1Completed", "true"),
                        new Claim("Phase2Completed", "true")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    // Sign in the user
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    // Store session tokens for validation
                    HttpContext.Session.SetString("Phase2SessionToken", result.SessionToken ?? "");
                    HttpContext.Session.SetInt32("ExamSessionId", result.ExamSessionId ?? 0);

                    // Create user session with Phase 2 completion
                    _sessionService.CreateUserSession(result.User, phase1Completed: true, phase2Completed: true);

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase2 session created, checking password change requirement");

                    // Check if password change is required
                    if (result.RequiresPasswordChange)
                    {
                        TempData["InfoMessage"] = "Please change your password before accessing the exam.";
                        return RedirectToAction("ChangePassword");
                    }

                    // Redirect to exam access page after successful Phase 2 login
                    TempData["SuccessMessage"] = "Phase 2 login successful. Welcome to the exam system!";
                    return RedirectToAction("ExamAccess", "Home");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase2 authentication failed - ErrorType: {result.ErrorType}, Message: {result.ErrorMessage}");
                    
                    // Handle different error types with appropriate user messages
                    switch (result.ErrorType)
                    {
                        case AuthenticationErrorType.InvalidIdNumberFormat:
                            model.ErrorMessage = "Invalid student ID number format. Please check your ID number and try again.";
                            break;
                        case AuthenticationErrorType.UserNotFound:
                            model.ErrorMessage = "Invalid student ID or session password. Please check your credentials and try again.";
                            break;
                        case AuthenticationErrorType.AccountLocked:
                            model.ErrorMessage = result.ErrorMessage;
                            ViewBag.LockoutEndTime = result.LockoutEndTime;
                            break;
                        case AuthenticationErrorType.Phase1NotCompleted:
                            model.ErrorMessage = "Please complete Phase 1 identity verification before attempting Phase 2 login.";
                            ViewBag.ShowPhase1Link = true;
                            break;
                        case AuthenticationErrorType.ConcurrentLoginAttempt:
                            model.ErrorMessage = "You already have an active exam session. Please complete your current session or contact your coordinator.";
                            break;
                        case AuthenticationErrorType.ExamSessionNotFound:
                            model.ErrorMessage = "No active exam session found. Please contact your coordinator to start the exam session.";
                            break;
                        case AuthenticationErrorType.ExpiredExamSession:
                            model.ErrorMessage = "The exam session has expired. Please contact your coordinator.";
                            break;
                        case AuthenticationErrorType.IncorrectExamPassword:
                        case AuthenticationErrorType.MultipleFailedAttempts:
                            model.ErrorMessage = result.ErrorMessage;
                            ViewBag.FailedAttemptCount = result.FailedAttemptCount;
                            break;
                        case AuthenticationErrorType.SystemError:
                        default:
                            model.ErrorMessage = "An error occurred during login. Please try again or contact support if the problem persists.";
                            break;
                    }
                    
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in Phase2Login: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                
                // Log error (in production, use proper logging)
                model.ErrorMessage = "A system error occurred during login. Please try again or contact support if the problem persists.";
                return View(model);
            }
        }

        /// <summary>
        /// GET: Change Password page
        /// </summary>
        [Authorize]
        public ActionResult ChangePassword()
        {
            // Validate session
            if (!_sessionService.ValidateSession())
            {
                return RedirectToAction("Phase1Login");
            }

            var model = new ChangePasswordViewModel();
            
            // Show info message if available
            if (TempData["InfoMessage"] != null)
            {
                ViewBag.InfoMessage = TempData["InfoMessage"];
            }

            return View(model);
        }

        /// <summary>
        /// POST: Change Password - Forces password change after Phase 2 login with comprehensive validation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get user ID from session service
                var userId = _sessionService.GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return RedirectToAction("Phase1Login");
                }

                // Change password with comprehensive validation
                var result = _authService.ChangePasswordWithValidation(userId.Value, model.NewPassword, model.ConfirmPassword);
                
                if (result.IsSuccess)
                {
                    // Update session to reflect password change
                    HttpContext.Session.SetString("MustChangePassword", "false");
                    
                    // Clear the form
                    ModelState.Clear();
                    model.NewPassword = "";
                    model.ConfirmPassword = "";

                    // Redirect to home after successful password change
                    TempData["SuccessMessage"] = "Password changed successfully. Welcome to HEMS!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Handle different error types with appropriate user messages
                    switch (result.ErrorType)
                    {
                        case AuthenticationErrorType.ValidationError:
                            model.ErrorMessage = result.ErrorMessage;
                            break;
                        case AuthenticationErrorType.ConfirmationMismatch:
                            model.ErrorMessage = "The password and confirmation password do not match. Please try again.";
                            break;
                        case AuthenticationErrorType.PasswordTooShort:
                        case AuthenticationErrorType.PasswordTooLong:
                        case AuthenticationErrorType.PasswordMissingRequiredCharacters:
                        case AuthenticationErrorType.WeakPassword:
                            model.ErrorMessage = result.ErrorMessage;
                            ViewBag.PasswordRequirements = GetPasswordRequirements();
                            break;
                        case AuthenticationErrorType.PasswordReuse:
                            model.ErrorMessage = result.ErrorMessage;
                            break;
                        case AuthenticationErrorType.UserNotFound:
                            model.ErrorMessage = "User account not found. Please log in again.";
                            break;
                        case AuthenticationErrorType.SystemError:
                        default:
                            model.ErrorMessage = "An error occurred while changing password. Please try again or contact support if the problem persists.";
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Log error (in production, use proper logging)
                model.ErrorMessage = "A system error occurred while changing password. Please try again or contact support if the problem persists.";
            }

            return View(model);
        }

        /// <summary>
        /// POST: Logout - Terminates user session and invalidates login sessions
        /// </summary>
        public async Task<ActionResult> Logout()
        {
            try
            {
                // Get current user ID before clearing session
                var userId = _sessionService.GetCurrentUserId();
                
                if (userId.HasValue)
                {
                    // Invalidate all login sessions for the user
                    _authService.InvalidateAllUserSessions(userId.Value);
                }

                // Clear session using session service
                _sessionService.ClearSession();

                // Clear session tokens
                HttpContext.Session.Remove("Phase1SessionToken");
                HttpContext.Session.Remove("Phase2SessionToken");
                HttpContext.Session.Remove("ExamSessionId");

                // Sign out from ASP.NET Core authentication
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception)
            {
                // Log error but continue with logout
            }

            // Redirect to Phase 1 login
            TempData["InfoMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Phase1Login");
        }

        /// <summary>
        /// Gets the client IP address from the request
        /// </summary>
        /// <returns>Client IP address</returns>
        private string GetClientIpAddress()
        {
            try
            {
                string? ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();

                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string[] addresses = ipAddress.Split(',');
                    if (addresses.Length != 0)
                    {
                        return addresses[0].Trim();
                    }
                }

                return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets password requirements for display to users
        /// </summary>
        /// <returns>List of password requirements</returns>
        private List<string> GetPasswordRequirements()
        {
            var requirements = new List<string>();
            
            requirements.Add($"At least {PasswordPolicy.MinimumLength} characters long");
            requirements.Add($"No more than {PasswordPolicy.MaximumLength} characters");
            
            if (PasswordPolicy.RequireUppercase)
                requirements.Add("At least one uppercase letter (A-Z)");
            
            if (PasswordPolicy.RequireLowercase)
                requirements.Add("At least one lowercase letter (a-z)");
            
            if (PasswordPolicy.RequireDigit)
                requirements.Add("At least one digit (0-9)");
            
            if (PasswordPolicy.RequireSpecialCharacter)
                requirements.Add("At least one special character (!@#$%^&*()_+-=[]{}|;':\"\\,.<>?/)");
            
            requirements.Add("Cannot be a common weak password");
            requirements.Add($"Cannot reuse any of your last {PasswordPolicy.PasswordHistoryCount} passwords");
            
            return requirements;
        }

        /// <summary>
        /// GET: Check authentication status (for AJAX calls)
        /// </summary>
        public JsonResult CheckAuthStatus()
        {
            return Json(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                Phase1Completed = _sessionService.IsPhase1Completed(),
                Phase2Completed = _sessionService.IsPhase2Completed(),
                UserId = _sessionService.GetCurrentUserId(),
                MustChangePassword = _sessionService.MustChangePassword(),
                UserRole = _sessionService.GetCurrentUser()?.Role?.RoleName
            });
        }



    }
}
