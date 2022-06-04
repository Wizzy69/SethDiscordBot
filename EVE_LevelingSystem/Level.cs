﻿using Discord.WebSocket;
using EVE_LevelingSystem.LevelingSystemCore;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace EVE_LevelingSystem
{
    internal class Level : DBEvent
    {
        public          string   name        => "Leveling System Event Handler";
        public          string   description => "The Leveling System Event Handler";
        internal static Settings globalSettings = new();


        public async void Start(DiscordSocketClient client)
        {
            Directory.CreateDirectory("./Data/Resources/LevelingSystem");
            Config.AddValueToVariables("LevelingSystemPath", "./Data/Resources/LevelingSystem");
            Config.AddValueToVariables("LevelingSystemSettingsFile", "./Data/Resources/LevelingSystemSettings.txt");

            if (!File.Exists(Config.GetValue("LevelingSystemSettingsFile")))
            {
                globalSettings = new Settings { TimeToWaitBetweenMessages = 5 };
                await Functions.SaveToJsonFile<Settings>(Config.GetValue("LevelingSystemSettingsFile"), globalSettings);
            }
            else
                globalSettings = await Functions.ConvertFromJson<Settings>(Config.GetValue("LevelingSystemSettingsFile"));

            // Console.WriteLine(globalSettings.TimeToWaitBetweenMessages);
            client.MessageReceived += ClientOnMessageReceived;
        }

        private async Task ClientOnMessageReceived(SocketMessage arg)
        {
            if (arg.Author.IsBot || arg.IsTTS || arg.Content.StartsWith(Config.GetValue("prefix"))) return;
            string userID = arg.Author.Id.ToString();
            User   user;
            if (File.Exists($"{Config.GetValue("LevelingSystemPath")}/{userID}.dat"))
            {
                user = await Functions.ConvertFromJson<User>(Config.GetValue("LevelingSystemPath")! + $"/{userID}.dat");
                // Console.WriteLine(Config.GetValue("LevelingSystemPath"));
                if (user.AddEXP()) await arg.Channel.SendMessageAsync($"{arg.Author.Mention} is now level {user.CurrentLevel}");
                await Functions.SaveToJsonFile(Config.GetValue("LevelingSystemPath") + $"/{userID}.dat", user);
                return;
            }

            user = new User() { CurrentEXP = 0, CurrentLevel = 1, RequiredEXPToLevelUp = LevelCalculator.GetNextLevelRequiredEXP(1), userID = userID };
            if (user.AddEXP()) await arg.Channel.SendMessageAsync($"{arg.Author.Mention} is now level {user.CurrentLevel}");
            await Functions.SaveToJsonFile($"{Config.GetValue("LevelingSystemPath")}/{userID}.dat", user);
        }
    }
}
