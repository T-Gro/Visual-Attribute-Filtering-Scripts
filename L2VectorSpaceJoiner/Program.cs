using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace L2VectorSpaceJoiner
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var newFile = File.ReadAllLines(args[0]).Select(ParseLine).ToArray();
            var oldFile = File.ReadAllLines(args[1]).Select(ParseLine).ToArray();

            Console.WriteLine($"Expecting {newFile.Length} * {oldFile.Length} = {newFile.Length * oldFile.Length} calculations.");

            var allPairs =
                 (from n in newFile.AsParallel()
                  from o in oldFile
                  let d = L2Distance(n.data, o.data)                 
                  select new { N = n.name, O = o.name, Distance =  d}).ToLookup(x => x.N);

            var sg = new SimilarityGraph
            {
                ResultsForNewImages = allPairs.ToDictionary(x => x.Key, x => x.Select(y => new DistancesToOldImages { OldPatchName = y.O, Distance = y.Distance }).ToArray())
            };

            using (var outS = File.Create(args[0].Replace(".","distances-bin.")))
            {
                Serializer.Serialize(outS, sg);
            }

      
        }

        static char[] Delimiter = new char[] { ';' };
        static long Counter = 0;

        private static float L2Distance(float[] x1,float[] x2)
        {
            if (Counter++ % 1000 == 0)
                Console.Write('.');

            float res = 0.0f;
            for (int i = 0; i < x1.Length; i++)
            {
                var diff = x1[i] - x2[i];
                res += (diff * diff);
            }
            return (float)Math.Sqrt(res);
        }

        private static (string name,float[] data) ParseLine(string line)
        {
            var parts = line.Split(Delimiter);
            var data = parts.Skip(1).Select(x => float.Parse(x)).ToArray();

            return (parts[0], data);
        }

        [ProtoContract]
        public class SimilarityGraph
        {
            [ProtoMember(1)]
            public Dictionary<string, DistancesToOldImages[]> ResultsForNewImages { get; set; }
        }

        [ProtoContract]
        public class DistancesToOldImages
        {
            [ProtoMember(1,AsReference = true)]
            public string OldPatchName { get; set; }
            [ProtoMember(2)]
            public float Distance { get; set; }
        }
    }
}
