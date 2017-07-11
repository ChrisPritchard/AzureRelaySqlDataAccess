
using AzureRelayDataAccess.RelaySharedModel;

namespace AzureRelaySqlDataAccess.RelayListenerDbCaller
{
    public class CallInfo
    {
        public string ProcName { get; set; }
        public ParameterInfo[] Parameters { get; set; }
    }
}