using RiotSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLTool
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG && ONE_TIME
            args = new string[] { "addjob", "/name=test" };
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(":{0}", string.Join(" ", args.Select(o =>
            {
                if (o.Contains(" "))
                {
                    return string.Format("\"{0}\"", o);
                }
                return o;
            })));
            Console.WriteLine();
            Console.ForegroundColor = prevColor;
#elif DEBUG
            while (true)
            {
                var argSuccess = false;
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                do
                {
                    try
                    {
                        Console.Write(":");
                        var line = Console.ReadLine();
                        var argsList = new List<string>();
                        var str = new StringBuilder();
                        var inQuote = false;
                        foreach (var c in line)
                        {
                            switch (c)
                            {
                                case ' ':
                                    if (!inQuote)
                                    {
                                        argsList.Add(str.ToString());
                                        str.Clear();
                                    }
                                    else
                                    {
                                        str.Append(c);
                                    }
                                    break;
                                case '\"':
                                    inQuote = !inQuote;
                                    break;
                                default:
                                    str.Append(c);
                                    break;
                            }
                        }
                        argsList.Add(str.ToString());

                        args = argsList
                            .Where(o => !string.IsNullOrEmpty(o))
                            .ToArray();
                        argSuccess = true;
                    }
                    catch
                    {
                        argSuccess = false;
                    }
                }
                while (!argSuccess);
                Console.ForegroundColor = prevColor;

                try
                {
#endif

                    CLAP.Parser.Run<MainApp>(args);

#if DEBUG && ONE_TIME
            prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Press any key to exit...]");
            Console.ReadKey(true);
            Console.ForegroundColor = prevColor;
#elif DEBUG
                }
                catch (Exception e)
                {
                    prevColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ForegroundColor = prevColor;
                }
            }
#endif
        }
    }
}
