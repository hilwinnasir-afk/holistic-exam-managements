using HEMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEMS.Services
{
    public class AuditService : IAuditService
    {
        private readonly HEMSContext _context;
        
        public AuditService(HEMSContext context) 
        { 
            _context = context; 
        }
        
        public void LogAction(string action, string userId, string details = null)
        {
            _context.AuditLogs.Add(new AuditLog 
            { 
                EventType = action, 
                Description = details ?? action,
                UserId = int.TryParse(userId, out int id) ? id : (int?)null,
                Timestamp = DateTime.UtcNow,
                Severity = AuditSeverity.Info
            });
            _context.SaveChanges();
        }

        public void LogExamAccess(int examId, string userId, string action)
        {
            _context.AuditLogs.Add(new AuditLog 
            { 
                EventType = action, 
                Description = $"Exam {examId} accessed by user {userId}",
                ExamId = examId,
                UserId = int.TryParse(userId, out int id) ? id : (int?)null,
                Timestamp = DateTime.UtcNow,
                Severity = AuditSeverity.Info
            });
            _context.SaveChanges();
        }

        public void LogAuthenticationAttempt(string email, bool success, string ipAddress = null)
        {
            _context.AuditLogs.Add(new AuditLog 
            { 
                EventType = success ? AuditEventTypes.UserLogin : "LOGIN_FAILED", 
                Description = $"Authentication attempt for {email}: {(success ? "Success" : "Failed")}",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                Severity = success ? AuditSeverity.Info : AuditSeverity.Warning
            });
            _context.SaveChanges();
        }

        public void LogSystemEvent(string eventType, string details)
        {
            _context.AuditLogs.Add(new AuditLog 
            { 
                EventType = eventType, 
                Description = details,
                Timestamp = DateTime.UtcNow,
                Severity = AuditSeverity.Info
            });
            _context.SaveChanges();
        }

        public void LogExamEvent(string eventType, string details)
        {
            _context.AuditLogs.Add(new AuditLog 
            { 
                EventType = eventType, 
                Description = details,
                Timestamp = DateTime.UtcNow,
                Severity = AuditSeverity.Info
            });
            _context.SaveChanges();
        }

        public List<AuditLog> GetAuditLogs(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();
            
            if (startDate.HasValue)
                query = query.Where(log => log.Timestamp >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(log => log.Timestamp <= endDate.Value);
                
            return query.OrderByDescending(log => log.Timestamp).ToList();
        }

        public List<AuditLog> GetUserAuditLogs(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!int.TryParse(userId, out int id))
                return new List<AuditLog>();
                
            var query = _context.AuditLogs.Where(log => log.UserId == id);
            
            if (startDate.HasValue)
                query = query.Where(log => log.Timestamp >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(log => log.Timestamp <= endDate.Value);
                
            return query.OrderByDescending(log => log.Timestamp).ToList();
        }

        public void ArchiveOldLogs(DateTime cutoffDate)
        {
            var oldLogs = _context.AuditLogs.Where(log => log.Timestamp < cutoffDate);
            _context.AuditLogs.RemoveRange(oldLogs);
            _context.SaveChanges();
        }
    }
}
