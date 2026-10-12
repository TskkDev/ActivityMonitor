using ActivityMonitor.Server.Endpoints;
using ActivityMonitor.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AgentRegistry>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "ActivityMonitor API",
        Version = "v1",
        Description = "Monitoring server for employee workstation agents"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ActivityMonitor API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.MapAgentEndpoints();

app.Run();
