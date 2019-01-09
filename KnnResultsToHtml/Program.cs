using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnnResultsToHtml
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Enter name of .csv file");
      var path = Console.ReadLine();
      Console.WriteLine("Enter number of items to fetch");
      var topN = int.Parse(Console.ReadLine() ?? "50");
      Console.WriteLine("Enter number of neighbours to show");
      var localTake = int.Parse(Console.ReadLine() ?? "20");

      Console.WriteLine("<table>");
      foreach (var line in TakeRandom(File.ReadLines(path),topN))
      {
        Console.WriteLine("<tr>");
        var parts = line.Split(';');
        var query = parts.Take(1);
        var hits = parts
          .Skip(1)
          .Take(localTake)
          .Select(x => x.Split(new[] { '@', '_', '|' }, StringSplitOptions.RemoveEmptyEntries))
          .GroupBy(x => x[2])
          .Skip(1)
          .ToList();

        OutputImage(query.Select(x => x.Split(new[] { '@', '_' }, StringSplitOptions.RemoveEmptyEntries)), isFirst:true);
        hits.ForEach(grouping => OutputImage(grouping));
        Console.WriteLine("</tr>");
      }
      Console.WriteLine("</table>");
    }

    private static IEnumerable<T> TakeRandom<T>(IEnumerable<T> input, int count)
    {
      for (int i = 0; i < count; i++)
      {
        yield return input.First();
        input = input.Skip(999);
      }
    } 

    private static void OutputImage(IEnumerable<string[]> allHits, bool isFirst = false)
    {
      Console.WriteLine($"<td><div id='container'><img src='images-cropped/{allHits.First()[2]}' />");
      if (!isFirst)
      {
        var minDistance = allHits.Select(x => x.Last()).Select(double.Parse).Min();
        Console.WriteLine(minDistance);
      }

      foreach (var parts in allHits)
      {
        var size = parts[0];
        var position = int.Parse(parts[1]);
        if (size == "6x8")
        {
          var row = position/6;
          var column = position%6;
          Console.WriteLine($"<div class='highlight crop{column}-left crop{row}-top' ></div>");
        }
        else if (size == "3x4")
        {
          var row = 2*(position/3);
          var column = 2*(position%4);

          for (int i = 0; i < 2; i++)
          {
            for (int j = 0; j < 2; j++)
            {
              Console.WriteLine($"<div class='highlight crop{column + i}-left crop{row + j}-top'></div>");
            }
          }
        }
      }
      Console.WriteLine("</div></td>");
    }
  }
}
