
using Deftpower.Onb.Assessment.Controllers;
using Deftpower.Onb.Assessment.Data;
using Deftpower.Onb.Assessment.Middleware;
using Deftpower.Onb.Assessment.Repositories;
using Deftpower.Onb.Assessment.Services;
using Microsoft.EntityFrameworkCore;

namespace Deftpower.Onb.Assessment;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.Services.AddDbContext<ChargingSessionDbContext>(options => options.UseInMemoryDatabase("ChargingSessions"));
        
        builder.Services.AddScoped<IChargingSessionService, ChargingSessionService>();
        builder.Services.AddScoped<IChargingSessionRepository, ChargingSessionRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.AddChargingSessionEndpoints();

        app.Run();
    }
}