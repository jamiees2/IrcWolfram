using System;
using System.Collections.Generic;
using System.Linq;

namespace IRCWolfram
{
    public sealed class Command
    {
        public static Dictionary<string,Command> Commands = new Dictionary<string, Command>()
            {
                {"ME", new Command((x, y, z) => "PRIVMSG " + z.Channel + " :\u0001" + "ACTION " + y + "\u0001")},
            };

        public static void Register(string name, Command cmd)
        {
            Commands.Add(name,cmd);
        }


        private readonly Func<string, string, IrcClient, string> _applier = ((x, y, z) => x);
        private readonly Action<string, IrcClient> _postprocess = delegate { };
        private readonly List<Reply> _errors = new List<Reply>(); 
        
        public Command()
        {
        }

        public Command(params Reply[] errors)
        {
            _errors = errors.ToList();
        }

        public Command(Func<string,string,IrcClient,string> applier = null, Action<string,IrcClient> postprocess = null  )
        {
            if (applier != null)
                _applier = applier;
            if (postprocess != null)
                _postprocess = postprocess;
            _errors = new List<Reply>();
        }

        public Command(Func<string, string, IrcClient, string> applier = null, Action<string, IrcClient> postprocess = null, params Reply[] errors)
        {
            if (applier != null)
                _applier = applier;
            if (postprocess != null)
                _postprocess = postprocess;
            _errors = errors.ToList();
        }

        public string Apply(string cmd, string param, IrcClient client)
        {
            return _applier(cmd, param, client);
        }

        public void Postprocess(string cmd, IrcClient client)
        {
            _postprocess(cmd, client);
        }

        public bool HasError(Reply r)
        {
            return _errors.Contains(r);
        }
    }
}
