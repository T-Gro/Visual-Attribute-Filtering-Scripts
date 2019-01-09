using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack;

namespace VadetBrowsingResultsProcessing
{
  class Program
  {
    static void Main(string[] args)
    {
      var categories = File
        .ReadAllLines("createdFeatures.csv")
        .Select(x => x.Split(';')[1])
        .ToList();

      var allLines = File.ReadAllLines("queryResultsSummary.csv").Select(line =>
      {
        var parts = line.Split(';').Select(x => x.Trim('\'')).ToList();

        var numberOfselections = parts[5].Split(',').Sum(query => Regex.Matches(line, @"[\'\;]" + Regex.Escape(query + ":")).Count);
        var globalLocalRatio = parts[2];

        return new
        {
          IP = parts[0],
          Category = parts[1] == "-1" ? "No category selected" : categories[int.Parse(parts[1]) -1 ],
          GlobalLocal = globalLocalRatio,
          IsGlobal = globalLocalRatio == "1.0" || numberOfselections == 0,
          IsPureLocal = globalLocalRatio == "0.0" && numberOfselections > 0,
          IsWeighted = !globalLocalRatio.EndsWith(".0") && numberOfselections > 0,
          WhiteRemoval = parts[3],
          Aggregation = parts[4],
          QueryObjects = parts[5].Split(','),
          ReturnedObjects = parts[6].Split(','),
          Relevances = parts[7].Split(',').Select(int.Parse),
          Manualselections = line.Count(x => x == ':'),
          SelectionUsed = numberOfselections
        };
      }).Where(x => x.Category != "No category selected"). ToList();

      var lineLookup = allLines.ToLookup(x => x.Category);

      var relevantObjects = allLines.SelectMany(l =>
      {
        var objectsWithRelevance = l.ReturnedObjects
          .Zip(l.Relevances, Tuple.Create)
          .Where(t => t.Item2 == 1)
          .Select(x => new {l.Category, x.Item1});

        return objectsWithRelevance;
      }).ToLookup(x => x.Category, x => x.Item1).ToDictionary(x => x.Key, x => new HashSet<string>(x));

      var coverage = categories.Select((cat,idx) => new
      {
        CategoryId = idx + 1,
        Category = cat,
        Count = lineLookup[cat].Count(),
        Global = lineLookup[cat].Count(x => x.IsGlobal),
        PureLocal = lineLookup[cat].Count(x => x.IsPureLocal),
        Weighted = lineLookup[cat].Count(x => x.IsWeighted)
      }).ToList();


      var metrics = allLines.Select(x =>
      {
        var relevant = relevantObjects[x.Category];
        return new
        {
          x.Category,
          x.Aggregation,
          x.SelectionUsed,
          NUmberOfQueryObjects = x.QueryObjects.Length,
          Type = x.IsGlobal ? "Global" : (x.IsPureLocal ? "PureLocal" : "Weighted"),
          MaxTheoryRecall = relevant.Count,
          PrecAt5 = x.ReturnedObjects.Take(5).Intersect(relevant).Count()/5.0,
          PrecAt10 = x.ReturnedObjects.Take(10).Intersect(relevant).Count()/10.0,
          PrecAtAll = x.ReturnedObjects.Intersect(relevant).Count()/(double) x.ReturnedObjects.Count(),
          DcgAt5 = Dcg(x.ReturnedObjects.Take(5), relevant.Contains),
          DcgAt10 = Dcg(x.ReturnedObjects.Take(10), relevant.Contains),
          DcgAtAll = Dcg(x.ReturnedObjects, relevant.Contains),
        };
      });

      var bestResults = metrics
        .GroupBy(x => new {x.Category, x.Type})
        .Select(group => group.OrderByDescending(x => x.PrecAtAll).First())
        .ToList();

      File.WriteAllText("calculated-metrics-best.csv", bestResults.ToCsv());
      File.WriteAllText("calculated-metrics.csv", metrics.ToCsv());
      Console.WriteLine(bestResults.ToCsv());
      Console.ReadLine();
    }

    private static double Dcg(IEnumerable<string> items, Predicate<string> isRelevant)
    {
      var sum = 0.0;
      var logarithmPart = 2;
      foreach (var x in items)
      {
        sum += ((isRelevant(x) ? 1.0 : 0.0)/Math.Log(logarithmPart, 2.0));
        logarithmPart++;
      }
      return sum;
    }
  }
}
