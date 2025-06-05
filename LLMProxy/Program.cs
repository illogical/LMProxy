using LLMProxy.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<OllamaService>();

// Read PidogUrl from configuration
//var pidogUrl = builder.Configuration["PidogUrl"] ?? throw new InvalidOperationException("PidogUrl configuration is missing.");

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Request Origin: {context.Request.Headers["Origin"]}");
    await next();
});

app.UseCors("AllowAll");

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
