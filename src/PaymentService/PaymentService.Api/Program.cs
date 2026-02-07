using MediatR;
using PaymentService.Application;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PaymentRepository>());

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();