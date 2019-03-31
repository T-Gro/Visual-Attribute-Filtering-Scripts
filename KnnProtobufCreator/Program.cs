using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using KnnResults.Domain;
using ZootBataLabelsProcessing;

namespace KnnProtobufCreator
{
    public class Program
    {
        class CompositionWriter : TextWriter
        {
            private IEnumerable<TextWriter> originals;
            public CompositionWriter(IEnumerable<TextWriter> originals)
            {
                this.originals = originals;
                Encoding = originals.First().Encoding;
            }

            public override void WriteLine(string value)
            {
                foreach (var orig in originals)
                {
                    orig.WriteLine(value);
                }
            }

            public override Encoding Encoding { get; }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Provide original .bin filenames separated by ; to analyze");
            foreach (var f in Console.ReadLine().Split(';'))
            {
                CalculateStats(f);
            }
            Console.ReadLine();
            return;


            if (args.Length > 0 && args[0] == "csv-to-bin")
            {
                CsvToProtobuf.CreateProtobufFile();
                return;
            }

            if (args.Length > 0 && args[0] == "bin-all-smaller")
            {
                var wl = AllResults.Load(ConfigurationManager.AppSettings["FilteredPatchesBin"]);
                var wlie = wl.ImageEncoding.Reverse();
                var wlpe = wl.PatchEncoding.Reverse();
                var globalSet = OverallDataAccessor.allClusters;
                var interestingImagePatches = wl
                    .Rows.SelectMany(r => r.Hits.Select(h => h.Hit).Concat(new[] {r.Query}))
                    .Distinct()
                    .Select(p => new Patch{ImageId = globalSet.ImageEncoding[wlie[p.ImageId]], PatchId = globalSet.PatchEncoding[wlpe[p.PatchId]]})
                    .ToLookup(x => x);

                var removed = globalSet.Rows.RemoveAll(rr => !interestingImagePatches.Contains(rr.Query));
                globalSet.Save(ConfigurationManager.AppSettings["OverAllKnnGraphBin"].Replace(".bin","-essential-knn.bin"));

                Console.WriteLine("After filtering {0} rows remaining, {1} was removed",globalSet.Rows.Count, removed);

                return;
            }

            return;

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

        private static void CalculateStats(string filename)
        {
            
            using (var file = new StreamWriter("filtering-statistics-selection.csv", append: true))
            using (var sw = new CompositionWriter(new[] {file, Console.Out}))
            {
                AllResults loadedFile;
                void PrintStats(string stepName)
                {
                    loadedFile.PrintStats(filename, stepName, sw);
                }

                var smallerFileName = filename.Replace(".bin", "-tresholdBasedCleaned.bin");
                if (!File.Exists(smallerFileName))
                {
                    Console.WriteLine("Starting from scatch, no previous save point");
                    loadedFile = AllResults.Load(filename);
                    PrintStats("Default-all");

                    loadedFile.Rows.RemoveAll(r => r.HasNearDuplicates());
                    GC.Collect();
                    PrintStats("Near-duplicate-candidates-removed");

                    loadedFile.Rows.RemoveAll(r => r.HasTooManyCloseMatches());
                    PrintStats("Too-large-candidates-removed");

                    loadedFile.Rows.RemoveAll(r => r.IsTooEquidistant());
                    PrintStats("Equidistant-candidates-removed");
                   
                    loadedFile.Save(smallerFileName);
                }



                var combinations = new[]
                {
                    new {File = "conv3-local.bin", Ratio = 0.91, Max = 400, Min = 10},
                    new {File = "conv3-local.bin", Ratio = 0.88, Max = 400, Min = 8},
                    new {File = "conv3-local.bin", Ratio = 0.96, Max = 50, Min = 10},

                    new {File = "conv4-local.bin", Ratio = 0.91, Max = 800, Min = 12},
                    new {File = "conv4-local.bin", Ratio = 0.89, Max = 800, Min = 8},
                    new {File = "conv4-local.bin", Ratio = 0.94, Max = 400, Min = 12},

                    new {File = "conv5-local.bin", Ratio = 0.8, Max = 800, Min = 8},
                    new {File = "conv5-local.bin", Ratio = 0.76, Max = 50, Min = 6},
                    new {File = "conv5-local.bin", Ratio = 0.92, Max = 200, Min = 8},
                }.ToLookup(x => x.File);
                // new[]{5,6,7,9,10,11,13,14,15}

                var bigFile = AllResults.Load(filename);
                Console.WriteLine(filename + " was big-loaded.");
                foreach (var c in combinations[filename])
                {
                    var ratio = c.Ratio;
                    loadedFile = AllResults.Load(smallerFileName);
                    loadedFile.Rows.ForEach(r => r.FilterNeigbhoursUsingDistanceDerivative(ratio));
                    loadedFile.RefreshReferenceMap();
                    loadedFile.RefBasedShrink();
                    loadedFile.RefreshReferenceMap();

                    for (int i = 1; i < 31; i++)
                    {
                        var removed = loadedFile.RefBasedShrink();
                        loadedFile.RefreshReferenceMap();
                        if (removed == 0)
                        {
                            Console.WriteLine($"Nothing removed at iteration {i}, stopping ref-based shrink for {ratio}");
                            break;
                        }
                    }

                    // foreach (var maxImagesTreshold in new[]{25,50,100,200,400,800,1600})
                    //  foreach (var minImagesTreshold in new[]{2,4,6,8,10,12})
                    var clustered = ClusterDecomposition.GroupIntoClusters(loadedFile, c.Max, c.Min);
                    clustered.PrintStats(filename, $"After-clustering;{ratio};{c.Max};{c.Min}", sw);
                    var clusterName = filename.Replace(".bin", $"deriv_{ratio}-max_{c.Max}-min_{c.Min}.bin");
                    clustered.Save(clusterName);
                    Console.WriteLine(clusterName + " was saved.");
                    using (var htmlw = new StreamWriter(clusterName.Replace("bin",".html")))
                    {
                        clustered.Render(htmlw);
                    }
                    Console.WriteLine(clusterName + " was rendered.");

                    var wlie = clustered.ImageEncoding.Reverse();
                    var wlpe = clustered.PatchEncoding.Reverse();
                    var interestingImagePatches = clustered
                        .Rows.SelectMany(r => r.Hits.Select(h => h.Hit).Concat(new[] { r.Query }))
                        .Distinct()
                        .Select(p => new Patch { ImageId = bigFile.ImageEncoding[wlie[p.ImageId]], PatchId = bigFile.PatchEncoding[wlpe[p.PatchId]] })
                        .ToLookup(x => x);

                    var newBigFile = new AllResults
                    {
                        ImageEncoding = bigFile.ImageEncoding,
                        PatchEncoding =  bigFile.PatchEncoding,
                        Rows = bigFile.Rows.Where(rr => interestingImagePatches.Contains(rr.Query)).ToList()
                    };
                    Console.WriteLine(clusterName + " 's essential knn was shrinked.");
                    newBigFile.Save(clusterName.Replace(".bin", "-essential-knn.bin"));

                    Console.WriteLine("After filtering of {2} = {0} rows remaining, {1} was removed", newBigFile.Rows.Count, bigFile.Rows.Count - newBigFile.Rows.Count, clusterName);
                }
            }

            Console.WriteLine(filename + " Done");
        }
    }
}
