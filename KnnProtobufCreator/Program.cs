using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using KnnResults.Domain;
using ZootBataLabelsProcessing;

namespace KnnProtobufCreator
{
  class Program
  {
    static void Main(string[] args)
    {

      //SparkMasketBasketParsing();

 
      var loaded = AllResults.Load(@"G:\siret\zoot\protobuf\local-conv5-cleaned-shrinked-patchClusters.bin");

      var zootLabels = ZootBataLabelsProcessing.ZootLabelProcessingTests.AllRecords;
      var byName = zootLabels.CreateIndex(x => new[]{x.id }).Unique();
      var fromIdToName = loaded.ImageEncoding.ToDictionary(x => x.Value, x => Path.GetFileNameWithoutExtension(x.Key).ToLower());

      foreach (var r in loaded.Rows.Take(5))
      {
        var name = fromIdToName[r.Query.ImageId];
        var label = byName[name];
        Console.WriteLine(String.Join(";", label.AllTextAttributes()));
      }

      //ProtobufToCsv(path, loaded);
      //GraphComponentDecomposition(path);
      //ClusterDecomposition.AgglomerativeClustering(loaded);
    }
  }
}
