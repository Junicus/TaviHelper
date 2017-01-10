using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech;
using Meebey.SmartIrc4net;
using System.Speech.Synthesis;

namespace TaviHelper
{
    class Program
    {
        public const string Server = "irc.chat.twitch.tv";
        public const int Port = 6667;
        public const string Channel = "#tavinnea";
        public const string Nickname = "LastRobot";
        public const string Password = "";
        private static IrcClient ircClient = new IrcClient();
        public static SpeechSynthesizer reader = new SpeechSynthesizer();

        static void Main(string[] args)
        {
            
            ircClient.OnConnected += IrcClient_OnConnected;
            ircClient.OnWriteLine += IrcClient_OnWriteLine;
            ircClient.OnReadLine += IrcClient_OnReadLine;
            ircClient.OnErrorMessage += IrcClient_OnErrorMessage;
            ircClient.OnChannelMessage += IrcClient_OnChannelMessage;
            ircClient.Connect(Server, Port);
        }

        private static void IrcClient_OnChannelMessage(object sender, IrcEventArgs e)
        {
            if(e.Data.Channel == Channel)
            {
                if (e.Data.Nick != "tavinnea" && e.Data.Nick != "lastrobot")
                {
                    reader.Speak(e.Data.Message);
                }
            }
        }

        private static void IrcClient_OnConnected(object sender, EventArgs e)
        {
            ircClient.Login(Nickname, Nickname, 0, "", Password);
            //ircClient.RfcNick(Nickname);
            //ircClient.RfcPass(Password);
            ircClient.WriteLine("CAP REQ :twitch.tv/membership");
            ircClient.RfcJoin(Channel);
            ircClient.RfcPrivmsg(Channel, "Testing");
            ircClient.Listen();
        }

        private static void IrcClient_OnReadLine(object sender, ReadLineEventArgs e)
        {
            var currColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" > {e.Line}");
            Console.ForegroundColor = currColor;
        }

        private static void IrcClient_OnWriteLine(object sender, WriteLineEventArgs e)
        {
            var currColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" < {e.Line}");
            Console.ForegroundColor = currColor;
        }

        private static void IrcClient_OnErrorMessage(object sender, IrcEventArgs e)
        {
            var currColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"ERROR : {e.Data.RawMessage}");
            Console.ForegroundColor = currColor;
        }
    }
}
