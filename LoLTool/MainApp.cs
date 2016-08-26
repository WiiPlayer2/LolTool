using CLAP;
using CLAP.Validation;
using LoLTool.Properties;
using RiotSharp;
using System;
using System.Collections.Generic;
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

        [Verb(Description = "Waits until the given summoner has finished its game and afterwards plays a sound notification")]
        public static void Chime(
            [Description("The summoner of which the game should be waited for")]
            string username,
            [Description("The check rate per second"), DefaultValue(5), MoreThan(0)]
            int checkRate,
            [Description("The .wav to be played or empty for default")]
            string sound)
        {
            RiotSharp.SummonerEndpoint.Summoner summoner;
            try
            {
                summoner = Api.GetSummoner(Settings.Default.Region, username);
            }
            catch (RiotSharpException e)
            {
                if (e.Message.StartsWith("404,"))
                {
                    Console.WriteLine("No such summoner found.");
                }
                else
                {
                    Console.WriteLine("Unknown error: {0}", e.Message);
                }
                return;
            }

            RiotSharp.CurrentGameEndpoint.CurrentGame game = null;
            try
            {
                game = Api.GetCurrentGame(Platform, summoner.Id);
                Console.WriteLine("Mode:      {0}", game.GameMode);
                Console.WriteLine("QueueType: {0}", game.GameQueueType);
                Console.WriteLine("GameType:  {0}", game.GameType);
                Console.WriteLine("StartTime: {0}", game.GameStartTime.ToLocalTime());
                Console.WriteLine();
            }
            catch (RiotSharpException)
            {
                Console.WriteLine("No game found.");
                return;
            }

            var top = Console.CursorTop;
            while (game != null)
            {
                try
                {
                    game = Api.GetCurrentGame(Platform, summoner.Id);

                    Console.CursorTop = top;
                    Console.WriteLine("Game is running for {0}", TimeSpan.FromSeconds(game.GameLength));
                }
                catch (RiotSharpException e)
                {
                    if (e.Message.StartsWith("404,"))
                    {
                        game = null;
                    }
                }
                Thread.Sleep(1000 * checkRate);
            }

            Console.WriteLine("Game finished.");

            SoundPlayer sndPlayer;
            if (string.IsNullOrWhiteSpace(sound))
            {
                sndPlayer = new SoundPlayer(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream("LoLTool.ChimeSound.wav"));
            }
            else
            {
                sndPlayer = new SoundPlayer(Path.GetFullPath(sound));
            }
            sndPlayer.PlaySync();
        }

        [Verb(Description = "Waits until the given summoner has started a game")]
        public static void Wait(
            [Description("The summoner of which the game should be waited for")]
            string username,
            [Description("The check rate per second"), DefaultValue(5), MoreThan(0)]
            int checkRate)
        {
            RiotSharp.SummonerEndpoint.Summoner summoner;
            try
            {
                summoner = Api.GetSummoner(Settings.Default.Region, username);
            }
            catch (RiotSharpException e)
            {
                if (e.Message.StartsWith("404,"))
                {
                    Console.WriteLine("No such summoner found.");
                }
                else
                {
                    Console.WriteLine("Unknown error: {0}", e.Message);
                }
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

        [Error]
        public static void HandleError(ExceptionContext context)
        {
            Console.WriteLine(context.Exception);
        }
    }
}
