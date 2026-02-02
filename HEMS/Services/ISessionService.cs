using System;
using HEMS.Models;

namespace HEMS.Services
{
    public interface ISessionService
    {
        LoginSession CreateSession(string userId);
        LoginSession CreateUserSession(User user);
        LoginSession CreateUserSession(User user, bool phase1Completed = false, bool phase2Completed = false);
        bool ValidateSession(string sessionId);
        bool ValidateSession();
        void InvalidateSession(string sessionId);
        void ExtendSession(string sessionId);
        LoginSession GetSession(string sessionId);
        void CleanupExpiredSessions();
        bool IsSessionActive(string sessionId);
        void UpdateSessionActivity(string sessionId);
        User GetCurrentUser();
        int? GetCurrentUserId();
        bool IsPhase1Completed();
        bool IsPhase2Completed();
        bool MustChangePassword();
        void ClearSession();
    }
}