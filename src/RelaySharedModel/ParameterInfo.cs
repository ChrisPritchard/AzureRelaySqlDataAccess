
namespace AzureRelayDataAccess.RelaySharedModel
{
    /// <summary>
    /// A serialisable representation of a stored procedure parameter
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}