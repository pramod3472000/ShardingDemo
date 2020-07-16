using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using ShardingAPI.Helper;

namespace ShardingAPI.Services
{
    public interface IShardingQueueService
    {
        Task SendMessageAsync<T>(T item);
    }

    /// <summary>
    /// Manages writing orders to an Azure Queue
    /// </summary>
    public class ShardingQueueService : IShardingQueueService
    {
        IConfiguration config;

        public ShardingQueueService(IConfiguration configuration)
        {
            config = configuration;
        }

        public async Task SendMessageAsync<T>(T item)
        {
            #region Commented Code
            //string msgBody = JsonConvert.SerializeObject(item);

            //CloudStorageAccount acct = CloudStorageAccount.Parse(
            //    config[Constants.KEY_STORAGE_CNN]);

            //CloudQueueClient qClient = acct.CreateCloudQueueClient();
            //CloudQueue orderQueue = qClient.GetQueueReference(
            //    config[Constants.KEY_QUEUE]);

            //await orderQueue.CreateIfNotExistsAsync();

            //await orderQueue.AddMessageAsync(
            //    new CloudQueueMessage(msgBody)); 
            #endregion

            //serialize the object
            string msgBody = JsonConvert.SerializeObject(item);

            //Parse the connection string
            CloudStorageAccount acct = CloudStorageAccount.Parse(
                config[Constants.KEY_STORAGE_CONN]);

            //Create the queue client
            CloudQueueClient qClient = acct.CreateCloudQueueClient();
            //get the queue reference
            CloudQueue orderQueue = qClient.GetQueueReference(
                config[Constants.KEY_QUEUE]);

            //If the queue doesnt exist then create it
            await orderQueue.CreateIfNotExistsAsync();

            //Add the message to the queue
            await orderQueue.AddMessageAsync(
                new CloudQueueMessage(msgBody));

        }

    }
}