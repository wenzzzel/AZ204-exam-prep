using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Azure_durable_function_Aggregator_stateful_entities
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Counter([EntityTrigger] IDurableEntityContext ctx,
            ILogger log)
        {
            int currentValue = ctx.GetState<int>();
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "add":
                    //int amount = ctx.GetInput<int>();
                    int amount = 1;
                    ctx.SetState(currentValue + amount);
                    break;
                case "reset":
                    ctx.SetState(0);
                    break;
                case "get":
                    ctx.Return(currentValue);
                    break;
            }
        }

        [FunctionName("HttpStart")]
        public static async Task<EntityStateResponse<Counter>> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableEntityClient entityClient,
            ILogger log)
        {
            string metricType;
            int random = new Random().Next(0, 3);
            switch (random)
            {
                case 0:
                    metricType = "add";
                    break;
                case 1:
                    metricType = "reset";
                    break;
                case 2:
                    metricType = "get";
                    break;
                default:
                    metricType = "add";
                    break;
            }

            log.LogInformation($"metricType set to {metricType}");

            // The "Counter/{metricType}" entity is created on-demand.
            var entityId = new EntityId("Counter", metricType);
            await entityClient.SignalEntityAsync(entityId, "add");

            return await entityClient.ReadEntityStateAsync<Counter>(entityId);
        }

    }
    public class Counter
    {
        [JsonProperty("value")]
        public int CurrentValue { get; set; }

        public void Add(int amount) => this.CurrentValue += amount;

        public void Reset() => this.CurrentValue = 0;

        public int Get() => this.CurrentValue;

        [FunctionName(nameof(Counter))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Counter>();
    }
}