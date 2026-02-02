# Requirements Document

## Introduction

The Holistic Examination Management System (HEMS) is a web-based online exam platform designed to conduct high-stakes academic exams for Software Engineering students. The system provides a secure, distraction-free environment for students to take comprehensive examinations while enabling coordinators to manage the entire examination process efficiently. The system implements a two-phase authentication process to ensure academic integrity and prevent unauthorized access.

## Glossary

- **HEMS**: The Holistic Examination Management System
- **Student**: A registered user who can take examinations
- **Coordinator**: An administrative user who manages exams and students
- **Exam**: A collection of questions available for a specific academic year
- **Question**: An individual multiple-choice question with exactly one correct answer
- **Choice**: A possible answer option for a question
- **Student_Exam**: A record of a student's attempt at an exam
- **Student_Answer**: A record of a student's response to a specific question
- **Question_Palette**: The UI component showing question status overview on the left side
- **Academic_Year**: The year for which an exam is created and available
- **ID_Number**: The student ID number used for identification (university student ID)
- **Phase_1_Login**: Identity verification login using university email and calculated password
- **Phase_2_Login**: Exam-day login using student ID and announced password
- **Ethiopian_Academic_Year**: The current Ethiopian academic year used for password calculation
- **University_Email**: Student's institutional email address in the domain format

## Requirements

### Requirement 1: Two-Phase Authentication System

**User Story:** As a system administrator, I want a two-phase login process, so that student identity is verified before exam day and access is controlled during the exam.

#### Acceptance Criteria

1. THE HEMS SHALL implement a two-phase authentication system for students
2. THE HEMS SHALL require Phase 1 completion before allowing Phase 2 access
3. THE HEMS SHALL track login phase completion status for each student
4. THE HEMS SHALL prevent bypassing Phase 1 authentication
5. THE HEMS SHALL enforce one-time use restrictions for both phases

### Requirement 2: Phase 1 Identity Verification Login

**User Story:** As a student, I want to verify my identity before exam day, so that I can be authorized to take the exam.

#### Acceptance Criteria

1. THE HEMS SHALL allow Phase 1 login using university email as username
2. THE HEMS SHALL validate university email against the institutional domain format
3. THE HEMS SHALL calculate password as ID_Number concatenated with last two digits of current Ethiopian academic year
4. THE HEMS SHALL allow Phase 1 login exactly once per student
5. WHEN Phase 1 login succeeds, THE HEMS SHALL mark loginPhaseCompleted as true and disable Phase 1 credentials
6. THE HEMS SHALL validate that the university email exists in imported student records
7. THE HEMS SHALL prevent Phase 1 login after successful completion

### Requirement 3: Phase 2 Exam-Day Login

**User Story:** As a student, I want to access the exam during the exam session, so that I can take the examination in the controlled environment.

#### Acceptance Criteria

1. THE HEMS SHALL allow Phase 2 login using ID_Number as username
2. THE HEMS SHALL use a coordinator-generated password that is announced in the exam room
3. THE HEMS SHALL require password change immediately after successful Phase 2 login
4. THE HEMS SHALL set mustChangePassword to true initially for Phase 2
5. WHEN password is changed, THE HEMS SHALL set mustChangePassword to false and allow exam access
6. THE HEMS SHALL validate that Phase 1 is completed before allowing Phase 2 login
7. THE HEMS SHALL make Phase 2 password valid only during the exam session

### Requirement 4: Coordinator Password Management

**User Story:** As an exam coordinator, I want to generate and manage Phase 2 passwords, so that I can control exam access on exam day.

#### Acceptance Criteria

1. THE HEMS SHALL allow coordinators to generate Phase 2 passwords for exam sessions
2. THE HEMS SHALL allow coordinators to set the Phase 2 password before the exam begins
3. THE HEMS SHALL make the coordinator-generated password valid for all students during the exam session
4. THE HEMS SHALL allow coordinators to change the Phase 2 password if needed during the exam
5. THE HEMS SHALL expire Phase 2 passwords after the exam session ends

### Requirement 5: Student Management and Import

**User Story:** As an exam coordinator, I want to import students for specific batch years, so that only authorized students can access the exam.

#### Acceptance Criteria

1. THE HEMS SHALL allow coordinators to import student records for specific batch years
2. THE HEMS SHALL prevent student self-registration functionality
3. WHEN creating a student record, THE HEMS SHALL require id_number, batch_year, and university_email fields
4. THE HEMS SHALL validate university_email format against institutional domain
5. THE HEMS SHALL assign the Student role to all imported student accounts
6. THE HEMS SHALL initialize loginPhaseCompleted as false and mustChangePassword as true for new students

### Requirement 6: Exam Creation and Management

**User Story:** As an exam coordinator, I want to create and manage holistic exams, so that students can take comprehensive assessments.

#### Acceptance Criteria

1. THE HEMS SHALL support exactly one holistic exam per academic year
2. WHEN creating an exam, THE HEMS SHALL associate it with a specific academic year
3. THE HEMS SHALL allow coordinators to add multiple-choice questions to exams
4. WHEN adding questions, THE HEMS SHALL support questions with exactly one correct answer choice
5. THE HEMS SHALL maintain a fixed question order for each exam
6. THE HEMS SHALL allow coordinators to publish exams to make them available to students
7. THE HEMS SHALL support variable number of questions per exam across different years

### Requirement 7: Exam Availability Logic

**User Story:** As a student, I want to access available exams, so that I can take examinations when they are published and I am authorized.

#### Acceptance Criteria

1. WHEN an exam is published, THE HEMS SHALL make it available only to students who have completed Phase 1 login
2. THE HEMS SHALL allow student access to an exam if and only if the exam is published
3. THE HEMS SHALL allow student access to an exam if and only if the exam is for the current academic year
4. THE HEMS SHALL allow student access to an exam if and only if the student has not already submitted that exam
5. THE HEMS SHALL prevent access to unpublished exams
6. THE HEMS SHALL make exams available to students regardless of their batch_year once published

### Requirement 8: Single-Question Display Interface

**User Story:** As a student, I want a distraction-free single-question display with centered content, so that I can focus on each question individually.

#### Acceptance Criteria

1. THE HEMS SHALL display exactly one question at a time during exam taking
2. THE HEMS SHALL center the question text and answer choices on the screen
3. THE HEMS SHALL provide a question palette on the left side showing the status of all questions
4. WHEN displaying the question palette, THE HEMS SHALL show answered questions as half-gray rectangles (top half white, bottom half gray)
5. WHEN displaying the question palette, THE HEMS SHALL show unanswered questions as full white rectangles with black borders
6. WHEN displaying the question palette, THE HEMS SHALL show the current question with a blue border
7. WHEN displaying the question palette, THE HEMS SHALL show flagged questions with a red triangle overlay in the top-right corner

### Requirement 9: Question Flagging System

**User Story:** As a student, I want to flag questions for review, so that I can easily return to questions I want to revisit.

#### Acceptance Criteria

1. THE HEMS SHALL provide a flag button at the bottom of each question text and choices
2. WHEN a student flags a question, THE HEMS SHALL persist the flag status in the database
3. THE HEMS SHALL store flag status in the Student_Answer.isFlagged field
4. THE HEMS SHALL allow students to unflag previously flagged questions
5. THE HEMS SHALL maintain flag status throughout the entire exam session

### Requirement 10: Answer Selection and Auto-Save

**User Story:** As a student, I want my single answer selection to be automatically saved, so that I don't lose my progress if there are technical issues.

#### Acceptance Criteria

1. WHEN a student selects an answer choice, THE HEMS SHALL immediately save the selection to the database
2. THE HEMS SHALL support selection of exactly one answer per question
3. THE HEMS SHALL allow students to change their selected answer at any time before submission
4. WHEN an answer is changed, THE HEMS SHALL automatically update the saved response
5. THE HEMS SHALL provide visual feedback when answers are successfully saved

### Requirement 11: Exam Timer System

**User Story:** As a student, I want to see how much time remains in my exam, so that I can manage my time effectively.

#### Acceptance Criteria

1. THE HEMS SHALL display a countdown timer showing remaining exam time
2. THE HEMS SHALL compute remaining time dynamically using the start_date_time from the backend
3. THE HEMS SHALL update the timer display in real-time during the exam
4. WHEN the timer reaches zero, THE HEMS SHALL automatically submit the exam
5. THE HEMS SHALL not store remaining_seconds in the database (computed dynamically)

### Requirement 12: Question Navigation

**User Story:** As a student, I want to navigate between questions easily, so that I can answer questions in any order I prefer.

#### Acceptance Criteria

1. THE HEMS SHALL allow students to navigate to any question by clicking on the question palette buttons
2. WHEN a student clicks on a question in the palette, THE HEMS SHALL immediately display that question
3. THE HEMS SHALL maintain the current question state when navigating between questions
4. THE HEMS SHALL preserve all answer selections when navigating between questions
5. THE HEMS SHALL update the question palette to reflect the newly selected current question

### Requirement 13: Exam Submission Process

**User Story:** As a student, I want a clear submission process with confirmation, so that I can review my exam status before final submission.

#### Acceptance Criteria

1. WHEN a student initiates exam submission, THE HEMS SHALL display a confirmation modal
2. THE HEMS SHALL show the count of unanswered questions in the confirmation modal
3. THE HEMS SHALL show the count of answered questions in the confirmation modal
4. THE HEMS SHALL show the count of flagged questions in the confirmation modal
5. THE HEMS SHALL require explicit confirmation before processing the final submission
6. WHEN confirmation is provided, THE HEMS SHALL submit the exam and prevent any further changes
7. THE HEMS SHALL display "Once submitted, you cannot change your answers" message after submission

### Requirement 14: Automatic Grading System

**User Story:** As an exam coordinator, I want automatic grading of submitted exams, so that results are available immediately after submission.

#### Acceptance Criteria

1. WHEN a student submits an exam, THE HEMS SHALL automatically calculate the grade
2. THE HEMS SHALL compare student answers against correct answer keys
3. THE HEMS SHALL store the calculated grade in the database
4. THE HEMS SHALL assign exactly one point per question
5. THE HEMS SHALL complete grading immediately upon submission

### Requirement 15: Results Display and Percentage Calculation

**User Story:** As a student, I want to view my exam results as a percentage, so that I can understand my performance regardless of the total number of questions.

#### Acceptance Criteria

1. WHEN a student has submitted an exam, THE HEMS SHALL make results viewable
2. THE HEMS SHALL calculate percentage score based on correct answers divided by total questions
3. THE HEMS SHALL display the percentage score out of 100% regardless of total question count
4. THE HEMS SHALL show question-by-question results indicating correct and incorrect answers
5. THE HEMS SHALL prevent access to results before exam submission
6. THE HEMS SHALL maintain result availability for future viewing

### Requirement 16: Security and Data Integrity

**User Story:** As a system administrator, I want secure exam submission with backend validation, so that the examination process maintains academic integrity.

#### Acceptance Criteria

1. THE HEMS SHALL enforce timer limits on the backend to prevent client-side manipulation
2. THE HEMS SHALL prevent multiple submissions of the same exam by the same student
3. THE HEMS SHALL validate all submitted answers on the server side
4. THE HEMS SHALL maintain data integrity throughout the examination process
5. THE HEMS SHALL log all critical exam events for audit purposes
6. THE HEMS SHALL prevent Phase 1 credential reuse after successful completion
7. THE HEMS SHALL validate Phase 2 password changes and enforce one-time use

### Requirement 17: Database Schema and Relationships

**User Story:** As a system architect, I want a well-designed database schema, so that the system can efficiently store and retrieve examination data with authentication tracking.

#### Acceptance Criteria

1. THE HEMS SHALL implement tables for Role, User, Student, Exam, Question, Choice, Student_Exam, and Student_Answer
2. THE HEMS SHALL establish proper foreign key relationships between all related tables
3. THE HEMS SHALL use appropriate primary keys and data types for all table columns
4. THE HEMS SHALL store batch_year, id_number, and university_email in the Student table
5. THE HEMS SHALL store isFlagged in the Student_Answer table
6. THE HEMS SHALL store start_date_time in Student_Exam for timer calculations
7. THE HEMS SHALL store loginPhaseCompleted and mustChangePassword in the User table
