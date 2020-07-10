using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Newtonsoft.Json;
using ShardingDemo.Helper;

namespace FunctionAppByQueue
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([QueueTrigger("myqueue-items", Connection = "quequeconnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            dynamic json = JsonConvert.DeserializeObject<Sample>(myQueueItem);
            int id;



            Int32.TryParse(json.id, out id);
            Shardinghelper obj = new Shardinghelper();
            obj.AddShard(id);
        }
        
    }
    public class Sample  
    {
        public string id { get; set; }

    }
}
