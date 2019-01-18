using System;
using System.Globalization;
using System.IO;
using System.Linq;
using KnnResults.Domain;

namespace KnnProtobufCreator
{
    public class CsvToProtobuf
    {
        public static void CreateProtobufFile()
        {
            Console.WriteLine("Enter name of .csv file");
            var path = Console.ReadLine();
            Console.WriteLine("Enter number of items to fetch");
            var topN = Int32.Parse(Console.ReadLine() ?? "50");
            Console.WriteLine("Enter number of neighbours to show");
            var localTake = Int32.Parse(Console.ReadLine() ?? "100");

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
                  .Select<string, SearchHit>(x => NameToHit(x, parseInfo))
                  .ToArray();

                var queryObject = NameToPatch(query.First(), parseInfo);

                parseInfo.Rows.Add(new ResultsRow { Hits = hits, Query = queryObject });

                if (rowsProcessed++ % 1000 == 0)
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

        private static readonly char[] Delimiters = new[] { '_', '|' };
        private static readonly CultureInfo FloatParser = CultureInfo.CreateSpecificCulture("en-US");


        private static SearchHit NameToHit(string name, AllResults results)
        {
            var tokens = name.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            var p = NameToPatch(results, tokens);
            return new SearchHit { Hit = p, Distance = Single.Parse(tokens.Last(), FloatParser) };
        }

        private static Patch NameToPatch(string name, AllResults results)
        {
            var tokens = name.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            return NameToPatch(results, tokens);
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

            var p = new Patch { ImageId = imgId, PatchId = patchId };
            return p;
        }
    }
}