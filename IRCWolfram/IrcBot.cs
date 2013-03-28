using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRClient
{
    class IrcBot : IrcClient
    {
        public IrcBot(string server, int port, string nick, string name, string host) : base(server, port, nick, name, host)
        {
            Message += IrcBot_Message;
        }

        private void IrcBot_Message(object sender, IrcEventArgs e)
        {
            Console.WriteLine(e.Message);
            var ex = e.Message.Split(new[]{' '}, 5);
            if (ex.Length > 4) //is the command received long enough to be a bot command?
            {
                string command = ex[3]; //grab the command sent
                if (command.StartsWith(":!") && BotCommands.ContainsKey(command.Substring(2)))
                {
                    Write(BotCommands[command.Substring(2)].Apply(command, ex[4], this));
                }
            }
        }

        private static readonly Dictionary<string, Command> BotCommands = new Dictionary<string, Command>()
            {
                {"privmsg", new Command((x, y, z) =>
                    {
                        var i = y.Split(new[] {' '}, 2);
                        return "PRIVMSG " + i[0] + " :" + i[1];
                    })},
                {"nick", new Command((x, y, z) =>"NICK " + z.Nick)},
                {"join", new Command((x, y, z) =>
                    {
                        z.Channel = y.Split(' ')[0];
                        return "JOIN " + z.Channel;
                    })},
                {"ping", new Command((x, y, z) => "PING " + y.Split(' ')[0])},
                {"quit", new Command((x,y,z) => "QUIT")},
                {"me", new Command((x, y, z) => "PRIVMSG " + z.Channel + " :\u0001" + "ACTION " + y + "\u0001")},
                {"say", new Command((x,y,z) => y)},
            };

        public void Register(string name, Command command)
        {
            BotCommands.Add(name, command);
        }
    }
}
