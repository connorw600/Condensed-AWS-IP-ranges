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
                Console.WriteLine(
                    "Please add Amazon Regions as comma seperated params \"./Condense IP Ranges.exe 'eu-west-1,eu-west-2,eu-west-3'\"");
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

            var ipRange = awsIpPrefixes
                .FindAll(x => regionIds.Contains(x.Region))
                .Select(x => x.GetIpAddressRange())
                .OrderBy(y => y.GetPrefixLength())
                .ToList();
            var originalIpCount = ipRange.Count;

            short maximumCidrSize = 16;
            while (ipRange.Count() > 180)
            {
                var newIpRanges = new List<IPAddressRange>();
                foreach (var currentRange in ipRange)
                {
                    var rangeToEvaluate = currentRange;
                    if (currentRange.GetPrefixLength() > maximumCidrSize)
                    {
                        rangeToEvaluate = new IPAddressRange(currentRange.Begin, maximumCidrSize);
                    }
                    
                    Console.WriteLine($"Evaluating {rangeToEvaluate}");
                    var rangeContainsCount = ipRange.Count(x => !x.Equals(currentRange) && rangeToEvaluate.Contains(x));
                    if (rangeContainsCount <= 1)
                    {
                        if (!newIpRanges.Contains(currentRange))
                        {
                            newIpRanges.Add(currentRange);
                        }

                        continue;
                    }

                    Console.WriteLine($"Range contains {rangeContainsCount} other ranges");
                    newIpRanges.RemoveAll(x => rangeToEvaluate.Contains(x));
                    newIpRanges.Add(rangeToEvaluate);
                }

                ipRange = newIpRanges;
                maximumCidrSize -= 1;
            }

            Console.WriteLine($"Started with {originalIpCount} now we have {ipRange.Count()}");
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            var fileStream = File.Create("output/ip-ranges.txt");
            ipRange.OrderBy(x => x.ToString())
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
