using System;
using System.Linq;
using HEMS.Models;

namespace HEMS.Services
{
    public class TimerService : ITimerService
    {
        private readonly HEMSContext _context;
        
        public TimerService(HEMSContext context) 
        { 
            _context = context; 
        }

        public TimeSpan GetRemainingTime(int examSessionId)
        {
            var studentExam = _context.StudentExams.Find(examSessionId);
            if (studentExam == null) return TimeSpan.Zero;
            
            var exam = _context.Exams.Find(studentExam.ExamId);
            if (exam == null) return TimeSpan.Zero;
            
            var duration = TimeSpan.FromMinutes(exam.DurationMinutes);
            var elapsed = DateTime.Now - studentExam.StartDateTime;
            var remaining = duration - elapsed;
            
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public bool IsTimeExpired(int examSessionId)
        {
            return GetRemainingTime(examSessionId) <= TimeSpan.Zero;
        }

        public bool IsExamExpired(int studentExamId)
        {
            return IsTimeExpired(studentExamId);
        }

        public void StartTimer(int examSessionId)
        {
            var studentExam = _context.StudentExams.Find(examSessionId);
            if (studentExam != null && studentExam.StartDateTime == null)
            {
                studentExam.StartDateTime = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public void StopTimer(int examSessionId)
        {
            // Implementation for stopping timer
        }

        public void ExtendTime(int examSessionId, TimeSpan additionalTime)
        {
            // Implementation for extending time
        }

        public SecureTimestamp GetSecureTimestamp()
        {
            return new SecureTimestamp
            {
                ServerTime = DateTime.Now,
                Hash = GenerateHash(DateTime.Now.ToString())
            };
        }

        public SecureTimestamp GetSecureTimestamp(int studentExamId)
        {
            var remaining = GetRemainingTime(studentExamId);
            return new SecureTimestamp
            {
                ServerTime = DateTime.Now,
                Hash = GenerateHash($"{DateTime.Now}_{studentExamId}"),
                RemainingTime = remaining,
                IsExpired = remaining <= TimeSpan.Zero
            };
        }

        public bool ValidateTimestamp(SecureTimestamp timestamp)
        {
            var expectedHash = GenerateHash(timestamp.ServerTime.ToString());
            return timestamp.Hash == expectedHash;
        }

        public bool ValidateTimestampHash(int studentExamId, DateTime timestamp, string hash)
        {
            var expectedHash = GenerateHash($"{timestamp}_{studentExamId}");
            return hash == expectedHash;
        }

        public bool ValidateExamTimeIntegrity(int studentExamId)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null) return false;
            
            var exam = _context.Exams.Find(studentExam.ExamId);
            if (exam == null) return false;
            
            var elapsed = DateTime.Now - studentExam.StartDateTime;
            var maxAllowedTime = TimeSpan.FromMinutes(exam.DurationMinutes + 5); // 5 minute buffer
            
            return elapsed <= maxAllowedTime;
        }

        public bool DetectSuspiciousTimingActivity(int studentExamId)
        {
            // Simple implementation - could be enhanced with more sophisticated detection
            return !ValidateExamTimeIntegrity(studentExamId);
        }

        private string GenerateHash(string input)
        {
            // Simple hash implementation - in production, use proper cryptographic hash
            return input.GetHashCode().ToString();
        }
    }
}
