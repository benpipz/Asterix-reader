using AsterixReader.Backend.Extensions;
using AsterixReader.Backend.Hubs;
using AsterixReader.Backend.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add all Asterix Reader services using extension method
builder.Services.AddAsterixReaderServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
// CORS must be early in the pipeline, before other middleware
app.UseCors("AllowFrontend");

// Enable Swagger in all environments (including Docker/Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Asterix Reader API v1");
    c.RoutePrefix = "swagger"; // Swagger UI will be available at /swagger
});

// Only use HTTPS redirection in production if HTTPS is configured
// Skip in Docker if HTTPS_PORT is not set
var httpsPort = builder.Configuration["ASPNETCORE_HTTPS_PORT"];
if (!app.Environment.IsDevelopment() && !string.IsNullOrEmpty(httpsPort))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.MapHub<DataHub>("/datahub");

app.Run();
