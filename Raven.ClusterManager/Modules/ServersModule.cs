using System.Linq;
using Nancy;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Connection;
using Raven.ClusterManager.Models;
using Nancy.ModelBinding;
using Raven.Json.Linq;

namespace Raven.ClusterManager.Modules
{
	public class ServersModule : NancyModule
	{
		public ServersModule(IAsyncDocumentSession session)
			: base("/api/servers")
		{
			Get["" , true] = async (parameters, ct) =>
			{
				var statistics = new ClusterStatistics
				{
				    Servers = await session.Query<ServerRecord>()
				                           .OrderBy(record => record.Id)
				                           .Take(1024)
				                           .ToListAsync(),
				    Credentials = await session.Query<ServerCredentials>()
				                               .OrderByDescending(credentials => credentials.Id)
				                               .Take(1024)
				                               .ToListAsync()
				};

			    return statistics;
			};

		    Post["/createNewServer" , true] = async (parameters, ct) =>
		    {
                var newServer = this.Bind<ServerRecord>(); // Bind the Data from the apiCtrl, Instead of Request.Form["apiKeysToSave.Secret"] for each var.
                await session.StoreAsync(newServer);
		        await session.SaveChangesAsync();
                return true;
		    };

            Put["/editServer", true] = async (parameter, ct) =>
            {
                var serverInfo = this.Bind<ServerRecord>();
                await session.StoreAsync(serverInfo);
                await session.SaveChangesAsync();
                return true;
            };

            Post["/save-api", true] = async (parameters, ct) =>
            {
                var apiKeys = this.Bind<ApiKeyViewModel[]>(); // Bind the Data from the apiCtrl, Instead of Request.Form["apiKeysToSave.Secret"] for each var.

                //pre-load
                await session.Include<ServerRecord>(x => x.CredentialsId)   
                       .LoadAsync(apiKeys.Select(x => x.Server));
                
                foreach (var apiKey in apiKeys)
                {
                    // load(serverRecords/fitzchak-pc8074)
                    var server = await session.LoadAsync<ServerRecord>(apiKey.Server); //Data about the server
                    var client = await ServerHelpers.CreateAsyncServerClient(session,server);// Creating a new client
                    var apiKeyDefinition = new ApiKeyDefinition
                    {
                        Name = apiKey.Name,
                        Enabled = true,
                        Secret = apiKey.Secret,
                        Databases =
                        {
                            new DatabaseAccess
                            {
                                Admin = false,
                                ReadOnly = false,
                                TenantId = "*"
                            }
                        }
                    };

                    await client.PutAsync("Raven/ApiKeys/" + apiKey.Name, null,
                               RavenJObject.FromObject(apiKeyDefinition), new RavenJObject());// Saving in raven
                }

                return true;
            };

			Delete["/{id}"] = parameters =>
			{
				var id = (string)parameters.id;
				session.Advanced.DocumentStore.DatabaseCommands.Delete(id, null);
				return true;
			};
		}
	}


    public class ApiKeyViewModel
    {
        public string Secret { get; set; }
        public string Name { get; set; }
        public string Server { get; set; }
    }
    public class ServerViewModel
    {

        public string Url { get; set; }
        public string Credendial { get; set; }
    }
}