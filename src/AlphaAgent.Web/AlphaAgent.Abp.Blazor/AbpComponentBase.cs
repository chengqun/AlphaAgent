using AlphaAgent.Abp.Localization;
using Volo.Abp.AspNetCore.Components;

namespace AlphaAgent.Abp.Blazor;

public abstract class AbpComponentBase : Volo.Abp.AspNetCore.Components.AbpComponentBase
{
    protected AbpComponentBase()
    {
        LocalizationResource = typeof(AbpResource);
    }
}

