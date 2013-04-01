using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Configuration;
using ChatterBotAPI;
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
            return match.Success ? match.Groups[1].Value + (match.Groups[2].Value != "" ? " " + match.Groups[2].Value : "") : "";
        }

        private void IrcWolfram_Message(object sender, IrcEventArgs e)
        {
            Console.WriteLine(e.Message.FullText);
            var cmd = e.Message.Text.Substring(Math.Min(1,e.Message.Text.Length)); //grab the command sent
            cmd = ParseCommand(cmd);

            if (string.IsNullOrEmpty(cmd) && !_activeChatClients.Contains(e.Message.User)) return;
            //Begin cleverbot shiz
            if (cmd == "begin") { 
                _activeChatClients.Add(e.Message.User);
                WriteMsg("STARTED");
                return;
            }
            if (cmd == "stahp") { 
                _activeChatClients.Remove(e.Message.User);
                return;
            }

            //If it is the MASTAH
            if (BotCommands.ContainsKey(cmd.Split(new[] { ' ' }, 2)[0]))
            {
                if (e.Message.Host == "tal.is" && e.Message.User.Contains("finalC"))
                    Command(cmd);
                else WriteMsg("FAK YOU");
            }
            else if (!_activeChatClients.Contains(e.Message.User))
            {
                //WolframAlpha(cmd);
            }
            else Cleverbot(e.Message.Text);
        }

        #region Cleverbot

        readonly ChatterBotFactory _factory = new ChatterBotFactory();
        ChatterBot _bot;
        ChatterBotSession _botSession;
        private List<string> _activeChatClients = new List<string>();
        public void Cleverbot(string cmd)
        {
            if (_bot == null) _bot = _factory.Create(ChatterBotType.JABBERWACKY);
            if (_botSession == null) _botSession = _bot.CreateSession();
            var thought = _botSession.Think(cmd);
            WriteMsg(WebUtility.HtmlDecode(thought));
        }
        #endregion

        #region Wolfram|Alpha
        private void WolframAlpha(string cmd)
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
        #endregion

        #region Commands
        public void Command(string cmd)
        {
            var ex = cmd.Split(new[] { ' ' }, 2);
            var command = ex[0];
            if (BotCommands.ContainsKey(command) && ex.Length > 1)
                Write(BotCommands[command].Apply(command, ex[1], this));
            else if (BotCommands.ContainsKey(command))
                Write(BotCommands[command].Apply(command, "", this));
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
        #endregion
    }
}
