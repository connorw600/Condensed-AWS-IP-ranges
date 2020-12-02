using System.Text.Json.Serialization;
using NetTools;

namespace Condense_IP_Ranges.Json
{
    public class AwsIpPrefix
    {
        [JsonPropertyName("ip_prefix")]
        public string IpPrefix { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }
        
        [JsonPropertyName("service")]
        public string Service { get; set; }
        
        [JsonPropertyName("network_border_group")]
        public string NetworkBorderGroup { get; set; }

        public IPAddressRange GetIpAddressRange()
        {
            return IPAddressRange.Parse(IpPrefix);
        }
    }
}