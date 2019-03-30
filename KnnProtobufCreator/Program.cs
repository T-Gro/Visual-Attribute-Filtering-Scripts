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
            
            using (var file = new StreamWriter("filtering-statistics.csv", append: true))
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
                


                

                foreach (var derivativeTreshold in new[]{1,2,4,8,16,32})
                {
                    var ratio = (100 - derivativeTreshold) / 100.0;
                    loadedFile = AllResults.Load(smallerFileName);
                    loadedFile.Rows.ForEach(r => r.FilterNeigbhoursUsingDistanceDerivative(ratio));
                    loadedFile.PrintStats(filename, "Candidates-shrinked-using-distance-derivative;" + ratio + ";999999;0", sw);
                    loadedFile.RefreshReferenceMap();
                    for (int i = 0; i < 31; i++)
                    {
                        loadedFile.RefBasedShrink();
                        loadedFile.RefreshReferenceMap();
                    }
                    loadedFile.PrintStats(filename, "Symmetrical-filter;" + ratio + ";999999;0", sw);
                    foreach (var maxImagesTreshold in new[]{25,50,100})
                    {
                        foreach (var minImagesTreshold in new[]{2,4,6,8})
                        {
                            var clustered = ClusterDecomposition.GroupIntoClusters(loadedFile, maxImagesTreshold, minImagesTreshold);
                            clustered.PrintStats(filename, $"After-clustering;{derivativeTreshold};{maxImagesTreshold};{minImagesTreshold}", sw);
                        }
                    }

                }

                
                
            }

            Console.WriteLine("Done");
        }
    }
}
