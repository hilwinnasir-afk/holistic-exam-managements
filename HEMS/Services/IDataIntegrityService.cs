using System;
using System.Collections.Generic;
using HEMS.Models;

namespace HEMS.Services
{
    public interface IDataIntegrityService
    {
        bool ValidateExamIntegrity(int examId);
        bool ValidateStudentExamIntegrity(int studentExamId);
        ValidationResult ValidateAnswerIntegrity(int studentExamId, int questionId, int? choiceId);
        ValidationResult ValidateExamSubmissionIntegrity(int studentExamId);
        bool ExecuteInTransaction(Func<bool> operation);
        List<DataIntegrityIssue> CheckDatabaseIntegrity();
        bool RepairDataIntegrityIssue(int issueId);
        void BackupCriticalData();
        bool RestoreFromBackup(string backupId);
    }

    public class DataIntegrityIssue
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string TableName { get; set; }
        public string RecordId { get; set; }
        public bool CanAutoRepair { get; set; }
    }
}