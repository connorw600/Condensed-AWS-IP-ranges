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
            if (args.Length == 0)
            {
                Console.WriteLine("Please add Amazon Regions as comma seperated params \"./Condense IP Ranges.exe 'eu-west-1,eu-west-2,eu-west-3'\"");
            }

            var regionIds = args[0].Split(',');

            var httpClient = new HttpClient();
            var jsonIpRanges = httpClient
                .GetStringAsync(new Uri("https://ip-ranges.amazonaws.com/ip-ranges.json"))
                .Result;

            var awsIpPrefixes = JsonSerializer.Deserialize<AwsIpRanges>(jsonIpRanges)?.Prefixes;

            if (awsIpPrefixes == null)
            {
                return;
            }

            var awsIpv4Ranges = awsIpPrefixes
                .FindAll(x => regionIds.Contains(x.Region))
                .Select(x => x.GetIpAddressRange())
                .OrderBy(y => y.GetPrefixLength())
                .ToArray();
            var newIpRanges = new List<IPAddressRange>();

            foreach (var euWestRange in awsIpv4Ranges)
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

            Console.WriteLine($"Started with {awsIpv4Ranges.Count()} now we have {newIpRanges.Count()}");
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }
            
            var fileStream = File.Create("output/ip-ranges.txt");
            newIpRanges.OrderBy(x => x.ToString())
                .ToList()
                .ForEach(x =>
                {
                    var lineBytes = Encoding.ASCII.GetBytes(x.ToCidrString() + Environment.NewLine);
                    fileStream.Write(lineBytes);
                });
            fileStream.Close();
        }
    }
}