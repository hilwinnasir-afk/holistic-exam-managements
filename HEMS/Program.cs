using Microsoft.EntityFrameworkCore;
using HEMS.Models;
using HEMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add CORS support for testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTestOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:5500", "null") // null for file:// protocol
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Entity Framework - Use SQL Server Database
builder.Services.AddDbContext<HEMSContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Authentication/Phase1Login";
        options.LogoutPath = "/Authentication/Logout";
        options.AccessDeniedPath = "/Error/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add HttpContextAccessor for session service
builder.Services.AddHttpContextAccessor();

// Add custom services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<ITimerService, TimerService>();
builder.Services.AddScoped<IGradingService, GradingService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDataIntegrityService, DataIntegrityService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ICacheManagementService, CacheManagementService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IDatabaseOptimizationService, DatabaseOptimizationService>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure database is created and seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HEMSContext>();
    try
    {
        // Use migrations for SQL Server database
        context.Database.Migrate();
        SeedDatabase(context);
    }
    catch (Exception ex)
    {
        // Log error but continue - database might already exist
        Console.WriteLine($"Database initialization warning: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowTestOrigins");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void SeedDatabase(HEMSContext context)
{
    // Seed Roles
    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Role { RoleName = "Student" },
            new Role { RoleName = "Coordinator" }
        );
        context.SaveChanges();
    }

    // Seed Coordinator User
    if (!context.Users.Any(u => u.Username == "coordinator@university.edu.et"))
    {
        var coordinatorRole = context.Roles.First(r => r.RoleName == "Coordinator");
        var coordinatorUser = new User
        {
            Username = "coordinator@university.edu.et",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            RoleId = coordinatorRole.RoleId,
            LoginPhaseCompleted = true,
            MustChangePassword = false,
            CreatedDate = DateTime.Now
        };
        context.Users.Add(coordinatorUser);
        context.SaveChanges();
        
        Console.WriteLine("✅ Default coordinator account created:");
        Console.WriteLine("   Username: coordinator@university.edu.et");
        Console.WriteLine("   Password: Admin123!");
    }

    // Seed sample student for testing
    if (!context.Users.Any(u => u.Username == "student@university.edu.et"))
    {
        var studentRole = context.Roles.First(r => r.RoleName == "Student");
        var studentUser = new User
        {
            Username = "student@university.edu.et",
            PasswordHash = "", // Students don't use stored password hash - password is calculated dynamically
            RoleId = studentRole.RoleId,
            LoginPhaseCompleted = false,
            MustChangePassword = true,
            CreatedDate = DateTime.Now
        };
        context.Users.Add(studentUser);
        context.SaveChanges();

        var student = new Student
        {
            UserId = studentUser.UserId,
            IdNumber = "ST001",
            UniversityEmail = "student@university.edu.et",
            Email = "student@university.edu.et",
            FirstName = "Test",
            LastName = "Student",
            BatchYear = "Year IV Sem II",
            CreatedDate = DateTime.Now
        };
        context.Students.Add(student);
        context.SaveChanges();
        
        Console.WriteLine("✅ Sample student account created:");
        Console.WriteLine("   Email: student@university.edu.et");
        Console.WriteLine("   ID: ST001");
        Console.WriteLine("   Password: ST0018 (for Phase 1 - calculated from ID + Ethiopian year)");
    }
    else
    {
        // Reset existing student's Phase1 completion for testing
        var existingUser = context.Users.FirstOrDefault(u => u.Username == "student@university.edu.et");
        if (existingUser != null && existingUser.LoginPhaseCompleted)
        {
            existingUser.LoginPhaseCompleted = false;
            context.SaveChanges();
            Console.WriteLine("🔄 Reset Phase1 completion status for existing student");
            Console.WriteLine("   Email: student@university.edu.et");
            Console.WriteLine("   Password: ST0018 (ready for Phase 1 testing)");
        }
    }

    // Seed test exam session for testing Phase 2 logic
    if (!context.ExamSessions.Any(es => es.SessionPassword.Contains("EXAM2026")))
    {
        // First, create a simple exam if it doesn't exist
        if (!context.Exams.Any(e => e.Title == "Test Exam for Phase 2"))
        {
            var testExam = new Exam
            {
                Title = "Test Exam for Phase 2",
                AcademicYear = 2026,
                DurationMinutes = 120,
                IsPublished = true,
                CreatedDate = DateTime.Now,
                ExamStartDateTime = DateTime.Now.AddMinutes(-30), // Started 30 minutes ago
                ExamEndDateTime = DateTime.Now.AddHours(2) // Ends in 2 hours
            };
            context.Exams.Add(testExam);
            context.SaveChanges();
        }

        var exam = context.Exams.First(e => e.Title == "Test Exam for Phase 2");
        
        // Add test questions if they don't exist
        if (!context.Questions.Any(q => q.ExamId == exam.ExamId))
        {
            var question1 = new Question
            {
                ExamId = exam.ExamId,
                QuestionText = "What is the capital of Ethiopia?",
                QuestionOrder = 1
            };
            context.Questions.Add(question1);
            context.SaveChanges();
            
            // Add choices for question 1
            context.Choices.AddRange(
                new Choice { QuestionId = question1.QuestionId, ChoiceText = "Addis Ababa", IsCorrect = true, ChoiceOrder = 1 },
                new Choice { QuestionId = question1.QuestionId, ChoiceText = "Dire Dawa", IsCorrect = false, ChoiceOrder = 2 },
                new Choice { QuestionId = question1.QuestionId, ChoiceText = "Bahir Dar", IsCorrect = false, ChoiceOrder = 3 },
                new Choice { QuestionId = question1.QuestionId, ChoiceText = "Mekelle", IsCorrect = false, ChoiceOrder = 4 }
            );
            
            var question2 = new Question
            {
                ExamId = exam.ExamId,
                QuestionText = "Which programming language is used for this HEMS system?",
                QuestionOrder = 2
            };
            context.Questions.Add(question2);
            context.SaveChanges();
            
            // Add choices for question 2
            context.Choices.AddRange(
                new Choice { QuestionId = question2.QuestionId, ChoiceText = "Java", IsCorrect = false, ChoiceOrder = 1 },
                new Choice { QuestionId = question2.QuestionId, ChoiceText = "C#", IsCorrect = true, ChoiceOrder = 2 },
                new Choice { QuestionId = question2.QuestionId, ChoiceText = "Python", IsCorrect = false, ChoiceOrder = 3 },
                new Choice { QuestionId = question2.QuestionId, ChoiceText = "JavaScript", IsCorrect = false, ChoiceOrder = 4 }
            );
            
            var question3 = new Question
            {
                ExamId = exam.ExamId,
                QuestionText = "What does HEMS stand for?",
                QuestionOrder = 3
            };
            context.Questions.Add(question3);
            context.SaveChanges();
            
            // Add choices for question 3
            context.Choices.AddRange(
                new Choice { QuestionId = question3.QuestionId, ChoiceText = "Higher Education Management System", IsCorrect = false, ChoiceOrder = 1 },
                new Choice { QuestionId = question3.QuestionId, ChoiceText = "Holistic Examination Management System", IsCorrect = true, ChoiceOrder = 2 },
                new Choice { QuestionId = question3.QuestionId, ChoiceText = "Health Emergency Management System", IsCorrect = false, ChoiceOrder = 3 },
                new Choice { QuestionId = question3.QuestionId, ChoiceText = "Human Enterprise Management System", IsCorrect = false, ChoiceOrder = 4 }
            );
            
            context.SaveChanges();
            Console.WriteLine("✅ Test questions added to exam:");
            Console.WriteLine("   - 3 multiple choice questions with 4 options each");
        }
        
        var testExamSession = new ExamSession
        {
            ExamId = exam.ExamId,
            SessionPassword = BCrypt.Net.BCrypt.HashPassword("EXAM2026"),
            IsActive = true,
            CreatedDate = DateTime.Now,
            ExpiryDate = DateTime.Now.AddHours(3) // Active for 3 hours
        };
        context.ExamSessions.Add(testExamSession);
        context.SaveChanges();

        Console.WriteLine("✅ Test exam session created:");
        Console.WriteLine($"   Exam: {exam.Title}");
        Console.WriteLine($"   Session expires: {testExamSession.ExpiryDate}");
        Console.WriteLine("   Session Password: EXAM2026 (for Phase 2 testing)");
    }
}