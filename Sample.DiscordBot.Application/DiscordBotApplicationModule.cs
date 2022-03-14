using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace Sample.DiscordBot
{
    [DependsOn(
        typeof(AbpDddApplicationModule),
        typeof(DiscordBotApplicationContractsModule)
    )]
    public class DiscordBotApplicationModule : AbpModule
    {

    }
}