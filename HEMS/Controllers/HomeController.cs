using System.Diagnostics;
using System;
using System.Linq;
using HEMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HEMS.Attributes;
using Microsoft.AspNetCore.Http;

namespace HEMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly HEMSContext _context;

        public HomeController(HEMSContext context)
        {
            _context = context;
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Privacy()
        {
            return View();
        }

        public ActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var model = new ErrorViewModel { RequestId = requestId };
            return View(model);
        }

        [Authorize]
        [RoleAuthorize(Roles = "Student")]
        public ActionResult StudentDashboard()
        {
            return View();
        }

        [Authorize]
        [CoordinatorAuthorize]
        public ActionResult CoordinatorDashboard()
        {
            return View();
        }

        public ActionResult ExamAccess()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Loading ExamAccess page");

                List<Exam> availableExams = new List<Exam>();

                // Check if user logged in via Phase 2 with a session password
                var examSessionId = HttpContext.Session.GetInt32("ExamSessionId");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExamAccess - ExamSessionId from session: {examSessionId}");

                if (examSessionId.HasValue && examSessionId.Value > 0)
                {
                    // User logged in with session password - show only the exam for that session
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Session-based access detected for ExamSessionId: {examSessionId.Value}");
                    
                    var examSession = _context.ExamSessions
                        .Include(es => es.Exam)
                        .ThenInclude(e => e.Questions)
                        .FirstOrDefault(es => es.ExamSessionId == examSessionId.Value);

                    if (examSession != null && examSession.Exam != null && examSession.Exam.IsPublished)
                    {
                        availableExams.Add(examSession.Exam);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Session exam found: ID={examSession.Exam.ExamId}, Title='{examSession.Exam.Title}', Questions={examSession.Exam.Questions?.Count ?? 0}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] No valid exam found for session {examSessionId.Value} or exam not published");
                        ViewBag.ErrorMessage = "Invalid session or exam not available. Please contact your coordinator.";
                    }
                }
                else
                {
                    // No session-based login - show all published exams (fallback for non-session logins)
                    System.Diagnostics.Debug.WriteLine("[DEBUG] No ExamSessionId found - showing all published exams");
                    
                    availableExams = _context.Exams
                        .Include(e => e.Questions)
                        .Where(e => e.IsPublished)
                        .OrderBy(e => e.CreatedDate)
                        .ToList();
                }

                // Debug logging
                var totalExams = _context.Exams.Count();
                var availableCount = availableExams.Count;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExamAccess - Total exams in database: {totalExams}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExamAccess - Available exams count: {availableCount}");
                
                if (availableExams.Any())
                {
                    foreach (var exam in availableExams)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Available exam: ID={exam.ExamId}, Title='{exam.Title}', Questions={exam.Questions?.Count ?? 0}, IsPublished={exam.IsPublished}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] No available exams found");
                }

                // Create dummy student info for demo
                var dummyStudent = new Student
                {
                    IdNumber = "ST001",
                    UniversityEmail = "student@university.edu.et",
                    BatchYear = "2025"
                };

                // Pass data to the view
                ViewBag.PublishedExams = availableExams;
                ViewBag.Student = dummyStudent;
                ViewBag.Message = TempData["SuccessMessage"]?.ToString() ?? "Authentication completed successfully!";

                return View();
            }
            catch (Exception ex)
            {
                // Log error and show fallback view
                System.Diagnostics.Debug.WriteLine($"[ERROR] ExamAccess exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Unable to load exam information. Please try again or contact support.";
                
                // Provide empty data to prevent view errors
                ViewBag.PublishedExams = new List<Exam>();
                ViewBag.Student = new Student
                {
                    IdNumber = "ST001",
                    UniversityEmail = "student@university.edu.et",
                    BatchYear = "2025"
                };
                
                return View();
            }
        }
    }
}
