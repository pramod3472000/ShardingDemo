using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.Extensions.Logging;
using ShardingDemo.Helper;

namespace ShardingDemo.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private Shardinghelper objShard;
        public int Clientid { get; set; }
        public IndexModel(ILogger<IndexModel> logger, Shardinghelper _objShard)
        {
            _logger = logger;
            objShard = _objShard;
        }

        public void OnGet( int CustId)
        {
            ListShardMap<int> shardMap = objShard.TryGetShardMap();
            if(CustId==0)
            {
                CustId = 1;
            }
            if (shardMap != null)
            {
                objShard.ExecuteDataDependentRoutingQuery(
                    shardMap,
                    Configuration.GetCredentialsConnectionString(), CustId);
            }
        }
        private  void MultiShardQuery()
        {
            ListShardMap<int> shardMap = objShard.TryGetShardMap();
            if (shardMap != null)
            {
                objShard.ExecuteMultiShardQuery(
                    shardMap,
                    Configuration.GetCredentialsConnectionString());
            }
        }
    }
}
