using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparseVectorNormalizer
{
  class Program
  {
    struct SparseItem
    {
      public int BinId;
      public float Value;
    }

    static void Main(string[] args)
    {
      var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.svf");
      foreach (var file in files)
      {
        Console.WriteLine(file);
        var maximas = new float[10000];

        var allVectors = new List<Tuple<string,List<SparseItem>>>(50000);
        
        using (var reader = File.OpenText(file))
        {
          string line = null;
          while ((line = reader.ReadLine()) != null)
          {
              var parts = line.Split(';');
              var fileId = parts[0];
            if (line.Contains("@"))
            {
              Console.WriteLine(fileId + " is broken");
              continue;
            }
              var data = parts.Skip(3).Select(x =>
              {
                var idAndValue = x.Split(':');
                var binId = int.Parse(idAndValue[0]);
                var value = float.Parse(idAndValue[1]);

                maximas[binId] = Math.Max(maximas[binId], value);

                return new SparseItem { BinId = binId, Value = value };
              }).ToList();
              allVectors.Add(Tuple.Create(fileId, data));
          }
        }

        using (var writer = File.CreateText(file + ".normalized"))
        {
          foreach (var vector in allVectors)
          {
            writer.Write($"{vector.Item1};0;0");
            foreach (var item in vector.Item2)
            {
              var normalized = item.Value == 0.0 ? item.Value : (item.Value/maximas[item.BinId]);
              writer.Write($";{item.BinId}:{normalized.ToString("R")}");
            }
            writer.WriteLine();
          }
        }
      }
    }
  }
}
