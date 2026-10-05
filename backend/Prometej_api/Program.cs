using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Prometej_api.Auth;
using Prometej_api.ErrorHandling;
using Prometej_api.Seed;
using Prometej_core.DataAccessLayer;
using Prometej_core.Models.efModels;
using Prometej_core.Services.Contracts;
using Prometej_core.Services.Implementations;
using Prometej_persistance;
using Prometej_persistance.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<DataContext>(options =>
{
    // Read when the context is first built, not above: the tests supply the connection
    // string after this file has started running.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not set.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("client-app",
           policyBuilder =>
           {
               policyBuilder.WithOrigins("http://localhost:5173")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
           }
             );
});

builder.Services.AddAutoMapper(typeof(UserService).Assembly);
builder.Services.AddAutoMapper(typeof(PeriodService).Assembly);
builder.Services.AddAutoMapper(typeof(QuizService).Assembly);

#region Repo DI

builder.Services.AddTransient<IRepository<User>, Repository<User>>();
builder.Services.AddTransient<IRepository<Period>, Repository<Period>>();
builder.Services.AddTransient<IRepository<PeriodContent>, Repository<PeriodContent>>();
builder.Services.AddTransient<IRepository<Quiz>, Repository<Quiz>>();
builder.Services.AddTransient<IRepository<Question>, Repository<Question>>();
builder.Services.AddTransient<IRepository<Answer>, Repository<Answer>>();
builder.Services.AddTransient<IRepository<QuizGame>, Repository<QuizGame>>();

#endregion Repo DI

#region Service DI

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPeriodService, PeriodService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddSingleton<TokenService>();

#endregion Service DI

#region Authentication

// A missing or short signing key stops the app at startup instead of issuing weak tokens.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(jwt => jwt.HasUsableKey(), "Jwt:Key must be the base64 of at least 32 bytes.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// JwtOptions is resolved here rather than read from builder.Configuration above, so the
// key that validates a token is always the one TokenService signed it with.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        // Keep "sub" and "role" as written instead of mapping them to the long claim URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.Key)),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            // The token is in the session cookie, not in an Authorization header.
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[AuthCookie.Name];
                return Task.CompletedTask;
            },
            // A token stays validly signed after its account is deleted. Without this check
            // it would keep working, with its old role, until it expires.
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.GetUserIdOrNull();
                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                if (userId is null || !userService.Exists(userId.Value))
                {
                    AuthCookie.Delete(context.Response);
                    context.Fail("The account no longer exists.");
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

#endregion Authentication

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    if (!app.Environment.IsProduction())
    {
        db.Database.Migrate();
    }
    DbSeeder.Run(scope.ServiceProvider, app.Configuration);
    DemoContentSeeder.Run(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("client-app");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
