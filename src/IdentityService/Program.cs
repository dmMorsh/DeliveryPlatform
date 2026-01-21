// var builder = WebApplication.CreateBuilder(args);
//
// // Add services to the container.
//
// builder.Services.AddControllers();
//
// var app = builder.Build();
//
// // Configure the HTTP request pipeline.
//
// app.UseAuthorization();
//
// app.MapControllers();
//
// app.Run();
using IdentityService.Infrastructure.Security;
using IdentityService.Domain.Users;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// Db
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

// Controllers
builder.Services.AddControllers();

// Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var app = builder.Build();

app.MapControllers();
app.Run();
