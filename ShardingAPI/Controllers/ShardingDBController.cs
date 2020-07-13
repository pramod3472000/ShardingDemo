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
            return shardingDBService.GetShardDB();
        }

        // GET api/<AllDBController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
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
        [HttpPost]
        public async Task<IActionResult> CreateOrder(QueueMessage queueMessage)
        {
            await shardingQueueService.SendMessageAsync<QueueMessage>(queueMessage);

            return Ok();

        }

        // PUT api/<AllDBController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AllDBController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
