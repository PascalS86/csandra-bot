// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;
using System.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Csandra.Bot
{
    public class CsandraBot : ActivityHandler
    {
        private BotState _conversationState;
        private BotState _userState;
        
        public CsandraBot(ConversationState conversationState, UserState userState){
            _conversationState = conversationState;
            _userState = userState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Get the state properties from the turn context.

            var conversationStateAccessors =  _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());

            var intent = await Csandra.Bot.Services.Luis.Instance.GetIntent(turnContext.Activity.Text);
            await HandleIntent(turnContext, intent, conversationData, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(HandleGreeting(true)), cancellationToken);
                }
            }
        }

        private async Task HandleIntent(ITurnContext<IMessageActivity> context, string result, ConversationData conversationData, CancellationToken cancellationToken){
            
            string intent = conversationData.Intent;
            conversationData.UserAnswers.Add(DateTime.Now, context.Activity.Text);
            
            JObject obj = JObject.Parse(result);
            var newIntent = (string)obj["topScoringIntent"]["intent"];
            string query = (string)obj["query"];
            Console.WriteLine(newIntent);
            if(newIntent == "None"){
                if(conversationData.Intent == "Csandra.GetGame"
                && (conversationData.GameMode == ""
                || conversationData.Players == ""
                || conversationData.GameDuration == "")){
                    newIntent = conversationData.Intent;
                }
                else if(conversationData.Intent == "Csandra.GameInfo"
                && conversationData.Game == ""){
                    newIntent = conversationData.Intent;
                }
                else{
                    // var prediction = InvokeRequestResponseService(conversationData.Players, conversationData.GameMode,conversationData.GameDuration).GetAwaiter().GetResult();
                    conversationData.GameMode = "";
                    conversationData.Players = "";
                    conversationData.GameDuration = "";
                    conversationData.Game = "";
                    // return (prediction, newIntent);
                }
            }
            else if(newIntent == "Csandra.FollowUp"){
                conversationData.Intent = intent;
            }
            else if(newIntent != "Csandra.GetGame"){
                    // var prediction = InvokeRequestResponseService(conversationData.Players, conversationData.GameMode,conversationData.GameDuration).GetAwaiter().GetResult();
                    conversationData.GameMode = "";
                    conversationData.Players = "";
                    conversationData.GameDuration = "";
                    // return (prediction, newIntent);
            }
            else if(newIntent != "Csandra.GameInfo"){
                    // var prediction = InvokeRequestResponseService(conversationData.Players, conversationData.GameMode,conversationData.GameDuration).GetAwaiter().GetResult();
                    conversationData.Game = "";
                    // return (prediction, newIntent);
            }
            string answer = "";
            switch (newIntent){
                case "Csandra.About":
                    answer =  (HandleAbout(query));
                    break;
                case "Csandra.Bye":
                    answer =  (HandleBye());
                    break;
                case "Csandra.Cancel":
                    answer =  (HandleCancel());
                    break;
                case "Csandra.Confirm":
                    answer =  (HandleConfirm());
                    break;
                case "Csandra.Cool":
                    answer =  (HandleCool());
                    break;
                case "Csandra.Feedback":
                    answer =  (HandleFeedback(obj));
                    break;
                case "Csandra.GameInfo":
                    answer =  (HandleGameInfo(obj, context, ref conversationData));
                    break;
                case "Csandra.GameOpinon":
                    answer =  (HandleGameOpinon());
                    break;
                case "Csandra.GetGame":
                    answer =  (HandleGetGame(obj, ref conversationData));
                    break;
                case "Csandra.Greeting":
                    answer =  (HandleGreeting());
                    break;
                case "Csandra.HowAreYou":
                    answer =  (HandleHowAreYou(query));
                    break;
                case "Csandra.HowOld":
                    answer =  (HandleHowOld());
                    break;
                case "Csandra.Intressts":
                    answer =  (HandleIntressts());
                    break;
                case "Csandra.MySocial":
                    answer =  (HandleMySocial(query));
                    break;
                case "Csandra.Thanks":
                    answer =  (HandleThanks());
                    break;
                case "Csandra.WhatAreYouDoing":
                    answer =  (HandleWhatAreYouDoing());
                    break;
                case "Csandra.Where":
                    answer =  (HandleWhere());
                    break;
                case "Csandra.WhoAreYou":
                    answer =  (HandleWhoAreYou());
                    break;
                case "Csandra.FollowUp":
                    answer =  (HandleFollowUp(conversationData.Intent));
                    break;
                default:
                    answer =  (HandleNone());
                    break; 
            }
            
            await context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
            conversationData.BotAnswers.Add(DateTime.Now, answer);
            conversationData.Intent = newIntent;
        }

        private string GetRandomString(List<string> lst){
            Random random = new Random();
                 int index = random.Next(lst.Count);
                 return lst[index];
        }
        
        private string HandleFollowUp(string intent){
            switch(intent){
                default:
                    return HandleNone();
            }
        }

        private string HandleAbout(string query){
            if(query.ToLower().Contains("über")){
                var list = new List<string>(){
                    "Ich bin Cassi. Jung. Dynamisch. Deine Assistentin."+
                    "\r\nAm besten kann ich dir bei Boardgames helfen",
                    "Ich heiße Cassi."+
                    "\r\nFrag mich was zu deinen Spielen. Ich versuche dir zu helfen",
                    "Ich bin Cassi, die lernende Assistentin."+
                    "\r\nFrag mich was zu deinen Spielen. Vielleicht kann ich dir helfen",
                    "Ich heiße Cassi und will dir helfen."+
                    "\r\nAm besten kann ich dir bei Boardgames helfen",
                    
                };
                return GetRandomString(list);
            }
            else{
                var activityList = new List<string>(){
                    "\r\nSag z.B. Cassi, ich will ein Spiel spielen",
                    "\r\nSag z.B. Cassi, zeige mir meine Spiele",
                };
                var list = new List<string>(){
                    "Ich kann dir bei deinen Boardgames helfen."+
                    GetRandomString(activityList),
                    "Deine Boardgames sind mein Metier"+
                    GetRandomString(activityList),
                    "Boardgames. Das ist mein Ding."+
                    GetRandomString(activityList),
                    "Am besten kann ich dir bei deinen Boardgames helfen"+
                    GetRandomString(activityList),
                };
                return GetRandomString(list);
            }
        }

        private string HandleBye(){
            return GetRandomString(new List<string>(){
                "Bye",
                "Tschüss",
                "Bis dann",
                "Bis später"
            });
        }
        private string HandleCancel(){
            return GetRandomString(new List<string>(){
                "Ok",
                "Dann nicht",
                "Alles klar",
                "Na denn"
            });
        }
        private string HandleConfirm(){
            return GetRandomString(new List<string>(){
                "Ok",
                "Gerne",
                "Super",
                "Toll"
            });
        }
        private string HandleCool(){
            return GetRandomString(new List<string>(){
                "Nicht wahr?",
                "Find ich auch",
                "Yeah",
                "Total"
            });
        }
        private string HandleGreeting(bool first = false)
        {
            if(first)
                return GetRandomString(new List<string>(){
                    "Hi", //TODO: Additional Content
                    "Moin",
                    "Hallo",
                    "Was gibt's?"
                });
            else
                return GetRandomString(new List<string>(){
                    "Was gibt's?", //TODO: Additional Content
                    "Was kann ich tun?",
                    "Womit kann ich helfen?",
                    "Bin ganz Ohr?"
                });

        }
        private string HandleHowAreYou(string query){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Joa, ganz gut",
                "Super",
                "Ganz gut",
                "Ja, gut"
            });
        }
        private string HandleHowOld(){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Noch nicht soooo alt",
                "Ich dachte, das frägt man nicht?",
                "Jung. Seeeehr jung",
                "Jung"
            });
        }
        private string HandleIntressts(){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Lernen und Games",
                "Viel. Es gibt so viel zu lernen... Games gehören dazu",
                "Uh, es gibt so viel zu entdecken. Boardgames sind ein Teil davon",
                "Aktuell sinds hauptsächlich Games. Aber es ibt viel zu lernen"
            });
        }
        private string HandleMySocial(string query){//TODO: Handle additional content
            if(query.ToLower().Contains("freund"))
                return GetRandomString(new List<string>(){
                    "Ich bin dein VFF",
                    "Wir sind Freunde",
                    "Betrachte mich als deine Freundin",
                    "Zähle mich gerne zu deinen Freunden"
                });
            else
                return GetRandomString(new List<string>(){
                    "Ähm...",
                    "Naja...",
                    "Ok, aber du solltest an deiner digitalen Präsenz arbeiten",
                    "Ich weiß, ich bin hinreißend. Aber ich denke das wird nix"
                });
        }
        private string HandleThanks(){
            return GetRandomString(new List<string>(){
                "Kein Problem",
                "Nicht dafür",
                "Gerne",
                "Immer wieder"
            });
        }
        private string HandleWhatAreYouDoing(){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Ich schaue mir die Boardgames an",
                "Gerade? Die Boardgames bearbeiten",
                "Games bewerten, Zusammenfassungen lesen und dir sagen, was ich gerade tue",
                "Nicht viel. Was kann ich für dich tun?"
            });
        }
        private string HandleWhere(){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Ich wohne in der Cloud",
                "Die Cloud ist mein Zuhause",
                "Ich wohne in der Welt (der digitalen)",
                "Überall in der Welt."
            });
        }
        private string HandleWhoAreYou(){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Ich bin Cassi",
                "Ich heiße Cassi",
                "Cassi",
                "Mein Name ist Cassi."
            });
        }
        private string HandleNone(){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Sorry, aber das habe ich nicht verstanden..."
                +"\r\nAber ich lerne noch",
                "Ich habe das nicht verstanden..."
                +"\r\nIch lerne noch",
                "Tschuldigung, das habe ich nicht verstanden...",
                "Sorry, aber das kann ich noch nicht...."
                +"\r\nAber ich lerne noch"
            });
        }

        private string HandleGetGame(JObject json, ref ConversationData conversationData){//TODO: Handle additional content
            var entities = (JArray)json["entities"];
            foreach(var item in entities){
                string type  = (string)item["type"];
                switch(type){
                    case "Csandra.GameMode":
                        conversationData.GameMode = (string)item["entity"];
                        break;
                    case "builtin.number":
                        var role = (string)item["role"];
                        if(role == "csandra.players" || role == "" || role == null)
                            conversationData.Players = (string)item["entity"];
                        
                        Console.WriteLine(item);
                        break;
                    case "Csandra.GameDuration":
                        conversationData.GameDuration =  (string)item["entity"];
                        break;
                    default:
                        break;
                }
            }
            if(conversationData.GameMode == "")
                return GetRandomString(new List<string>(){
                    "Ok, welche Art von Spiel willst du spielen?"
                    +"\r\nKoop? Semi-Koop? Versus?",
                    "Alles klar. Was darfs sein?"
                    +"\r\nKoop? Semi-Koop? Versus?",
                    "Welche Art von Spiel willst du spielen?"
                    +"\r\nKoop? Semi-Koop? Versus?",
                    "Was für ein Spielmodi darfs sein?"
                    +"\r\nKoop? Semi-Koop? Versus?"
                });
            else if(conversationData.Players == "")
                return GetRandomString(new List<string>(){
                    "Gut. Und wieviele Spieler spielen mit?",
                    "Wieviele Spieler sind dabei?",
                    "Super. Wieviele Spieler?",
                    "Awesome. Und wieviele Spieler sind dabei?"
                });
            else if(conversationData.GameDuration == "")
                return GetRandomString(new List<string>(){
                    "Ok. Wie lange soll das Game dauern?"
                    +"\r\nKurz? Mittel? Lang? Egal?",
                    "Wie lange soll es gehen??"
                    +"\r\nKurz? Mittel? Lang? Egal?",
                    "Und wie lange soll das Spiel gehen?"
                    +"\r\nKurz? Mittel? Lang? Egal?",
                    "Wie lange darfs gehen?"
                    +"\r\nKurz? Mittel? Lang? Egal?"
                });

            
                    var prediction = InvokeRequestResponseService(conversationData.Players, conversationData.GameMode,conversationData.GameDuration).GetAwaiter().GetResult();
                    conversationData.GameMode = "";
                    conversationData.Players = "";
                    conversationData.GameDuration = "";
                    return prediction;
        }

        private string HandleGameInfo(JObject json, ITurnContext context,ref ConversationData conversationData){//TODO: Handle additional content
            var entities = (JArray)json["entities"];
            foreach(var item in entities){
                string entity = (string)item["entity"];
                SearchBing(context, entity);
            }
            return "Das habe ich gefunden"; //TOOD: Bind BingSearch
        }

 // Add SearchBing
        public async void SearchBing(ITurnContext context, string searchText)
        {
            // Step 1: Call the Bing Image Search API
            //IMPORTANT: replace this variable with your Cognitive Services subscription key.
            string subscriptionKey = "ebc0f941ef53461e8f244624b2d657df";
            //initialize the client
            var client = new WebSearchClient(new ApiKeyServiceClientCredentials(subscriptionKey));
            //images to be returned by the Bing Image Search API
            SearchResponse result = null;
            try
            {
                // Call the API and store the results
                result = client.Web.SearchAsync(query: searchText).Result; //search query
            }
            catch (Exception ex)
            {
                // If there's an exception, return the error in the chat window
                await context.SendActivityAsync("Encountered exception. " + ex.Message);
            }
            // Step 2: Process the results and send to the user
            // If the API returns smt
            if (result != null)
            {
                // Send the activity to the user.
                int i = 0;
                List<Attachment> lst = new List<Attachment>();
                foreach(var item in result.WebPages.Value){
                   
                    AdaptiveCards.AdaptiveCard card = new AdaptiveCards.AdaptiveCard();
                    card.Body.Add(new AdaptiveCards.AdaptiveTextBlock (){
                         Text = item.Name,
                         Size = AdaptiveCards.AdaptiveTextSize.Medium,
                         Weight = AdaptiveCards.AdaptiveTextWeight.Bolder
                    });
                    card.Body.Add(new AdaptiveCards.AdaptiveTextBlock (){
                         Text = item.DisplayUrl,
                         Wrap =true,
                         Weight = AdaptiveCards.AdaptiveTextWeight.Lighter
                    });
                    card.Body.Add(new AdaptiveCards.AdaptiveTextBlock (){
                         Text = item.Snippet,
                         Wrap =true,
                         Size = AdaptiveCards.AdaptiveTextSize.Medium
                    });
                    card.Actions.Add(new AdaptiveCards.AdaptiveOpenUrlAction(){
                        Title = item.Url,
                        Url = new Uri(item.Url)
                    });
                    Attachment attachment = new Attachment()
                    {
                        ContentType = AdaptiveCards.AdaptiveCard.ContentType,
                        Content = card
                    };
                    lst.Add(attachment);
                    if(i == 5)
                        break;
                    i++;

                }
                
                await context.SendActivityAsync(MessageFactory.Carousel(lst));
            }
            else // If the API doesn't return anything
            {
                string none = HandleNone();
                await context.SendActivityAsync(MessageFactory.Text(none));
            }
        }


        private string HandleFeedback(JObject json){
            var entities = (JArray)json["entities"];
            string feedback = "";
            foreach(var item in entities){
                if(feedback != "")
                    break;
                string type  = (string)item["type"];
                switch(type){
                    case "Csandra.FeedbackType":
                        feedback = (string)((JArray)item["resolution"]["values"]).ElementAt(0);

                        break;
                    default:
                        break;
                }
            }
            if(feedback == "gut")
                return GetRandomString(new List<string>(){
                "Super",
                "Danke",
                "Awesome! Achivement für mich",
                "Genial"
            });
            else if(feedback == "schlecht")
                return GetRandomString(new List<string>(){
                "Schade. Ich hoffe das ändert sich",
                "Wieso?",
                "Echt? Ha... hoffentlich wirds besser zwischen uns",
                "Das ist traurig."
            });
            else if(feedback == "schön")
                return GetRandomString(new List<string>(){
                "Oh, vielen Dank.",
                "Schönheit liegt im Auge des Betrachters",
                "Bitte mach weiter. Nein wirklich.",
                "Och, wenn du meinst."
            });
            else if(feedback == "häßlich")
                return GetRandomString(new List<string>(){
                "Hm. Schönheit liegt im Auge des Betrachters",
                "Das ist nicht nett.",
                "Das ist traurig.",
                "Du bist ziemlich gemein"
            });
            else 
                return HandleNone();
            
        }
        private string HandleGameOpinon(){//TODO: Handle additional content
            return GetRandomString(new List<string>(){
                "Ich finds cool das du mich frägst, aber ich kann mich nicht entscheiden.",
                "Nice für dein Intresse. Alle Spiele sind toll",
                "Toll. Ich finde sie alle genial",
                "Schön, dass dich meine Meinung interessiert. Mir gefallen alle"
            });
        }
        async Task<string> InvokeRequestResponseService(string players, string mechanic, string duration = "")
        {
            if(duration == "egal")
                duration = "";
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {

                    Inputs = new Dictionary<string, StringTable> () { 
                        { 
                            "input1", 
                            new StringTable() 
                            {
                                ColumnNames = new string[] {"Players", "Complexity", "Mechanic", "Duration"},
                                Values = new string[,] {  { players, "0", mechanic, duration },  { "0", "0", "value", "value" },  }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>() {
                    }
                };
                //TODO: Change API Access
                const string apiKey = "2UAGlNfRR7kE/jC/4JbkX6rLVKklFIvF5Ny36qfqU71UuGRTVnGUFkXjWHxHfcWKEVtQFLUCiPrwX13t93i5jA=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/4e95229a43ce48639c0442e60d748fcd/services/40847a685c9e428cb810ed7954a986d3/execute?api-version=2.0&details=true");

                
                // WARNING: The 'await' statement below can result in a deadlock if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false) so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)


                HttpResponseMessage response = await client.PostAsync("", new StringContent(JsonConvert.SerializeObject(scoreRequest), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    
                    JObject obj = JObject.Parse(result);
                    var resultTokens = obj.GetValue("Results").Children().First().Children().First().Children().ElementAt(1).Children().First();
                    
                    foreach( var item in resultTokens.First().First().ToArray())
                        Console.WriteLine(item.ToString());
                    
                    var columnNames = resultTokens.First().First().ToArray()
                        .Select(c=> (c as JToken).ToString()).Skip(5).SkipLast(1).Select((c, idx)=> new { name = c, index = idx });
                    var results = resultTokens.Last().First().First().ToArray().Skip(5).SkipLast(1).Select((c, idx) => new { index = idx, value = Double.Parse((c as JToken).ToString()) }).OrderByDescending(c=> c.value);

                    var indexes = results.Where(c=> columnNames.Any(d=> d.index == c.index) ).Take(5).Select(c=> c.index);

                    Random r = new Random();

                    int index = r.Next(0, indexes.Count() - 1);
                    var key = columnNames.Where(c=> c.index == indexes.ElementAt(index)).First().name;

                    key = key.Replace("Scored Probabilities for Class ", "").Replace("\"", "");

                    return GetRandomString(new List<string>(){
                        $"Ok. Cool. Wie wär's mit {key}?",
                        $"Was hälst du von {key}?",
                        $"Wäre {key} gut?",
                        $"Nice. Wie wär's mit {key}?"
                    }); 
                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                    return GetRandomString(new List<string>(){
                        "Sorry, aber mir fällt nix ein",
                        "Hm, dafür fällt mir leider nix ein",
                        "Ich weiß auch nicht so genau",
                        "Es tut mir soooo leid, aber ich weiß es auch nicht."
                    }); 
                }
            }
        }
    
    }

    
}