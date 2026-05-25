using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Common;

public interface ICoreInitializer
{
    Task InitializeAsync();
}