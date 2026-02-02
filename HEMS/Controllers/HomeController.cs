using System.Diagnostics;
using System;
using System.Linq;
using HEMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HEMS.Attributes;

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
                // DEMO MODE: Get all published exams without authentication checks
                var publishedExams = _context.Exams
                    .Where(e => e.IsPublished)
                    .OrderBy(e => e.CreatedDate)
                    .ToList();

                // Debug logging
                var totalExams = _context.Exams.Count();
                var publishedCount = publishedExams.Count;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExamAccess - Total exams in database: {totalExams}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExamAccess - Published exams count: {publishedCount}");
                
                if (publishedExams.Any())
                {
                    foreach (var exam in publishedExams)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Published exam: ID={exam.ExamId}, Title='{exam.Title}', IsPublished={exam.IsPublished}");
                    }
                }

                // DEMO MODE: Create dummy student info
                var dummyStudent = new Student
                {
                    IdNumber = "ST001",
                    UniversityEmail = "student@university.edu.et",
                    BatchYear = "Year IV Sem II"
                };

                // Pass data to the view
                ViewBag.PublishedExams = publishedExams;
                ViewBag.Student = dummyStudent;
                ViewBag.Message = TempData["SuccessMessage"]?.ToString() ?? "Authentication completed successfully!";

                return View();
            }
            catch (Exception ex)
            {
                // Log error and show fallback view
                System.Diagnostics.Debug.WriteLine($"[ERROR] ExamAccess exception: {ex.Message}");
                ViewBag.ErrorMessage = "Unable to load exam information. Please try again or contact support.";
                return View();
            }
        }
    }
}
