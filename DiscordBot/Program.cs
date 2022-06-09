﻿using DiscordBot.Discord.Core;
using PluginManager;
using PluginManager.Items;
using PluginManager.Others;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.Online;

namespace DiscordBot
{
    public class Program
    {
        private static bool loadPluginsOnStartup  = false;
        private static bool listPluginsAtStartup  = false;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        [Obsolete]
        public static void Main(string[] args)
        {
            Directory.CreateDirectory("./Data/Resources");
            Directory.CreateDirectory("./Data/Plugins/Commands");
            Directory.CreateDirectory("./Data/Plugins/Events");
            PreLoadComponents().Wait();

            if (!Config.ContainsKey("token") || Config.GetValue<string>("token") == null || Config.GetValue<string>("token")?.Length != 70)
            {
                while (true)
                {
                    Console.WriteLine("Please insert your token");
                    Console.Write("Token = ");
                    string token = Console.ReadLine();
                    if (token?.Length == 59 || token?.Length == 70)
                        Config.AddValueToVariables("token", token, true);
                    else
                    {
                        Console.WriteLine("Invalid token");
                        continue;
                    }

                    Console.WriteLine("Please insert your prefix (max. 1 character long):");
                    Console.WriteLine("For a prefix longer then one character, the first character will be saved and the others will be ignored.\n No spaces or numbers allowed");
                    Console.Write("Prefix = ");
                    char prefix = Console.ReadLine()![0];

                    if (prefix == ' ' || char.IsDigit(prefix)) continue;
                    Config.AddValueToVariables("prefix", prefix.ToString(), false);
                    break;
                }
            }

            if (!Config.ContainsKey("prefix"))
            {
                Console.WriteLine("Please insert your prefix (max. 1 character long):");
                Console.WriteLine("For a prefix longer then one character, the first character will be saved and the others will be ignored.\n No spaces or numbers allowed");
                Console.Write("Prefix = ");
                char prefix = Console.ReadLine()![0];

                if (prefix == ' ' || char.IsDigit(prefix)) return;
                Config.AddValueToVariables("prefix", prefix.ToString(), false);
            }

            HandleInput(args).Wait();
        }

        /// <summary>
        /// The main loop for the discord bot
        /// </summary>
        /// <param name="discordbooter">The discord booter used to start the application</param>
        private static Task NoGUI(Boot discordbooter)
        {
            ConsoleCommandsHandler consoleCommandsHandler = new ConsoleCommandsHandler(discordbooter.client);
            if (loadPluginsOnStartup) consoleCommandsHandler.HandleCommand("lp");
            if (listPluginsAtStartup) consoleCommandsHandler.HandleCommand("listplugs");
            Config.SaveConfig();
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                string cmd = Console.ReadLine();
                consoleCommandsHandler.HandleCommand(cmd);
            }
        }

        /// <summary>
        /// Start the bot without user interface
        /// </summary>
        /// <returns>Returns the boot loader for the Discord Bot</returns>
        private static async Task<Boot> StartNoGUI()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Discord BOT for Cross Platform");
            Console.WriteLine("Created by: Wizzy\nDiscord: Wizzy#9181");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("============================ Discord BOT - Cross Platform ============================");

            try
            {
                string token  = Config.GetValue<string>("token");
                string prefix = Config.GetValue<string>("prefix");

                var discordbooter = new Boot(token, prefix);
                await discordbooter.Awake();
                return discordbooter;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Clear folder
        /// </summary>
        /// <param name="d">Directory path</param>
        private static Task ClearFolder(string d)
        {
            string[] files    = Directory.GetFiles(d);
            int      fileNumb = files.Length;
            for (var i = 0; i < fileNumb; i++)
            {
                File.Delete(files[i]);
                Console.WriteLine("Deleting : " + files[i]);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle user input arguments from the startup of the application
        /// </summary>
        /// <param name="args">The arguments</param>
        private static async Task HandleInput(string[] args)
        {
            int len = args.Length;
            if (len == 1 && args[0] == "--help")
            {
                Console.WriteLine("Available commands:\n--exec -> start the bot with tools enabled");
                return;
            }

            if (len == 1 && args[0] == "--logout")
            {
                File.Delete(Functions.dataFolder + "config.json");
                await Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        Environment.Exit(0x08);
                    }
                );
                return;
            }

            if (len >= 2 && args[0] == "--encrypt")
            {
                string s2e = args.MergeStrings(1);
                Console.WriteLine("MD5: " + await Cryptography.CreateMD5(s2e));
                Console.WriteLine("SHA356: " + await Cryptography.CreateSHA256(s2e));
                return;
            }

            if (len == 3 && args[0] == "/download")
            {
                string url      = args[1];
                string location = args[2];

                await ServerCom.DownloadFileAsync(url, location);

                return;
            }

            if (len > 0 && (args.Contains("--cmd") || args.Contains("--args") || args.Contains("--nomessage")))
            {
                if (args.Contains("lp") || args.Contains("loadplugins")) loadPluginsOnStartup = true;
                if (args.Contains("listplugs")) listPluginsAtStartup                          = true;
                len = 0;
            }


            if (len == 0 || args[0] != "--exec" && args[0] != "--execute")
            {
                Boot b = await StartNoGUI();
                await NoGUI(b);
                return;
            }


            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Execute command interface noGUI\n\n");
            Console.WriteLine(
                "\tCommand name\t\t\t\tDescription\n" +
                "-- help | -help\t\t ------ \tDisplay the help message\n" +
                "--reset-full\t\t ------ \tReset all files (clear files)\n" +
                "--reset-logs\t\t ------ \tClear up the output folder\n" +
                "--start\t\t ------ \tStart the bot\n" +
                "exit\t\t\t ------ \tClose the application"
            );
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("> ");
                string[] message = Console.ReadLine().Split(' ');

                switch (message[0])
                {
                    case "--help":
                    case "-help":
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(
                            "\tCommand name\t\t\t\tDescription\n" +
                            "-- help | -help\t\t ------ \tDisplay the help message\n" +
                            "--reset-full\t\t ------ \tReset all files (clear files)\n" +
                            "--reset-settings\t ------ \tReset only bot settings\n" +
                            "--reset-logs\t\t ------ \tClear up the output folder\n" +
                            "--start\t\t ------ \tStart the bot\n" +
                            "exit\t\t\t ------ \tClose the application"
                        );
                        break;
                    case "--reset-full":
                        await ClearFolder("./Data/Resources/");
                        await ClearFolder("./Output/Logs/");
                        await ClearFolder("./Output/Errors");
                        await ClearFolder("./Data/Languages/");
                        await ClearFolder("./Data/Plugins/Commands");
                        await ClearFolder("./Data/Plugins/Events");
                        Console.WriteLine("Successfully cleared all folders");
                        break;
                    case "--reset-logs":
                        await ClearFolder("./Output/Logs");
                        await ClearFolder("./Output/Errors");
                        Console.WriteLine("Successfully cleard logs folder");
                        break;
                    case "--exit":
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "--start":
                        Boot booter = await StartNoGUI();
                        await NoGUI(booter);
                        return;

                    default:
                        Console.WriteLine("Failed to execute command " + message[0]);
                        break;
                }
            }
        }

        private static async Task PreLoadComponents()
        {
            await Config.LoadConfig();
            if (Config.ContainsKey("DeleteLogsAtStartup"))
                if (Config.GetValue<bool>("DeleteLogsAtStartup"))
                    foreach (string file in Directory.GetFiles("./Output/Logs/"))
                        File.Delete(file);
        }
    }
}
