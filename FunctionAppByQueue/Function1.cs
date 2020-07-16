using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System.Text;

namespace FunctionAppByQueue
{
    public static class FunctionStorage
    {
        [FunctionName("FunctionStorage")]
        public static void Run([QueueTrigger("myqueue-items", Connection = "quequeconnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            dynamic json = JsonConvert.DeserializeObject<Sample>(myQueueItem);
            int id;



            //Int32.TryParse(json.id, out id);
            //Shardinghelper obj = new Shardinghelper();
            //obj.AddShard(id);


            const string connectionString = "Endpoint=sb://servicebus347.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=5tDP6HC7kthwV7+bOzdhE+PxRERAHu0AmleGJxgK2lU=";


            const string queueName = "myqueue-items";
            ServiceBusConnectionStringBuilder objconnection = new ServiceBusConnectionStringBuilder(connectionString);
            var _client = new Microsoft.Azure.ServiceBus.QueueClient(connectionString, queueName);

            string Message = "I'm in Azure Service Bus Queue";
            Microsoft.Azure.ServiceBus.Message message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(Message));
             _client.SendAsync(message);
        }
        
    }
    public class Sample  
    {
        public string id { get; set; }

    }
}
