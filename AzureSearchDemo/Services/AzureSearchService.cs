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

namespace AzureSearchDemo.Services
{
    public interface IAzureSearchService
    {
        void InitializeAndCreateIndex();
    }
    public class AzureSearchService : IAzureSearchService
    {
        string searchServiceName =
            ConfigurationManager.AppSettings["AzureSearchName"];
        string adminKey = ConfigurationManager.AppSettings["AzureSearchAdminKey"];
        string queryKey = ConfigurationManager.AppSettings["AzureSearchQueryApiKey"];

        string indexName = "accounts";

        public AzureSearchService()
        {

            
        }

        public void InitializeAndCreateIndex()
        {

            SearchServiceClient serviceClient =
                new SearchServiceClient(searchServiceName,
                new SearchCredentials(adminKey));

            //This method is required only when we create the index first time programatically.
            //We may not require this method at all, if we use the import wizard of Azure and import the data there
            CreateAzureIndex(serviceClient);

            //We need to Add Synonyms and Update the index
            AddSynonymsAndUpdateIndex(serviceClient);

            //Create a queryClient and pass it to every function
            SearchIndexClient queryClient = new SearchIndexClient(
                searchServiceName, indexName, new SearchCredentials(queryKey));

            //Azure Search - Select only firstName and lastName and filter
            var nameResults = SearchByName(queryClient);

            var ageResults = SearchByAge(queryClient);

            var orderResults = SearchAndOrder(queryClient);

            var boostResults = BoostLastName(serviceClient, queryClient);

            var suggestorResults = GetSuggestorResults(serviceClient, queryClient);

            var facetResults = GetFacetResults(serviceClient, queryClient);
        }


        private void CreateAzureIndex(SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists(indexName))
            {
                serviceClient.Indexes.Delete(indexName);
            }

            var accountIndexDefinition = new Index()
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Account>()
            };

            serviceClient.Indexes.Create(accountIndexDefinition);

            ISearchIndexClient indexClient =
                serviceClient.Indexes.GetClient(indexName);

            ImportDocuments(indexClient);
        }

        private void ImportDocuments(ISearchIndexClient indexClient)
        {
            var actions = new List<IndexAction<Account>>();

            string line;

            using (System.IO.StreamReader file =
                new System.IO.StreamReader("accounts.json"))
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
                throw ex;
            }
        }

        private void AddSynonymsAndUpdateIndex(SearchServiceClient serviceClient)
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

            SearchIndexClient queryClient = new SearchIndexClient(
                searchServiceName, indexName, new SearchCredentials(queryKey));

            DocumentSearchResult<Account> results;
            SearchParameters parameters = new SearchParameters
            {
                SearchFields = new[] { "state" },
                Select = new[] { "state", "firstName", "lastName" }
            };

            //Since we do not have any state matching cal, ideally it should not return any values.
            //But we have used synonyms and therefore cal will map to CA and return the states matching cal, CA or california
            results = queryClient.Documents.Search<Account>("cal", parameters);
        }

        private DocumentSearchResult<Account> SearchByName(SearchIndexClient queryClient)
        {
            //I only want to select the limited parameters. 
            //This can be done through API as well, where you can specify the column names 
            SearchParameters parameters = new SearchParameters
            {
                Select = new[] { "firstName", "lastName" }
            };

            return queryClient.Documents.Search<Account>("Hughes", parameters);
        }

        private DocumentSearchResult<Account> SearchByAge(SearchIndexClient queryClient)
        {
            SearchParameters parameters = new SearchParameters
            {
                Filter = "age lt 45",
                Select = new[] { "age", "firstName", "lastName" }
            };

            return queryClient.Documents.Search<Account>("*", parameters);


        }

        private DocumentSearchResult<Account> SearchAndOrder(SearchIndexClient queryClient)
        {
            SearchParameters parameters = new SearchParameters
            {
                OrderBy = new[] { "state desc", "city" },
                Select = new[] { "state", "city", "lastName" },
                Top = 10 //We can define how many top records we need to get
            };
            return queryClient.Documents.Search<Account>("*", parameters);
        }

        private DocumentSearchResult<Account> BoostLastName(SearchServiceClient serviceClient, SearchIndexClient queryClient)
        {
            var textWeights = new TextWeights();

            textWeights.Weights = new Dictionary<string, double>();
            textWeights.Weights.Add("firstName", 2);
            textWeights.Weights.Add("lastName", 5);

            var accountIndexDefinition = new Index
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Account>()
            };

            accountIndexDefinition.ScoringProfiles = new List<ScoringProfile>
            {
                new ScoringProfile
                {
                    Name = "boostLastName",
                    TextWeights = textWeights
                }
            };

            serviceClient.Indexes.CreateOrUpdate(accountIndexDefinition);

            //DocumentSearchResult<Account> results;

            try
            {
                SearchParameters parameters = new SearchParameters
                {
                    Select = new[] { "firstName", "lastName" }
                };

                parameters.ScoringProfile = "boostLastName";
                //SearchIndexClient queryClient = new SearchIndexClient(
                //    searchServiceName, indexName, new SearchCredentials(queryKey));

                return queryClient.Documents.Search<Account>("Hughes", parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private DocumentSuggestResult<Document> GetSuggestorResults(SearchServiceClient serviceClient, SearchIndexClient queryClient)
        {
            var accountIndexDefinition = new Index
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Account>()
            };

            var suggestor = new Suggester("autoComplete");
            suggestor.SourceFields.Add("firstName");
            suggestor.SourceFields.Add("lastName");

            accountIndexDefinition.Suggesters = new List<Suggester>();
            accountIndexDefinition.Suggesters.Add(suggestor);

            serviceClient.Indexes.CreateOrUpdate(accountIndexDefinition);

            //The following 2 lines may or may not be required. When Azure search came, 
            //this was a limitation but might not a limitation anymore. So, I will have to check this
            //ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);
            //ImportDocuments(indexClient);

            try
            {
                SuggestParameters suggestParameters = new SuggestParameters
                {
                    UseFuzzyMatching = true,
                    Top = 20
                };

                //SearchIndexClient queryClient = new SearchIndexClient(
                    //searchServiceName, indexName, new SearchCredentials(queryKey));

                return queryClient.Documents.Suggest("Hug", "autoComplete", suggestParameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private DocumentSearchResult<Account> GetFacetResults(SearchServiceClient serviceClient, SearchIndexClient queryClient)
        {
            var accountIndexDefinition = new Index
            {
                Name = "accounts",
                Fields = FieldBuilder.BuildForType<Account>()
            };

            var suggestor = new Suggester("autoComplete");
            suggestor.SourceFields.Add("firstName");
            suggestor.SourceFields.Add("lastName");

            accountIndexDefinition.Suggesters = new List<Suggester>();
            accountIndexDefinition.Suggesters.Add(suggestor);

            serviceClient.Indexes.CreateOrUpdate(accountIndexDefinition);

            //DocumentSearchResult<Account> results;

            try
            {
                SearchParameters searchParameters = new SearchParameters
                {
                    Facets = new List<string> { "balance", "age" }
                };

                return queryClient.Documents.Search<Account>("CA", searchParameters);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
