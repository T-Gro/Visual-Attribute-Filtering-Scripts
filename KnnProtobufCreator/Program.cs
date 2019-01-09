using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using Aglomera;
using Aglomera.Linkage;
using KnnResults.Domain;

namespace KnnProtobufCreator
{
  class Program
  {
    

    private static   Dictionary<Patch, int> imgColors = new Dictionary<Patch, int>(capacity: 160000);
    private static int totalColors = 0;
    private static ILookup<Patch, Patch> neighbours;
    private static Stack<Patch> stack;

    static void Visit(Patch key)
    {
      if (!imgColors.ContainsKey(key))
      {
        imgColors[key] = ++totalColors; ;
      }
      var thisColor = imgColors[key];

      foreach (var nn in neighbours[key].Where(x => !imgColors.ContainsKey(x)))
      {
        imgColors[nn] = thisColor;
        stack.Push(nn);
      }
    }

    static void Main(string[] args)
    {
      Console.WriteLine("Enter pattern of Spark .txt file(s)");
      var lines = Directory.EnumerateFiles(@"G:\siret\spark-out\rules_conv_5-occur_5.0-conf_0.1", "part*").SelectMany(File.ReadLines);
      var results = SparkResults.Parse(lines);
      var loadedNameMapping = AllResults.Load(@"G:\siret\zoot\protobuf\local-conv5-cleaned-shrinked.bin").ImageEncoding;
      using (var sw = new StreamWriter(@"G:\siret\spark-viz\market-basket-conv5-large-filtered.html"))
      {
        results.Print(sw, loadedNameMapping.ToDictionary(x => x.Value, x => x.Key), r => r.Input.Length < 4 && r.Input.Length > 1);
      }
      Console.WriteLine("Printed. Pres enter...");
      Console.ReadLine();
      return;


      Console.WriteLine("Enter name of .bin protobuf file");
      var path = Console.ReadLine();
      var loaded = AllResults.Load(path);
      using (var sw = new StreamWriter(@"G:\siret\spark-in\" + Path.GetFileNameWithoutExtension(path) + ".csv"))
      {
        loaded.ToCsv(sw);
      }

      return;

     
      //GraphComponentDecomposition(path);
      return;

      var imgName = "241666.jpg";//"159161.jpg";
      var imgId = loaded.ImageEncoding[imgName];

      var relevantRows = loaded.Rows
        //.Where(r => r.Query.ImageId == imgId)
        .ToList();

      // cluster all of them together? Include query into dissimilarity function then

      // Or product by product, filter down to big elements, and offer to transitively load more and more?


      var metric = new ResultsRowSetBasedDistance();
      var linkage = new AverageLinkage<ResultsRow>(metric);
      var algorithm = new AgglomerativeClusteringAlgorithm<ResultsRow>(linkage);

      var clusters = algorithm.GetClustering(new HashSet<ResultsRow>(relevantRows));
      clusters.SaveToCsv(@"G:\siret\zoot\protobuf\clustertest.csv");
      //RenderData();

      var dummyResults = new AllResults {ImageEncoding = loaded.ImageEncoding, PatchEncoding = loaded.PatchEncoding};
      var clusterQueue = new Queue<Cluster<ResultsRow>>(new []{clusters.SingleCluster});
      while (clusterQueue.Count > 0)
      {
        var item = clusterQueue.Dequeue();
        if (item.Dissimilarity <= 0.70 && item.Count < 50)
        {
          dummyResults.Rows.Add(new ResultsRow
          {
            Query = item.First().Query,
            Hits = item.SelectMany(x => x.Hits)
              .GroupBy(x => x.Hit)
              .Select(x => new SearchHit{Hit = x.Key, Distance = x.Min(y => y.Distance)})
              .Concat(item.Select(i => new SearchHit{Hit = i.Query, Distance = -1}))
              .ToArray()
          });
        }
        else
        {
          clusterQueue.Enqueue(item.Parent1);
          clusterQueue.Enqueue(item.Parent2);
        }
      }
      loaded.CrossReferenceFilter();
      foreach (var k in AllResults.ReferenceMap.Keys)
      {
        AllResults.ReferenceMap[k][k] = 1;
      }
      using (var sw = new StreamWriter(@"G:\siret\zoot\protobuf\clusteringTestMega.html", append: false))
      {
        dummyResults.Render(sw);
      }
    }

    private static void GraphComponentDecomposition(string path)
    {
      var loaded = AllResults.Load(path);

      var directional = loaded.Rows.SelectMany(x => x.Hits.Select(y => new {x.Query, y.Hit})).Distinct().ToList();
      neighbours = directional.Union(directional.Select(a => new {Query = a.Hit, Hit = a.Query}))
        .ToLookup(x => x.Query, x => x.Hit);

      stack = new Stack<Patch>(neighbours.Select(x => x.Key));
      while (stack.Count > 0)
      {
        Visit(stack.Pop());
      }


      var stats = imgColors.GroupBy(x => x.Value)
        .Where(x => x.Count() < 2000 && x.Select(y => y.Key.ImageId).Distinct().Count() > 5);
      Console.WriteLine("Having " + stats.Count() + " nice clusters");
      var rareImgs = new HashSet<Patch>(stats.SelectMany(x => x.Select(y => y.Key)));
      loaded.Rows.RemoveAll(row => !rareImgs.Contains(row.Query));

      var artificalResults = new AllResults {ImageEncoding = loaded.ImageEncoding, PatchEncoding = loaded.PatchEncoding};
      var colorGroups = loaded.Rows.GroupBy(x => imgColors[x.Query]);
      foreach (var g in colorGroups)
      {
        artificalResults.Rows.Add(new ResultsRow
        {
          Query = g.First().Query,
          Hits = g.SelectMany(x => x.Hits)
            .GroupBy(x => x.Hit)
            .Select(x => new SearchHit {Hit = x.Key, Distance = x.Min(y => y.Distance)})
            .Concat(g.Select(i => new SearchHit {Hit = i.Query, Distance = -1}))
            .ToArray()
        });
      }

      artificalResults.CrossReferenceFilter();
      artificalResults.Save(path.Replace(".bin", "-patchClusters.bin"));
      using (var sw = new StreamWriter(path.Replace(".bin", "-patchLevelClustersReduced.html"), append: false))
      {
        artificalResults.Render(sw);
      }
    }

    private static void RenderData()
    {
      var protobufFolder = @"G:\siret\zoot\protobuf";
      foreach (var file in Directory.GetFiles(protobufFolder, "*conv4*.bin"))
      {
        Console.WriteLine(file + " started");
        var loadedFile = AllResults.Load(file);

        for (int i = 0; i < 31; i++)
        {
          Console.WriteLine($"iteration {i} starting");
          loadedFile.CrossReferenceFilter();
          loadedFile.RefBasedShrink();
        }
        var newName = file.Replace(".bin", "-refShrink" + 30 + ".bin");
        loadedFile.Save(newName);
        using (var sw = new StreamWriter(newName.Replace(".bin", ".html"), append: false))
        {
          loadedFile.Render(sw);
        }
        Console.WriteLine($"{file} is done now");
      }
    }

    private static void CreateProtobufFile()
    {
      Console.WriteLine("Enter name of .csv file");
      var path = Console.ReadLine();
      Console.WriteLine("Enter number of items to fetch");
      var topN = int.Parse(Console.ReadLine() ?? "50");
      Console.WriteLine("Enter number of neighbours to show");
      var localTake = int.Parse(Console.ReadLine() ?? "100");

      var parseInfo = new AllResults();
      int rowsProcessed = 0;

      foreach (var line in File.ReadLines(path).Take(topN))
      {
        var parts = line.Split(';');
        var query = parts.Take(1);
        var hits = parts
          .Skip(1)
          .Take(localTake)
          .Skip(1)
          .Select(x => NameToHit(x, parseInfo))
          .ToArray();

        var queryObject = NameToPatch(query.First(), parseInfo);

        parseInfo.Rows.Add(new ResultsRow {Hits = hits, Query = queryObject});

        if (rowsProcessed++%1000 == 0)
          Console.WriteLine(rowsProcessed);
      }

      parseInfo.Rows.Sort((first, second) => first.Query.CompareTo(second.Query));


      Console.WriteLine(
        $"Images = {parseInfo.ImageEncoding.Count}, Patches = {parseInfo.PatchEncoding.Count}, Rows = {parseInfo.Rows.Count}");
      var name = Path.GetFileNameWithoutExtension(path) + ".bin";
      parseInfo.Save(name);

      Console.WriteLine("Done, press enter");
      Console.ReadLine();
    }

    private static readonly char[] Delimiters = new[] {'_', '|'};
    static readonly CultureInfo FloatParser  = CultureInfo.CreateSpecificCulture("en-US");


    private static SearchHit NameToHit(string name, AllResults results)
    {
      var tokens = name.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
      var p = NameToPatch(results, tokens);
      return new SearchHit {Hit = p, Distance = float.Parse(tokens.Last(), FloatParser) };
    }

    private static Patch NameToPatch(string name, AllResults results)
    {
      var tokens = name.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
      return NameToPatch(results,tokens);
    }

    private static Patch NameToPatch(AllResults results, string[] tokens)
    {
      if (!results.ImageEncoding.TryGetValue(tokens[2], out var imgId))
      {
        imgId = results.ImageEncoding.Count;
        results.ImageEncoding[tokens[2]] = imgId;
      }

      var patchPart = tokens[0] + tokens[1];
      if (!results.PatchEncoding.TryGetValue(patchPart, out var patchId))
      {
        patchId = results.PatchEncoding.Count;
        results.PatchEncoding[patchPart] = patchId;
      }

      var p = new Patch {ImageId = imgId, PatchId = patchId};
      return p;
    }
  }
}
