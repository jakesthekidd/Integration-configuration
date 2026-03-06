using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.Interfaces;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:51748")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<FieldMappingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ITmsSystemRepository, TmsSystemRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IFieldMappingRepository, FieldMappingRepository>();
builder.Services.AddScoped<ILookupTableRepository, LookupTableRepository>();
builder.Services.AddScoped<ITransformationLogRepository, TransformationLogRepository>();

// Register services
builder.Services.AddScoped<IJsonParserService, JsonParserService>();

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
