using API_V2._0.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
          .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
          .AddGoogle(options =>
          {
              options.ClientId = "1003918984977-48siulklljg68n7hftgiaqjhambnlr6a.apps.googleusercontent.com";
              options.ClientSecret = "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";
              options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          });

// Add services to the container.

builder.Services.AddControllers();
//CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Ensure this matches exactly
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Needed if authentication uses cookies or tokens
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("UPMConnection");

builder.Services.AddDbContext<UPMDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");

app.UseAuthorization();

app.MapControllers();

app.Run();
