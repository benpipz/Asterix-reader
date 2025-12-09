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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.MapHub<DataHub>("/datahub");

app.Run();
