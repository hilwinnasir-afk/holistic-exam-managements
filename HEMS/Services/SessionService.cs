using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using HEMS.Models;

namespace HEMS.Services
{
    public class SessionService : ISessionService
    {
        private readonly HEMSContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionService(HEMSContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public SessionService(HEMSContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = null!; // This will be null in test scenarios
        }

        public LoginSession CreateSession(string userId)
        {
            if (!int.TryParse(userId, out int userIdInt))
                return null;

            var session = new LoginSession
            {
                SessionToken = Guid.NewGuid().ToString(),
                UserId = userIdInt,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddHours(8), // 8 hour session
                IsActive = true,
                LoginTime = DateTime.Now,
                LoginPhase = 1,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetClientUserAgent()
            };

            _context.LoginSessions.Add(session);
            _context.SaveChanges();

            return session;
        }

        public LoginSession CreateUserSession(User user)
        {
            return CreateUserSession(user, false, false);
        }

        public LoginSession CreateUserSession(User user, bool phase1Completed = false, bool phase2Completed = false)
        {
            var session = new LoginSession
            {
                SessionToken = Guid.NewGuid().ToString(),
                UserId = user.UserId,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddHours(8),
                IsActive = true,
                LoginTime = DateTime.Now,
                LoginPhase = phase2Completed ? 2 : (phase1Completed ? 1 : 0),
                IpAddress = GetClientIpAddress(),
                UserAgent = GetClientUserAgent()
            };

            _context.LoginSessions.Add(session);
            _context.SaveChanges();

            // Store session token in HTTP session
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.Session != null)
            {
                httpContext.Session.SetString("SessionToken", session.SessionToken);
                httpContext.Session.SetInt32("UserId", user.UserId);
            }

            return session;
        }

        public bool ValidateSession()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var sessionToken = httpContext?.Session?.GetString("SessionToken");
            if (!string.IsNullOrEmpty(sessionToken))
            {
                return ValidateSession(sessionToken);
            }
            return false;
        }

        public bool ValidateSession(string sessionId)
        {
            var session = GetSession(sessionId);
            return session != null && session.IsActive && 
                   (!session.ExpiryDate.HasValue || session.ExpiryDate > DateTime.Now);
        }

        public void InvalidateSession(string sessionId)
        {
            var session = _context.LoginSessions.FirstOrDefault(s => s.SessionToken == sessionId);
            if (session != null)
            {
                session.IsActive = false;
                session.LogoutTime = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public void ExtendSession(string sessionId)
        {
            var session = _context.LoginSessions.FirstOrDefault(s => s.SessionToken == sessionId);
            if (session != null && session.IsActive)
            {
                session.ExpiryDate = DateTime.Now.AddHours(8);
                _context.SaveChanges();
            }
        }

        public LoginSession GetSession(string sessionId)
        {
            return _context.LoginSessions.FirstOrDefault(s => s.SessionToken == sessionId);
        }

        public void CleanupExpiredSessions()
        {
            var expiredSessions = _context.LoginSessions
                .Where(s => s.ExpiryDate < DateTime.Now || !s.IsActive)
                .ToList();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                if (!session.LogoutTime.HasValue)
                {
                    session.LogoutTime = DateTime.Now;
                }
            }

            _context.SaveChanges();
        }

        public bool IsSessionActive(string sessionId)
        {
            return ValidateSession(sessionId);
        }

        public void UpdateSessionActivity(string sessionId)
        {
            var session = _context.LoginSessions.FirstOrDefault(s => s.SessionToken == sessionId);
            if (session != null && session.IsActive)
            {
                // Update last activity - could add a LastActivity field to model if needed
                _context.SaveChanges();
            }
        }

        public User GetCurrentUser()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var userId = httpContext?.Session?.GetInt32("UserId");
            if (userId.HasValue)
            {
                return _context.Users.Find(userId.Value);
            }
            return null;
        }

        public int? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            return httpContext?.Session?.GetInt32("UserId");
        }

        public bool IsPhase1Completed()
        {
            var user = GetCurrentUser();
            return user?.LoginPhaseCompleted == true;
        }

        public bool IsPhase2Completed()
        {
            var user = GetCurrentUser();
            return user?.LoginPhaseCompleted == true; // Assuming phase 2 is the final phase
        }

        public bool MustChangePassword()
        {
            var user = GetCurrentUser();
            return user?.MustChangePassword == true;
        }

        public void ClearSession()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.Session != null)
            {
                var sessionToken = httpContext.Session.GetString("SessionToken");
                if (!string.IsNullOrEmpty(sessionToken))
                {
                    InvalidateSession(sessionToken);
                }
                httpContext.Session.Clear();
            }
        }

        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.Request != null)
            {
                var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',')[0].Trim();
                }
                return httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            }
            return "127.0.0.1";
        }

        private string GetClientUserAgent()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.Request != null)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
                return !string.IsNullOrEmpty(userAgent) ? userAgent : "Unknown";
            }
            return "Unknown";
        }
    }
}