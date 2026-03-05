using messenger.Extensions;
using messenger.Hubs;
using messenger.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Configure all services using Extension methods
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureJWT(builder.Configuration);
builder.Services.ConfigureCORS();
builder.Services.ConfigureSignalR();
builder.Services.ConfigureRepositories();
builder.Services.ConfigureServices();
builder.Services.ConfigureOtherServices();
builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();

// Configure pipeline
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();