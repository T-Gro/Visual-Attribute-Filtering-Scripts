using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aglomera;
using Aglomera.Linkage;
using KnnResults.Domain;

namespace KnnProtobufCreator
{
    public class ClusterDecomposition
    {
        private static Dictionary<Patch, int> imgColors = new Dictionary<Patch, int>(capacity: 160000);
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

        private static void GraphComponentDecomposition(string path)
        {
            var loaded = AllResults.Load(path);

            var artificalResults = GroupIntoClusters(loaded);
            artificalResults.Save(path.Replace(".bin", "-patchClusters.bin"));
            using (var sw = new StreamWriter(path.Replace(".bin", "-patchLevelClustersReduced.html"), append: false))
            {
                artificalResults.Render(sw);
            }
        }

        public static AllResults GroupIntoClusters(AllResults loaded, int imagePerCandidateMaxTreshold = 2000, int imagesPerCandidateMinTreshold = 4)
        {
            imgColors.Clear();
            totalColors = 0;
            var directional = loaded.Rows.SelectMany(x => x.Hits.Select(y => new { x.Query, y.Hit })).Distinct().ToList();
            neighbours = directional.Union(directional.Select(a => new { Query = a.Hit, Hit = a.Query }))
                .ToLookup(x => x.Query, x => x.Hit);

         
            stack = new Stack<Patch>(neighbours.Select(x => x.Key));
            while (stack.Count > 0)
            {
                Visit(stack.Pop());
            }


            var stats = imgColors.GroupBy(x => x.Value)
                .Where(x => x.Select(z => z.Key.ImageId).Distinct().Count() < imagePerCandidateMaxTreshold &&
                            x.Select(y => y.Key.ImageId).Distinct().Count() >= imagesPerCandidateMinTreshold);
            Console.WriteLine("Having " + stats.Count() + " nice clusters");
            var rareImgs = new HashSet<Patch>(stats.SelectMany(x => x.Select(y => y.Key)));
            loaded.Rows.RemoveAll(row => !rareImgs.Contains(row.Query));

            var artificalResults = new AllResults { ImageEncoding = loaded.ImageEncoding, PatchEncoding = loaded.PatchEncoding };
            var colorGroups = loaded.Rows.GroupBy(x => imgColors[x.Query]);
            foreach (var g in colorGroups)
            {
                artificalResults.Rows.Add(new ResultsRow
                {
                    Query = g.First().Query,
                    Hits = g.SelectMany(x => x.Hits.Concat(new[]{new SearchHit{Hit = x.Query, Distance = g.Max(colorGroup => colorGroup.Hits.Min(h => h.Distance))} }))
                        .GroupBy(x => x.Hit)
                        .Select(x => new SearchHit { Hit = x.Key, Distance = x.Min(y => y.Distance) })
                        .Distinct()
                        .ToArray()
                });
            }

            artificalResults.RefreshReferenceMap();
            return artificalResults;
        }

        public static void AgglomerativeClustering(AllResults loaded)
        {
            var imgName = "241666.jpg"; //"159161.jpg";
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

            var dummyResults = new AllResults { ImageEncoding = loaded.ImageEncoding, PatchEncoding = loaded.PatchEncoding };
            var clusterQueue = new Queue<Cluster<ResultsRow>>(new[] { clusters.SingleCluster });
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
                        .Select(x => new SearchHit { Hit = x.Key, Distance = x.Min(y => y.Distance) })
                        .Concat(item.Select(i => new SearchHit { Hit = i.Query, Distance = -1 }))
                        .ToArray()
                    });
                }
                else
                {
                    clusterQueue.Enqueue(item.Parent1);
                    clusterQueue.Enqueue(item.Parent2);
                }
            }

            loaded.RefreshReferenceMap();
            foreach (var k in AllResults.ReferenceMap.Keys)
            {
                AllResults.ReferenceMap[k][k] = 1;
            }

            using (var sw = new StreamWriter(@"G:\siret\zoot\protobuf\clusteringTestMega.html", append: false))
            {
                dummyResults.Render(sw);
            }
        }
    }
}