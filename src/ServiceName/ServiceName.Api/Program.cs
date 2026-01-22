using MediatR;
using ServiceName.Application;
using ServiceName.Application.Interfaces;
using ServiceName.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddScoped<IServiceNameRepository, ServiceNameRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ServiceNameRepository>());

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();