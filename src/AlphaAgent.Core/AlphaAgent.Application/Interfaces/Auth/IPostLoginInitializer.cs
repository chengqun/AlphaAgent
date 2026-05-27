using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Auth;

public interface IPostLoginInitializer
{
    /// <summary>
    /// 执行登录后的初始化流程：连接 SignalR、加载 Agent 配置、同步股票数据
    /// </summary>
    Task<PostLoginResult> InitializeAsync(string serverBaseAddress);
}

public class PostLoginResult
{
    public bool SignalRConnected { get; set; }
    public bool AgentConfigLoaded { get; set; }
    public bool SecuritySynced { get; set; }
}
