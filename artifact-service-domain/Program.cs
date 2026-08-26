using artifact.service.domain.Configurations;
using artifact.service.domain.Configurations.Services;
using Logging;
using NetUtils.Aspnet.Configurations;
using NetUtils.Aspnet.Configurations.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.AddSerilogger();
// Add services to the container.

builder.Services.AddControllers();

builder.ConfigApiVersion();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.RegisterCommonServices();
builder.RegisterServices();
builder.Services.AllowCorsExt();
builder.AddSwaggerGenExt($"{typeof(Program).Assembly.GetName().Name}.xml");

var app = builder.Build();

app.ConfigApplication();

app.Run();
