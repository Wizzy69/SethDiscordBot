﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System;
using System.Threading.Tasks;

using static PluginManager.Others.Functions;

namespace PluginManager.Core
{
    internal class Boot
    {
        private readonly string botPrefix;
        private readonly string botToken;

        private bool isReady = false;

        public DiscordSocketClient? client;
        private CommandHandler? commandServiceHandler;
        private CommandService? service;

        public Boot(string botToken, string botPrefix)
        {
            this.botPrefix = botPrefix;
            this.botToken = botToken;
        }

        public async Task Awake()
        {
            client = new DiscordSocketClient();
            service = new CommandService();

            CommonTasks();

            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();

            commandServiceHandler = new CommandHandler(client, service, botPrefix);
            await commandServiceHandler.InstallCommandsAsync();

            while (!isReady) ;

        }

        public async Task ShutDown()
        {
            if (client == null) return;
            await client.StopAsync();
        }

        private void CommonTasks()
        {
            if (client == null)
                return;
            client.LoggedOut += Client_LoggedOut;
            client.Log += Log;
            client.LoggedIn += LoggedIn;
            client.Ready += Ready;
        }

        private Task Client_LoggedOut()
        {
            WriteLogFile("Successfully Logged Out");
            Log(new LogMessage(LogSeverity.Info, "Boot", "Successfully logged out from discord !"));
            return Task.CompletedTask;
        }

        private Task Ready()
        {
            Console.Title = "ONLINE";
            isReady = true;
            return Task.CompletedTask;
        }

        private Task LoggedIn()
        {
            Console.Title = "CONNECTED";
            WriteLogFile("The bot has been logged in at " + DateTime.Now.ToShortDateString() + " (" +
                         DateTime.Now.ToShortTimeString() + ")");
            return Task.CompletedTask;
        }

        private Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Error:
                case LogSeverity.Critical:
                    WriteErrFile(message.Message);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] " + message.Message);
                    Console.ForegroundColor = ConsoleColor.White;

                    break;

                case LogSeverity.Info:
                case LogSeverity.Debug:
                    WriteLogFile(message.Message);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[INFO] " + message.Message);
                    Console.ForegroundColor = ConsoleColor.White;


                    break;
            }

            return Task.CompletedTask;
        }
    }
}