namespace HEMS.Models
{
    public class GradingResult
    {
        public int StudentExamId { get; set; }
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public double Percentage { get; set; }
        public string Grade { get; set; }
        public bool IsValid { get; set; }
        public string[] Errors { get; set; }

        public GradingResult()
        {
            Errors = new string[0];
        }
    }
}