using System;
using System.Collections.Generic;

namespace Csandra.Bot
{
    // Defines a state property used to track conversation data.
    public class ConversationData
    {
        // The time-stamp of the most recent incoming message.
        public string Timestamp { get; set; }

        // The ID of the user's channel.
        public string ChannelId { get; set; }

        // Track whether we have already asked the user's name
        public bool PromptedUserForName { get; set; } = false;

        public Dictionary<DateTime, string> UserAnswers{get;set;} = new Dictionary<DateTime, string>();
        public Dictionary<DateTime, string> BotAnswers{get;set;} = new Dictionary<DateTime, string>();
        public string Intent{get;set;} = "";
        public string GameMode{get;set;} = "";
        public string Players{get;set;} = "";
        public string GameDuration{get;set;} = "";
        public int LastIndex {get;set;} = -1;
        public List<string> Games{get;set;}= new List<string>();
    }

    // Defines a state property used to track information about the user.
    public class UserProfile
    {
        public string Name { get; set; }
    }

    public class StringTable
    {
        public string[] ColumnNames { get; set; }
        public string[,] Values { get; set; }
    }
}