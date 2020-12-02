using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Condense_IP_Ranges.Json
{
    public class AwsIpRanges
    {
        [JsonPropertyName("syncToken")]        
        public string SyncToken { get; set; }
        
        [JsonPropertyName("createDate")]
        public string CreateDate { get; set; }
        
        [JsonPropertyName("prefixes")]
        public List<AwsIpPrefix> Prefixes { get; set; }
    }
}