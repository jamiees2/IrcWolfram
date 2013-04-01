using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace IRCWolfram
{
    public delegate void IrcEventHandler(object sender, IrcEventArgs e);
    
    public class IrcClient
    {
        public string Name { get; set; }
        public string Nick { get; set; }
        public string Channel { get; set; }
        public string Host { get; set; }
        public bool IsConnected { get; protected set;}
        public bool Debug { get; set; }
        public event IrcEventHandler Message;

        protected readonly string Server;
        protected readonly int Port;
        protected readonly TcpClient Irc;
        protected readonly Dictionary<Reply,Action<Message>> OnNextCode = new Dictionary<Reply, Action<Message>>();

        private string _actualHost;

        public StreamWriter Writer { get; protected set; }
        public StreamReader Reader { get; protected set; }

        public IrcClient(string server, int port, string nick, string name, string host)
        {
            Server = server;
            Port = port;
            Nick = nick;
            Name = name;
            Host = host;
            Irc = new TcpClient(Server,Port);
        }

        public void Connect()
        {
            var stream = Irc.GetStream();
            Writer = new StreamWriter(stream, Encoding.Default);
            Reader = new StreamReader(stream, Encoding.Default);
            
            IsConnected = true;
            Write("USER " + Nick + " " + Host + " " + Host + " :" + Name);

            //Somehow wait for the server to connect
            Write("NICK " + Nick);
            JoinChannel(Channel);
        }

        public void Disconnect()
        {
            Writer.Close();
            Reader.Close();
            Irc.Close();
            IsConnected = false;
        }

        public void JoinChannel(string chan)
        {
            Channel = chan;
            Write("JOIN " + chan);
            Console.WriteLine(Read().ToString());
            //Block all threads
            //new Channel() {};
        }

        public void Write(string cmd)
        {
            if (!IsConnected) return;
            if (string.IsNullOrEmpty(cmd)) return;
            /*var cparam = cmd.Split(new[]{' '},2);
            var command = cparam[0];
            var param = cparam.Length > 1 ? cparam[1] : "";*/

            /*
            Command run;
            if (Command.Commands.ContainsKey(command))
            {
                run = Command.Commands[command];
                cmd = run.Apply(cmd, param, this);
            }
            else
            {
                WriteMsg(cmd);
                return;
            }*/
            
            if(Debug) Console.WriteLine(cmd);
            Writer.WriteLine(cmd);
            Writer.Flush();
            if (!cmd.ToUpper().StartsWith("QUIT")) return;
            Disconnect();

        }

        public void WriteMsg(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            Write("PRIVMSG " + Channel + " :" + msg);
        }

        public Message Read()
        {
            lock (Reader)
            {
                return ParseMsg(Reader.ReadLine());
            }
        }

        public void Start()
        {
            while (IsConnected)
            {
                string data;
                try
                {
                    data = Reader.ReadLine();
                }
                catch (Exception)
                {
                    break;
                }
                if (data == null) break;
                //Console.WriteLine(data);

                if (data.StartsWith("PING"))
                {
                    Write("PONG " + data.Split(new[]{' '},2)[1]);
                    continue;
                }
                //Check if the message is from the server.
                //or if the message was directed at us
                var msg = ParseMsg(data);
                if (_actualHost == null)
                    _actualHost = msg.User;
                
                //var command = string.Join("",)

                Message(this,new IrcEventArgs(){ Message =  msg});
            }

            
        }
        public Message ParseMsg(string data)
        {
            var r = Reply.RplNone;
            var cmd = "";
            var msg = data.Split(new[] { ' ' }, 2);
            var ex = data.Split(new[] { ' ' }, 3);
            int o;
            if (msg[1].Split(' ').Length >= 2 && int.TryParse(ex[1].Split(' ')[0], out o))
            {
                r = (Reply)o;
            }
            else cmd = string.Join("", msg[1].TakeWhile(x => x != ':'));

            var user = string.Join("", msg[0].Skip(1).TakeWhile(x => x != '!'));
            var host = string.Join("", msg[0].SkipWhile(x => x != '@').Skip(1).TakeWhile(x => x != ' '));
            var text = string.Join("", data.Skip(1).SkipWhile(x => x != ':'));
            return new Message() { Code = r, Command = cmd, FullText = data, Host = host, Text = text, User = user };
        }

    }
}
