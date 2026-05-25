using Microsoft.Extensions.Localization;
using AlphaAgent.Abp.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AlphaAgent.Abp.Blazor;

[Dependency(ReplaceServices = true)]
public class AbpBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AbpResource> _localizer;

    public AbpBrandingProvider(IStringLocalizer<AbpResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
