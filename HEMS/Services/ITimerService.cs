using System;
using HEMS.Models;

namespace HEMS.Services
{
    public interface ITimerService
    {
        TimeSpan GetRemainingTime(int examSessionId);
        bool IsTimeExpired(int examSessionId);
        bool IsExamExpired(int studentExamId);
        void StartTimer(int examSessionId);
        void StopTimer(int examSessionId);
        void ExtendTime(int examSessionId, TimeSpan additionalTime);
        SecureTimestamp GetSecureTimestamp();
        SecureTimestamp GetSecureTimestamp(int studentExamId);
        bool ValidateTimestamp(SecureTimestamp timestamp);
        bool ValidateTimestampHash(int studentExamId, DateTime timestamp, string hash);
        bool ValidateExamTimeIntegrity(int studentExamId);
        bool DetectSuspiciousTimingActivity(int studentExamId);
    }
}