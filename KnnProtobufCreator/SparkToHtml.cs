using System;
using System.IO;
using System.Linq;
using KnnResults.Domain;

namespace KnnProtobufCreator
{
  class SparkToHtml
  {
    private static void SparkMasketBasketParsing()
    {
      Console.WriteLine("Enter pattern of Spark .txt file(s)");
      var lines = Directory.EnumerateFiles(@"G:\siret\spark-out\rules_conv_5-occur_5.0-conf_0.1", "part*")
        .SelectMany(File.ReadLines);
      var results = SparkResults.Parse(lines);
      var loadedNameMapping = AllResults.Load(@"G:\siret\zoot\protobuf\local-conv5-cleaned-shrinked.bin").ImageEncoding;
      using (var sw = new StreamWriter(@"G:\siret\spark-viz\market-basket-conv5-large-filtered.html"))
      {
        results.Print(sw, loadedNameMapping.ToDictionary(x => x.Value, x => x.Key),
          r => r.Input.Length < 4 && r.Input.Length > 1);
      }

      Console.WriteLine("Printed. Pres enter...");
      Console.ReadLine();
    }
  }
}