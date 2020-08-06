using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json.Linq;
using Index = Microsoft.Azure.Search.Models.Index;
using AzureSearchDemo.Models;
using AzureSearchDemo.Helpers;
using Serilog;

namespace AzureSearchDemo.Services
{
    public interface IAzureSearchService
    {
        void InitializeAndCreateIndex();
    }
    public class AzureSearchService : IAzureSearchService
    {
        private readonly string searchServiceName;// = ConfigurationManager.AppSettings["AzureSearchName"];
        private readonly string adminKey;// = ConfigurationManager.AppSettings["AzureSearchAdminKey"];
        private readonly string queryKey;// = ConfigurationManager.AppSettings["AzureSearchQueryApiKey"];

        private readonly string autoCompleteSuggestor = "autoComplete";

        string indexName = "accounts";

        private readonly ICustomConfigurationSettings _CustomConfigurationSettings;
        private readonly ILogger _logger;

        public AzureSearchService(ICustomConfigurationSettings customConfigurationSettings, ILogger logger)
        {
            _CustomConfigurationSettings = customConfigurationSettings;
            _logger = logger;
            searchServiceName = _CustomConfigurationSettings.AzureSearchName;
            adminKey = _CustomConfigurationSettings.AzureSearchAdminKey;
            queryKey = _CustomConfigurationSettings.AzureSearchQueryApiKey;
        }

        /// <summary>
        /// This function uses the push method to populate the search index
        /// </summary>
        public void InitializeAndCreateIndex()
        {

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminKey));

            //This method is required only when we create the index first time programatically.
            //We may not require this method at all, if we use the import wizard of Azure and import the data there
            CreateAzureIndex(serviceClient);

            //We need to Add Synonyms and Update the index
            var synonymousResults = AddSynonymsAndUpdateIndex(serviceClient);

            //Create a queryClient and pass it to every function
            SearchIndexClient queryClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(queryKey));

            //Azure Search - Select only firstName and lastName and filter
            var nameResults = SearchByName(queryClient);

            var ageResults = SearchByAge(queryClient);

            var orderResults = SearchAndOrder(queryClient);

            var facetResults = GetFacetResults(serviceClient, queryClient);

            var boostResults = BoostLastName(serviceClient);

            var suggestorResults = GetSuggestorResults(serviceClient);

            
        }


        private void CreateAzureIndex(SearchServiceClient serviceClient)
        {
            #region Delete and Create an Index
            DeleteIndexIfExists(serviceClient);

            var accountIndexDefinition = new Index()
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Account>()
            };

            #region Add Suggestors
            var suggestor = new Suggester(autoCompleteSuggestor);
            suggestor.SourceFields.Add("firstName");
            suggestor.SourceFields.Add("lastName");

            accountIndexDefinition.Suggesters = new List<Suggester>();
            accountIndexDefinition.Suggesters.Add(suggestor);
            #endregion

            serviceClient.Indexes.CreateOrUpdate(accountIndexDefinition);
            #endregion

            //if (!serviceClient.Indexes.Exists(indexName))
            //{
            //    var accountIndexDefinition = new Index()
            //    {
            //        Name = indexName,
            //        Fields = FieldBuilder.BuildForType<Account>()
            //    };
            //    serviceClient.Indexes.Create(accountIndexDefinition);
            //}

            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

            if (indexClient != null)
                ImportData(indexClient);
        }

        private void DeleteIndexIfExists(SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists(indexName))
            {
                serviceClient.Indexes.Delete(indexName);
            }
        }

        private void ImportData(ISearchIndexClient indexClient)
        {
            var actions = new List<IndexAction<Account>>();

            string line;

            using (System.IO.StreamReader file = new System.IO.StreamReader("accounts.json"))
            {
                while ((line = file.ReadLine()) != null)
                {
                    JObject json = JObject.Parse(line);
                    Account account = json.ToObject<Account>();
                    actions.Add(IndexAction.Upload(account));
                }
                file.Close();
            }
            var batch = IndexBatch.New(actions);

            try
            {
                indexClient.Documents.Index(batch);
            }
            catch (IndexBatchException ex)
            {
                _logger.Error(ex, ex.Message);
                throw ex;
            }
        }

        private DocumentSearchResult<Account> AddSynonymsAndUpdateIndex(SearchServiceClient serviceClient)
        {
            var accountsSynonymns = new SynonymMap()
            {
                Name = "city-state-synonym-map",
                //Format = "solr",
                Synonyms = "cal, california, CA\ntexas=>TX"
            };

            serviceClient.SynonymMaps.CreateOrUpdate(accountsSynonymns);
            Index index = serviceClient.Indexes.Get(indexName);

            index.Fields.First(f => f.Name == "state").SynonymMaps =
                new[] { "city-state-synonym-map" };
            index.Fields.First(f => f.Name == "city").SynonymMaps =
                new[] { "city-state-synonym-map" };

            //After adding synonyms, we need to create/update the existing index
            serviceClient.Indexes.CreateOrUpdate(index);

            SearchIndexClient queryClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(queryKey));

            //DocumentSearchResult<Account> results;
            SearchParameters parameters = new SearchParameters
            {
                SearchFields = new[] { "state" },
                Select = new[] { "state", "firstName", "lastName" }
            };

            //Since we do not have any state matching cal, ideally it should not return any values.
            //But we have used synonyms and therefore cal will map to CA and return the states matching cal, CA or california
            var californiaResults = queryClient.Documents.Search<Account>("california", parameters);
            _logger.Information("AddSynonymsAndUpdateIndex {@californiaResults}", californiaResults);
            var result = queryClient.Documents.Search<Account>("cal", parameters);
            _logger.Information("AddSynonymsAndUpdateIndex {@result}", result);
            return result;
        }

        private DocumentSearchResult<Account> SearchByName(SearchIndexClient queryClient)
        {
            //I only want to select the limited parameters. 
            //This can be done through API as well, where you can specify the column names 
            SearchParameters parameters = new SearchParameters
            {
                Select = new[] { "firstName", "lastName" }
            };

            var result = queryClient.Documents.Search<Account>("Hughes", parameters);
            _logger.Information("SearchByName {@result}", result);
            return result;
        }

        private DocumentSearchResult<Account> SearchByAge(SearchIndexClient queryClient)
        {
            SearchParameters parameters = new SearchParameters
            {
                Filter = "age lt 45",
                Select = new[] { "age", "firstName", "lastName" }
            };

            var result = queryClient.Documents.Search<Account>("*", parameters);
            _logger.Information("SearchByAge {@result}", result);
            return result;
        }

        private DocumentSearchResult<Account> SearchAndOrder(SearchIndexClient queryClient)
        {
            SearchParameters parameters = new SearchParameters
            {
                OrderBy = new[] { "state desc", "city" },
                Select = new[] { "state", "city", "lastName" },
                Top = 10 //We can define how many top records we need to get
            };
            var result = queryClient.Documents.Search<Account>("*", parameters);
            _logger.Information("SearchAndOrder {@result}", result);
            return result;
        }

        private DocumentSearchResult<Account> BoostLastName(SearchServiceClient serviceClient)
        {
            //var textWeights = new TextWeights();

            //textWeights.Weights = new Dictionary<string, double>();
            //textWeights.Weights.Add("firstName", 2);
            //textWeights.Weights.Add("lastName", 5);

            //var accountIndexDefinition = new Index
            //{
            //    Name = indexName,
            //    Fields = FieldBuilder.BuildForType<Account>()
            //};

            //accountIndexDefinition.ScoringProfiles = new List<ScoringProfile>
            //{
            //    new ScoringProfile
            //    {
            //        Name = "boostLastName",
            //        TextWeights = textWeights
            //    }
            //};

            //serviceClient.Indexes.CreateOrUpdate(accountIndexDefinition);

            //DocumentSearchResult<Account> results;

            try
            {
                var index = serviceClient.Indexes.Get(indexName);
                #region Add Text Weights
                var textWeights = new TextWeights();

                textWeights.Weights = new Dictionary<string, double>();
                textWeights.Weights.Add("firstName", 2);
                textWeights.Weights.Add("lastName", 5);

                index.ScoringProfiles = new List<ScoringProfile>
                {
                    new ScoringProfile
                    {
                        Name = "boostLastName",
                        TextWeights = textWeights
                    }
                };
                serviceClient.Indexes.CreateOrUpdate(index);
                #endregion
                //Since we are updating the main index in the upper code, we need to get the searchIndexClient here again
                SearchIndexClient searchIndexClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(queryKey));
                SearchParameters parameters = new SearchParameters
                {
                    Select = new[] { "firstName", "lastName" },
                    ScoringProfile = "boostLastName"
                };

                var result = searchIndexClient.Documents.Search<Account>("Hughes", parameters);
                _logger.Information("BoostLastName {@result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw ex;
            }
        }

        private DocumentSuggestResult<Document> GetSuggestorResults(SearchServiceClient serviceClient)
        {
            try
            {
                //var index = serviceClient.Indexes.Get(indexName);
                //#region Add Suggestors
                //var suggestor = new Suggester(autoCompleteSuggestor);
                //suggestor.SourceFields.Add("firstName");
                //suggestor.SourceFields.Add("lastName");

                //index.Suggesters = new List<Suggester>();
                //index.Suggesters.Add(suggestor);
                //#endregion

                //serviceClient.Indexes.CreateOrUpdate(index);

                SuggestParameters suggestParameters = new SuggestParameters
                {
                    Select = new[] { "firstName", "lastName" },
                    UseFuzzyMatching = true,
                    Top = 20
                };

                //Since we are updating the main index in the upper code, we need to get the searchIndexClient here again
                SearchIndexClient searchIndexClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(queryKey));

                var result = searchIndexClient.Documents.Suggest("Hug", autoCompleteSuggestor, suggestParameters);
                _logger.Information("GetSuggestorResults {@result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw ex;
            }
        }


        private DocumentSearchResult<Account> GetFacetResults(SearchServiceClient serviceClient, SearchIndexClient queryClient)
        {
            try
            {
                SearchParameters searchParameters = new SearchParameters
                {
                    Facets = new List<string> { "balance", "age" }
                };

                var result = queryClient.Documents.Search<Account>("CA", searchParameters);
                _logger.Information("GetFacetResults {@result}", result);
                return result;

            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw ex;
            }
        }

    }
}
