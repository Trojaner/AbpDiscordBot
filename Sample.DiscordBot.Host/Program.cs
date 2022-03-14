using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Volo.Abp;

namespace Sample.DiscordBot
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                var host = CreateHostBuilder(args).Build();
                using (host)
                {
                    var initializer = host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
                    await initializer.InitializeAsync(host.Services);

                    await host.RunAsync();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        internal static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .UseCommandService((context, config) =>
                {
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = RunMode.Async;
                })
                .ConfigureHostConfiguration(builder =>
                {
                    ConfigureConfiguration(args, builder);
                })
                .ConfigureAppConfiguration(builder =>
                {
                    ConfigureConfiguration(args, builder);
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddApplication<DiscordBotHostModule>();
                })
                .ConfigureDiscordHost((context, configurationBuilder) =>
                {
                    configurationBuilder.Token = context.Configuration["Discord:Token"];
                })
                .UseAutofac()
                .UseSerilog()
                .UseConsoleLifetime();
        }

        internal static void ConfigureConfiguration(string[] args, IConfigurationBuilder builder)
        {
            builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("APP_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .AddUserSecrets(typeof(Program).Assembly, true)
                .AddCommandLine(args)
                .AddEnvironmentVariables();
        }
    }
}
