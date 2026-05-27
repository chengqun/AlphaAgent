using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Domain.Abstractions;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Common;

public class CoreInitializer : ICoreInitializer
{
    private readonly IDatabaseInitializer _databaseInitializer;

    public CoreInitializer(IDatabaseInitializer databaseInitializer)
    {
        _databaseInitializer = databaseInitializer;
    }

    public async Task InitializeAsync()
    {
        await _databaseInitializer.InitializeAsync();
    }
}
