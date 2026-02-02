# Implementation Plan: Holistic Examination Management System (HEMS)

## Overview

This implementation plan converts the HEMS design into a series of discrete coding tasks for a C# ASP.NET MVC (.NET Framework) application with SQL Server database. The tasks are organized to build incrementally, starting with core infrastructure and progressing through authentication, exam management, and testing.

## Tasks

- [x] 1. Set up project structure and database foundation
  - [x] 1.1 Create ASP.NET MVC project with proper folder structure (Controllers, Models, Views, Services)
  - [x] 1.2 Set up Entity Framework with SQL Server connection
  - [x] 1.3 Create database schema with all required tables (Role, User, Student, Exam, Question, Choice, StudentExam, StudentAnswer, ExamSession)
  - [x] 1.4 Configure Entity Framework relationships and constraints
  - [x] 1.5 Create initial data models with proper attributes and navigation properties

- [ ] 2. Implement core authentication system
  - [x] 2.1 Create Role and User models with authentication fields (loginPhaseCompleted, mustChangePassword)
  - [x] 2.2 Implement AuthenticationService with Phase 1 and Phase 2 login methods
  - [x] 2.3 Create AuthenticationController with Phase1Login, Phase2Login, and ChangePassword actions
  - [x] 2.4 Implement password calculation logic (ID number + Ethiopian academic year)
  - [x] 2.5 Add university email domain validation
  - [x] 2.6 Create login views for Phase 1 and Phase 2 authentication
  - [x] 2.7 Implement session management and role-based authorization

- [ ] 3. Implement student management system
  - [x] 3.1 Create Student model with IdNumber, UniversityEmail, and BatchYear fields
  - [x] 3.2 Implement CoordinatorController with student import functionality
  - [x] 3.3 Create student import view with CSV/Excel upload capability
  - [x] 3.4 Add validation for student data (unique ID numbers, email format)
  - [x] 3.5 Implement automatic user account creation for imported students
  - [x] 3.6 Create student management views for coordinators

- [x] 4. Implement exam creation and management
  - [x] 4.1 Create Exam model with AcademicYear, Title, DurationMinutes, and IsPublished fields
  - [x] 4.2 Create Question and Choice models with proper relationships
  - [x] 4.3 Implement ExamService for exam creation and management
  - [x] 4.4 Add exam creation functionality to CoordinatorController
  - [x] 4.5 Create exam creation views with question and choice management
  - [x] 4.6 Implement exam publishing functionality
  - [x] 4.7 Add validation for one exam per academic year constraint

- [x] 5. Implement exam session management
  - [x] 5.1 Create ExamSession model for Phase 2 password management
  - [x] 5.2 Implement exam session password generation in CoordinatorController
  - [x] 5.3 Add password expiry and validation logic
  - [x] 5.4 Create exam session management views for coordinators
  - [x] 5.5 Implement exam availability logic based on publication status and academic year

- [x] 6. Implement exam taking interface
  - [x] 6.1 Create StudentExam model to track exam attempts with StartDateTime
  - [x] 6.2 Create StudentAnswer model with IsFlagged field for question flagging
  - [x] 6.3 Implement ExamController with StartExam, GetQuestion, and navigation methods
  - [x] 6.4 Create single-question display view with centered content layout
  - [x] 6.5 Implement question palette with visual status indicators (answered, unanswered, current, flagged)
  - [x] 6.6 Add auto-save functionality for answer selections
  - [x] 6.7 Implement question flagging and unflagging functionality

- [x] 7. Implement timer system
  - [x] 7.1 Create TimerService for dynamic time calculations from StartDateTime
  - [x] 7.2 Add client-side countdown timer with real-time updates
  - [x] 7.3 Implement backend timer validation and enforcement
  - [x] 7.4 Add automatic exam submission when timer reaches zero
  - [x] 7.5 Create timer display component for exam interface

- [x] 8. Implement exam submission and confirmation
  - [x] 8.1 Create submission confirmation modal with question counts (answered, unanswered, flagged)
  - [x] 8.2 Implement SubmitExam action in ExamController with validation
  - [x] 8.3 Add prevention of multiple submissions for same student/exam
  - [x] 8.4 Create submission confirmation view with detailed status
  - [x] 8.5 Implement post-submission state management

- [x] 9. Implement automatic grading system
  - [x] 9.1 Create GradingService for automatic answer evaluation
  - [x] 9.2 Implement score calculation logic (1 point per question)
  - [x] 9.3 Add percentage calculation based on correct answers
  - [x] 9.4 Integrate grading with exam submission process
  - [x] 9.5 Store calculated scores and percentages in StudentExam model

- [x] 10. Implement results display system
  - [x] 10.1 Create results view for students showing percentage scores
  - [x] 10.2 Implement question-by-question results display
  - [x] 10.3 Add result access control (only after submission)
  - [x] 10.4 Create coordinator results view with exam statistics
  - [x] 10.5 Implement result persistence and future viewing capability

- [x] 11. Implement security and data integrity
  - [x] 11.1 Add server-side validation for all exam operations
  - [x] 11.2 Implement audit logging for critical exam events
  - [x] 11.3 Add prevention of client-side timer manipulation
  - [x] 11.4 Implement credential reuse prevention for both authentication phases
  - [x] 11.5 Add data integrity checks and transaction management

- [-] 12. Create comprehensive test suite
  - [x] 12.1 Write unit tests for AuthenticationService (Phase 1 and Phase 2 login)
  - [x] 12.2 Write unit tests for ExamService (exam creation, availability, navigation)
  - [x] 12.3 Write unit tests for GradingService (score calculation, percentage computation)
  - [x] 12.4 Write unit tests for TimerService (dynamic time calculation, auto-submission)
  - [x] 12.5 Create property-based tests for authentication logic
  - [x] 12.6 Create property-based tests for exam access control
  - [x] 12.7 Create property-based tests for grading calculations
  - [x] 12.8 Write integration tests for complete exam workflow

- [ ] 13. Implement error handling and user experience
  - [x] 13.1 Add comprehensive error handling for authentication failures
  - [x] 13.2 Implement user-friendly error messages for exam access issues
  - [x] 13.3 Add validation error handling for data input
  - [x] 13.4 Create error pages for system failures
  - [x] 13.5 Implement graceful handling of network issues during exam taking

- [ ] 14. Finalize UI/UX and styling
  - [x] 14.1 Create responsive CSS for single-question exam interface
  - [x] 14.2 Style question palette with proper visual indicators
  - [x] 14.3 Implement distraction-free exam environment styling
  - [x] 14.4 Add visual feedback for auto-save operations
  - [x] 14.5 Create professional styling for coordinator interfaces

- [ ] 15. Performance optimization and deployment preparation
  - [x] 15.1 Optimize database queries and add appropriate indexes
  - [x] 15.2 Implement caching for frequently accessed data
  - [x] 15.3 Add connection string configuration for different environments
  - [x] 15.4 Create deployment scripts and configuration
  - [x] 15.5 Perform load testing for concurrent exam sessions
