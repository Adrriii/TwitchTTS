﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using System.Text.RegularExpressions;
using NTextCat;

namespace TextToSpeechTTV
{
    class TwitchBot
    {
        private TwitchClient client;
        private Config config;
        private SpeechWordHandler speechWordHandler;
        private SpeechHelper speechHelper;
        
        //Set some defaults
        private int maxWordLength = 100;
        private string messageConnector = "said";
        private string voice = "Microsoft David Desktop";
        private string antiswear = "beep";
        private string longMessage = "to be continued";

        public TwitchBot()
        {

            //Set up Config Informations
            config = new Config();
            maxWordLength = config.GetMaxCharacterLength();
            messageConnector = config.SetMessageConnector();
            antiswear = config.ReplaceSwearWord();
            voice = config.SetVoice();
            longMessage = config.GetLongMessage();

            //Set up Speech Helper
            speechHelper = new SpeechHelper(voice, 0);
            speechWordHandler = new SpeechWordHandler();
            //Show all available voices to users
            List<string> voices = SpeechHelper.GetAllInstalledVoices();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("All available voices: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            foreach (string s in voices)
                Console.WriteLine(s);
            Console.WriteLine("----------------------------------------------------------------");
            //Set up Twitch Info
            ConnectionCredentials credentials = new ConnectionCredentials(config.GetUsername(), config.GetOAuth());
            
            client = new TwitchClient();
            client.Initialize(credentials, config.GetChannel());
            client.OnConnected += OnConnected;
            client.OnJoinedChannel += OnJoinedChannel;
            client.OnMessageReceived += OnMessageReceived;


            //Log in Twitch
            client.Connect();
        }
        private void OnConnected(object sender, OnConnectedArgs e)
        {
            Console.Write($"Connected to ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(e.AutoJoinChannel);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Currently using voice: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(speechHelper.GetCurrentVoice());
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        private void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully joined Channel: {e.Channel}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void SetLanguageSpeaker(string text)
        {
            RankedLanguageIdentifierFactory factory = new RankedLanguageIdentifierFactory();
            RankedLanguageIdentifier identifier = factory.Load("LanguageModels\\Core14.profile.xml");
            IEnumerable<Tuple<LanguageInfo, double>> find = identifier.Identify(text);

            int tolerance = 6;

            if (find.Count() > 1 && find.First().Item1.Iso639_3.Equals("eng"))
            {
                int i = 1;
                while (find.Count() > i && (!find.ElementAt(i).Item1.Iso639_3.Equals("fra") || find.ElementAt(i).Item2 - find.First().Item2 >= tolerance))
                {
                    i++; 
                }

                if(find.Count() > i && find.ElementAt(i).Item1.Iso639_3.Equals("fra") && find.ElementAt(i).Item2 - find.First().Item2 < tolerance)
                {
                    speechHelper.ChangeSpeaker(config.GetLanguageVoice(find.ElementAt(1).Item1.Iso639_3));
                    messageConnector = "dit";
                } else
                {
                    speechHelper.ChangeSpeaker(config.GetLanguageVoice(find.First().Item1.Iso639_3));
                    messageConnector = config.SetMessageConnector();
                }
            } else
            {
                speechHelper.ChangeSpeaker(config.GetLanguageVoice(find.First().Item1.Iso639_3));
                if(find.First().Item1.Iso639_3.Equals("fra"))
                {
                    messageConnector = "dit";
                } else
                {
                    messageConnector = config.SetMessageConnector();
                }
            }
        }

        private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            CommandHandler commandHandler = new CommandHandler();
            Console.WriteLine($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");

            string newUsername = speechWordHandler.ContainsUsername(e.ChatMessage.Username);

            if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster)
            {
                if (e.ChatMessage.Message.StartsWith("!block"))
                {
                    bool blocked = commandHandler.BlockUser(e.ChatMessage.Message);
                    if (blocked)
                        client.SendMessage(config.GetChannel(), "The user has been successfully blocked.");
                    else
                        client.SendMessage(config.GetChannel(), "The user is already blocked or the input was wrong.");
                }
                else if (e.ChatMessage.Message.StartsWith("!unblock"))
                {
                    bool unblocked = commandHandler.UnblockUser(e.ChatMessage.Message);
                    if (unblocked)
                        client.SendMessage(config.GetChannel(), "The user has been successfully unblocked.");
                    else
                        client.SendMessage(config.GetChannel(), "The user isn't blocked or the input was wrong.");
                }
            }

            if (speechWordHandler.CheckBlocked(e.ChatMessage.Username)) //Ignore blocked users
                return;
            if (e.ChatMessage.Message.StartsWith("!")) //Ignore Commands starting with !
                return;

            //Check if URL is in Message
            Regex UrlMatch = new Regex(@"(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?");
            Match url = UrlMatch.Match(e.ChatMessage.Message);

            //Create a List for multiple bad Words in sentence
            //Add first replaced sentence
            //Get first replaced sentence and replace it and continue this loop for each bad word.
            List<string> badWords = new List<string>();

            badWords = speechWordHandler.ContainsBadWord(e.ChatMessage.Message);


            string newMessageEdited = e.ChatMessage.Message;

            if (url.Success) //Check if contains URL
            {
                newMessageEdited = e.ChatMessage.Message.Replace(url.Value, "url");

                SetLanguageSpeaker(e.ChatMessage.Message.Replace(url.Value, " "));
            } else
            {
                SetLanguageSpeaker(newMessageEdited);
            }

            if (badWords.Count != 0) //Check if containing bad words
            {
                for (int i = 0; i < badWords.Count; i++)
                    newMessageEdited = newMessageEdited.Replace(badWords.ElementAt(i), antiswear);
            }

            if (maxWordLength <= newMessageEdited.Length && maxWordLength != 0) //Check if Sentence is too long
            {
                newMessageEdited = newMessageEdited.Substring(0, Math.Min(newMessageEdited.Length, maxWordLength)) + "....... " + longMessage;
                speechHelper.Speak($"{newUsername} {messageConnector} {newMessageEdited}");
                return;
            }
            speechHelper.Speak($"{newUsername} {messageConnector} {newMessageEdited}");
                
        }
    }
}
