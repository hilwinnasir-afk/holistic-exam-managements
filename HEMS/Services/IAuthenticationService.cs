using HEMS.Models;

namespace HEMS.Services
{
    public interface IAuthenticationService
    {
        AuthenticationResult ValidatePhase1LoginWithDetails(string email, string password, string ipAddress, string userAgent);
        AuthenticationResult ValidatePhase2LoginWithDetails(string idNumber, string password, string ipAddress, string userAgent);
        AuthenticationResult ChangePasswordWithValidation(int userId, string newPassword, string confirmPassword);
        void InvalidateAllUserSessions(int userId);
        User GetUserByEmail(string email);
        bool IsExamTimeNow();
    }
}