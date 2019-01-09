using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace knnJoinResultsMapperMerger
{
  class Program
  {
    static void Main(string[] args)
    {
      var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.map");
      foreach (var mapFile in files)
      {
        Console.WriteLine(mapFile);
        var mapping = File.ReadAllLines(mapFile)
          .Where(l => !String.IsNullOrWhiteSpace(l))
          .Select(l =>
        {
          var parts = l.Split(':');
          return new {ResultId = parts[0] + "_0", Original = parts[1]};
        }).ToDictionary(x => x.ResultId, x => x.Original);

        var resultFile = mapFile.Replace("-mapping-for-future-Tomas.map", "-sparse-vector-file-for-Premek.svf_out");
        var mappedResultsFile = mapFile.Replace("-mapping-for-future-Tomas.map", "-mappedBackToFileNames-For-Lada.csv");

        using (var reader = File.OpenText(resultFile))
        using (var writer = File.CreateText(mappedResultsFile))
        {
          int lindexIndex = 0;
          string line;
          while ((line = reader.ReadLine()) != null)
          {
            lindexIndex++;
            var parts = line.Split('\t');
            if (parts.Length < 10)
            {
              Console.WriteLine("Empty line");
              Console.WriteLine(line);
              continue;
            }
            var query = parts[0].Substring(0, parts[0].IndexOf(','));
            if (!mapping.ContainsKey(query))
            {
              Console.WriteLine($"Query = {query}, Length = {query.Length}; lindexIndex = {lindexIndex}");
            }
            var queryName = mapping[query];

            writer.Write(queryName);
            
            for (int i = 1; i < parts.Length; i++)
            {
              var neighparts = parts[i].Split('|');
              if (!mapping.ContainsKey(neighparts[0]))
              {
                Console.WriteLine($"NeighPart = '{parts[i]}', Neihbour = '{neighparts[0]}'; lindexIndex = {lindexIndex}; i = {i}");
                Console.WriteLine(line);
              }
              var neighborName = mapping[neighparts[0]];
              var distance = neighparts[1];

              writer.Write(';');
              writer.Write(neighborName);
              writer.Write('|');
              writer.Write(distance);
            }
            writer.WriteLine();
          }
        }
      }
    }
  }
}
