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

            Console.WriteLine("Parsing");

            var newFile = File.ReadAllLines(args[0]).Select(ParseLine).ToArray();
            var oldFile = File.ReadAllLines(args[1]).Select(ParseLine).ToArray();
            Console.WriteLine("Parsed");
            Console.WriteLine($"Expecting {newFile.Length} * {oldFile.Length} = {newFile.Length * oldFile.Length} calculations.");

            var perOld =
                (from o in oldFile.AsParallel()
                 let mark512 = newFile.Select(x => new { x.name, D = L2Distance(x.data, o.data) }).OrderBy(x => x.D).ElementAt(512)
                 select new { OldName = o.name, Treshold = mark512.D }).ToDictionary(x => x.OldName, x => x.Treshold);

            Console.WriteLine("Old treshold calced");

            var sg = new SimilarityGraph
            {
                OldNameTreshold512 = perOld,
                ResultsForNewImages = newFile.AsParallel()
                .Select(nf => new { Name = nf.name, Hits = oldFile.Select(of => new DistancesToOldImages { OldPatchName = of.name, Distance = L2Distance(nf.data, of.data) }).Where(x => x.Distance < perOld[x.OldPatchName]).ToArray() })
                .ToDictionary(x => x.Name, x => x.Hits)                
            };

            Console.WriteLine("Graph created");

            using (var outS = File.Create(args[0].Replace(".","distances-bin.")))
            {
                Serializer.Serialize(outS, sg);
            }

            Console.WriteLine("Graph saved");

        }

        static char[] Delimiter = new char[] { ';' };
        static long Counter = 0;

        private static float L2Distance(float[] x1,float[] x2)
        {
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
            [ProtoMember(2)]
            public Dictionary<string,float> OldNameTreshold512 { get; set; }
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
