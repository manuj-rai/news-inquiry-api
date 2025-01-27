using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MyApi;
using MyApi.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Configure CORS policy to allow any origin, method, and header
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()    // Allows all origins
               .AllowAnyMethod()    // Allows all HTTP methods (GET, POST, PUT, DELETE, etc.)
               .AllowAnyHeader();   // Allows any header
    });
});

// Add DbContext with SQL Server connection
builder.Services.AddScoped<INewsRepository>(provider =>
    new NewsRepository(provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));

// Add the filter to the DI container
builder.Services.AddScoped(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
    return new AllowSpecificOriginFilter(string.Join(",", allowedOrigins));
});

// Add controllers
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable Swagger for all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
    c.RoutePrefix = string.Empty;
});

// Apply CORS policy globally
app.UseCors(builder => builder.WithOrigins("http://localhost:4200", "http://www.inquiry-management.com", "https://inquiry-management-rho.vercel.app")
                                .AllowAnyMethod()
                                .AllowAnyHeader());

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Global exception handler
app.UseExceptionHandler("/error");
app.Map("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    return Results.Problem(title: "An error occurred", detail: exception?.Message);
});

// Root endpoint for basic verification
app.MapGet("/", () => "Welcome to My API!");

app.Run();

