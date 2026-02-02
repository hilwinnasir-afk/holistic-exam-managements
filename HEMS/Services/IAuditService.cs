using System;
using System.Collections.Generic;
using HEMS.Models;

namespace HEMS.Services
{
    public interface IAuditService
    {
        void LogAction(string action, string userId, string details = null);
        void LogExamAccess(int examId, string userId, string action);
        void LogAuthenticationAttempt(string email, bool success, string ipAddress = null);
        void LogSystemEvent(string eventType, string details);
        void LogExamEvent(string eventType, string details);
        List<AuditLog> GetAuditLogs(DateTime? startDate = null, DateTime? endDate = null);
        List<AuditLog> GetUserAuditLogs(string userId, DateTime? startDate = null, DateTime? endDate = null);
        void ArchiveOldLogs(DateTime cutoffDate);
    }
}