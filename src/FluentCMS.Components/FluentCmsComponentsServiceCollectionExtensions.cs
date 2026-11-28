using FluentCMS.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class FluentCmsComponentsServiceCollectionExtensions
{
    public static IServiceCollection AddFluentCmsComponents(this IServiceCollection services)
    {
        services.TryAddSingleton<IFlTextProvider>(EnglishTextProvider.Instance);
        return services;
    }
}
