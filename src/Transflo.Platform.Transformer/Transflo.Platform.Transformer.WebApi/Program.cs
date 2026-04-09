using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Configurations;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.CustomerService;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;
using Transflo.Platform.Transformer.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application configuration
await builder.AddApplicationConfigurationAsync();
var config = builder.Configuration.Get<ApplicationConfiguration>() ?? new();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(config.Cors.AllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddHttpClient<ICustomerService, CustomerService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var options = config.ExternalApis.CustomersApi;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", options.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// PostgreSQL
builder.Services.AddDbContext<FieldMappingDbContext>((sp, options) =>
{
    options.UseNpgsql(config.ConnectionStrings.DefaultConnection);
});

// AWS DynamoDB registration 
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();

// Repositories
builder.Services.AddScoped<ITmsSystemRepository, TmsSystemRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<ITemplateVersionRepository, TemplateVersionRepository>();
builder.Services.AddScoped<IFieldMappingRepository, FieldMappingRepository>();
builder.Services.AddScoped<ILookupTableRepository, LookupTableRepository>();
builder.Services.AddScoped<ITransformationLogRepository, TransformationLogRepository>();
builder.Services.AddScoped<IApiClientRepository, ApiClientRepository>();

// Register services
builder.Services.AddScoped<IJsonParserService, JsonParserService>();
builder.Services.AddScoped<ILookupDataProvider, LookupDataProvider>();

// Register transformation strategies (Strategy Pattern)
builder.Services.AddScoped<ITransformationStrategy, DirectTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, ConstantTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, LookupTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, ConcatTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, DateFormatTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, ArrayMapTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, ArrayFlattenTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, ConditionalTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, SubstringTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, TemplateTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, MathTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, PrefixMapTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategy, ConditionalDateFormatTransformationStrategy>();
builder.Services.AddScoped<ITransformationStrategyFactory, TransformationStrategyFactory>();

builder.Services.AddScoped<ITransformationService, TransformationService>();
builder.Services.AddScoped<ITransformationCoordinator, TransformationCoordinator>();
builder.Services.AddScoped<IFieldMappingValidationService, FieldMappingValidationService>();
builder.Services.AddScoped<ITemplatesService, TemplatesService>();

var app = builder.Build();

// Apply database pending migrations if any
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FieldMappingDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment()
    || app.Environment.EnvironmentName.Equals("qa", StringComparison.OrdinalIgnoreCase)
    || app.Environment.EnvironmentName.Equals("dev", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/", () => Results.Ok("Transformer API is running"));

app.MapControllers();
app.Run();
