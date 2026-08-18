using System.Text;
using Messenger.Realtime;
using Messenger.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Messenger.Services;
using Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<MessengerDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("SQL_Connection"));
        }
    );

//JWT Authentication
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section is missing or invalid.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddScoped<RealtimeNotifier>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<WebSocketHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Messenger API v1");
    });
}
// Apply migrations automatically at startup

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MessengerDbContext>();
    context.Database.Migrate();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await handler.HandleAsync(context);
});

app.MapControllerRoute(
    name: "api",
    pattern: "api/v1/{controller}/{action}");

app.Urls.Add("http://0.0.0.0:5069");


app.Run();
