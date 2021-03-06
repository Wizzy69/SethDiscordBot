using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;

namespace PluginManager.Items;

public class ConsoleCommandsHandler
{
    private static readonly PluginsManager       manager     = new("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/Plugins.txt");
    public static           List<ConsoleCommand> commandList = new();
    private readonly        DiscordSocketClient? client;

    public ConsoleCommandsHandler(DiscordSocketClient client)
    {
        this.client = client;
        InitializeBasicCommands();
        Console.WriteLine("Initialized console command handler !");
    }

    private void InitializeBasicCommands()
    {
        var pluginsLoaded = false;
        commandList.Clear();

        AddCommand("help", "Show help", "help <command>", args =>
            {
                if (args.Length <= 1)
                {
                    Console.WriteLine("Available commands:");
                    List<string[]> items = new List<string[]>();
                    items.Add(new[] { "-", "-", "-" });
                    items.Add(new[] { "Command", "Description", "Usage" });
                    items.Add(new[] { " ", " ", "Argument type: <optional> [required]" });
                    items.Add(new[] { "-", "-", "-" });

                    foreach (var command in commandList)
                    {
                        var pa = from p in command.Action.Method.GetParameters()
                                 where p.Name != null
                                 select p.ParameterType.FullName;
                        items.Add(new[] { command.CommandName, command.Description, command.Usage });
                    }

                    items.Add(new[] { "-", "-", "-" });
                    Console_Utilities.FormatAndAlignTable(items);
                }
                else
                {
                    foreach (var command in commandList)
                        if (command.CommandName == args[1])
                        {
                            Console.WriteLine("Command description: " + command.Description);
                            Console.WriteLine("Command execution format:" + command.Usage);
                            return;
                        }

                    Console.WriteLine("Command not found");
                }
            }
        );


        AddCommand("lp", "Load plugins", () =>
            {
                if (pluginsLoaded) return;
                var loader = new PluginLoader(client!);
                loader.onCMDLoad += (name, typeName, success, exception) =>
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (name == null || name.Length < 2) name = typeName;
                    if (success)
                        Console.WriteLine("[CMD] Successfully loaded command : " + name);
                    else
                        Console.WriteLine("[CMD] Failed to load command : " + name + " because " + exception!.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                };
                loader.onEVELoad += (name, typeName, success, exception) =>
                {
                    if (name == null || name.Length < 2) name = typeName;
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (success)
                        Console.WriteLine("[EVENT] Successfully loaded event : " + name);
                    else
                        Console.WriteLine("[EVENT] Failed to load event : " + name + " because " + exception!.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                };
                loader.LoadPlugins();
                pluginsLoaded = true;
            }
        );

        AddCommand("listplugs", "list available plugins", async () => { await manager.ListAvailablePlugins(); });

        AddCommand("dwplug", "download plugin", "dwplug [name]", async args =>
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("Please specify plugin name");
                    return;
                }

                var name = args.MergeStrings(1);
                // info[0] = plugin type
                // info[1] = plugin link
                // info[2] = if others are required, or string.Empty if none
                var info = await manager.GetPluginLinkByName(name);
                if (info[1] == null) // link is null
                {
                    if (name == "")
                    {
                        Console_Utilities.WriteColorText("Name is invalid");
                        return;
                    }

                    Console_Utilities.WriteColorText($"Failed to find plugin &b{name} &c!" +
                                                     " Use &glistplugs &ccommand to display all available plugins !"
                    );
                    return;
                }

                string path;
                if (info[0] == "Command" || info[0] == "Event")
                    path = "./Data/Plugins/" + info[0] + "s/" + name + ".dll";
                else
                    path = $"./{info[1].Split('/')[info[1].Split('/').Length - 1]}";
                await ServerCom.DownloadFileAsync(info[1], path);
                if (info[0] == "Command" || info[0] == "Event")
                    if (info[0] == "Event")
                        Config.PluginConfig.InstalledPlugins.Add(new(name, PluginType.Event));
                    else if (info[0] == "Command") Config.PluginConfig.InstalledPlugins.Add(new(name, PluginType.Command));


                Console.WriteLine("\n");

                // check requirements if any

                if (info.Length == 3 && info[2] != string.Empty && info[2] != null)
                {
                    Console.WriteLine($"Downloading requirements for plugin : {name}");

                    var lines = await ServerCom.ReadTextFromFile(info[2]);

                    foreach (var line in lines)
                    {
                        if (!(line.Length > 0 && line.Contains(","))) continue;
                        var split = line.Split(',');
                        Console.WriteLine($"\nDownloading item: {split[1]}");
                        await ServerCom.DownloadFileAsync(split[0], "./" + split[1]);
                        Console.WriteLine();

                        if (split[0].EndsWith(".zip"))
                        {
                            Console.WriteLine($"Extracting {split[1]}");
                            var proc         = 0d;
                            var isExtracting = true;
                            var bar          = new Console_Utilities.ProgressBar { Max = 100, Color = ConsoleColor.Green };

                            IProgress<float> extractProgress = new Progress<float>(value => { proc = value; });
                            new Thread(new Task(() =>
                                           {
                                               while (isExtracting)
                                               {
                                                   bar.Update((int)proc);
                                                   if (proc >= 99.9f) break;
                                                   Thread.Sleep(500);
                                               }
                                           }
                                       ).Start
                            ).Start();
                            await Functions.ExtractArchive("./" + split[1], "./", extractProgress);
                            bar.Update(100);
                            isExtracting = false;
                            await Task.Delay(1000);
                            bar.Update(100);
                            Console.WriteLine("\n");
                            File.Delete("./" + split[1]);
                        }
                    }

                    Console.WriteLine();
                }
            }
        );


        AddCommand("value", "read value from VariableStack", "value [key]",args =>
            {
                if (args.Length != 2) return;
                if (!Config.ContainsKey(args[1])) return;

                var data = Config.GetValue<string>(args[1]);
                Console.WriteLine($"{args[1]} => {data}");
            }
        );

        AddCommand("add", "add variable to the system variables","add [key] [value] [isReadOnly=true/false]", args =>
            {
                if (args.Length < 4) return;
                var key        = args[1];
                var value      = args[2];
                var isReadOnly = args[3].Equals("true", StringComparison.CurrentCultureIgnoreCase);

                try
                {
                    Config.GetAndAddValueToVariable(key, value, isReadOnly);
                    Console.WriteLine($"Updated config file with the following command: {args[1]} => {value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        );

        AddCommand("remv", "remove variable from system variables", "remv [key]", args =>
            {
                if (args.Length < 2) return;
                Config.RemoveKey(args[1]);
            }
        );

        AddCommand("vars", "Display all variables", () =>
            {
                var d    = Config.GetAllVariables();
                var data = new List<string[]>();
                data.Add(new[] { "-", "-" });
                data.Add(new[] { "Key", "Value" });
                data.Add(new[] { "-", "-" });
                foreach (var kvp in d) data.Add(new[] { kvp.Key, kvp.Value.ToString()! });
                data.Add(new[] { "-", "-" });
                Console_Utilities.FormatAndAlignTable(data);
            }
        );

        AddCommand("sd", "Shuts down the discord bot", async () =>
            {
                if (client is null) return;
                await client.StopAsync();
                await client.DisposeAsync();
                Config.SaveConfig();
                Environment.Exit(0);
            }
        );
        //Sort the commands by name
        commandList.Sort((x, y) => x.CommandName.CompareTo(y.CommandName));
    }

    public static void AddCommand(string command, string description, string usage, Action<string[]> action)
    {
        commandList.Add(new ConsoleCommand
        {
            CommandName = command,
            Description = description,
            Action      = action,
            Usage       = usage
        });
        Console.ForegroundColor = ConsoleColor.White;
        Console_Utilities.WriteColorText($"Command &r{command} &cadded to the list of commands");
    }

    public static void AddCommand(string command, string description, Action action)
    {
        AddCommand(command, description, command, args => action());
    }

    public static void RemoveCommand(string command)
    {
        commandList.RemoveAll(x => x.CommandName == command);
    }

    public static bool CommandExists(string command)
    {
        return !(GetCommand(command) is null);
    }

    public static ConsoleCommand? GetCommand(string command)
    {
        return commandList.FirstOrDefault(t => t.CommandName == command);
    }

    public bool HandleCommand(string command, bool removeCommandExecution = true)
    {
        var args = command.Split(' ');
        foreach (var item in commandList.ToList())
            if (item.CommandName == args[0])
            {
                if (removeCommandExecution)
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    for (int i = 0; i < command.Length; i++) Console.Write(" ");
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                Console.WriteLine();
                item.Action(args);
                return true;
            }

        return false;
        //Console.WriteLine($"Executing: {args[0]} with the following parameters: {args.MergeStrings(1)}");
    }
}
