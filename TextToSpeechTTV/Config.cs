using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextToSpeechTTV
{
    class Config
    {
        //Path for every Config
        private readonly string creds = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config", "creds.txt");
        private readonly string options = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config", "options.txt");
        private readonly string blocklist = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config", "blocklist.txt");
        private readonly string badwords = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config", "badwords.txt");
        private readonly string usernames = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config", "usernames.txt");
        private readonly string voices = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config", "voices.txt");
        private readonly string foldername = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config");

        public Config()
        {
            CreateConfig();

            if (GetOAuth() == "oauth:youroauthkey")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Your OAuth Key hasn't been set! Can't connect anywhere without setting it.");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        private void CreateConfig()
        {
            if (Directory.Exists(foldername))
                return;

            if (!Directory.Exists(foldername))
                Directory.CreateDirectory(foldername);
            if (!File.Exists(options))
                FillOptionsFile();
            if (!File.Exists(blocklist))
                FillBlocklistExamples();
            if (!File.Exists(badwords))
                File.Create(badwords);
            if (!File.Exists(usernames))
                FillUsernamesExamples();
            if (!File.Exists(creds))
                FillCredsFile();
            if (!File.Exists(voices))
                FillVoicesFile();

            Console.WriteLine("Created config File. Please set up your creds and settings!\nPress any key to quit...");
            Console.ReadKey();
            Environment.Exit(0);
            
        }

        private void FillUsernamesExamples()
        {
            File.WriteAllLines(usernames, new string[]
            {
                "drdisrespectlive=doctor disrespect",
                "loltyler1=tyler 1",
                "riot games=rito"
            });
        }
        private void FillBlocklistExamples()
        {
            File.WriteAllLines(blocklist, new string[]
            {
                "nightbot",
                "moobot",
                "phantombot",
                "coebot",
                "deepbot"
            });
        }
        private void FillCredsFile()
        {
            File.WriteAllLines(creds, new string[] {
                "Twitch ID (lowercase):",
                "yourbotname",
                "OAUTH (Twitch TMI):",
                "oauth:youroauthkey",
                "Channel(lowercase):",
                "yourchannelname" });
        }

        private void FillOptionsFile()
        {
            File.WriteAllLines(options, new string[] {
                "Set Message Connector:",
                "said",
                "Maximum allowed Characters, 0 for no limit:",
                "100",
                "Replace swear word with:",
                "beep",
                "Say this, if long Sentence:",
                "to be continued"
            });
        }

        private void FillVoicesFile()
        {
            File.WriteAllLines(options, new string[] {
                "eng:",
                "Microsoft Zira Desktop",
            });
        }

        public string GetUsername()
        {
            string id = File.ReadAllLines(creds)[1];
            return id;
        }

        public string GetOAuth()
        {
            string password = File.ReadAllLines(creds)[3];
            return password;
        }

        public string GetChannel()
        {
            string channel = File.ReadAllLines(creds)[5];
            return channel;
        }

        public string SetVoice()
        {
            string voice = File.ReadAllLines(voices)[1];
            return voice;
        }
        public string SetMessageConnector()
        {
            string say = File.ReadAllLines(options)[1];
            return say;
        }

        public int GetMaxCharacterLength()
        {
            string wordLength = File.ReadAllLines(options)[3];
            int.TryParse(wordLength, out int result);
            return result;
        }

        public string ReplaceSwearWord()
        {
            string antiswear = File.ReadAllLines(options)[5];
            return antiswear;
        }

        public string GetLongMessage()
        {
            string longMessage = File.ReadAllLines(options)[7];
            return longMessage;
        }

        public string GetLanguageVoice(string lang)
        {
            string[] voiceLangs = File.ReadAllLines(voices);
            string voice = SetVoice();

            for(int i = 0; i < voiceLangs.Length; i++)
            {
                if(i%2==0)
                {
                    try
                    {
                        if (voiceLangs[i].Split(':')[0].Equals(lang))
                        {
                            voice = voiceLangs[++i];
                            break;
                        }
                    } catch { }
                }
            }
            return voice;
        }
    }
}
