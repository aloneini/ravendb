using System;
using System.Net;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Client.Connection.Async;
using Raven.Client.Document;
using Raven.Client.Listeners;

namespace Raven.ClusterManager.Models
{
	public static class ServerHelpers
	{
		public static async Task<AsyncServerClient> CreateAsyncServerClient(IAsyncDocumentSession session, ServerRecord server)
		{
		    var documentStore = (DocumentStore) session.Advanced.DocumentStore;
		    var replicationInformer = new ReplicationInformer(new DocumentConvention
		                                                      {
		                                                          FailoverBehavior = FailoverBehavior.FailImmediately
		                                                      });

		    ICredentials credentials = null;
		    if (server.CredentialsId != null)
		    {
		        var serverCredentials = await session.LoadAsync<ServerCredentials>(server.CredentialsId);
		        if (serverCredentials == null)
		        {
		            server.CredentialsId = null;
		        }
		        else
		        {
		            credentials = serverCredentials.GetCredentials();
		        }
		    }

		    return new AsyncServerClient(server.Url, documentStore.Conventions, credentials,
		                                 documentStore.JsonRequestFactory, null, s => replicationInformer, null,
		                                 new IDocumentConflictListener[0]);
		}
	}
}