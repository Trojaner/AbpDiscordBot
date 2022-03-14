namespace Sample.DiscordBot.Commands
{
    using Authentication;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Volo.Abp.DependencyInjection;
    using Volo.Abp.Security.Claims;
    using Volo.Abp.Uow;

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
            if (incomingMessage is not SocketUserMessage { Source: MessageSource.User } message)
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

                var userResolver = scope.ServiceProvider.GetRequiredService<IDiscordUserResolver>();
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