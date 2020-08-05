using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureSearchDemo.Helpers;
using AzureSearchDemo.Services;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
using Serilog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AzureSearchDemo.Controllers
{
    [Route("api/searchapi")]
    [ApiController]
    public class AzureSearchDemoController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IAzureSearchService _azureSearchService;
        private readonly ICustomConfigurationSettings _CustomConfigurationSettings;

        public AzureSearchDemoController(ILogger logger, IAzureSearchService azureSearchService, ICustomConfigurationSettings customConfigurationSettings)
        {
            _logger = logger;
            _azureSearchService = azureSearchService;
            _CustomConfigurationSettings = customConfigurationSettings;
        }

        // GET: api/<AzureSearchDemoController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            _logger.Information("Inside Controller Get method - Logging Information");
           
            return new string[] { "value1", "value2" };
        }

        // GET api/<AzureSearchDemoController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            //return "";
            _logger.Information("Inside Controller GetID method");
            _azureSearchService.InitializeAndCreateIndex();
            return _CustomConfigurationSettings.Testazurekey.Testquerykey;
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
