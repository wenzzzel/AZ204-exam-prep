using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Azure_durable_function_Monitor
{
    public static class E3_Monitor
    {
        [FunctionName("E3_Monitor")]
        public static async Task Run([OrchestrationTrigger] IDurableOrchestrationContext monitorContext, ILogger log)
        {
            if (!monitorContext.IsReplaying) { log.LogInformation($"Received monitor request."); }

            DateTime endTime = monitorContext.CurrentUtcDateTime.AddHours(6);
            if (!monitorContext.IsReplaying) { log.LogInformation($"Instantiating monitor. Expires: {endTime}."); }

            while (monitorContext.CurrentUtcDateTime < endTime)
            {
                // Check the weather
                if (!monitorContext.IsReplaying) { log.LogInformation($"Checking current weather conditions at {monitorContext.CurrentUtcDateTime}."); }

                bool isClear = await monitorContext.CallActivityAsync<bool>("E3_GetIsClear", "");

                if (isClear)
                {
                    // It's not raining! Or snowing. Or misting. Tell our user to take advantage of it.
                    if (!monitorContext.IsReplaying) { log.LogInformation($"Detected clear weather. Notifying."); }

                    await monitorContext.CallActivityAsync("E3_SendGoodWeatherAlert", "");
                    break;
                }
                else
                {
                    // Wait for the next checkpoint
                    var nextCheckpoint = monitorContext.CurrentUtcDateTime.AddMinutes(2);
                    if (!monitorContext.IsReplaying) { log.LogInformation($"Next check at {nextCheckpoint}."); }

                    await monitorContext.CreateTimer(nextCheckpoint, CancellationToken.None);
                }
            }

            log.LogInformation($"Monitor expiring.");
        }

        [FunctionName("E3_GetIsClear")]
        public static async Task<bool> GetIsClear([ActivityTrigger] string asdfg)
        {
            // Making this random for testing purposes
            if(new Random().Next() % 2 == 0)
                return true;
            else
                return false;
        }

        [FunctionName("E3_SendGoodWeatherAlert")]
        public static void SendGoodWeatherAlert(
            [ActivityTrigger] string asdf,
            ILogger log)
        {
            log.LogInformation("Running E3_SendGoodWeatherAlert");
        }

        [FunctionName("E3_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
        {
            string instanceId = await starter.StartNewAsync("E3_Monitor", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}