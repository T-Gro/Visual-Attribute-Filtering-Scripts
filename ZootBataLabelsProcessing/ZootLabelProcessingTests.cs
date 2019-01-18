using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit;
using NUnit.Framework;

namespace ZootBataLabelsProcessing
{

    [TestFixture]
    public class ZootLabelProcessingTests
    {
        public static List<ZootLabel> AllRecords;

        static ZootLabelProcessingTests()
        {
            using (var reader = new StreamReader(@"C:\sir-files\productData.csv"))
            using (var csv = new CsvReader(reader))
            {
                csv.Configuration.Delimiter = ";";
                AllRecords = csv.GetRecords<ZootLabel>().GroupBy(x => x.id).Select(g => g.First()).ToList();
                AllRecords.ForEach(l => l.tags = l.tags.Trim());
            }
        }

        [Test]
        public void AllIndexFilter()
        {
            var allIndex = AllRecords.CreateIndex(r => r.AllTextAttributes());
            var singleIndex = allIndex.WithoutSingles();

            Assert.Greater(allIndex.Count, singleIndex.Count);
        }

        public static void LabelProcessing(string[] args)
        {
            var tagIndex = AllRecords.CreateIndex(r => r.AllTags);
            var categoryIndex = AllRecords.CreateIndex(r => r.AllCategories);
            var nameIndex = AllRecords.CreateIndex(r => new[] { r.id }).Unique();
            var brandIndex = AllRecords.CreateIndex(r => new[] { r.brand });
            var allIndex = AllRecords.CreateIndex(r => r.AllTextAttributes());
            var interesting = allIndex.OrderByDescending(x => x.Count()).Take(15).ToList();


            var sampleData = nameIndex.Values
                .Take(5)
                .Select(x => new { x.id, Attrs = x.AllTextAttributes().Select(z => z.ToLower()).ToArray() })
                .ToArray();

            var commonTerms = sampleData
                .SelectMany(x => x.Attrs)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Where(g => g.Count() > 1)
                .Select(g => new { Term = g.Key, Presense = g.Count(), Coverage = allIndex[g.Key].Count(), Total = nameIndex.Count })
                .ToList();

            Console.ReadLine();
        }
    }
}
