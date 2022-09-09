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
            MonitorRequest input = monitorContext.GetInput<MonitorRequest>();
            if (!monitorContext.IsReplaying) { log.LogInformation($"Received monitor request. Location: {input?.Location}. Phone: {input?.Phone}."); }

            DateTime endTime = monitorContext.CurrentUtcDateTime.AddHours(6);
            if (!monitorContext.IsReplaying) { log.LogInformation($"Instantiating monitor for {input.Location}. Expires: {endTime}."); }

            while (monitorContext.CurrentUtcDateTime < endTime)
            {
                // Check the weather
                if (!monitorContext.IsReplaying) { log.LogInformation($"Checking current weather conditions for {input.Location} at {monitorContext.CurrentUtcDateTime}."); }

                bool isClear = await monitorContext.CallActivityAsync<bool>("E3_GetIsClear", input.Location);

                if (isClear)
                {
                    // It's not raining! Or snowing. Or misting. Tell our user to take advantage of it.
                    if (!monitorContext.IsReplaying) { log.LogInformation($"Detected clear weather for {input.Location}. Notifying {input.Phone}."); }

                    await monitorContext.CallActivityAsync("E3_SendGoodWeatherAlert", input.Phone);
                    break;
                }
                else
                {
                    // Wait for the next checkpoint
                    var nextCheckpoint = monitorContext.CurrentUtcDateTime.AddMinutes(30);
                    if (!monitorContext.IsReplaying) { log.LogInformation($"Next check for {input.Location} at {nextCheckpoint}."); }

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
            [ActivityTrigger] string phoneNumber,
            ILogger log)
        {
            log.LogInformation("Running E3_SendGoodWeatherAlert");
        }
    }
}