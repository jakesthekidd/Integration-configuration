using System.Text.Json;

namespace Transflo.Platform.Transformer.Core.DTOs;

/// <summary>
/// Request body for the route-based transform endpoints on <c>TemplateVersionsController</c>.
/// The template ID and version number are supplied in the route; this record carries only
/// the source document.
///
/// <para>
/// <c>SourceDocument</c> accepts either form:
/// <list type="bullet">
///   <item>A raw JSON object: <c>{ "field": "value" }</c></item>
///   <item>A serialized JSON string: <c>"{ \"field\": \"value\" }"</c></item>
/// </list>
/// </para>
/// </summary>
public sealed record VersionTransformRequest
{
    /// <summary>
    /// The source document to transform. May be a raw JSON object/array or a
    /// JSON-encoded string containing the document.
    /// </summary>
    public JsonElement SourceDocument { get; init; }
}
