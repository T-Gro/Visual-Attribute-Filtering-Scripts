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
  public class NamedHit
  {
    public float Distance { get; set; }
    public string Img { get; set; }
    public string Patch { get; set; }
  }

  class OverallDataAccessor
  {
    private static AllResults allClusters;
    private static IDictionary<int, string> allImages;
    private static IDictionary<int, string> allPatches;
    private static Dictionary<Tuple<int, int>, ResultsRow> distanceLookup;

    static OverallDataAccessor()
    {
      allClusters = AllResults.Load(@"G:\siret\zoot\protobuf\local-conv5-cleaned-shrinked-patchClusters.bin");
      allImages = allClusters.ImageEncoding.Reverse();
      allPatches = allClusters.PatchEncoding.Reverse();
      distanceLookup = allClusters.Rows.ToDictionary(x => Tuple.Create(x.Query.ImageId, x.Query.PatchId));
    }

    public static IEnumerable<NamedHit> FindHitsInBigFile(string sampleImage, string samplePatch)
    {
      var imageId = allClusters.ImageEncoding[sampleImage];
      var patchId = allClusters.PatchEncoding[samplePatch];
      var distances = distanceLookup[Tuple.Create(imageId, patchId)];
      var namedHits = distances.Hits.Select(x => new NamedHit{ Distance = x.Distance, Img = allImages[x.Hit.ImageId], Patch = allPatches[x.Hit.PatchId] });
      return namedHits;
    }
  }

  class Program
  {
    static void Main(string[] args)
    {

      //SparkMasketBasketParsing();

 
      var filteredClusters = AllResults.Load(@"G:\siret\zoot\protobuf\local-conv5-cleaned-shrinked-patchClusters.bin");
  

      var hits = OverallDataAccessor.FindHitsInBigFile("dada","6x8");

      Console.WriteLine(hits.Count());

      var zootLabels = ZootBataLabelsProcessing.ZootLabelProcessingTests.AllRecords;
      var byName = zootLabels.CreateIndex(x => new[]{x.id }).Unique();
      var fromIdToName = filteredClusters.ImageEncoding.ToDictionary(x => x.Value, x => Path.GetFileNameWithoutExtension(x.Key).ToLower());

      foreach (var r in filteredClusters.Rows.Take(5))
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
