using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.Extensions.Logging;
using ShardingAPI.Helper;

namespace ShardingAPI.Services
{
    public interface IShardingDBService
    {
        List<string[]> GetShardDB();

        void ExecuteDataDependentRoutingQuery(int CustId);
    }
    public class ShardingDBService : IShardingDBService
    {
        //private ILogger<ShardingDBService> _logger;
        private Shardinghelper objShard;

        public int Clientid { get; set; }
        public ShardingDBService(Shardinghelper _objShard)
        {
            //ILogger<ShardingDBService> logger,  _logger = logger;
            objShard = _objShard;
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

        public List<string[]> GetShardDB()
        {
            return this.MultiShardQuery();
            //var count = data.Count;
        }

        //This function enters data for the existing Customer Id
        public void ExecuteDataDependentRoutingQuery(int CustId)
        {
            ListShardMap<int> shardMap = objShard.TryGetShardMap();
            if (CustId == 0)
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
    }
}
