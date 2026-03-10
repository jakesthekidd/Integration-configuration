using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.Core.Services;

public class LookupDataProvider : ILookupDataProvider
{
    private readonly ILookupTableRepository _repository;

    public LookupDataProvider(ILookupTableRepository repository)
    {
        _repository = repository;
    }

    public async Task<LookupData?> GetAsync(string lookupTableId)
    {
        var table = await _repository.GetByIdAsync(lookupTableId);
        if (table == null)
        {
            return null;
        }

        return new LookupData
        {
            Mappings = table.Mappings ?? string.Empty,
            IsCaseSensitive = table.IsCaseSensitive,
            DefaultValue = table.DefaultValue
        };
    }
}
