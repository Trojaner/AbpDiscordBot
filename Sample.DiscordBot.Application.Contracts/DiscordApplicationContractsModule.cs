using Sample.DiscordBot.Permissions;
using Volo.Abp.Application;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;

namespace Sample.DiscordBot
{
    [DependsOn(
        typeof(AbpDddApplicationContractsModule)
    )]
    public class DiscordBotApplicationContractsModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpPermissionOptions>(options =>
            {
                options.DefinitionProviders.Add<DiscordBotPermissionDefinitionProvider>();
            });
        }
    }
}