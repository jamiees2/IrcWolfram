using System;
using System.Threading;

namespace IRCWolfram
{
    class Program
    {
        static void Main(string[] args)
        {
            const string server = "irc.rizon.net";
            const int port = 6670;
            var ircClient = new IrcWolfram(server, port, "Dawg", "finalC", "tal.is") { Channel = "#TS" };
            ircClient.Connect();
            new Thread(ircClient.Start).Start();
            new Thread(delegate()
            {
                while (ircClient.IsConnected)
                {
                    var line = Console.ReadLine();
                    ircClient.Write(line);
                }
            }).Start();
        }
    }
}
