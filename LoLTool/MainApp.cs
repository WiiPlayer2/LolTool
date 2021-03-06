﻿using CLAP;
using CLAP.Validation;
using LoLTool.Properties;
using RiotSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoLTool
{
    class MainApp
    {
        private static RiotApi riotApi;
        private static StaticRiotApi staticRiotApi;

        public static RiotApi Api
        {
            get
            {
                if (riotApi != null)
                {
                    return riotApi;
                }
                if (!string.IsNullOrWhiteSpace(Settings.Default.ApiKey))
                {
                    riotApi = RiotApi.GetInstance(Settings.Default.ApiKey);
                }
                return riotApi;
            }
        }

        public static StaticRiotApi StaticApi
        {
            get
            {
                if (staticRiotApi != null)
                {
                    return staticRiotApi;
                }
                if (!string.IsNullOrWhiteSpace(Settings.Default.ApiKey))
                {
                    staticRiotApi = StaticRiotApi.GetInstance(Settings.Default.ApiKey);
                }
                return staticRiotApi;
            }
        }

        [Empty, Verb(IsDefault = true, Description = "Shows the version")]
        public static void Version()
        {
            Help(null);
        }

        [Help]
        public static void Help(string help)
        {
            Console.WriteLine("LolTool 0.1");
            Console.WriteLine("Copyright © DarkLink 2016");
            if (!string.IsNullOrWhiteSpace(help))
            {
                Console.WriteLine(help);
            }
        }

        #region Global Parameters
        [Global(Description = "Sets and saves the Riot Development Key")]
        public static void SetApiKey(string key)
        {
            Settings.Default.ApiKey = key;
            Settings.Default.Save();
        }

        [Global(Description = "Sets and saves the region for which data should be received")]
        public static void SetRegion(Region region)
        {
            Settings.Default.Region = region;
            Settings.Default.Save();
        }

        [Global(Description = "Sets the parent directory of League of Legends (by default \"C:\\Riot Games\")")]
        public static void SetLoLDir(string lolDirectory)
        {
            Settings.Default.LolDirectory = lolDirectory;
            Settings.Default.Save();
        }
        #endregion

        private static Platform Platform
        {
            get
            {
                switch (Settings.Default.Region)
                {
                    case Region.euw:
                        return Platform.EUW1;
                    default:
                        throw new NotImplementedException(Settings.Default.Region.ToString());
                }
            }
        }

        #region Verbs
        [Verb(Description = "Waits until the given summoner has finished its game and afterwards plays a sound notification")]
        public static void Chime(
            [Description("The summoner of which the game should be waited for")]
            string username,
            [Description("The check rate per second"), DefaultValue(5), MoreThan(0)]
            int checkRate,
            [Description("The .wav to be played or empty for default")]
            string sound)
        {
            var summoner = Api.TryGetSummoner(Settings.Default.Region, username, checkRate);
            if (summoner == null)
            {
                Console.WriteLine("No such summoner found.");
                return;
            }

            var game = Api.TryGetCurrentGame(Platform, summoner.Id, checkRate);
            if (game == null)
            {
                Console.WriteLine("No game found.");
                return;
            }

            while (game.GameStartTime.Year < 2000)
            {
                Thread.Sleep(1000 * checkRate);
                game = Api.TryGetCurrentGame(Platform, summoner.Id, checkRate);
            }

            Console.WriteLine("Mode:      {0}", game.GameMode);
            Console.WriteLine("QueueType: {0}", game.GameQueueType);
            Console.WriteLine("GameType:  {0}", game.GameType);
            Console.WriteLine("StartTime: {0}", game.GameStartTime.ToLocalTime());

            var top = Console.CursorTop;
            while (game != null)
            {
                Console.CursorTop = top;
                Console.WriteLine("Game is running for {0} ", TimeSpan.FromSeconds(game.GameLength));
                Thread.Sleep(1000 * checkRate);

                game = Api.TryGetCurrentGame(Platform, summoner.Id);
            }

            Console.WriteLine("Game finished.");
            Helper.PlaySound(sound, "LoLTool.ChimeSound.wav");
        }

        [Verb(Description = "Waits until the given summoner has started a game")]
        public static void Wait(
            [Description("The summoner of which the game should be waited for")]
            string username,
            [Description("The check rate per second"), DefaultValue(5), MoreThan(0)]
            int checkRate)
        {
            var summoner = Helper.TryApi(() => Api.GetSummoner(Settings.Default.Region, username), checkRate);
            if (summoner == null)
            {
                Console.WriteLine("No such summoner found.");
                return;
            }


            RiotSharp.CurrentGameEndpoint.CurrentGame game = null;
            do
            {
                try
                {
                    game = Api.GetCurrentGame(Platform, summoner.Id);
                }
                catch (RiotSharpException)
                {
                    Thread.Sleep(1000 * checkRate);
                }
            }
            while (game == null);
            Console.WriteLine("Game found.");
        }

        [Verb(Description = "Waits until the given summoner has started a custom game and plays a sound after 5 minutes")]
        public static void CSTrainer(
            [Description("The summoner of which the game should be waited for")]
            string username,
            [Description("The check rate per second"), DefaultValue(5), MoreThan(0)]
            int checkRate,
            [Description("The .wav to be played or empty for default")]
            string sound)
        {
            var summoner = Helper.TryApi(() => Api.GetSummoner(Settings.Default.Region, username), checkRate);
            if (summoner == null)
            {
                Console.WriteLine("No such summoner found.");
                return;
            }

            Console.WriteLine("Waiting for custom game...");
            RiotSharp.CurrentGameEndpoint.CurrentGame game = null;
            while (game == null
                || game.GameType != GameType.CustomGame
                || game.GameStartTime.Year < 2000)
            {
                game = Helper.TryApi(() => Api.GetCurrentGame(Platform, summoner.Id), checkRate);
                Thread.Sleep(1000 * checkRate);
            }

            var localTime = game.GameStartTime.ToLocalTime();
            var targetTime = localTime.AddMinutes(5).AddSeconds(5);
            Console.WriteLine("Game found. Started at {0}", localTime);
            Console.WriteLine("Waiting until {0}...", targetTime);

            var diff = targetTime - DateTime.Now;
            Thread.Sleep((int)diff.TotalMilliseconds);
            Console.WriteLine("5 Minutes have passed. Finish up your wave!");
            Helper.PlaySound(sound, "LoLTool.ChimeSound.wav");
        }

        [Verb(Description = "Checks the winning or losing streak for every summoner in the game of the given summoner")]
        public static void StreakCheck(
            [Description("The summoner of which the game should be waited for")]
            string username,
            [Description("Shows only enemy summoners")]
            bool enemyOnly)
        {
            var summoner = Helper.TryApi(() => Api.GetSummoner(Settings.Default.Region, username));
            if (summoner == null)
            {
                Console.WriteLine("No such summoner found.");
                return;
            }

            var game = Helper.TryApi(() => Api.GetCurrentGame(Platform, summoner.Id));
            if (game == null)
            {
                Console.WriteLine("No game found.");
                return;
            }

            var ownTeam = game.Participants
                .Single(o => o.SummonerId == summoner.Id).TeamId;
            foreach (var p in game.Participants
                .Where(o => !o.Bot))
            {
                if (!enemyOnly || ownTeam != p.TeamId)
                {
                    var champ = Helper.TryApi(() => StaticApi.GetChampion(Settings.Default.Region, (int)p.ChampionId), 2);
                    var recents = Helper.TryApi(() => Api.GetRecentGames(Settings.Default.Region, p.SummonerId), 2);
                    var recentWins = recents.Select(o => o.Statistics.Win);
                    var isWinningStreak = recentWins.FirstOrDefault();
                    var streakCount = recentWins.TakeWhile(o => o == isWinningStreak).Count();
                    Console.WriteLine("{0,-20} ({1,-15}) is on a {2} streak with {3,2} games",
                        p.SummonerName, champ.Name, isWinningStreak ? "winning" : "losing ", streakCount);
                }
            }
        }

        [Verb(Description = "Spectates the given summoner")]
        public static void Spectate(
            [Description("The summoner of which the game should be spectated")]
            string username)
        {
            var summoner = Api.TryGetSummoner(Settings.Default.Region, username);
            if (summoner == null)
            {
                Console.WriteLine("No such summoner found.");
                return;
            }

            var game = Api.TryGetCurrentGame(Platform, summoner.Id);
            if (game == null)
            {
                Console.WriteLine("No game found.");
                return;
            }

            var folder = Path.Combine(Settings.Default.LolDirectory, "League of Legends", "RADS", "solutions", "lol_game_client_sln", "releases");
            if (!Directory.Exists(folder))
            {
                Console.WriteLine("League of Legends not found at \"{0}\"", Settings.Default.LolDirectory);
                return;
            }

            var versionFolder = Directory.EnumerateDirectories(folder).FirstOrDefault();
            if (versionFolder == null)
            {
                Console.WriteLine("League of Legends game client not found.");
                return;
            }

            var exeFile = Path.Combine(versionFolder, "deploy", "League of Legends.exe");
            Environment.CurrentDirectory = Path.GetDirectoryName(exeFile);
            var arg = $"\"8394\" \"LoLLauncher.exe\" \"\" {GetSpectatorArgument(game)}";
            Process.Start(exeFile, arg).WaitForExit();
        }
        #endregion

        [Error]
        public static void HandleError(ExceptionContext context)
        {
            Console.WriteLine(context.Exception);
        }

        #region Helper
        private static string GetSpectatorArgument(RiotSharp.CurrentGameEndpoint.CurrentGame game)
        {
            var endpoint = "";
            switch (Settings.Default.Region)
            {
                case Region.euw:
                    endpoint = "spectator.euw1.lol.riotgames.com:80";
                    break;
                default:
                    throw new NotImplementedException();
            }

            return $"\"spectator {endpoint} {game.Observers.EncryptionKey} {game.GameId} {Platform}\"";
        }
        #endregion
    }
}
