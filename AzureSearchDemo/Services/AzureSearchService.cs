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
    public class AzureSearchService
    {
        string searchServiceName =
            ConfigurationManager.AppSettings["CaztonSearchName"];
        string adminKey = ConfigurationManager.AppSettings["CaztonSearchAdminKey"];
        string queryKey = ConfigurationManager.AppSettings["CaztonSearchQueryApiKey"];

        public AzureSearchService()
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
                searchServiceName, "accounts", new SearchCredentials(queryKey));

            //Azure Search - Select only firstName and lastName and filter
            var nameResults = SearchByName(queryClient);

            var ageResults = SearchByAge(queryClient);

            var orderResults = SearchAndOrder(queryClient);
        }

        
        private void CreateAzureIndex(SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists("accounts"))
            {
                serviceClient.Indexes.Delete("accounts");
            }

            var accountIndexDefinition = new Index()
            {
                Name = "accounts",
                Fields = FieldBuilder.BuildForType<Account>()
            };

            serviceClient.Indexes.Create(accountIndexDefinition);

            ISearchIndexClient indexClient =
                serviceClient.Indexes.GetClient("accounts");

            ImportDocuments(indexClient);
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
            Index index = serviceClient.Indexes.Get("accounts");
            index.Fields.First(f => f.Name == "state").SynonymMaps =
                new[] { "city-state-synonym-map" };
            index.Fields.First(f => f.Name == "city").SynonymMaps =
                new[] { "city-state-synonym-map" };

            //After adding synonyms, we need to create/update the existing index
            serviceClient.Indexes.CreateOrUpdate(index);

            SearchIndexClient queryClient = new SearchIndexClient(
                searchServiceName, "accounts", new SearchCredentials(queryKey));

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

    }
}
