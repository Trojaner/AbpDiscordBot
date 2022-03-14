using Discord.Commands;
using Sample.DiscordBot.Authorization;
using Sample.DiscordBot.Permissions;
using Volo.Abp.Users;

namespace Sample.DiscordBot.Modules
{
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
}
