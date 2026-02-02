using System.Collections.Generic;

namespace HEMS.Models
{
    public class ExamQuestionViewModel
    {
        public Question Question { get; set; }
        public int? SelectedChoiceId { get; set; }
        public bool IsFlagged { get; set; }
        public List<Question> AllQuestions { get; set; }
        public List<StudentAnswer> StudentAnswers { get; set; }
        public int CurrentQuestionId { get; set; }
        public int StudentExamId { get; set; }
    }
}