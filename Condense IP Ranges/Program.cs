using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Condense_IP_Ranges.Json;
using NetTools;

namespace Condense_IP_Ranges
{
    class Program
    {
        static void Main(string[] args)
        {
            var httpClient = new HttpClient();
            var jsonIpRanges = httpClient
                .GetStringAsync(new Uri("https://ip-ranges.amazonaws.com/ip-ranges.json"))
                .Result;

            var awsIpPrefixes = JsonSerializer.Deserialize<AwsIpRanges>(jsonIpRanges)?.Prefixes;

            if (awsIpPrefixes == null)
            {
                return;
            }

            var euWestRanges = awsIpPrefixes
                .FindAll(x => x.Region == "eu-west-1" || x.Region == "eu-west-2")
                .Select(x => x.GetIpAddressRange())
                .OrderBy(y => y.GetPrefixLength())
                .ToArray();
            var newIpRanges = new List<IPAddressRange>();

            foreach (var euWestRange in euWestRanges)
            {
                if (newIpRanges.Any(x => x.Contains(euWestRange))) continue;

                if (euWestRange.GetPrefixLength() < 16)
                {
                    newIpRanges.Add(euWestRange);
                }
                else
                {
                    var ipAddress = euWestRange.Begin.ToString().Split('.');
                    var newAddress = IPAddressRange.Parse($"{ipAddress[0]}.{ipAddress[1]}.0.0/16");

                    if (!newIpRanges.Any(x => x.Contains(newAddress)))
                    {
                        newIpRanges.Add(newAddress);
                    }
                }
            }

            Console.WriteLine($"Started with {euWestRanges.Count()} now we have {newIpRanges.Count()}");
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }
            
            var fileStream = File.Create("output/ip-ranges.txt");
            newIpRanges.ForEach(x =>
            {
                var lineBytes = Encoding.ASCII.GetBytes(x.ToCidrString() + Environment.NewLine);
                fileStream.Write(lineBytes);
            });
            fileStream.Close();
        }
    }
}