
namespace AzureRelayDataAccess.RelayForwarder
{
    /// <summary>
    /// Contains configuration properties needed to access an Azure Relay instance
    /// </summary>
    public class RelayConfiguration
    {
        public string RelayNameSpace { get; set; }
        /// <summary>
        /// The name of the connection on the namespace you want to use, e.g. the name of the hybrid connection instance
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// The name of the Azure Access Key to use for authentication (e.g. RootManageSharedAccessKey, though you should create your own key)
        /// </summary>
        public string AccessKeyName { get; set; }
        /// <summary>
        /// The value of the access key specified
        /// </summary>
        public string AccessKeyValue { get; set; }
    }
}