using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Azure_durable_function_Chaining
{
    public static class ChainingFunction
    {
        /*
         * NOTE: When running this chained durable function it will look like the same methods are run 
         * multiple times, however they are not. It's just the logging that's being run multiple times
         * but the actual awaited methods are only run once each.
         *  
         * https://stackoverflow.com/questions/52288577/azure-durable-orchestration-function-triggering-twice
         * 
         */


        [FunctionName("Chaining")]
        public static async Task<int> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogInformation("starting orchestrator");

            int myValue = 0;
            log.LogInformation($"myValue = {myValue}");

            myValue = await context.CallActivityAsync<int>("Function1", myValue);
            log.LogInformation($"Function1 ran and myValue is now = {myValue}");

            myValue = await context.CallActivityAsync<int>("Function2", myValue);
            log.LogInformation($"Function2 ran and myValue is now = {myValue}");

            myValue = await context.CallActivityAsync<int>("Function3", myValue);
            log.LogInformation($"Function3 ran and myValue is now = {myValue}");

            myValue = await context.CallActivityAsync<int>("Function4", myValue);
            log.LogInformation($"Function4 ran and myValue is now = {myValue}");

            return myValue;
        }

        [FunctionName("Function1")]
        public static int Function1([ActivityTrigger] int baseValue)
        {
            return baseValue + 1;
        }

        [FunctionName("Function2")]
        public static int Function2([ActivityTrigger] int baseValue)
        {
            return baseValue + 2;
        }

        [FunctionName("Function3")]
        public static int Function3([ActivityTrigger] int baseValue)
        {
            return baseValue + 3;
        }

        [FunctionName("Function4")]
        public static int Function4([ActivityTrigger] int baseValue)
        {
            return baseValue + 4;
        }
    }

    public static class HttpStart
    {
        [FunctionName("HttpStart")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = "orchestrators/{functionName}")] HttpRequestMessage req,
            [DurableClient] IDurableClient starter,
            string functionName,
            ILogger log)
        {
            // Function input comes from the request content.
            object eventData = await req.Content.ReadAsAsync<object>();
            string instanceId = await starter.StartNewAsync(functionName, eventData);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}