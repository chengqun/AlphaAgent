using System.Threading.Tasks;

namespace AlphaAgent.Domain.Abstractions;

public interface IDatabaseInitializer
{
    Task InitializeAsync();
}
