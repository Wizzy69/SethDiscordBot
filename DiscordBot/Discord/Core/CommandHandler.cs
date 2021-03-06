using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Loaders;
using PluginManager.Others;
using PluginManager.Others.Permissions;

namespace DiscordBot.Discord.Core;

internal class CommandHandler
{
    private readonly string              botPrefix;
    private readonly DiscordSocketClient client;
    private readonly CommandService      commandService;

    /// <summary>
    ///     Command handler constructor
    /// </summary>
    /// <param name="client">The discord bot client</param>
    /// <param name="commandService">The discord bot command service</param>
    /// <param name="botPrefix">The prefix to watch for</param>
    public CommandHandler(DiscordSocketClient client, CommandService commandService, string botPrefix)
    {
        this.client         = client;
        this.commandService = commandService;
        this.botPrefix      = botPrefix;
    }

    /// <summary>
    ///     The method to initialize all commands
    /// </summary>
    /// <returns></returns>
    public async Task InstallCommandsAsync()
    {
        client.MessageReceived += MessageHandler;
        await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
    }

    /// <summary>
    ///     The message handler for the bot
    /// </summary>
    /// <param name="Message">The message got from the user in discord chat</param>
    /// <returns></returns>
    private async Task MessageHandler(SocketMessage Message)
    {
        try
        {
            if (Message as SocketUserMessage == null) return;

            var message = Message as SocketUserMessage;

            if (message == null) return;

            if (!message.Content.StartsWith(botPrefix)) return;

            var argPos = 0;

            if (message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                await message.Channel.SendMessageAsync("Can not exec mentioned commands !");
                return;
            }

            if (message.Author.IsBot) return;

            var context = new SocketCommandContext(client, message);

            await commandService.ExecuteAsync(
                context,
                argPos,
                null
            );

            var plugin = PluginLoader.Commands!.Where(p => p.Command == message.Content.Split(' ')[0].Substring(botPrefix.Length)).FirstOrDefault();


            if (plugin != null)
            {
                if (message.Channel == await message.Author.CreateDMChannelAsync())
                {
                    if (plugin.canUseDM)
                    {
                        if (plugin.requireAdmin)
                        {
                            if (message.Author.isAdmin())
                            {
                                plugin.Execute(context, message, client, true);
                                Functions.WriteLogFile($"[{message.Author.Id}] Executed command (DM) : " + plugin.Command);
                                return;
                            }

                            await message.Channel.SendMessageAsync("This command is for administrators only !");
                            return;
                        }

                        plugin.Execute(context, message, client, true);
                        Functions.WriteLogFile($"[{message.Author.Id}] Executed command (DM) : " + plugin.Command);
                        return;
                    }

                    await message.Channel.SendMessageAsync("This command is not for DMs");
                    return;
                }

                if (plugin.canUseServer)
                {
                    if (plugin.requireAdmin)
                    {
                        if (message.Author.isAdmin())
                        {
                            plugin.Execute(context, message, client, false);
                            Functions.WriteLogFile($"[{message.Author.Id}] Executed command : " + plugin.Command);
                            return;
                        }

                        await message.Channel.SendMessageAsync("This command is for administrators only !");
                        return;
                    }

                    plugin.Execute(context, message, client, false);
                    Functions.WriteLogFile($"[{message.Author.Id}] Executed command : " + plugin.Command);
                }
            }
        }
        catch
        {
        }
    }
}
