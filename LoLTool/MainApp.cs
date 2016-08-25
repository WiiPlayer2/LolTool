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

        [Empty, Help, Verb(IsDefault = true)]
        public static void Help(string help)
        {
            Console.WriteLine("LolTool 0.1");
            Console.WriteLine("Copyright © DarkLink 2016");
            Console.WriteLine(help);
        }

        [Global]
        public static void SetApiKey(string key)
        {
            Settings.Default.ApiKey = key;
            Settings.Default.Save();
        }

        [Global]
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

        [Verb]
        public static void Chime(string username,
            [DefaultValue(5), MoreThan(0)]int checkRate,
            string sound)
        {
            var summoner = Api.GetSummoner(Settings.Default.Region, username);
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
            if (sound == null)
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

        [Error]
        public static void HandleError(ExceptionContext context)
        {
            Console.WriteLine(context.Exception);
        }
    }
}
