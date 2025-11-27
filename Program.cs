// Program.cs

using TaskOrganizer.Models;
using TaskOrganizer.Services;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
// ❗ DAPAT KASAMA ITO PARA SA AUTHENTICATION ❗
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// --- MongoDB Configuration and Services ---

// 1. Tiyakin na ang MongoDBSettings at EmailSettings ay ni-rerehistro sa DI
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

// ❗ KRITIKAL NA PAGBABAGO: Gamitin ang tamang model name para sa configuration ❗
// I-a-assume na mayroon kayong 'EmailSettings' na model class para sa configuration
builder.Services.Configure<EmailService>( // Pinalitan ang EmailService ng EmailSettings
    builder.Configuration.GetSection("EmailSettings"));

// 2. I-register ang IMongoClient at IMongoDatabase (Ginamit ang IOptions)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;

    if (string.IsNullOrEmpty(settings.ConnectionString))
    {
        Console.WriteLine("CRITICAL WARNING: MongoDB ConnectionString is missing in appsettings.json. Using dummy client.");
        return new MongoClient("mongodb://dummy:27017");
    }
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();

    if (string.IsNullOrEmpty(settings.DatabaseName))
    {
        throw new InvalidOperationException("MongoDB DatabaseName is not configured.");
    }
    return client.GetDatabase(settings.DatabaseName);
});


// 3. I-register ang mga Services
builder.Services.AddScoped<EmployeeServices>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<TaskService>();

// ❗ SOLUSYON PARA SA DASHBOARD ERROR (IHttpContextAccessor) ❗
// Ito ang nagre-register ng service na kailangan ng DashboardModel at iba pang components.
builder.Services.AddHttpContextAccessor();


// =============================================================
// ❗ KRITIKAL NA SOLUSYON PARA SA "No sign-in authentication handlers" ERROR ❗
// 4. I-register ang Authentication Services at itakda ang Cookie Scheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TaskOrganizerAuth";
        // Ang Login page ay /Login
        options.LoginPath = "/Login";
        // Ang Access Denied page (opsyonal, pero maganda kung meron)
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Halimbawa: 60 minutes session
    });

builder.Services.AddAuthorization();
// =============================================================


// 5. Build at Configuration ng Pipeline
var app = builder.Build();


// ❗ KRITIKAL NA PAGBABAGO: ILAGAY ANG AUTHENTICATION MIDDLWARE ❗
app.UseRouting();

// Dapat mauna ang Authentication bago ang Authorization
app.UseAuthentication();
app.UseAuthorization();


// Default redirect to Login
app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return System.Threading.Tasks.Task.CompletedTask;
});

app.MapRazorPages();
app.Run();