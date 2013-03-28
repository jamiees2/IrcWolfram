using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace IRClient
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

        private string _actualHost = null;

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
            Write("NICK " + Nick);
            Write("JOIN " + Channel);
        }

        public void Disconnect()
        {
            Writer.Close();
            Reader.Close();
            Irc.Close();
            IsConnected = false;
        }

        public void Write(string cmd)
        {
            if (!IsConnected) return;
            string[] cparam = cmd.Split(new char[]{' '},2);
            string command = cparam[0];
            var param = cparam.Length > 1 ? cparam[1] : "";

            Command run = null;
            if (Command.Commands.ContainsKey(command))
            {
                run = Command.Commands[command];
                cmd = run.Apply(cmd, param, this);
            }
            else
            {
                WriteMsg(cmd);
                return;
            }
            
            if(Debug) Console.WriteLine(cmd);
            Writer.WriteLine(cmd);
            Writer.Flush();
            if (cmd.ToUpper().StartsWith("QUIT"))
            {
                Disconnect();
                return;
            }
            //Wait for a reply
            //Or register a handler to run after next reply


        }

        public void WriteMsg(string msg)
        {
            Write("PRIVMSG " + Channel + " :" + msg);
        }

        public void Start()
        {
            while (IsConnected)
            {
                var data = Reader.ReadLine();
                if (data == null) break;
                //Console.WriteLine(data);

                if (data.StartsWith("PING"))
                {
                    Write("PONG " + data.Split(new[]{' '},2)[1]);
                    continue;
                }
                //Check if the message is from the server.
                //or if the message was directed at us
                var ex = data.Split(new[] {' '}, 3);
                if (_actualHost == null)
                    _actualHost = ex[0].Substring(1);
                
                if (ex.Length >= 3 && ex[0].Contains(_actualHost))
                {
                    int o;
                    //Let's get the error, if there was one
                    if (int.TryParse(ex[1], out o))
                    {
                        Console.WriteLine((Reply)o);
                    }
                    else if (Command.Commands.ContainsKey(ex[1]))
                    {
                        Command.Commands[ex[1]].Postprocess(ex[2].Substring(1),this);
                    }
                }

                Message(this,new IrcEventArgs(){ Message = data });
            }
        }

    }
}
