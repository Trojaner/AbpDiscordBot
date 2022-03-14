using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Sample.DiscordBot
{
    using Permissions;

    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(DiscordBotPermissionDefinitionProvider))]
    public class DiscordBotHostModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            //...
        }
    }
}