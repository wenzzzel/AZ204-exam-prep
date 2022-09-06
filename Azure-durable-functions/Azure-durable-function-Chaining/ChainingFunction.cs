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

        

        [FunctionName("Chaining")]
        public static async Task<object> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var x = await context.CallActivityAsync<object>("Function1", 0);
            var y = await context.CallActivityAsync<object>("Function2", x);
            var z = await context.CallActivityAsync<object>("Function3", y);
            return await context.CallActivityAsync<object>("Function4", z);
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