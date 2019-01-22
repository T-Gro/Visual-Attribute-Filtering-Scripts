using System;
using System.Configuration;
using System.IO;
using System.Linq;
using KnnResults.Domain;
using ZootBataLabelsProcessing;

namespace KnnProtobufCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "csv-to-bin")
            {
                CsvToProtobuf.CreateProtobufFile();
                return;
            }

            //SparkMasketBasketParsing();
            var zootLabels = ZootLabelProcessingTests.AllRecords;
            var zootLabelsByName = zootLabels.CreateIndex(x => new[] { x.id }).Unique();

            var filteredClusters = AllResults.Load(ConfigurationManager.AppSettings["FilteredPatchesBin"]);
          

            var fromIdToName = filteredClusters.ImageEncoding.ToDictionary(x => x.Value, x => Path.GetFileNameWithoutExtension(x.Key).ToLower());
            var patchIdToname = filteredClusters.PatchEncoding.Reverse();
            var imageIdToFullName = filteredClusters.ImageEncoding.Reverse();

            foreach (var cluster in filteredClusters.Rows.AsParallel())
            {
                var involved = cluster.Hits.Select(x => x.Hit).ToList();
                involved.Add(cluster.Query);

                var withDistancesAndLabels =
                  (from i in involved.Distinct()
                   let name = fromIdToName[i.ImageId]
                   let Zoot = zootLabelsByName[name]
                   let Distances = OverallDataAccessor.FindHitsInBigFile(imageIdToFullName[i.ImageId], patchIdToname[i.PatchId])
                   select new { Patch = i, Zoot, Distances })
                  .ToList();

                var commonLabels = withDistancesAndLabels
                  .SelectMany(x => x.Zoot.AllTextAttributes())
                  .GroupBy(x => x)
                  .OrderByDescending(g => g.Count())
                  .Where(g => g.Count() > 1 && g.Key != "zoot")
                  .Take(20);

                var allFoundMatches = withDistancesAndLabels
                  .SelectMany(x => x.Distances)
                  .GroupBy(x => x.Img)
                  .Select(g => new { g.Key, MinDist = g.Min(i => i.Distance), ZootLabel = zootLabelsByName[OverallDataAccessor.GetCleanName(g.Key)] })
                  .OrderBy(x => x.MinDist)
                  .Select(x => Tuple.Create(x.ZootLabel, x.MinDist))
                  .ToList();

                cluster.Labels =
                  (from cl in commonLabels
                   let corr = allFoundMatches.PointBiserialCorrelation(zl => zl.AllTextAttributes().Contains(cl.Key))
                   orderby Math.Abs(corr) descending
                   select new ClusterLabel { Correlation = corr, Label = cl.Key, Count = cl.Count() }).ToArray();
            }

            filteredClusters.Rows = filteredClusters.Rows.OrderByDescending(x => x.Labels.Max(l => Math.Abs(l.Correlation))).ToList();
            filteredClusters.Save(ConfigurationManager.AppSettings["FilteredPatchesBin"].Replace(".bin","-with-labels.bin"));


            var withLabels = AllResults.Load(ConfigurationManager.AppSettings["FilteredPatchesBin"].Replace(".bin", "-with-labels.bin"));
            using (var sw = new StreamWriter(ConfigurationManager.AppSettings["FilteredPatchesBin"]
                .Replace(".bin", "-with-labels.html")))
            {
                withLabels.Render(sw);
            }

            //ProtobufToCsv(path, loaded);
            //GraphComponentDecomposition(path);
            //ClusterDecomposition.AgglomerativeClustering(loaded);
        }
    }
}
