using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparseVectorFormatMapper
{
  class Program
  {
    static void Main(string[] args)
    {
      var mapping = new Dictionary<int,string>();
      int rows = 0;
      var folderName = Path.GetFileName(Directory.GetCurrentDirectory());
      var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csv");

      using (var output = File.CreateText(folderName + "-sparse-vector-file-for-Premek.svf"))
      {
        foreach (var file in allFiles)
        {
          Console.WriteLine(file);
          using (var reader = File.OpenText(file))
          {
            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
              var parts = line.Split(';');
              mapping.Add(++rows,parts[0]);

              output.Write($"{rows}_0;0;0");
              for (int i = 1; i < parts.Length; i++)
              {
                output.Write($";{i-1}:{parts[i]}");
              }
              output.WriteLine();
            }
          }
        }
      }

      File.WriteAllLines(folderName + "-mapping-for-future-Tomas.map", mapping.OrderBy(x => x.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));
    }
  }
}
