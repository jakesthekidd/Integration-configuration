using Microsoft.EntityFrameworkCore;
using FieldMappingApi.Data;
using FieldMappingApi.DTOs;
using FieldMappingApi.Models;
using FieldMappingApi.Repositories;
using FieldMappingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Accept enum values as strings in request bodies (e.g. "DateFormat" instead of an integer)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=fieldmapping;Username=postgres;Password=postgres";

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
builder.Services.AddScoped<FieldMappingApi.Services.IJsonParserService, FieldMappingApi.Services.JsonParserService>();
builder.Services.AddScoped<FieldMappingApi.Services.ITransformationService, FieldMappingApi.Services.TransformationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

// TMS Systems API endpoints
var tmsGroup = app.MapGroup("/api/v1/tms-systems")
    .WithTags("TMS Systems")
    .WithOpenApi();

tmsGroup.MapGet("/", async (ITmsSystemRepository repo, bool activeOnly = false) =>
{
    var systems = activeOnly
        ? await repo.GetActiveSystemsAsync()
        : await repo.GetAllAsync();

    var response = new TmsSystemListResponse
    {
        Systems = systems.Select(s => new TmsSystemResponse
        {
            Id = s.Id,
            Name = s.Name,
            DisplayName = s.DisplayName,
            Description = s.Description,
            Version = s.Version,
            IsActive = s.IsActive,
            SampleJsonSchema = s.SampleJsonSchema,
            ConnectionConfig = s.ConnectionConfig,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            CreatedBy = s.CreatedBy,
            Metadata = s.Metadata
        }).ToList(),
        TotalCount = systems.Count
    };

    return Results.Ok(ApiResponse<TmsSystemListResponse>.SuccessResponse(response));
})
.WithName("GetAllTmsSystems")
.Produces<ApiResponse<TmsSystemListResponse>>();

tmsGroup.MapGet("/{id}", async (string id, ITmsSystemRepository repo) =>
{
    var system = await repo.GetByIdAsync(id);
    if (system == null)
    {
        return Results.NotFound(ApiResponse<TmsSystemResponse>.ErrorResponse($"TMS system not found: {id}"));
    }

    var response = new TmsSystemResponse
    {
        Id = system.Id,
        Name = system.Name,
        DisplayName = system.DisplayName,
        Description = system.Description,
        Version = system.Version,
        IsActive = system.IsActive,
        SampleJsonSchema = system.SampleJsonSchema,
        ConnectionConfig = system.ConnectionConfig,
        CreatedAt = system.CreatedAt,
        UpdatedAt = system.UpdatedAt,
        CreatedBy = system.CreatedBy,
        Metadata = system.Metadata
    };

    return Results.Ok(ApiResponse<TmsSystemResponse>.SuccessResponse(response));
})
.WithName("GetTmsSystemById")
.Produces<ApiResponse<TmsSystemResponse>>();

tmsGroup.MapPost("/", async (CreateTmsSystemRequest request, ITmsSystemRepository repo) =>
{
    // Check for duplicates
    var existing = await repo.GetByNameAsync(request.Name);
    if (existing != null)
    {
        return Results.Conflict(ApiResponse<TmsSystemResponse>.ErrorResponse(
            $"A TMS system with name '{request.Name}' already exists"));
    }

    var tmsSystem = new TmsSystem
    {
        Name = request.Name,
        DisplayName = request.DisplayName,
        Description = request.Description,
        Version = request.Version,
        IsActive = true,
        SampleJsonSchema = request.SampleJsonSchema,
        ConnectionConfig = request.ConnectionConfig,
        Metadata = request.Metadata
    };

    var created = await repo.CreateAsync(tmsSystem);

    var response = new TmsSystemResponse
    {
        Id = created.Id,
        Name = created.Name,
        DisplayName = created.DisplayName,
        Description = created.Description,
        Version = created.Version,
        IsActive = created.IsActive,
        SampleJsonSchema = created.SampleJsonSchema,
        ConnectionConfig = created.ConnectionConfig,
        CreatedAt = created.CreatedAt,
        UpdatedAt = created.UpdatedAt,
        CreatedBy = created.CreatedBy,
        Metadata = created.Metadata
    };

    return Results.Created($"/api/v1/tms-systems/{created.Id}",
        ApiResponse<TmsSystemResponse>.SuccessResponse(response));
})
.WithName("CreateTmsSystem")
.Produces<ApiResponse<TmsSystemResponse>>(StatusCodes.Status201Created);

tmsGroup.MapPut("/{id}", async (string id, UpdateTmsSystemRequest request, ITmsSystemRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
    {
        return Results.NotFound(ApiResponse<TmsSystemResponse>.ErrorResponse($"TMS system not found: {id}"));
    }

    existing.DisplayName = request.DisplayName;
    existing.Description = request.Description;
    existing.Version = request.Version;
    existing.IsActive = request.IsActive;
    existing.SampleJsonSchema = request.SampleJsonSchema;
    existing.ConnectionConfig = request.ConnectionConfig;
    existing.Metadata = request.Metadata;

    var updated = await repo.UpdateAsync(existing);

    var response = new TmsSystemResponse
    {
        Id = updated.Id,
        Name = updated.Name,
        DisplayName = updated.DisplayName,
        Description = updated.Description,
        Version = updated.Version,
        IsActive = updated.IsActive,
        SampleJsonSchema = updated.SampleJsonSchema,
        ConnectionConfig = updated.ConnectionConfig,
        CreatedAt = updated.CreatedAt,
        UpdatedAt = updated.UpdatedAt,
        CreatedBy = updated.CreatedBy,
        Metadata = updated.Metadata
    };

    return Results.Ok(ApiResponse<TmsSystemResponse>.SuccessResponse(response));
})
.WithName("UpdateTmsSystem")
.Produces<ApiResponse<TmsSystemResponse>>();

tmsGroup.MapDelete("/{id}", async (string id, ITmsSystemRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
    {
        return Results.NotFound(ApiResponse<object>.ErrorResponse($"TMS system not found: {id}"));
    }

    await repo.DeleteAsync(id);
    return Results.NoContent();
})
.WithName("DeleteTmsSystem")
.Produces(StatusCodes.Status204NoContent);

// Templates API endpoints
var templatesGroup = app.MapGroup("/api/v1/templates")
    .WithTags("Templates")
    .WithOpenApi();

templatesGroup.MapGet("/", async (ITemplateRepository repo, string? tmsSystemId = null) =>
{
    var templates = string.IsNullOrEmpty(tmsSystemId)
        ? await repo.GetAllAsync()
        : await repo.GetByTmsSystemIdAsync(tmsSystemId);

    var response = new TemplateListResponse
    {
        Templates = templates.Select(t => new TemplateResponse
        {
            TemplateId = t.TemplateId,
            Name = t.Name,
            Description = t.Description,
            TmsSystemId = t.TmsSystemId,
            CustomerId = t.CustomerId,
            Version = t.Version,
            Status = t.Status.ToString(),
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            CreatedBy = t.CreatedBy,
            SampleInputJson = t.SampleInputJson,
            Metadata = t.Metadata
        }).ToList(),
        TotalCount = templates.Count
    };

    return Results.Ok(ApiResponse<TemplateListResponse>.SuccessResponse(response));
})
.WithName("GetAllTemplates")
.Produces<ApiResponse<TemplateListResponse>>();

templatesGroup.MapGet("/{templateId}", async (string templateId, ITemplateRepository repo, int? version = null) =>
{
    var template = await repo.GetByIdAsync(templateId, version);
    if (template == null)
    {
        return Results.NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));
    }

    var response = new TemplateResponse
    {
        TemplateId = template.TemplateId,
        Name = template.Name,
        Description = template.Description,
        TmsSystemId = template.TmsSystemId,
        CustomerId = template.CustomerId,
        Version = template.Version,
        Status = template.Status.ToString(),
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt,
        CreatedBy = template.CreatedBy,
        SampleInputJson = template.SampleInputJson,
        Metadata = template.Metadata
    };

    return Results.Ok(ApiResponse<TemplateResponse>.SuccessResponse(response));
})
.WithName("GetTemplateById")
.Produces<ApiResponse<TemplateResponse>>();

templatesGroup.MapPost("/", async (CreateTemplateRequest request, ITemplateRepository repo) =>
{
    var template = new FieldMappingTemplate
    {
        TemplateId = Guid.NewGuid().ToString(),
        Name = request.Name,
        Description = request.Description,
        TmsSystemId = request.TmsSystemId,
        CustomerId = request.CustomerId,
        Version = 1,
        Status = TemplateStatus.Draft,
        SampleInputJson = request.SampleInputJson,
        Metadata = request.Metadata
    };

    var created = await repo.CreateAsync(template);

    var response = new TemplateResponse
    {
        TemplateId = created.TemplateId,
        Name = created.Name,
        Description = created.Description,
        TmsSystemId = created.TmsSystemId,
        CustomerId = created.CustomerId,
        Version = created.Version,
        Status = created.Status.ToString(),
        CreatedAt = created.CreatedAt,
        UpdatedAt = created.UpdatedAt,
        CreatedBy = created.CreatedBy,
        SampleInputJson = created.SampleInputJson,
        Metadata = created.Metadata
    };

    return Results.Created($"/api/v1/templates/{created.TemplateId}",
        ApiResponse<TemplateResponse>.SuccessResponse(response));
})
.WithName("CreateTemplate")
.Produces<ApiResponse<TemplateResponse>>(StatusCodes.Status201Created);

templatesGroup.MapPut("/{templateId}", async (string templateId, UpdateTemplateRequest request, ITemplateRepository repo) =>
{
    var existing = await repo.GetLatestVersionAsync(templateId);
    if (existing == null)
    {
        return Results.NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));
    }

    // Create new version
    var newVersion = new FieldMappingTemplate
    {
        TemplateId = existing.TemplateId,
        Name = request.Name ?? existing.Name,
        Description = request.Description ?? existing.Description,
        TmsSystemId = existing.TmsSystemId,
        CustomerId = request.CustomerId ?? existing.CustomerId,
        Version = existing.Version + 1,
        Status = request.Status ?? existing.Status,
        SampleInputJson = request.SampleInputJson ?? existing.SampleInputJson,
        Metadata = request.Metadata ?? existing.Metadata
    };

    var updated = await repo.CreateAsync(newVersion);

    var response = new TemplateResponse
    {
        TemplateId = updated.TemplateId,
        Name = updated.Name,
        Description = updated.Description,
        TmsSystemId = updated.TmsSystemId,
        CustomerId = updated.CustomerId,
        Version = updated.Version,
        Status = updated.Status.ToString(),
        CreatedAt = updated.CreatedAt,
        UpdatedAt = updated.UpdatedAt,
        CreatedBy = updated.CreatedBy,
        SampleInputJson = updated.SampleInputJson,
        Metadata = updated.Metadata
    };

    return Results.Ok(ApiResponse<TemplateResponse>.SuccessResponse(response));
})
.WithName("UpdateTemplate")
.Produces<ApiResponse<TemplateResponse>>();

templatesGroup.MapDelete("/{templateId}", async (string templateId, ITemplateRepository repo, int? version = null) =>
{
    var existing = version.HasValue
        ? await repo.GetByIdAsync(templateId, version)
        : await repo.GetLatestVersionAsync(templateId);

    if (existing == null)
    {
        return Results.NotFound(ApiResponse<object>.ErrorResponse($"Template not found: {templateId}"));
    }

    await repo.DeleteAsync(templateId, version);
    return Results.NoContent();
})
.WithName("DeleteTemplate")
.Produces(StatusCodes.Status204NoContent);

templatesGroup.MapPost("/{templateId}/duplicate", async (
    string templateId,
    ITemplateRepository templateRepo,
    IFieldMappingRepository mappingRepo) =>
{
    var source = await templateRepo.GetLatestVersionAsync(templateId);
    if (source == null)
    {
        return Results.NotFound(ApiResponse<TemplateResponse>.ErrorResponse($"Template not found: {templateId}"));
    }

    // Create the duplicate template with a new identity
    var copy = new FieldMappingTemplate
    {
        TemplateId = Guid.NewGuid().ToString(),
        Name = $"{source.Name} - Copy",
        Description = source.Description,
        TmsSystemId = source.TmsSystemId,
        CustomerId = source.CustomerId,
        Version = 1,
        Status = TemplateStatus.Draft,
        SampleInputJson = source.SampleInputJson,
        Metadata = source.Metadata
    };

    var createdTemplate = await templateRepo.CreateAsync(copy);

    // Copy all field mappings from the source template
    var sourceMappings = await mappingRepo.GetByTemplateIdOrderedAsync(templateId);
    if (sourceMappings.Count > 0)
    {
        var copiedMappings = sourceMappings.Select(m => new FieldMapping
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = createdTemplate.TemplateId,
            SourcePath = m.SourcePath,
            TargetPath = m.TargetPath,
            TransformationType = m.TransformationType,
            TransformationConfig = m.TransformationConfig,
            ExecutionOrder = m.ExecutionOrder,
            IsRequired = m.IsRequired,
            DefaultValue = m.DefaultValue,
            ValidationRules = m.ValidationRules
        }).ToList();

        await mappingRepo.CreateBulkAsync(copiedMappings);
    }

    var response = new TemplateResponse
    {
        TemplateId = createdTemplate.TemplateId,
        Name = createdTemplate.Name,
        Description = createdTemplate.Description,
        TmsSystemId = createdTemplate.TmsSystemId,
        CustomerId = createdTemplate.CustomerId,
        Version = createdTemplate.Version,
        Status = createdTemplate.Status.ToString(),
        CreatedAt = createdTemplate.CreatedAt,
        UpdatedAt = createdTemplate.UpdatedAt,
        CreatedBy = createdTemplate.CreatedBy,
        SampleInputJson = createdTemplate.SampleInputJson,
        Metadata = createdTemplate.Metadata
    };

    return Results.Created($"/api/v1/templates/{createdTemplate.TemplateId}",
        ApiResponse<TemplateResponse>.SuccessResponse(response));
})
.WithName("DuplicateTemplate")
.Produces<ApiResponse<TemplateResponse>>(StatusCodes.Status201Created);

// Field Mappings API endpoints
var mappingsGroup = app.MapGroup("/api/v1/field-mappings")
    .WithTags("Field Mappings")
    .WithOpenApi();

mappingsGroup.MapGet("/", async (IFieldMappingRepository repo, string? templateId = null) =>
{
    var mappings = string.IsNullOrEmpty(templateId)
        ? new List<FieldMapping>() // Return empty if no template specified
        : await repo.GetByTemplateIdOrderedAsync(templateId);

    var response = new FieldMappingListResponse
    {
        Mappings = mappings.Select(m => new FieldMappingResponse
        {
            Id = m.Id,
            TemplateId = m.TemplateId,
            SourcePath = m.SourcePath,
            TargetPath = m.TargetPath,
            TransformationType = m.TransformationType.ToString(),
            TransformationConfig = m.TransformationConfig,
            ExecutionOrder = m.ExecutionOrder,
            IsRequired = m.IsRequired,
            DefaultValue = m.DefaultValue,
            ValidationRules = m.ValidationRules,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }).ToList(),
        TotalCount = mappings.Count
    };

    return Results.Ok(ApiResponse<FieldMappingListResponse>.SuccessResponse(response));
})
.WithName("GetFieldMappings")
.Produces<ApiResponse<FieldMappingListResponse>>();

mappingsGroup.MapGet("/{id}", async (string id, IFieldMappingRepository repo) =>
{
    var mapping = await repo.GetByIdAsync(id);
    if (mapping == null)
    {
        return Results.NotFound(ApiResponse<FieldMappingResponse>.ErrorResponse($"Field mapping not found: {id}"));
    }

    var response = new FieldMappingResponse
    {
        Id = mapping.Id,
        TemplateId = mapping.TemplateId,
        SourcePath = mapping.SourcePath,
        TargetPath = mapping.TargetPath,
        TransformationType = mapping.TransformationType.ToString(),
        TransformationConfig = mapping.TransformationConfig,
        ExecutionOrder = mapping.ExecutionOrder,
        IsRequired = mapping.IsRequired,
        DefaultValue = mapping.DefaultValue,
        ValidationRules = mapping.ValidationRules,
        CreatedAt = mapping.CreatedAt,
        UpdatedAt = mapping.UpdatedAt
    };

    return Results.Ok(ApiResponse<FieldMappingResponse>.SuccessResponse(response));
})
.WithName("GetFieldMappingById")
.Produces<ApiResponse<FieldMappingResponse>>();

mappingsGroup.MapPost("/", async (CreateFieldMappingRequest request, IFieldMappingRepository repo) =>
{
    var mapping = new FieldMapping
    {
        TemplateId = request.TemplateId,
        SourcePath = request.SourcePath,
        TargetPath = request.TargetPath,
        TransformationType = request.TransformationType,
        TransformationConfig = request.TransformationConfig,
        ExecutionOrder = request.ExecutionOrder,
        IsRequired = request.IsRequired,
        DefaultValue = request.DefaultValue,
        ValidationRules = request.ValidationRules
    };

    var created = await repo.CreateAsync(mapping);

    var response = new FieldMappingResponse
    {
        Id = created.Id,
        TemplateId = created.TemplateId,
        SourcePath = created.SourcePath,
        TargetPath = created.TargetPath,
        TransformationType = created.TransformationType.ToString(),
        TransformationConfig = created.TransformationConfig,
        ExecutionOrder = created.ExecutionOrder,
        IsRequired = created.IsRequired,
        DefaultValue = created.DefaultValue,
        ValidationRules = created.ValidationRules,
        CreatedAt = created.CreatedAt,
        UpdatedAt = created.UpdatedAt
    };

    return Results.Created($"/api/v1/field-mappings/{created.Id}",
        ApiResponse<FieldMappingResponse>.SuccessResponse(response));
})
.WithName("CreateFieldMapping")
.Produces<ApiResponse<FieldMappingResponse>>(StatusCodes.Status201Created);

mappingsGroup.MapPut("/{id}", async (string id, UpdateFieldMappingRequest request, IFieldMappingRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
    {
        return Results.NotFound(ApiResponse<FieldMappingResponse>.ErrorResponse($"Field mapping not found: {id}"));
    }

    existing.SourcePath = request.SourcePath;
    existing.TargetPath = request.TargetPath;
    existing.TransformationType = request.TransformationType;
    existing.TransformationConfig = request.TransformationConfig;
    existing.ExecutionOrder = request.ExecutionOrder;
    existing.IsRequired = request.IsRequired;
    existing.DefaultValue = request.DefaultValue;
    existing.ValidationRules = request.ValidationRules;

    var updated = await repo.UpdateAsync(existing);

    var response = new FieldMappingResponse
    {
        Id = updated.Id,
        TemplateId = updated.TemplateId,
        SourcePath = updated.SourcePath,
        TargetPath = updated.TargetPath,
        TransformationType = updated.TransformationType.ToString(),
        TransformationConfig = updated.TransformationConfig,
        ExecutionOrder = updated.ExecutionOrder,
        IsRequired = updated.IsRequired,
        DefaultValue = updated.DefaultValue,
        ValidationRules = updated.ValidationRules,
        CreatedAt = updated.CreatedAt,
        UpdatedAt = updated.UpdatedAt
    };

    return Results.Ok(ApiResponse<FieldMappingResponse>.SuccessResponse(response));
})
.WithName("UpdateFieldMapping")
.Produces<ApiResponse<FieldMappingResponse>>();

mappingsGroup.MapDelete("/{id}", async (string id, IFieldMappingRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
    {
        return Results.NotFound(ApiResponse<object>.ErrorResponse($"Field mapping not found: {id}"));
    }

    await repo.DeleteAsync(id);
    return Results.NoContent();
})
.WithName("DeleteFieldMapping")
.Produces(StatusCodes.Status204NoContent);

// Lookup Tables API endpoints
var lookupsGroup = app.MapGroup("/api/v1/lookup-tables")
    .WithTags("Lookup Tables")
    .WithOpenApi();

lookupsGroup.MapGet("/", async (ILookupTableRepository repo, string? tmsSystemId = null) =>
{
    var lookupTables = string.IsNullOrEmpty(tmsSystemId)
        ? await repo.GetAllAsync()
        : await repo.GetByTmsSystemIdAsync(tmsSystemId);

    var response = new LookupTableListResponse
    {
        LookupTables = lookupTables.Select(l => new LookupTableResponse
        {
            Id = l.Id,
            TmsSystemId = l.TmsSystemId,
            FieldName = l.FieldName,
            Name = l.Name,
            Description = l.Description,
            Mappings = l.Mappings,
            DefaultValue = l.DefaultValue,
            IsCaseSensitive = l.IsCaseSensitive,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            CreatedBy = l.CreatedBy
        }).ToList(),
        TotalCount = lookupTables.Count
    };

    return Results.Ok(ApiResponse<LookupTableListResponse>.SuccessResponse(response));
})
.WithName("GetLookupTables")
.Produces<ApiResponse<LookupTableListResponse>>();

lookupsGroup.MapGet("/{id}", async (string id, ILookupTableRepository repo) =>
{
    var lookupTable = await repo.GetByIdAsync(id);
    if (lookupTable == null)
    {
        return Results.NotFound(ApiResponse<LookupTableResponse>.ErrorResponse($"Lookup table not found: {id}"));
    }

    var response = new LookupTableResponse
    {
        Id = lookupTable.Id,
        TmsSystemId = lookupTable.TmsSystemId,
        FieldName = lookupTable.FieldName,
        Name = lookupTable.Name,
        Description = lookupTable.Description,
        Mappings = lookupTable.Mappings,
        DefaultValue = lookupTable.DefaultValue,
        IsCaseSensitive = lookupTable.IsCaseSensitive,
        CreatedAt = lookupTable.CreatedAt,
        UpdatedAt = lookupTable.UpdatedAt,
        CreatedBy = lookupTable.CreatedBy
    };

    return Results.Ok(ApiResponse<LookupTableResponse>.SuccessResponse(response));
})
.WithName("GetLookupTableById")
.Produces<ApiResponse<LookupTableResponse>>();

lookupsGroup.MapPost("/", async (CreateLookupTableRequest request, ILookupTableRepository repo) =>
{
    var lookupTable = new LookupTable
    {
        TmsSystemId = request.TmsSystemId,
        FieldName = request.FieldName,
        Name = request.Name,
        Description = request.Description,
        Mappings = request.Mappings,
        DefaultValue = request.DefaultValue,
        IsCaseSensitive = request.IsCaseSensitive
    };

    var created = await repo.CreateAsync(lookupTable);

    var response = new LookupTableResponse
    {
        Id = created.Id,
        TmsSystemId = created.TmsSystemId,
        FieldName = created.FieldName,
        Name = created.Name,
        Description = created.Description,
        Mappings = created.Mappings,
        DefaultValue = created.DefaultValue,
        IsCaseSensitive = created.IsCaseSensitive,
        CreatedAt = created.CreatedAt,
        UpdatedAt = created.UpdatedAt,
        CreatedBy = created.CreatedBy
    };

    return Results.Created($"/api/v1/lookup-tables/{created.Id}",
        ApiResponse<LookupTableResponse>.SuccessResponse(response));
})
.WithName("CreateLookupTable")
.Produces<ApiResponse<LookupTableResponse>>(StatusCodes.Status201Created);

lookupsGroup.MapPut("/{id}", async (string id, UpdateLookupTableRequest request, ILookupTableRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
    {
        return Results.NotFound(ApiResponse<LookupTableResponse>.ErrorResponse($"Lookup table not found: {id}"));
    }

    existing.FieldName = request.FieldName;
    existing.Name = request.Name;
    existing.Description = request.Description;
    existing.Mappings = request.Mappings;
    existing.DefaultValue = request.DefaultValue;
    existing.IsCaseSensitive = request.IsCaseSensitive;

    var updated = await repo.UpdateAsync(existing);

    var response = new LookupTableResponse
    {
        Id = updated.Id,
        TmsSystemId = updated.TmsSystemId,
        FieldName = updated.FieldName,
        Name = updated.Name,
        Description = updated.Description,
        Mappings = updated.Mappings,
        DefaultValue = updated.DefaultValue,
        IsCaseSensitive = updated.IsCaseSensitive,
        CreatedAt = updated.CreatedAt,
        UpdatedAt = updated.UpdatedAt,
        CreatedBy = updated.CreatedBy
    };

    return Results.Ok(ApiResponse<LookupTableResponse>.SuccessResponse(response));
})
.WithName("UpdateLookupTable")
.Produces<ApiResponse<LookupTableResponse>>();

lookupsGroup.MapDelete("/{id}", async (string id, ILookupTableRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
    {
        return Results.NotFound(ApiResponse<object>.ErrorResponse($"Lookup table not found: {id}"));
    }

    await repo.DeleteAsync(id);
    return Results.NoContent();
})
.WithName("DeleteLookupTable")
.Produces(StatusCodes.Status204NoContent);

// Transformation API endpoints
var transformGroup = app.MapGroup("/api/v1/transform")
    .WithTags("Transformation")
    .WithOpenApi();

transformGroup.MapPost("/", async (TransformRequest request, ITransformationService service) =>
{
    var result = await service.TransformAsync(request.SourceJson, request.TemplateId, request.Version);
    // Always return 200 with the full result so the client can display ALL errors/warnings at once
    return Results.Ok(ApiResponse<TransformationResult>.SuccessResponse(result));
})
.WithName("TransformJson")
.Produces<ApiResponse<TransformationResult>>();

transformGroup.MapPost("/preview", async (TransformRequest request, ITransformationService service) =>
{
    var result = await service.PreviewTransformationAsync(request.SourceJson, request.TemplateId, request.Version);
    return Results.Ok(ApiResponse<TransformationResult>.SuccessResponse(result));
})
.WithName("PreviewTransformation")
.Produces<ApiResponse<TransformationResult>>();

transformGroup.MapPost("/batch", async (BatchTransformRequest request, ITransformationService service) =>
{
    if (request.Records == null || request.Records.Count == 0)
        return Results.BadRequest(ApiResponse<object>.ErrorResponse("'records' must be a non-empty array."));

    var result = await service.TransformBatchAsync(
        request.TemplateId,
        request.Records,
        request.Version,
        new TransformOptions { Source = "BatchAPI", UserId = request.UserId });

    return Results.Ok(ApiResponse<BatchTransformResult>.SuccessResponse(result));
})
.WithName("BatchTransform")
.Produces<ApiResponse<BatchTransformResult>>();

// Transformation Log endpoints
var logGroup = app.MapGroup("/api/v1/transform-logs")
    .WithTags("TransformationLogs")
    .WithOpenApi();

logGroup.MapGet("/", async (ITransformationLogRepository repo, string? templateId = null, string? status = null, int limit = 100) =>
{
    var logs = templateId != null
        ? await repo.GetByTemplateIdAsync(templateId, limit)
        : await repo.GetAllAsync(limit);

    if (status != null)
        logs = logs.Where(l => l.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

    var response = logs.Select(l => new
    {
        l.Id,
        l.TemplateId,
        l.Timestamp,
        Status = l.Status.ToString(),
        l.ExecutionTimeMs,
        l.RecordCount,
        l.Source,
        l.UserId,
        l.ExpiresAt,
        HasErrors = l.Errors != null,
        HasOutput = l.OutputData != null
    });

    return Results.Ok(ApiResponse<object>.SuccessResponse(new { logs = response, totalCount = response.Count() }));
})
.WithName("GetTransformationLogs")
.Produces<ApiResponse<object>>();

logGroup.MapGet("/{id}", async (string id, ITransformationLogRepository repo) =>
{
    var log = await repo.GetByIdAsync(id);
    if (log == null) return Results.NotFound(ApiResponse<object>.ErrorResponse("Log entry not found"));

    return Results.Ok(ApiResponse<object>.SuccessResponse(new
    {
        log.Id,
        log.TemplateId,
        log.Timestamp,
        Status = log.Status.ToString(),
        log.ExecutionTimeMs,
        log.RecordCount,
        log.Source,
        log.UserId,
        log.ExpiresAt,
        log.InputData,
        log.OutputData,
        log.Errors
    }));
})
.WithName("GetTransformationLogById")
.Produces<ApiResponse<object>>();

// Customers API endpoints
var customersGroup = app.MapGroup("/api/v1/customers")
    .WithTags("Customers")
    .WithOpenApi();

customersGroup.MapGet("/", async (ICustomerRepository repo, bool? activeOnly = null) =>
{
    var customers = await repo.GetAllAsync(activeOnly);

    var response = new CustomerListResponse
    {
        Customers = customers.Select(c => new CustomerResponse
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            ContactEmail = c.ContactEmail,
            ContactPhone = c.ContactPhone,
            IsActive = c.IsActive,
            Notes = c.Notes,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            CreatedBy = c.CreatedBy
        }).ToList(),
        TotalCount = customers.Count
    };

    return Results.Ok(ApiResponse<CustomerListResponse>.SuccessResponse(response));
})
.WithName("GetAllCustomers")
.Produces<ApiResponse<CustomerListResponse>>();

customersGroup.MapGet("/{id}", async (string id, ICustomerRepository repo) =>
{
    var customer = await repo.GetByIdAsync(id);
    if (customer == null)
        return Results.NotFound(ApiResponse<CustomerResponse>.ErrorResponse($"Customer not found: {id}"));

    var response = new CustomerResponse
    {
        Id = customer.Id,
        Name = customer.Name,
        Code = customer.Code,
        ContactEmail = customer.ContactEmail,
        ContactPhone = customer.ContactPhone,
        IsActive = customer.IsActive,
        Notes = customer.Notes,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt,
        CreatedBy = customer.CreatedBy
    };

    return Results.Ok(ApiResponse<CustomerResponse>.SuccessResponse(response));
})
.WithName("GetCustomerById")
.Produces<ApiResponse<CustomerResponse>>();

customersGroup.MapPost("/", async (CreateCustomerRequest request, ICustomerRepository repo) =>
{
    var customer = new Customer
    {
        Name = request.Name,
        Code = request.Code,
        ContactEmail = request.ContactEmail,
        ContactPhone = request.ContactPhone,
        IsActive = request.IsActive,
        Notes = request.Notes,
        CreatedBy = request.CreatedBy
    };

    var created = await repo.CreateAsync(customer);

    var response = new CustomerResponse
    {
        Id = created.Id,
        Name = created.Name,
        Code = created.Code,
        ContactEmail = created.ContactEmail,
        ContactPhone = created.ContactPhone,
        IsActive = created.IsActive,
        Notes = created.Notes,
        CreatedAt = created.CreatedAt,
        UpdatedAt = created.UpdatedAt,
        CreatedBy = created.CreatedBy
    };

    return Results.Created($"/api/v1/customers/{created.Id}",
        ApiResponse<CustomerResponse>.SuccessResponse(response));
})
.WithName("CreateCustomer")
.Produces<ApiResponse<CustomerResponse>>(StatusCodes.Status201Created);

customersGroup.MapPut("/{id}", async (string id, UpdateCustomerRequest request, ICustomerRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
        return Results.NotFound(ApiResponse<CustomerResponse>.ErrorResponse($"Customer not found: {id}"));

    if (request.Name != null) existing.Name = request.Name;
    if (request.Code != null) existing.Code = request.Code;
    if (request.ContactEmail != null) existing.ContactEmail = request.ContactEmail;
    if (request.ContactPhone != null) existing.ContactPhone = request.ContactPhone;
    if (request.IsActive.HasValue) existing.IsActive = request.IsActive.Value;
    if (request.Notes != null) existing.Notes = request.Notes;

    var updated = await repo.UpdateAsync(existing);

    var response = new CustomerResponse
    {
        Id = updated.Id,
        Name = updated.Name,
        Code = updated.Code,
        ContactEmail = updated.ContactEmail,
        ContactPhone = updated.ContactPhone,
        IsActive = updated.IsActive,
        Notes = updated.Notes,
        CreatedAt = updated.CreatedAt,
        UpdatedAt = updated.UpdatedAt,
        CreatedBy = updated.CreatedBy
    };

    return Results.Ok(ApiResponse<CustomerResponse>.SuccessResponse(response));
})
.WithName("UpdateCustomer")
.Produces<ApiResponse<CustomerResponse>>();

customersGroup.MapDelete("/{id}", async (string id, ICustomerRepository repo) =>
{
    var existing = await repo.GetByIdAsync(id);
    if (existing == null)
        return Results.NotFound(ApiResponse<object>.ErrorResponse($"Customer not found: {id}"));

    await repo.DeleteAsync(id);
    return Results.NoContent();
})
.WithName("DeleteCustomer")
.Produces(StatusCodes.Status204NoContent);

// JSON Parser endpoints
var parseGroup = app.MapGroup("/api/v1/json/parse")
    .WithTags("JSON Parser")
    .WithOpenApi();

parseGroup.MapPost("/", async (JsonParseRequest request, IJsonParserService service) =>
{
    var isValid = await service.ValidateJsonAsync(request.JsonString);
    if (!isValid)
    {
        return Results.BadRequest(ApiResponse<object>.ErrorResponse("Invalid JSON"));
    }

    var fields = await service.ExtractFieldPathsAsync(request.JsonString, request.IncludeSampleValues);

    var response = new
    {
        IsValid = true,
        Fields = fields,
        TotalFields = fields.Count
    };

    return Results.Ok(ApiResponse<object>.SuccessResponse(response));
})
.WithName("ParseJson")
.Produces<ApiResponse<object>>();

app.Run();

public class TransformRequest
{
    public string SourceJson { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public int? Version { get; set; }
}

public class BatchTransformRequest
{
    /// <summary>The template to apply to every record.</summary>
    public string TemplateId { get; set; } = string.Empty;
    public int? Version { get; set; }
    /// <summary>Optional caller identity forwarded to the log entry.</summary>
    public string? UserId { get; set; }
    /// <summary>Array of TMS JSON objects to transform. Each element is processed independently.</summary>
    public List<System.Text.Json.JsonElement> Records { get; set; } = new();
}

public class JsonParseRequest
{
    public string JsonString { get; set; } = string.Empty;
    public bool IncludeSampleValues { get; set; } = true;
}
