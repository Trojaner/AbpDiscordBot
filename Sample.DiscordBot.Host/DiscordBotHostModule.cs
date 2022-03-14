using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Sample.DiscordBot
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(DiscordBotApplicationModule))]
    public class DiscordBotHostModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            //...
        }
    }
}