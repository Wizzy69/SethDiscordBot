# Seth Discord Bot

This is a Discord Bot made with C# that accepts plugins as extensions for more commands and events. All basic commands are built in already in the PluginManager class library. 
This project is based on:

- [.NET 6 (C#)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [Discord.Net](https://github.com/discord-net/Discord.Net)
- [Avalonia UI](https://avaloniaui.net/) for `DiscordBotUI` extension


## Plugins
#### Requirements:
- [Visual Studio](https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&channel=Release&version=VS2022&source=VSLandingPage&cid=2030&passive=false)
- .NET 6 (downloaded with Visual Studio)

Plugin Types:
1. Commands
2. Events


### How to create a plugin

First of all, Create a new project (class library) in Visual Studio.
![Imgur Image](https://i.imgur.com/KUqzKsB.png)

![Imgur Image](https://i.imgur.com/JzpEViR.png)

![Imgur Image](https://i.imgur.com/vtoEepX.png)

![Imgur Image](https://i.imgur.com/ceaVR2R.png)

Now, let's add the PluginManager reference. It can be found inside the bot's main folder under
`DiscordBot/bin/Debug/net6.0/PluginManager.dll` or `PluginManager/bin/Debug/net6.0/PluginManager.dll`
after one successfull build.

![Imgur Image](https://i.imgur.com/UMSitk4.png)

![Imgur Image](https://i.imgur.com/GEjShdl.png)

1. Commands

Commands are loaded when all plugins are loaded into memory. When an user executes the command, only then the Execute function is called.
Commands are plugins that allow users to interact with them. 
Here is an example of class that is a command class
```cs
using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

namespace CMD_Utils
{
    class FlipCoin : DBCommand
    {
        public string Command => "flip";

        public string Description => "Flip a coin";

        public string Usage => "flip";

        public bool canUseDM => true;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            System.Random random = new System.Random();
            int r = random.Next(1, 3);
            if (r == 1)
                await message.Channel.SendMessageAsync("Heads");
            else await message.Channel.SendMessageAsync("Tails");
        }
    }
}

```
#### Code description:
- Command - The keyword that triggers the execution for the command. This is what players must type in order to execute your command
- Description - The description of your command. Can be anything you like
- Usage - The usage of your command. This is what `help [Command]` command will display
- canUseDM - true if you plan to let users execute this command in DM chat with bot
- canUseServer - true if you plan to let the users execute this command in a server chat
- requireAdmin - true if this command requres an user with Administrator permission in the server
- Execute () - the function of your command.
  - context - the command context
  - message - the message itself
  - client - the discord bot client
  - isDM - true if the message was sent from DM chat

From here on, start coding. When your plugin is done, build it as any DLL project then add it to the following path
`{bot_executable}/Data/Plugins/Commands/<optional subfolder>/yourDLLName.dll`
Then, reload bot and execute command `lp` in bot's console. The plugin should be loaded into memory or an error is thrown if not. If an error is thrown, then
there is something wrong in your command's code.

2. Events

Events are loaded when all plugins are loaded. At the moment when they are loaded, the Start function is called.
Events are used if you want the bot to do something when something happens in server. The following example shows you how to catch when a user joins the server
and send to that user a DM message with `Welcome to server !`.

```cs
using PluginManager.Others;
using PluginManager.Interfaces;

public class OnUserJoin : DBEvent
{
    public string name => "MyEvent";

    public string description => "This is a demo event";

    public async void Start(Discord.WebSocket.DiscordSocketClient client)
    {
        Console.WriteLine($"Hello World from {name}");
        
        client.UserJoined += async (user) => {
            await (await user.CreateDMChannelAsync()).SendMessageAsync("Welcome to server !");
        };
    }
}
```

#### Code description:
- name - The name of your event. It will appear in console when it loads
- description - The description of your event
- Start() - The main body of your event. This is executed when the bot loads all plugins
  - client - the discord bot client

