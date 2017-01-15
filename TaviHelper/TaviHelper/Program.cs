using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech;
using Meebey.SmartIrc4net;
using System.Speech.Synthesis;
using System.IO;
using NAudio.Wave;
using System.Threading;
using Newtonsoft.Json;

namespace TaviHelper
{
    class Program
    {
        public const string Server = "irc.chat.twitch.tv";
        public const int Port = 6667;
        public static BotInfo botinfo = new BotInfo()
        {
            Nickname = "",
            Password = "",
            Channel = ""
        };

        public static string g_channel = "";
        private static IrcClient ircClient = new IrcClient();
        private static SpeakTaskManager _speakTaskManager;

        static void Main(string[] args)
        {
            if (!File.Exists("botinfo.json"))
            {
                Console.WriteLine("Using default botsettings");
                var botinfoString = JsonConvert.SerializeObject(botinfo);
                File.WriteAllText("botinfo.json", botinfoString);
            }
            else
            {
                var botinfostring = File.ReadAllText("botinfo.json");
                botinfo = JsonConvert.DeserializeObject<BotInfo>(botinfostring);
            }

            if(!string.IsNullOrEmpty(botinfo.Channel))
            {
                if (botinfo.Channel.StartsWith("#"))
                {
                    g_channel = botinfo.Channel.ToLower();
                }else
                {
                    g_channel = $"#{botinfo.Channel.ToLower()}";
                }
            }

            var waveOutCount = WaveOut.DeviceCount;
            Console.WriteLine($"There are {waveOutCount} devices");
            for (var i = 0; i < waveOutCount; i++)
            {
                Console.WriteLine($"Device {i} = {WaveOut.GetCapabilities(i).ProductName}");
            }
            Console.WriteLine();
            Console.WriteLine("Select device output: ");
            var device = Convert.ToInt32(Console.ReadLine());

            _speakTaskManager = new SpeakTaskManager(device);
            _speakTaskManager.Start();

            ircClient.OnConnected += IrcClient_OnConnected;
            ircClient.OnWriteLine += IrcClient_OnWriteLine;
            ircClient.OnReadLine += IrcClient_OnReadLine;
            ircClient.OnErrorMessage += IrcClient_OnErrorMessage;
            ircClient.OnChannelMessage += IrcClient_OnChannelMessage;
            ircClient.Connect(Server, Port);
        }

        private static void IrcClient_OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Channel == g_channel)
            {
                if (e.Data.Nick != "tavinnea" && e.Data.Nick != botinfo.Nickname.ToLower())
                {
                    var data = Say($"{e.Data.Nick} said {e.Data.Message}");
                    _speakTaskManager.QueuePlayback(data);
                }
            }
        }

        private static void IrcClient_OnConnected(object sender, EventArgs e)
        {
            ircClient.Login(botinfo.Nickname, botinfo.Nickname, 0, "", botinfo.Password);
            ircClient.WriteLine("CAP REQ :twitch.tv/membership");
            ircClient.RfcJoin(g_channel);
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
            if (e.Line.StartsWith("PASS"))
            {
                Console.WriteLine($" < PASS ***");
            }
            else
            {
                Console.WriteLine($" < {e.Line}");
            }
            Console.ForegroundColor = currColor;
        }

        private static void IrcClient_OnErrorMessage(object sender, IrcEventArgs e)
        {
            var currColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"ERROR : {e.Data.RawMessage}");
            Console.ForegroundColor = currColor;
        }

        public static byte[] Say(string text)
        {
            using (var stream = new MemoryStream())
            {
                using (var synth = new SpeechSynthesizer())
                {
                    synth.SetOutputToWaveStream(stream);
                    synth.Speak(text);
                }
                return stream.ToArray();
            }
        }

        private class SpeakTaskManager
        {
            private CancellationTokenSource _cancelSource = new CancellationTokenSource();
            private int _playbackDevice = 0;
            private Queue<byte[]> _playbackQueue = new Queue<byte[]>();
            private AutoResetEvent _newPlayback = new AutoResetEvent(false);

            public SpeakTaskManager(int playbackDevice)
            {
                _playbackDevice = playbackDevice;
            }

            public void Start()
            {
                Console.WriteLine("Starting Speak Task");
                var cancelToken = _cancelSource.Token;
                Task.Factory.StartNew(() => speakPump(cancelToken), cancelToken);
            }

            public void Stop()
            {
                _cancelSource.Cancel();
            }

            public void QueuePlayback(byte[] data)
            {
                Console.WriteLine("Queue Text");
                _playbackQueue.Enqueue(data);
                _newPlayback.Set();
            }

            private void speakPump(CancellationToken cancelToken)
            {
                try
                {
                    while (!cancelToken.IsCancellationRequested)
                    {
                        _newPlayback.WaitOne();
                        Console.WriteLine("AutoResetEvent Triggered");
                        while (_playbackQueue.Any())
                        {
                            var data = _playbackQueue.Dequeue();
                            PlayData(data);
                        }
                    }
                }
                catch
                {

                }
            }

            public void PlayData(byte[] data)
            {
                Console.WriteLine("Playing data");
                var waveOut = new WaveOut();
                var stream = new MemoryStream(data);

                var reader = new WaveFileReader(stream);
                waveOut.DeviceNumber = _playbackDevice;
                waveOut.Init(reader);
                waveOut.Play();
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    Task.Delay(100);
                }
            }
        }
    }
}
