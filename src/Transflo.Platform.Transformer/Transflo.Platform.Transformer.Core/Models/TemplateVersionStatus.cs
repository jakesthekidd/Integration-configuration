namespace Transflo.Platform.Transformer.Core.Models;

/// <summary>
/// Lifecycle status for a <see cref="TemplateVersion"/>.
/// </summary>
public enum TemplateVersionStatus
{
    /// <summary>Work-in-progress; not yet active.</summary>
    Draft,

    /// <summary>The single active version for this template.</summary>
    Published,

    /// <summary>Was published but has been replaced by a newer published version.</summary>
    Superseded,

    /// <summary>Manually retired; no longer in use.</summary>
    Archived
}
