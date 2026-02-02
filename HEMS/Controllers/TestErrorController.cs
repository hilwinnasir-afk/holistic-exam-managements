using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HEMS.Attributes;

namespace HEMS.Controllers
{
    /// <summary>
    /// Test controller to demonstrate error page functionality
    /// This controller should be removed in production
    /// Restricted to coordinators only for testing purposes
    /// </summary>
    [Authorize]
    [CoordinatorAuthorize]
    public class TestErrorController : Controller
    {
        /// <summary>
        /// Test 404 Not Found error page
        /// </summary>
        public ActionResult Test404()
        {
            return RedirectToAction("NotFound", "Error");
        }

        /// <summary>
        /// Test 403 Access Denied error page
        /// </summary>
        public ActionResult Test403()
        {
            return RedirectToAction("AccessDenied", "Error", new { requiredRoles = "Coordinator", userRole = "Student" });
        }

        /// <summary>
        /// Test 500 Internal Server Error page
        /// </summary>
        public ActionResult Test500()
        {
            return RedirectToAction("InternalServerError", "Error", new { errorId = "TEST123" });
        }

        /// <summary>
        /// Test 503 Service Unavailable error page
        /// </summary>
        public ActionResult Test503()
        {
            return RedirectToAction("ServiceUnavailable", "Error");
        }

        /// <summary>
        /// Test Database Error page
        /// </summary>
        public ActionResult TestDatabase()
        {
            return RedirectToAction("DatabaseError", "Error");
        }

        /// <summary>
        /// Test Maintenance Mode page
        /// </summary>
        public ActionResult TestMaintenance()
        {
            return RedirectToAction("MaintenanceMode", "Error", new { 
                maintenanceStart = DateTime.Now.AddMinutes(-30),
                maintenanceEnd = DateTime.Now.AddMinutes(30)
            });
        }

        /// <summary>
        /// Test Session Timeout page
        /// </summary>
        public ActionResult TestSessionTimeout()
        {
            return RedirectToAction("SessionTimeout", "Error", new { 
                wasInExam = true,
                sessionDuration = "45 minutes",
                timeoutReason = "Inactivity"
            });
        }

        /// <summary>
        /// Test Exam System Failure page
        /// </summary>
        public ActionResult TestExamFailure()
        {
            return RedirectToAction("ExamSystemFailure", "Error", new { 
                errorType = "Critical System Error",
                errorId = "EXAM456",
                studentId = "ST001"
            });
        }

        /// <summary>
        /// Test Timer System Failure page
        /// </summary>
        public ActionResult TestTimerFailure()
        {
            return RedirectToAction("TimerSystemFailure", "Error", new { 
                lastKnownTime = "15:30 remaining",
                elapsedTime = "1 hour 15 minutes",
                errorId = "TIMER789"
            });
        }

        /// <summary>
        /// Test Submission Failure page
        /// </summary>
        public ActionResult TestSubmissionFailure()
        {
            return RedirectToAction("SubmissionFailure", "Error", new { 
                submissionAttempts = 3,
                errorType = "Network Timeout",
                errorId = "SUB101"
            });
        }

        /// <summary>
        /// Test Grading System Failure page
        /// </summary>
        public ActionResult TestGradingFailure()
        {
            return RedirectToAction("GradingSystemFailure", "Error", new { 
                submissionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                examTitle = "Software Engineering Final Exam",
                errorId = "GRADE202"
            });
        }

        /// <summary>
        /// Test page to show all available error page tests
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }
    }
}