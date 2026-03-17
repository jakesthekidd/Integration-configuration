using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Transflo.Platform.Transformer.Core.Configurations;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;
using Transflo.Platform.Transformer.Core.Services.CustomerService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins!)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// External API config
builder.Services.Configure<ExternalApiOptions>(
    builder.Configuration.GetSection("ExternalApis:CustomerApi"));

builder.Services.AddHttpClient<ICustomerService, CustomerService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", options.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FieldMappingDbContext>(options =>
    options.UseNpgsql(connectionString));

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
builder.Services.AddScoped<ITransformationStrategyFactory, TransformationStrategyFactory>();

builder.Services.AddScoped<ITransformationService, TransformationService>();
builder.Services.AddScoped<ITransformationCoordinator, TransformationCoordinator>();
builder.Services.AddScoped<ITemplatesService, TemplatesService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.MapControllers();
app.Run();
