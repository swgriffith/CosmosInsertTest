using System;
using System.Linq;
using System.Threading.Tasks;

using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DocDBTest
{
    class Program
    {
        private const string EndpointUrl = "<EndpointUrl>";
        private const string PrimaryKey = "<PrimaryKey>";
        private DocumentClient client;
        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.GetStartedDemo().Wait();

            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        private async Task GetStartedDemo()
        {
            this.client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
            
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = "DemoDB" });
            
            await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("DemoDB"), new DocumentCollection { Id = "DemoDBCollection" });


            Dictionary<string, UserContext> contexts = new Dictionary<string, UserContext>();
            UserContext uc = new UserContext();
            uc.Flag = true;
            uc.Stuff = new List<string>();
            uc.Stuff.Add("stuff1");
            contexts.Add("7bc21c0e-9622-4148-ab3d-befc32ece8d0", uc);

            Profile user1 = new Profile
            {
                Id = "fc959f9a-456c-48e3-b0b5-e18ef9582a64",
                UserId = "user1",
                DefaultContext = "7bc21c0e-9622-4148-ab3d-befc32ece8d0",
                Contexts = contexts            
            };

            await this.CreateDocumentIfNotExists("DemoDB", "DemoDBCollection", user1);

            uc = new UserContext();
            uc.Flag = true;
            uc.Stuff = new List<string>();
            uc.Stuff.Add("stuff2");
            contexts = new Dictionary<string, UserContext>();
            contexts.Add("9897b085-a500-4296-81e8-e6dde5be04aa", uc);
            Profile user2 = new Profile
            {
                Id = "7a72cf1e-ca8b-4d92-a7ac-ed7d35a8f26b",
                UserId = "user2",
                DefaultContext = "9897b085-a500-4296-81e8-e6dde5be04aa",
                Contexts = contexts
            };

            await this.CreateDocumentIfNotExists("DemoDB", "DemoDBCollection", user2);
            
            this.ExecuteSimpleQuery("DemoDB", "DemoDBCollection");

            //change some data and update user 2
            Console.WriteLine("Enter some text: ");
            uc.Stuff[0] = Console.ReadLine();
            user2.Contexts["9897b085-a500-4296-81e8-e6dde5be04aa"] = uc;
            
            await this.ReplaceProfileDocument("DemoDB", "DemoDBCollection", user2.Id, user2);

            this.ExecuteSimpleQuery("DemoDB", "DemoDBCollection");
        }

        private void WriteToConsoleAndPromptToContinue(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }

        private async Task CreateDocumentIfNotExists(string databaseName, string collectionName, Profile profile)
        {
            try
            {
                await this.client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, profile.Id));
                this.WriteToConsoleAndPromptToContinue("Found {0}", profile.Id);
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), profile);
                    this.WriteToConsoleAndPromptToContinue("Created Family {0}", profile.Id);
                }
                else
                {
                    throw;
                }
            }
        }

        private void ExecuteSimpleQuery(string databaseName, string collectionName)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            IQueryable<Profile> profileQuery = this.client.CreateDocumentQuery<Profile>(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                    .Where(f => f.UserId == "user2");

            // The query is executed synchronously here, but can also be executed asynchronously via the IDocumentQuery<T> interface
            Console.WriteLine("Running LINQ query...");
            foreach (Profile profile in profileQuery)
            {
                Console.WriteLine("\tRead {0}", profile);
            }

            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }

        private async Task ReplaceProfileDocument(string databaseName, string collectionName, string profileID, Profile updatedProfile)
        {
            await this.client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, profileID), updatedProfile);
            this.WriteToConsoleAndPromptToContinue("Replaced Profile {0}", profileID);
        }

    }

    public class Profile : Document
    {
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "defaultContext")]
        public string DefaultContext { get; set; }

        [JsonProperty(PropertyName = "contexts")]
        public Dictionary<string, UserContext> Contexts { get; set; }
    }

    public class UserContext
    {
        [JsonProperty(PropertyName = "flag")]
        public Boolean Flag { get; set; }

        [JsonProperty(PropertyName = "stuff")]
        public List<String> Stuff { get; set; }
    }
}
