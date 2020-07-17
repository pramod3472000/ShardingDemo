using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AzureSearchDemo.Controllers
{
    [Route("api/searchapi")]
    [ApiController]
    public class AzureSearchDemoController : ControllerBase
    {
        // GET: api/<AzureSearchDemoController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<AzureSearchDemoController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AzureSearchDemoController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<AzureSearchDemoController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AzureSearchDemoController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
