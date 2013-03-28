using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IRClient;
using WolframAlphaNET;
using WolframAlphaNET.Misc;
using WolframAlphaNET.Objects;

namespace IRCWolfram
{
    class IrcWolfram : IrcClient
    {
        private static readonly WolframAlpha Wolfram = new WolframAlpha("APJV98-3VQUV4P2TL");
        
        public IrcWolfram(string server, int port, string nick, string name, string host) : base(server, port, nick, name, host)
        {
            Message += IrcWolfram_Message;
            //wolfram.ScanTimeout = 0.1f;
            Wolfram.UseTLS = true;
            Wolfram.IgnoreCase = true;
            Wolfram.ReInterpret = true;
            //wolfram.
            RegisterAlias("gtfo","quit");
        }

        private string ParseCommand(string msg)
        {
            var spacegroups = msg.Split(new[] {' '}, 2);
            if (spacegroups.Length > 1 && msg.ToLower().StartsWith(Nick.ToLower())) return spacegroups[1];
            var match = Regex.Match(msg, @"([\w\d\s]*)\W?\s" + Nick + @"\W?", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "";
        }

        private void IrcWolfram_Message(object sender, IrcEventArgs e)
        {
            Console.WriteLine(e.Message);
            var x = e.Message.Split(new[]{' '}, 4);
            if (x.Length < 4 /*|| !x[0].Contains("tal.is") || !x[0].Contains("finalC")*/) return;
            var cmd = x[3].Substring(1); //grab the command sent
            cmd = ParseCommand(cmd);
            var ex = cmd.Split(new[] { ' ' }, 2);
            var command = ex[0];
            
            if (BotCommands.ContainsKey(command) && ex.Length > 1)
                Write(BotCommands[command].Apply(command, ex[1], this));
            else if (BotCommands.ContainsKey(command))
                Write(BotCommands[command].Apply(command, "", this));
            else if (cmd != "")
            {
                Console.WriteLine("Querying wolfram with: " + cmd);

                
                var results = Wolfram.Query(cmd);
                if (results.Error != null)
                    WriteMsg("Woops, there was an error: " + results.Error.Message);
                        
                if (results.DidYouMean.HasElements())
                {
                    foreach (var didYouMean in results.DidYouMean)
                    {
                        WriteMsg("Did you mean: " + didYouMean.Value);
                    }
                }
                //Results are split into "pods" that contain information. Those pods can also have subpods.
                var primaryPod = results.GetPrimaryPod();

                if (primaryPod != null)
                {
                    OutputResultPod(primaryPod);
                }
                else
                {

                    results.RecalculateResults();
                    if (results.Pods == null) return;
                    foreach (var pod in results.Pods)
                    {
                        OutputResultPod(pod);
                    }
                    //OutputResultPod(results.GetPrimaryPod());
                }
            }
        }

        private void OutputResultPod(Pod pod)
        {
            //Results are split into "pods" that contain information. Those pods can also have subpods.

            if (pod == null) return;
            if (!pod.SubPods.HasElements()) return;
            foreach (var subPod in pod.SubPods)
            {
                //WriteMsg(subPod.Title);
                
                foreach (var s in subPod.Plaintext.Split('\n'))
                {
                    if (Regex.IsMatch(s, @".*\|.*\|.*\|.*")) continue;
                    WriteMsg(s);
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
                {"nick", new Command((x, y, z) => "NICK " + y)},
                {"join", new Command((x, y, z) => "JOIN " + y)},
                {"ping", new Command((x, y, z) => "PING " + y.Split(' ')[0])},
                {"quit", new Command((x,y,z) => "QUIT :" + y )},
                {"me", new Command((x, y, z) => "PRIVMSG " + z.Channel + " :\u0001" + "ACTION " + y + "\u0001")},
                {"say", new Command((x,y,z) => y)},
            };

        public void RegisterAlias(string alias, string reference)
        {
            BotCommands.Add(alias, BotCommands[reference]);
        }

        public void Register(string name, Command command)
        {
            BotCommands.Add(name, command);
        }
    }
}
