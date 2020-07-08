﻿using Microsoft.Azure.SqlDatabase.ElasticScale.Query;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using MigraDoc.DocumentObjectModel.Tables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace ShardingDemo.Helper
{
    public class Shardinghelper
    {
        private  ShardMapManager s_shardMapManager;
        public Shardinghelper()
        {

        }

        public  ListShardMap<int> TryGetShardMap()
        {
            if (s_shardMapManager == null)
            {
                string shardMapManagerConnectionString =
                 Configuration.GetConnectionString(
                     Configuration.ShardMapManagerServerName,
                     Configuration.ShardMapManagerDatabaseName);

                s_shardMapManager = this.CreateOrGetShardMapManager(shardMapManagerConnectionString);
            }

            ListShardMap<int> shardMap;
            //  bool mapExists = s_shardMapManager.TryGetRangeShardMap(Configuration.ShardMapName, out shardMap);
            bool mapExists = s_shardMapManager.TryGetListShardMap(Configuration.ShardMapName, out shardMap);

            if (!mapExists)
            {
               // ConsoleUtils.WriteWarning("Shard Map Manager has been created, but the Shard Map has not been created");
                return null;
            }

            return shardMap;
        }
        public  void ExecuteDataDependentRoutingQuery(ListShardMap<int> shardMap, string credentialsConnectionString, int customerId)
        {
            // A real application handling a request would need to determine the request's customer ID before connecting to the database.
            // Since this is a demo app, we just choose a random key out of the range that is mapped. Here we assume that the ranges
            // start at 0, are contiguous, and are bounded (i.e. there is no range where HighIsMax == true)
            //  int currentMaxHighKey = shardMap.GetMappings().FirstOrDefault();

            string customerName = "Pramod" + System.DateTime.Now.ToString();
            int regionId = 0;
            int productId = 0;

            AddCustomer(
                shardMap,
                credentialsConnectionString,
                customerId,
                customerName,
                regionId);

            AddOrder(
                shardMap,
                credentialsConnectionString,
                customerId,
                productId);
        }
        private  void AddCustomer(
          ShardMap shardMap,
          string credentialsConnectionString,
          int customerId,
          string name,
          int regionId)
        {
            // Open and execute the command with retry for transient faults. Note that if the command fails, the connection is closed, so
            // the entire block is wrapped in a retry. This means that only one command should be executed per block, since if we had multiple
            // commands then the first command may be executed multiple times if later commands fail.
            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                // Looks up the key in the shard map and opens a connection to the shard
                using (SqlConnection conn = shardMap.OpenConnectionForKey(customerId, credentialsConnectionString))
                {
                    // Create a simple command that will insert or update the customer information
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                    IF EXISTS (SELECT 1 FROM Customers WHERE CustomerId = @customerId)
                        UPDATE Customers
                            SET Name = @name, RegionId = @regionId
                            WHERE CustomerId = @customerId
                    ELSE
                        INSERT INTO Customers (CustomerId, Name, RegionId)
                        VALUES (@customerId, @name, @regionId)";
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@regionId", regionId);
                    cmd.CommandTimeout = 60;

                    // Execute the command
                    cmd.ExecuteNonQuery();
                }
            });
        }

        /// <summary>
        /// Adds an order to the orders table for the customer.
        /// </summary>
        private static void AddOrder(
            ShardMap shardMap,
            string credentialsConnectionString,
            int customerId,
            int productId)
        {
            SqlDatabaseUtils.SqlRetryPolicy.ExecuteAction(() =>
            {
                // Looks up the key in the shard map and opens a connection to the shard
                using (SqlConnection conn = shardMap.OpenConnectionForKey(customerId, credentialsConnectionString))
                {
                    // Create a simple command that will insert a new order
                    SqlCommand cmd = conn.CreateCommand();

                    // Create a simple command
                    cmd.CommandText = @"INSERT INTO dbo.Orders (CustomerId, OrderDate, ProductId)
                                        VALUES (@customerId, @orderDate, @productId)";
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    cmd.Parameters.AddWithValue("@orderDate", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    cmd.CommandTimeout = 60;

                    // Execute the command
                    cmd.ExecuteNonQuery();
                }
            });

           // ConsoleUtils.WriteInfo("Inserted order for customer ID: {0}", customerId);
        }

        public  List<string[]> ExecuteMultiShardQuery(ListShardMap<int> shardMap, string credentialsConnectionString)
        {
            // Get the shards to connect to
            IEnumerable<Shard> shards = shardMap.GetShards();

            // Create the multi-shard connection
            using (MultiShardConnection conn = new MultiShardConnection(shards, credentialsConnectionString))
            {
                // Create a simple command
                using (MultiShardCommand cmd = conn.CreateCommand())
                {
                    // Because this query is grouped by CustomerID, which is sharded,
                    // we will not get duplicate rows.
                    cmd.CommandText = @"
                        SELECT 
                            c.CustomerId, 
                            c.Name AS CustomerName, 
                            o.OrderID AS OrderCount
                        FROM 
                            dbo.Customers AS c INNER JOIN 
                            dbo.Orders AS o
                            ON c.CustomerID = o.CustomerID
                        
                        ORDER BY 
                            1";

                    // Append a column with the shard name where the row came from
                    cmd.ExecutionOptions = MultiShardExecutionOptions.IncludeShardNameColumn;

                    // Allow for partial results in case some shards do not respond in time
                    cmd.ExecutionPolicy = MultiShardExecutionPolicy.PartialResults;

                    // Allow the entire command to take up to 30 seconds
                    cmd.CommandTimeout = 30;

                    // Execute the command. 
                    // We do not need to specify retry logic because MultiShardDataReader will internally retry until the CommandTimeout expires.
                    using (MultiShardDataReader reader = cmd.ExecuteReader())
                    {
                        // Get the column names
                        TableFormatter formatter = new TableFormatter(GetColumnNames(reader).ToArray());

                        int rows = 0;
                        while (reader.Read())
                        {
                            // Read the values using standard DbDataReader methods
                            object[] values = new object[reader.FieldCount];
                            reader.GetValues(values);

                            // Extract just database name from the $ShardLocation pseudocolumn to make the output formater cleaner.
                            // Note that the $ShardLocation pseudocolumn is always the last column
                            int shardLocationOrdinal = values.Length - 1;
                            values[shardLocationOrdinal] = ExtractDatabaseName(values[shardLocationOrdinal].ToString());

                            // Add values to output formatter
                            formatter.AddRow(values);

                            rows++;
                        }

                        Console.WriteLine(formatter.ToString());
                        Console.WriteLine("({0} rows returned)", rows);

                        return formatter._rows;
                    }
                }
            }
        }
        private  IEnumerable<string> GetColumnNames(DbDataReader reader)
        {
            List<string> columnNames = new List<string>();
            foreach (DataRow r in reader.GetSchemaTable().Rows)
            {
                columnNames.Add(r[SchemaTableColumn.ColumnName].ToString());
            }

            return columnNames;
        }
        private  string ExtractDatabaseName(string shardLocationString)
        {
            string[] pattern = new[] { "[", "DataSource=", "Database=", "]" };
            string[] matches = shardLocationString.Split(pattern, StringSplitOptions.RemoveEmptyEntries);
            return matches[1];
        }
        public  ShardMapManager CreateOrGetShardMapManager(string shardMapManagerConnectionString)
        {
            // Get shard map manager database connection string
            // Try to get a reference to the Shard Map Manager in the Shard Map Manager database. If it doesn't already exist, then create it.
            ShardMapManager shardMapManager;
            bool shardMapManagerExists = ShardMapManagerFactory.TryGetSqlShardMapManager(
                shardMapManagerConnectionString,
                ShardMapManagerLoadPolicy.Lazy,
                out shardMapManager);

            if (shardMapManagerExists)
            {
               
            }
            else
            {
                // The Shard Map Manager does not exist, so create it
                shardMapManager = ShardMapManagerFactory.CreateSqlShardMapManager(shardMapManagerConnectionString);
               
            }

            return shardMapManager;
        }
       
    }
}
