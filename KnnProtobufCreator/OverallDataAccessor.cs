using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using KnnResults.Domain;
using ZootBataLabelsProcessing;

namespace KnnProtobufCreator
{
    public class OverallDataAccessor
    {
        public static AllResults allClusters;
        private static IDictionary<int, string> allImages;
        private static IDictionary<int, string> allPatches;
        private static Dictionary<Tuple<int, int>, ResultsRow> distanceLookup;

        static OverallDataAccessor()
        {
            allClusters = AllResults.Load(ConfigurationManager.AppSettings["OverAllKnnGraphBin"]);
            allImages = allClusters.ImageEncoding.Reverse();
            allPatches = allClusters.PatchEncoding.Reverse();
            distanceLookup = allClusters.Rows.GroupBy(x => Tuple.Create<int, int>(x.Query.ImageId, x.Query.PatchId)).ToDictionary(x => x.Key, x => x.First());
        }

        public static string GetCleanName(string image)
        {
            return Path.GetFileNameWithoutExtension(image);
        }

        public static IEnumerable<NamedHit> FindHitsInBigFile(string sampleImage, string samplePatch)
        {
            var imageId = allClusters.ImageEncoding[sampleImage];
            var patchId = allClusters.PatchEncoding[samplePatch];

            if (!distanceLookup.ContainsKey(Tuple.Create(imageId, patchId)))
                return Enumerable.Empty<NamedHit>();

            var distances = distanceLookup[Tuple.Create(imageId, patchId)];
            var namedHits = distances.Hits.Select(x => new NamedHit { Distance = x.Distance, Img = allImages[x.Hit.ImageId], Patch = allPatches[x.Hit.PatchId] });
            return namedHits;
        }
    }
}