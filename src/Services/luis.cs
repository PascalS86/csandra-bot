using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;

namespace Csandra.Bot.Services{

    public class Luis{

        private LUISRuntimeClient client;
        private string applicationId;
        private static Luis instance;
        public static Luis Instance{get{
            if(instance == null){
                instance = Init();
            }
            return instance;
        }}

        public static Luis Init(){
            var luis = new Luis();
            var subscriptionKey = Startup.Configuration["LUIS.SubscriptionKey"];
            var endPoint  = Startup.Configuration["LUIS.EndPoint"];
            luis.applicationId = Startup.Configuration["LUIS.ApplicationId"];
            luis.client = new LUISRuntimeClient(new ApiKeyServiceClientCredentials(subscriptionKey));
            luis.client.Endpoint = endPoint;
            return luis;
        }

        public async Task<string> GetIntent(string input){
             try
             {
                var result = await client.Prediction.ResolveAsync(applicationId, input);
                // Print result
                var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                return json;
            }
            catch (Exception)
            {
                return "\nSomething went wrong. Please Make sure your app is published and try again.\n";
            }
        }
    }

}