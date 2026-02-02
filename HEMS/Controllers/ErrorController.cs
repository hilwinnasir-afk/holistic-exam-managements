using System;
using Microsoft.AspNetCore.Mvc;
using HEMS.Models;

namespace HEMS.Controllers
{
    /// <summary>
    /// Controller for handling various error scenarios and displaying appropriate error pages
    /// </summary>
    public class ErrorController : Controller
    {
        /// <summary>
        /// General error page for unhandled exceptions
        /// </summary>
        public ActionResult Index(string errorMessage = null, string errorId = null)
        {
            ViewBag.ErrorMessage = errorMessage;
            ViewBag.ErrorId = errorId ?? Guid.NewGuid().ToString("N")[..8];
            
            Response.StatusCode = 500;
            return View("Error");
        }

        /// <summary>
        /// 404 Not Found error page
        /// </summary>
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }

        /// <summary>
        /// 403 Forbidden/Access Denied error page
        /// </summary>
        public ActionResult AccessDenied(string requiredRoles = null, string userRole = null)
        {
            ViewData["RequiredRoles"] = requiredRoles;
            ViewData["UserRole"] = userRole;
            
            Response.StatusCode = 403;
            return View();
        }

        /// <summary>
        /// 500 Internal Server Error page
        /// </summary>
        public ActionResult InternalServerError(string errorId = null)
        {
            ViewBag.ErrorId = errorId ?? Guid.NewGuid().ToString("N")[..8];
            
            Response.StatusCode = 500;
            return View();
        }

        /// <summary>
        /// 503 Service Unavailable error page
        /// </summary>
        public ActionResult ServiceUnavailable()
        {
            Response.StatusCode = 503;
            return View();
        }

        /// <summary>
        /// 408 Request Timeout error page
        /// </summary>
        public ActionResult RequestTimeout()
        {
            Response.StatusCode = 408;
            return View();
        }

        /// <summary>
        /// Database connection failure error page
        /// </summary>
        public ActionResult DatabaseError()
        {
            Response.StatusCode = 503;
            return View();
        }

        /// <summary>
        /// System maintenance mode error page
        /// </summary>
        public ActionResult MaintenanceMode(DateTime? maintenanceStart = null, DateTime? maintenanceEnd = null)
        {
            ViewBag.MaintenanceStart = maintenanceStart?.ToString("yyyy-MM-dd HH:mm");
            ViewBag.MaintenanceEnd = maintenanceEnd?.ToString("yyyy-MM-dd HH:mm");
            
            Response.StatusCode = 503;
            return View();
        }

        /// <summary>
        /// Session timeout error page
        /// </summary>
        public ActionResult SessionTimeout(bool wasInExam = false, string sessionDuration = null, string timeoutReason = null)
        {
            ViewBag.WasInExam = wasInExam;
            ViewBag.SessionDuration = sessionDuration;
            ViewBag.TimeoutReason = timeoutReason;
            
            Response.StatusCode = 401;
            return View();
        }

        /// <summary>
        /// Exam system failure error page
        /// </summary>
        public ActionResult ExamSystemFailure(string errorType = null, string errorId = null, string studentId = null)
        {
            ViewBag.ErrorType = errorType ?? "Critical System Error";
            ViewBag.ErrorId = errorId ?? Guid.NewGuid().ToString("N")[..8];
            ViewBag.StudentId = studentId;
            
            Response.StatusCode = 500;
            return View();
        }

        /// <summary>
        /// Timer system failure error page
        /// </summary>
        public ActionResult TimerSystemFailure(string lastKnownTime = null, string elapsedTime = null, string errorId = null)
        {
            ViewBag.LastKnownTime = lastKnownTime;
            ViewBag.ElapsedTime = elapsedTime;
            ViewBag.ErrorId = errorId ?? Guid.NewGuid().ToString("N")[..8];
            
            Response.StatusCode = 500;
            return View();
        }

        /// <summary>
        /// Exam submission failure error page
        /// </summary>
        public ActionResult SubmissionFailure(int? submissionAttempts = null, string errorType = null, string errorId = null)
        {
            ViewBag.SubmissionAttempts = submissionAttempts ?? 1;
            ViewBag.ErrorType = errorType;
            ViewBag.ErrorId = errorId ?? Guid.NewGuid().ToString("N")[..8];
            
            Response.StatusCode = 500;
            return View();
        }

        /// <summary>
        /// Grading system failure error page
        /// </summary>
        public ActionResult GradingSystemFailure(string submissionTime = null, string examTitle = null, string errorId = null)
        {
            ViewBag.SubmissionTime = submissionTime;
            ViewBag.ExamTitle = examTitle;
            ViewBag.ErrorId = errorId ?? Guid.NewGuid().ToString("N")[..8];
            
            Response.StatusCode = 500;
            return View();
        }

        /// <summary>
        /// Exam access error page using the existing ExamAccessError model
        /// </summary>
        public ActionResult ExamAccessError(ExamAccessErrorType errorType, string technicalMessage = null)
        {
            var error = Models.ExamAccessError.Create(errorType, technicalMessage);
            return View("ExamError", error);
        }

        /// <summary>
        /// Authentication failure error page
        /// </summary>
        public ActionResult AuthenticationFailure(string errorMessage = null, string suggestedAction = null)
        {
            ViewBag.ErrorMessage = errorMessage ?? "Authentication failed. Please check your credentials and try again.";
            ViewBag.SuggestedAction = suggestedAction ?? "Verify your login information and ensure you're using the correct authentication phase.";
            
            Response.StatusCode = 401;
            return View();
        }

        /// <summary>
        /// Network connectivity issue error page
        /// </summary>
        public ActionResult NetworkError()
        {
            ViewBag.ErrorMessage = "Network connectivity issue detected.";
            ViewBag.SuggestedAction = "Please check your internet connection and try again.";
            
            Response.StatusCode = 503;
            return View();
        }
    }
}