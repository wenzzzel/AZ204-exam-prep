using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Azure_durable_function_Fan_out_fan_in
{
    public static class FanOutFanInFunction
    {
        [FunctionName("FanOutFanInFunction")]
        public static async Task<long> Run(
            [OrchestrationTrigger] IDurableOrchestrationContext backupContext)
        {
            string rootDirectory = @"C:\copyFrom\";
            if (string.IsNullOrEmpty(rootDirectory))
            {
                rootDirectory = Directory.GetParent(typeof(FanOutFanInFunction).Assembly.Location).FullName;
            }

            string[] files = await backupContext.CallActivityAsync<string[]>(
                "GetFileList",
                rootDirectory);

            var tasks = new Task<long>[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                tasks[i] = backupContext.CallActivityAsync<long>(
                    "CopyFileToBlob",
                    files[i]);
            }

            await Task.WhenAll(tasks);

            long totalBytes = tasks.Sum(t => t.Result);
            return totalBytes;
        }

        [FunctionName("GetFileList")]
        public static string[] GetFileList(
            [ActivityTrigger] string rootDirectory,
            ILogger log)
        {
            log.LogInformation($"Searching for files under '{rootDirectory}'...");

            string[] files = Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories);
            log.LogInformation($"Found {files.Length} file(s) under {rootDirectory}.");

            return files;
        }

        [FunctionName("CopyFileToBlob")]
        public static async Task<long> CopyFileToBlob(
            [ActivityTrigger] string filePath,
            Binder binder,
            ILogger log)
        {
            long byteCount = new FileInfo(filePath).Length;

            string fileName = filePath.Split('\\').Last();
            string outputLocation = @$"C:/copyTo/{fileName}";

            //Throwing exception to learn how state is tracked in Durable Functions
            if (fileName == "myTest - Copy (4).txt")
                throw new Exception();
            //...

            log.LogInformation($"Copying '{filePath}' to '{outputLocation}'. Total bytes = {byteCount}.");

            // copy the file contents to another location
            using (Stream source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Stream destination = new FileStream(outputLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                await source.CopyToAsync(destination);
            }

            return byteCount;
        }

        /*****************************************************
         * ONLY EXTERNALLY EXPOSED TRIGGERS BELOW THIS POINT *
         *****************************************************/

        [FunctionName("FanOutFanInFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("FanOutFanInFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("FanOutFanInFunction_HttpStartRewinding")]
        public static async Task<HttpResponseMessage> HttpStartRewinding(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = req.Headers.GetValues("InstanceId").First();

            await starter.RewindAsync(instanceId, "Testing rewind");

            log.LogInformation($"Started rewind of instance '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}