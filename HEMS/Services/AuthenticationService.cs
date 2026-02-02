using System;
using System.Linq;
using HEMS.Models;

namespace HEMS.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HEMSContext _context;
        
        public AuthenticationService() 
        { 
            _context = new HEMSContext(); 
        }
        
        public AuthenticationService(HEMSContext context) 
        { 
            _context = context; 
        }

        public string CalculatePhase1Password(string idNumber)
        {
            if (string.IsNullOrEmpty(idNumber)) return string.Empty;
            
            // Get first 4 characters of ID number
            string idPart = idNumber.Length <= 4 ? idNumber : idNumber.Substring(0, 4);
            
            // Get current Ethiopian year (Ethiopian calendar is 7-8 years behind Gregorian)
            // Current Gregorian year 2026 corresponds to Ethiopian year 2018-2019
            // We'll use 18 as the last 2 digits for 2018
            string ethiopianYearSuffix = "18";
            
            return idPart + ethiopianYearSuffix;
        }

        public bool ValidatePhase1Login(string email, string password)
        {
            // Find user by email (university email)
            var user = _context.Users.FirstOrDefault(u => u.Username == email);
            if (user == null) return false;
            
            // Find associated student record
            var student = _context.Students.FirstOrDefault(s => s.UserId == user.UserId);
            if (student == null) return false;
            
            // For students, validate against the calculated password (first 4 digits of ID + last 2 digits of Ethiopian year)
            if (user.Role?.RoleName == "Student")
            {
                var expectedPassword = CalculatePhase1Password(student.IdNumber);
                return !user.LoginPhaseCompleted && expectedPassword == password;
            }
            
            // For other users, validate against stored password hash
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception)
            {
                // Fallback to plain text comparison for testing
                return user.PasswordHash == password;
            }
        }

        public User GetUserByEmail(string email) 
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] GetUserByEmail called with email: {email}");
                
                var user = _context.Users.FirstOrDefault(u => u.Username == email);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] User query result: {user != null}");
                
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] User found - UserId: {user.UserId}, Username: {user.Username}, RoleId: {user.RoleId}");
                    
                    // Load the role separately to ensure it's properly loaded
                    user.Role = _context.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Role loaded: {user.Role != null}, RoleName: {user.Role?.RoleName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] No user found for email: {email}");
                    
                    // Debug: Let's see what users exist in the database
                    var allUsers = _context.Users.Select(u => new { u.Username, u.UserId }).ToList();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] All users in database: {string.Join(", ", allUsers.Select(u => $"{u.Username}({u.UserId})"))}");
                }
                
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in GetUserByEmail: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        public bool CompletePhase1Login(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return false;
            user.LoginPhaseCompleted = true;
            _context.SaveChanges();
            return true;
        }

        public bool ValidatePhase2Login(string idNumber, string plaintextPassword)
        {
            // Find any active exam session where the session password matches the provided plaintext.
            var sessions = _context.ExamSessions.Where(es => es.IsActive && es.ExpiryDate > DateTime.Now).ToList();
            foreach (var s in sessions)
            {
                if (VerifyHash(s.SessionPassword, plaintextPassword)) return true;
            }
            return false;
        }

        public bool ChangePassword(int userId, string newPassword)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return false;
            // Try to hash password using BCrypt if available at runtime
            var hashed = TryHashPassword(newPassword) ?? newPassword;
            user.PasswordHash = hashed;
            user.MustChangePassword = false;
            _context.SaveChanges();
            return true;
        }

        private static bool VerifyHash(string hashed, string plain)
        {
            try
            {
                var bcryptType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
                        try { return a.GetTypes(); } catch { return new Type[0]; }
                    })
                    .FirstOrDefault(t => t.FullName == "BCrypt.Net.BCrypt");
                if (bcryptType != null)
                {
                    var method = bcryptType.GetMethod("Verify", new[] { typeof(string), typeof(string) });
                    if (method != null) return (bool)method.Invoke(null, new object[] { plain, hashed });
                }
            }
            catch { }
            // Fallback: direct compare (useful for tests that store plain text)
            return hashed == plain;
        }

        private static string TryHashPassword(string plain)
        {
            try
            {
                var bcryptType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                    .FirstOrDefault(t => t.FullName == "BCrypt.Net.BCrypt");
                if (bcryptType != null)
                {
                    var method = bcryptType.GetMethod("HashPassword", new[] { typeof(string) });
                    if (method != null) return (string)method.Invoke(null, new object[] { plain });
                }
            }
            catch { }
            return null;
        }

        // Implementation of IAuthenticationService interface methods
        public AuthenticationResult ValidatePhase1LoginWithDetails(string email, string password, string ipAddress, string userAgent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ValidatePhase1LoginWithDetails called - Email: {email}, Password Length: {password?.Length ?? 0}");
                
                var user = GetUserByEmail(email);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] GetUserByEmail result - User found: {user != null}, Username: {user?.Username}, Role: {user?.Role?.RoleName}");
                
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] User not found for email: {email}");
                    return AuthenticationResult.Failure(AuthenticationErrorType.UserNotFound, "Invalid email or password");
                }

                // For students, validate against calculated password
                if (user.Role?.RoleName == "Student")
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Processing student login for user: {user.Username}");
                    
                    var student = _context.Students.FirstOrDefault(s => s.UserId == user.UserId);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Student record found: {student != null}, IdNumber: {student?.IdNumber}");
                    
                    if (student == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Student record not found for UserId: {user.UserId}");
                        return AuthenticationResult.Failure(AuthenticationErrorType.StudentRecordNotFound, "Student record not found");
                    }

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Checking LoginPhaseCompleted: {user.LoginPhaseCompleted}");
                    if (user.LoginPhaseCompleted)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase 1 already completed for user: {user.Username}");
                        
                        // Check if it's exam time now
                        if (IsExamTimeNowPrivate())
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Exam time detected - allowing redirect to Phase 2");
                            return AuthenticationResult.Success(user, "Phase 1 completed. Redirecting to Phase 2 login.");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Not exam time - showing exam day message");
                        return AuthenticationResult.Failure(AuthenticationErrorType.Phase1AlreadyCompleted, "Identity verification already completed. Please login on the exam date.");
                    }

                    var expectedPassword = CalculatePhase1Password(student.IdNumber);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Password validation - Expected: {expectedPassword}, Provided: {password}, Match: {expectedPassword == password}");
                    
                    if (expectedPassword != password)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Password mismatch for student: {student.IdNumber}");
                        return AuthenticationResult.Failure(AuthenticationErrorType.IncorrectPassword, "Invalid password");
                    }

                    // Complete Phase 1 login for students
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Completing Phase 1 login for user: {user.UserId}");
                    CompletePhase1Login(user.UserId);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Processing non-student login for role: {user.Role?.RoleName}");
                    
                    // For coordinators and other users, validate against stored password hash
                    bool passwordValid = false;
                    try
                    {
                        passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] BCrypt password validation result: {passwordValid}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] BCrypt failed, using fallback: {ex.Message}");
                        // Fallback to plain text comparison for testing
                        passwordValid = (user.PasswordHash == password);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Fallback password validation result: {passwordValid}");
                    }
                    
                    if (!passwordValid)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Password validation failed for user: {user.Username}");
                        return AuthenticationResult.Failure(AuthenticationErrorType.IncorrectPassword, "Invalid password");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Authentication successful for user: {user.Username}");
                return AuthenticationResult.Success(user, "Login successful");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in ValidatePhase1LoginWithDetails: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                return AuthenticationResult.Failure(AuthenticationErrorType.SystemError, $"System error: {ex.Message}");
            }
        }

        public AuthenticationResult ValidatePhase2LoginWithDetails(string idNumber, string password, string ipAddress, string userAgent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ValidatePhase2LoginWithDetails called - IdNumber: {idNumber}, Password Length: {password?.Length ?? 0}");
                
                // Find student by ID number
                var student = _context.Students.FirstOrDefault(s => s.IdNumber == idNumber);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Student lookup result - Student found: {student != null}, IdNumber: {student?.IdNumber}");
                
                if (student == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Student not found for IdNumber: {idNumber}");
                    return AuthenticationResult.Failure(AuthenticationErrorType.UserNotFound, "Invalid student ID or session password");
                }

                // Get the associated user
                var user = _context.Users.FirstOrDefault(u => u.UserId == student.UserId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] User lookup result - User found: {user != null}, Username: {user?.Username}");
                
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] User not found for student UserId: {student.UserId}");
                    return AuthenticationResult.Failure(AuthenticationErrorType.UserNotFound, "User account not found");
                }

                // Load the role
                user.Role = _context.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Role loaded: {user.Role != null}, RoleName: {user.Role?.RoleName}");

                // Check if Phase 1 is completed
                if (!user.LoginPhaseCompleted)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase 1 not completed for user: {user.Username}");
                    return AuthenticationResult.Failure(AuthenticationErrorType.Phase1NotCompleted, "Please complete Phase 1 identity verification first");
                }

                // Check for active exam sessions and validate session password
                var activeSessions = _context.ExamSessions.Where(es => es.IsActive && es.ExpiryDate > DateTime.Now).ToList();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Active exam sessions found: {activeSessions.Count}");
                
                if (!activeSessions.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] No active exam sessions found");
                    return AuthenticationResult.Failure(AuthenticationErrorType.ExamSessionNotFound, "No active exam session found. Please contact your coordinator.");
                }

                // Try to validate the session password against any active session
                bool passwordValid = false;
                ExamSession validSession = null;
                
                foreach (var session in activeSessions)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Checking session {session.ExamSessionId}, ExpiryDate: {session.ExpiryDate}");
                    
                    try
                    {
                        // Try BCrypt verification first
                        if (BCrypt.Net.BCrypt.Verify(password, session.SessionPassword))
                        {
                            passwordValid = true;
                            validSession = session;
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] BCrypt password match found for session {session.ExamSessionId}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] BCrypt verification failed for session {session.ExamSessionId}: {ex.Message}");
                        
                        // Fallback to plain text comparison
                        if (session.SessionPassword == password)
                        {
                            passwordValid = true;
                            validSession = session;
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] Plain text password match found for session {session.ExamSessionId}");
                            break;
                        }
                    }
                }

                if (!passwordValid || validSession == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Session password validation failed for all active sessions");
                    return AuthenticationResult.Failure(AuthenticationErrorType.IncorrectExamPassword, "Invalid session password");
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Phase 2 authentication successful for user: {user.Username}, Session: {validSession.ExamSessionId}");
                
                // Return success with user information and exam session ID
                var result = AuthenticationResult.Success(user, "Phase 2 login successful");
                result.ExamSessionId = validSession.ExamSessionId;
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in ValidatePhase2LoginWithDetails: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                return AuthenticationResult.Failure(AuthenticationErrorType.SystemError, $"System error: {ex.Message}");
            }
        }

        public AuthenticationResult ChangePasswordWithValidation(int userId, string newPassword, string confirmPassword)
        {
            try
            {
                if (newPassword != confirmPassword)
                {
                    return AuthenticationResult.Failure(AuthenticationErrorType.ConfirmationMismatch, "Password and confirmation do not match");
                }

                if (ChangePassword(userId, newPassword))
                {
                    var user = _context.Users.Find(userId);
                    if (user != null)
                    {
                        return AuthenticationResult.Success(user, "Password changed successfully");
                    }
                    else
                    {
                        return AuthenticationResult.Failure(AuthenticationErrorType.SystemError, "User not found after password change");
                    }
                }
                else
                {
                    return AuthenticationResult.Failure(AuthenticationErrorType.SystemError, "Failed to change password");
                }
            }
            catch (Exception ex)
            {
                return AuthenticationResult.Failure(AuthenticationErrorType.SystemError, $"System error: {ex.Message}");
            }
        }

        public void InvalidateAllUserSessions(int userId)
        {
            try
            {
                var loginSessions = _context.LoginSessions.Where(ls => ls.UserId == userId && ls.IsActive).ToList();
                foreach (var session in loginSessions)
                {
                    session.IsActive = false;
                    session.EndTime = DateTime.Now;
                }
                _context.SaveChanges();
            }
            catch (Exception)
            {
                // Log error in production
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        /// <summary>
        /// Check if current time is within an active exam session
        /// For now, using ExpiryDate as a simple check until we add proper exam scheduling
        /// </summary>
        private bool IsExamTimeNowPrivate()
        {
            var now = DateTime.Now;
            
            // Simple check: if there's an active exam session that hasn't expired
            var activeSession = _context.ExamSessions
                .FirstOrDefault(es =>
                    es.IsActive &&
                    (es.ExpiryDate == null || es.ExpiryDate > now)
                );
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] IsExamTimeNow check - Current time: {now}");
            if (activeSession != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Active exam session found - ExpiryDate: {activeSession.ExpiryDate}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] No active exam session found for current time");
            }
            
            return activeSession != null;
        }

        /// <summary>
        /// Public method to check exam time (for controller access)
        /// </summary>
        public bool IsExamTimeNow()
        {
            return IsExamTimeNowPrivate();
        }
    }
}
