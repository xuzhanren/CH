using System.Text;
using CanHappy.Data;
using CanHappy.Common;
using CanHappy.Services.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
var enableDebug = builder.Configuration.GetValue<bool>("EnableDebug");
var logDirectoryPath = Path.Combine(builder.Environment.ContentRootPath, "log");
Directory.CreateDirectory(logDirectoryPath);

var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: Path.Combine(logDirectoryPath, "app-.log"),
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 20,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");

if (enableDebug)
{
    loggerConfiguration = loggerConfiguration
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information);
}

Log.Logger = loggerConfiguration.CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    if (enableDebug)
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddScoped<IMessageNotificationEmailSender, MailKitMessageNotificationEmailSender>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is missing.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is missing.");
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

var authenticationBuilder = builder.Services
    .AddAuthentication();

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authenticationBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
    });
}

authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddControllersWithViews(options =>
{
    options.ModelMetadataDetailsProviders.Add(new SuffixTrimDisplayMetadataProvider());
});
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CanHappy API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "JWT Authorization header using the Bearer scheme."
    });
});

var app = builder.Build();

if (enableDebug)
{
    app.UseSerilogRequestLogging();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.MapRazorPages()
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed. The application will continue running, but database-backed features may not work until the connection is fixed.");
    }
}

app.Run();
