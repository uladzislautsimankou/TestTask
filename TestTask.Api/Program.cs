using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using TestTask.Api.Middlewares;
using TestTask.Core;
using TestTask.Core.Initializers;
using TestTast.Infrastructure.SQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCore();
builder.Services.AddInfrastructureSQL(builder.Configuration);

builder.Services
    .AddControllers()
    .AddNewtonsoftJson()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.EnableAnnotations();
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TestTask API",
        Version = "v1"
    });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestTask API V1");
        c.RoutePrefix = "swagger";
    });
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IApplicationInitializer>();
    await initializer.InitializeAsync();
}

app.MapControllers();

app.Run();
