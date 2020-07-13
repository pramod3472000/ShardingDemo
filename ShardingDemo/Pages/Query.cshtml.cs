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
    public class QueryModel : PageModel
    {
        private ILogger<IndexModel> _logger;
        private Shardinghelper objShard;
        public List<string[]> data;
        public int Clientid { get; set; }
        public QueryModel(ILogger<IndexModel> logger, Shardinghelper _objShard)
        {
            _logger = logger;
            objShard = _objShard;
        }
        public void OnGet()
        {
            data = this.MultiShardQuery();
            var count = data.Count;
        }
        private List<string[]> MultiShardQuery()
        {
            //
            ListShardMap<int> shardMap = objShard.TryGetShardMap();
            if (shardMap != null)
            {
                return objShard.ExecuteMultiShardQuery(
                        shardMap,
                        Configuration.GetCredentialsConnectionString());
            }
            else
            {

                return null;
            }
        }
    }
}