using RiotSharp;
using RiotSharp.CurrentGameEndpoint;
using RiotSharp.SummonerEndpoint;
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
    static class Helper
    {
        public static T TryApi<T>(Func<T> apiCall, int checkRate = 1, int maxRetries = -1)
        {
            for (var i = 0; i != maxRetries; i++)
            {
                try
                {
                    return apiCall();
                }
                catch (RiotSharpException e)
                {
                    if (e.Message.StartsWith("404,"))
                    {
                        return default(T);
                    }
                    if (!e.Message.StartsWith("500,"))
                    {
                        throw;
                    }
                    Thread.Sleep(1000 * checkRate);
                }
                catch (NullReferenceException)
                {
                    Thread.Sleep(2000 * checkRate);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            return default(T);
        }

        #region API Try Functions
        public static Summoner TryGetSummoner(this RiotApi api, Region region, string summonerName,
            int checkRate = 1, int maxRetries = -1)
        {
            return TryApi(() => api.GetSummoner(region, summonerName), checkRate, maxRetries);
        }

        public static CurrentGame TryGetCurrentGame(this RiotApi api, Platform platform, long summonerId,
            int checkRate = 1, int maxRetries = -1)
        {
            return TryApi(() => api.GetCurrentGame(platform, summonerId), checkRate, maxRetries);
        }
        #endregion

        public static void PlaySound(string soundFile, string defaultResource)
        {
            SoundPlayer sndPlayer;
            if (string.IsNullOrWhiteSpace(soundFile))
            {
                sndPlayer = new SoundPlayer(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(defaultResource));
            }
            else
            {
                sndPlayer = new SoundPlayer(Path.GetFullPath(soundFile));
            }
            sndPlayer.PlaySync();
        }
    }
}
