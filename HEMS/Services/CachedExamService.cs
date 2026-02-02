using HEMS.Models;
using System.Collections.Generic;

namespace HEMS.Services
{
    public class CachedExamService
    {
        private readonly IExamService _examService;
        private readonly ICacheService _cacheService;

        public CachedExamService(IExamService examService, ICacheService cacheService)
        {
            _examService = examService;
            _cacheService = cacheService;
        }

        public Exam GetExamById(int examId)
        {
            string cacheKey = $"exam_{examId}";
            var cachedExam = _cacheService.Get<Exam>(cacheKey);
            
            if (cachedExam == null)
            {
                // Implementation would call _examService.GetExamById(examId)
                // and cache the result
            }
            
            return cachedExam;
        }
    }
}