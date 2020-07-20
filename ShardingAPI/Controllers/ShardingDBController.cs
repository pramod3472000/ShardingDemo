using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShardingAPI.Services;
using ShardingAPI.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ShardingAPI.Controllers
{
    //Need to convert all methods to async methods
    [Route("api/sharding")]
    [ApiController]
    public class ShardingDBController : ControllerBase
    {
        public IShardingQueueService shardingQueueService { get; }
        public IShardingDBService shardingDBService { get; }

        public ShardingDBController(IShardingQueueService ShardingQueueService, IShardingDBService ShardingDBServic)
        {
            shardingQueueService = ShardingQueueService;
            shardingDBService = ShardingDBServic;
        }

        // GET: api/<AllDBController>
        [HttpGet]
        public IEnumerable<string[]> Get()
        {
            List<string[]> abc = new List<string[]>();
            abc.Add(new string[] { "a", "b", "c" });
            abc.Add(new string[] { "x", "y", "z" });
            return abc;
            //return shardingDBService.GetShardDB();
        }

        // POST api/<AllDBController>
        [HttpPost]
        public IActionResult Post([FromBody] int CustId)
        {
            if (CustId <= 0)
            {
                return BadRequest("Check the values being passed!!!");
            }
            //Need to see what can we return here
            shardingDBService.ExecuteDataDependentRoutingQuery(CustId);
            return Ok();
        }

        /// <summary>
        /// Add a new message to Azure Queue for processing.
        /// </summary>
        /// <param name="queueMessage">the queueMessage to write</param>
        /// <returns></returns>
        [HttpPost("AddQueueMessage")]
        public async Task<IActionResult> AddQueueMessage(QueueMessage queueMessage)
        {
            await shardingQueueService.SendMessageAsync<QueueMessage>(queueMessage);

            return Ok();

        }

       
    }
}
