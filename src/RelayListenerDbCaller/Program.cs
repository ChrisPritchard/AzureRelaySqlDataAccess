using Microsoft.Azure.Relay;
using Newtonsoft.Json;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder;
using Microsoft.Extensions.Configuration.Json;
using AzureRelayDataAccess.RelaySharedModel;
using System.Data;
using System.Collections.Generic;

namespace AzureRelaySqlDataAccess.RelayListenerDbCaller
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile($"appsettings.json", true, true);
            var configuration = builder.Build();

            var relayConfig = new RelayConfiguration();
            configuration.GetSection("RelayConfiguration").Bind(relayConfig);

            var dbConnString = configuration.GetConnectionString("Default");

            RunAsync(relayConfig, dbConnString).GetAwaiter().GetResult();
        }

        private static async Task RunAsync(RelayConfiguration config, string dbConnString)
        {
            var cts = new CancellationTokenSource();

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(config.AccessKeyName, config.AccessKeyValue);
            var listener = new HybridConnectionListener(new Uri(string.Format("sb://{0}/{1}", config.RelayNameSpace, config.ConnectionName)), tokenProvider);
            
            listener.Connecting += (o, e) => { Console.WriteLine("Connecting"); };
            listener.Offline += (o, e) => { Console.WriteLine("Offline"); };
            listener.Online += (o, e) => { Console.WriteLine("Online"); };
            
            await listener.OpenAsync(cts.Token);
            Console.WriteLine("Server listening");
            
            cts.Token.Register(() => listener.CloseAsync(CancellationToken.None));

            Console.WriteLine("Press enter to kill listener");
            new Task(() => Console.In.ReadLineAsync().ContinueWith((s) => { cts.Cancel(); })).Start();

            while (true)
            {
                var relayConnection = await listener.AcceptConnectionAsync();
                if (relayConnection == null)
                {
                    break;
                }

                ProcessMessagesOnConnection(relayConnection, dbConnString, cts);
            }

            await listener.CloseAsync(cts.Token);
        }
        
        private static async void ProcessMessagesOnConnection(HybridConnectionStream relayConnection, string dbConnString, CancellationTokenSource cts)
        {
            Console.WriteLine("New session");

            var reader = new StreamReader(relayConnection);
            var writer = new StreamWriter(relayConnection) { AutoFlush = true };

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var json = await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(json))
                    {
                        await relayConnection.ShutdownAsync(cts.Token);
                        break;
                    }

                    Console.WriteLine($"Received message, {json.Length} length");
                    Console.WriteLine(json);

                    await CallDatabase(json, writer, dbConnString);
                }
                catch (IOException)
                {
                    Console.WriteLine("Client closed connection");
                    break;
                }
            }

            Console.WriteLine("End session");
            
            await relayConnection.CloseAsync(cts.Token);
        }

        private static async Task CallDatabase(string json, StreamWriter writer, string dbConnString)
        {
            var message = JsonConvert.DeserializeObject<CallInfo>(json);

            try
            {
                using (var connection = new SqlConnection(dbConnString))
                {
                    using (var command = new SqlCommand(message.ProcName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(message.Parameters.Select(o => 
                            new SqlParameter(o.Name, o.Value)).ToArray());

                        connection.Open();
                        var reader = await command.ExecuteReaderAsync();
                        var rows = new List<Dictionary<string, object>>();
                        while(reader.Read())
                            rows.Add(Enumerable.Range(0, reader.Depth).ToDictionary(o => reader.GetName(o), o => reader.GetValue(o)));
                        var responseJson = JsonConvert.SerializeObject(rows);

                        Console.WriteLine($"Called DB, received {rows.Count} rows.");

                        await writer.WriteLineAsync(responseJson);

                        Console.WriteLine("Wrote response to stream.");
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to call db with error: " + ex.Message);
            }
        }
    }
}
