
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureRelayDataAccess.RelayForwarder
{
    public class RelayClient
    {
        private readonly RelayConfiguration config;

        public RelayClient(RelayConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Calls the database via an Azure Relay stream, and returns the result.
        /// Note: will throw a RelayException if there is a connection error (e.g. if no listener is on the other end).
        /// </summary>
        public async Task<IEnumerable<TResultRow>> ForwardToRelay<TResultRow>(string storedProcName, ParameterInfo[] parameters, CancellationToken cancelToken = default(CancellationToken))
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(config.AccessKeyName, config.AccessKeyValue);
            var client = new HybridConnectionClient(new Uri(string.Format("sb://{0}/{1}", config.RelayNameSpace, config.ConnectionName)), tokenProvider);

            try
            {
                var relayConnection = await client.CreateConnectionAsync();
                string resultsJson;

                var writer = new StreamWriter(relayConnection) { AutoFlush = true };
                var reader = new StreamReader(relayConnection);

                var message = new
                {
                    ProcName = storedProcName,
                    Parameters = parameters
                };

                await writer.WriteLineAsync(JsonConvert.SerializeObject(message));
                resultsJson = await reader.ReadLineAsync();

                await relayConnection.CloseAsync(cancelToken);

                var array = JArray.Parse(resultsJson);
                return array.Select(o => JsonConvert.DeserializeObject<TResultRow>(o.ToString()));
            }
            catch (RelayException)
            {
                throw;
            }
        }
    }
}