# What is Discord?
[Discord](https://discord.com/) is a popular chat and VoIP platform. In this article we will write a Discord bot that integrates with ABP. This way we can use ABP features such as unit of work, authorization, users, etc.

Note: this article expects that you already have a running ABP website and that the bot will complement it.  

 ABP has also just created [it's own Discord server](https://discord.gg/CrYrd5vcGh) too. Don't forget join it!

## Discord Integration Libraries for .NET
There are two popular unofficial Discord integration libraries for .NET: [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) and [Discord.NET](https://github.com/discord-net/Discord.Net). We will use Discord .NET as it is modular and supports [.NET Generic Host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host) integration via a [third party package](https://github.com/Hawxy/Discord.Addons.Hosting).

# Getting started
We will create a Discord bot with .NET generic hosting support first.

Create a new .NET console project and name it Sample.DiscordBot.Host. After that, install `Discord.Net`, `Serilog.Extensions.Hosting`, `Serilog.Sinks.Console` and `Discord.Addons.Hosting` from NuGet.

## Setting up .NET Generic Host for Discord
Replace the Main method with the following code:
```c#
    
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
```

## Integrating ABP
### Initializing ABP
We have initialize ABP first. To do that, install the `Volo.Abp.Autofac` and `Volo.Abp.Security` packages from NuGet and add the following lines:
```c#
using (host)
{
    // Initialize ABP
    var initializer = host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
    await initializer.InitializeAsync(host.Services);

    await host.RunAsync();
}
```

### Adding the Host module
Create a new class called `DiscordBotHostModule`:
```c#
namespace Sample.DiscordBot
{
    [DependsOn(typeof(AbpAutofacModule))]
    public class DiscordBotHostModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            //...
        }
    }
}
```

Register the module in the host builder and add Autofac:
```c#
Host
    ...
    .ConfigureServices((_, services) =>
    {
        services.AddApplication<DiscordBotHostModule>();
    })
    .UseAutofac()
```

### Linking Discord and ABP Users
Create a new project, name it `Sample.DiscordBot.Application` and install `Discord.NET`, `Microsoft.AspNetCore.Identity` and `Volo.Abp.Ddd.Application`. 

Create and implement the following interface:
```c#
namespace Sample.DiscordBot.Authentication
{
    public interface IDiscordUserResolver
    {
        // Get's the ABP identity of a discord user
        // Return null if you fail to resolve a user (e.g. user is not registered or linked yet)
        Task<ClaimsPrincipal> ResolveAsync(IUser user);
    }
}
```

How you resolve users is up to you. For example, you could add a command like "!link" and redirect to your website with a token. Another alternative would be linking the discord account via the website with OpenID Connect. After that you could resolve the user with e.g. API and http proxies. 

Create and register a principal accessor and register it in the application module. The principal accessor will allow services and commands to resolve the current ABP user.
```c#
namespace Sample.DiscordBot.Authentication
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ICurrentPrincipalAccessor))]
    public class DiscordCurrentPrincipalAccessor : ICurrentPrincipalAccessor, IScopedDependency
    {
        public IDisposable Change(ClaimsPrincipal principal)
        {
            var previous = Principal;
            Principal = principal;
            return new DisposeAction(() => { Principal = previous; });
        }

        public ClaimsPrincipal Principal { get; set; }
    }
}
```

### Adding Command Permissions

 Create another new project, name it Sample.DiscordBot.Application.Contracts and install `Volo.Abp.Ddd.Domain` and `Volo.Abp.Authorization.Abstractions`.
 
 After that create and [register your permissions](https://docs.abp.io/en/abp/4.4/Authorization#permission-system) to ABP:
 ```c#
namespace Sample.DiscordBot.Permissions
{
    public class DiscordBotPermissions
    {
        public const string GroupName = "SampleDiscordBot";

        public static class Commands
        {
            public const string Default = GroupName + ".Commands";
            public const string Echo = Default + ".Echo"; 
        }
    }
}
```

```c#
namespace Sample.DiscordBot.Permissions
{
    public class DiscordBotPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            if (context.GetGroupOrNull(DiscordBotPermissions.GroupName) != null)
            {
                return;
            }

            var discordPermisssionGroup = context.AddGroup(DiscordBotPermissions.GroupName);
            discordPermisssionGroup.AddPermission(DiscordBotPermissions.Commands.Echo);
        }
    }
}
```

Create the application contracts module and register the permission definitions:
```c#
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
```
Don't forget to include the module as dependency in the host module.


### Handling Discord commands
Similar to the principal accessor, create a command context accessor in the application layer so we can access the current command context from commands and services:
```c#
namespace Sample.DiscordBot.Commands
{
    public interface IDiscordCommandContextAccessor
    {
        ICommandContext CommandContext { get; set; }
        int ArgsPos { get; set; }
    }
}
```

```c#
namespace Sample.DiscordBot.Commands
{
    public class DiscordCommandContextAccessor : IDiscordCommandContextAccessor, IScopedDependency
    {
        public ICommandContext CommandContext { get; set; }
        public int ArgsPos { get; set; }
    }
}
```

Now we can implement the command handler. The command handler will
* create a unit of work scope for each command execution,
* create a DI scope for each command execution for scoped dependencies,
* set the current command context and
* set the current user and principal.

The command handler is the heart of the discord bot ABP integration.

```c#
namespace Sample.DiscordBot.Commands
{
    public class DiscordCommandHandler : ISingletonDependency, IDisposable
    {
        public const string CommandPrefix = "!"; // alternatively read it from the config
        
        private readonly ILogger<DiscordCommandHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordSocketClient _discordClient;
        private readonly IdentityOptions _identityOptions;
        private readonly CommandService _commandService;
        private readonly Dictionary<ICommandContext, List<IDisposable>> _disposables;

        private bool _isSubscribed;

        public DiscordCommandHandler(
            ILogger<DiscordCommandHandler> logger,
            IServiceProvider serviceProvider,
            DiscordSocketClient discordClient,
            IOptions<IdentityOptions> identityOptionsAccessor,
            CommandService commandService)
        {
            _disposables = new Dictionary<ICommandContext, List<IDisposable>>();
            _logger = logger;
            _serviceProvider = serviceProvider;
            _identityOptions = identityOptionsAccessor.Value;
            _discordClient = discordClient;
            _commandService = commandService;
        }

        public void Subscribe()
        {
            if (!_isSubscribed)
            {
                _discordClient.MessageReceived += HandleMessage;
                _commandService.CommandExecuted += CommandExecutedAsync;
                _isSubscribed = true;
            }
        }

        public void Unsubscribe()
        {
            if (_isSubscribed)
            {
                _discordClient.MessageReceived -= HandleMessage;
                _commandService.CommandExecuted -= CommandExecutedAsync;
                _isSubscribed = false;
            }
        }

        private async Task HandleMessage(SocketMessage incomingMessage)
        {
            if (!(incomingMessage is SocketUserMessage message 
                || message.Source != MessageSource.User)
            {
                // Message is not from a user
                return;
            }

            // Optionally log all messages
            // _logger.LogInformation($"#{message.Channel.Name} <{message.Author.Username}#{message.Author.Discriminator}>: {message.Content}");
          
            var argPos = 0;
            if (!message.HasStringPrefix(CommandPrefix, ref argPos))
            {
                // message is not a command; ignore
                return;
            }
            
            var context = new SocketCommandContext(_discordClient, message);

            var scope = _serviceProvider.CreateScope();
            var uow = _serviceProvider.GetService<IUnitOfWorkManager>().Begin();

            var disposableContainer = new List<IDisposable> { uow, scope };
            _disposables.Add(context, disposableContainer);

            try
            {
                var contextAccessor = scope.ServiceProvider.GetRequiredService<IDiscordCommandContextAccessor>();
                contextAccessor.ArgsPos = argPos;
                contextAccessor.CommandContext = context;

                var userResolver= scope.ServiceProvider.GetRequiredService<IDiscordUserResolver>();
                var user = await userResolver.ResolveAsync(context.User);

                var discordPrincipalAccessor =
                    (DiscordCurrentPrincipalAccessor)scope.ServiceProvider
                        .GetRequiredService<ICurrentPrincipalAccessor>();
                discordPrincipalAccessor.Principal = user;

                await _commandService.ExecuteAsync(context, argPos, scope.ServiceProvider);
            }
            catch
            {
                DisposeContext(context);
                throw;
            }
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            DisposeContext(context);

            if (command.IsSpecified && !result.IsSuccess)
            {
                // Error or exception occurred; notify user 
                var prefix = "";
                if (context.Guild != null)
                {
                    // Not a private message; so ping user
                    prefix = $"<@!{context.User.Id}>: ";
                }

                await context.Channel.SendMessageAsync(prefix + result.ErrorReason);
            }
        }

        private void DisposeContext(ICommandContext context)
        {
            var disposables = _disposables[context];
            _disposables.Remove(context);

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }
} 
```
Update the host module to listen to Discord .NET events on application initialization:
```c#
public override void OnApplicationInitialization(ApplicationInitializationContext context)
{
    var commandHandler = context.ServiceProvider.GetRequiredService<DiscordCommandHandler>();
    commandHandler.Subscribe();
}
```

### Linking ABP Permissions
Create a new attribute called RequireAuthorization:
```c#
namespace Sample.DiscordBot.Authorization
{
    public class RequireAuthorizationAttribute : PreconditionAttribute
    {
        public string Permission { get; }

        public RequireAuthorizationAttribute(string permission = null)
        {
            Permission = permission;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var currentUser = services.GetRequiredService<ICurrentUser>();
            if (currentUser?.Id == null)
            {
                // Failed to resolve user
                return PreconditionResult.FromError(
                    "You can not use this command because your discord account is not linked yet.\n");
            }

            if (string.IsNullOrEmpty(Permission))
            {
                return PreconditionResult.FromSuccess();
            }

            var permissionChecker = services.GetRequiredService<IPermissionChecker>();
            var isGranted = await permissionChecker.IsGrantedAsync(Permission);   

            if (!isGranted)
            {
                return PreconditionResult.FromError(
                    $"You do not have access to this command (missing permission: {Permission}).");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
```

### Adding Commands
Create the application module:
```c#
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
```

Now you can add Discord .NET command modules:
```c#
public class PublicModule : ModuleBase<SocketCommandContext>
{
    private readonly ICurrentUser _currentUser;
    public PublicModule(
       ICurrentUser currentUser
    )
    {
        _currentUser = currentUser;
    }

    [Command("whoami")]
    public async Task WhoAmIAsync()
    {
        await ReplyAsync($"You are user {_currentUser.UserName}/{_currentUser.Id}");
    }
    
    [Command("echo")]
    [RequireAuthorization(DiscordBotPermissions.Commands.Echo)]
    public async Task EchoAsync(string message) 
    {
        await ReplyAsync($"You typed: {message}");
    }
}
```

Note: you don't have to register this module anywhere. Discord .NET will automatically register it.

## Adding Discord Bot Token
Create a Discord bot token as explained in [this article](https://www.writebots.com/discord-bot-token/).  After that add it to your appconfig.json like this: 
```json
{
   "Discord": {
      "Token": "..."
   }
}
```

Add the following to the host builder to read the token from the config:
```c#
return Host
    ...
    .ConfigureDiscordHost((context, configurationBuilder) =>
    {
        configurationBuilder.Token = context.Configuration["Discord:Token"];
    })
```

## Conclusion
We now have a working discord bot that integrates with ABP's  modularity, unit of work, principals/users and authorization. You can now easily use EntityFrameworkCore, auditing, local and distributed events, domain services,  i18n, etc. 

# Source Code
https://github.com/Trojaner/AbpDiscordBot
