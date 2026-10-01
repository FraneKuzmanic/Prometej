using Microsoft.EntityFrameworkCore;
using Prometej_api.ErrorHandling;
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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
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

#endregion Service DI

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
