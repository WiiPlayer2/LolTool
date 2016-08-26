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
    static class Helper
    {
        public static T TryApi<T>(Func<T> apiCall, int checkRate = 5, int maxRetries = 5)
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
            }
            return default(T);
        }

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
