using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace PatternBasedFileSplitter
{
  class Program
  {

    static string GetInput(string text)
    {
      Console.WriteLine(text);
      return Console.ReadLine();
    }

    static Dictionary<string, StreamWriter> sws = new Dictionary<string, StreamWriter>();

    static StreamWriter GetSw(string prefix)
    {
      if (sws.ContainsKey(prefix))
        return sws[prefix];

      var sw = new StreamWriter(Path.Combine(outputFolder,prefix + ".partial.csv"));
      sws[prefix] = sw;

      return sw;
    }

    private static string outputFolder;

    static void Main(string[] args)
    {
      var file = GetInput("What file would you like to split? Enter full path or path relative to this .exe:");
      var pattern = GetInput("What is the splitting regex pattern? Use meta characters to beginning of file. E.g. ^[.*]{6} matches the first 6 characters");
      outputFolder = GetInput("What is the output folder? Either full path or relative to this .exe");

      var rgx = new Regex(pattern, RegexOptions.Compiled);
      

      using (var f = File.OpenText(file))
      {
        var x = 0;
        var line = f.ReadLine();
        do
        {
          var prefix = rgx.Match(line).Value;
          GetSw(prefix).WriteLine(line);
          if(++x %100 == 0)
            Console.WriteLine(x);
        } while ((line = f.ReadLine()) != null);
      }

      foreach (var streamWriter in sws)
      {
        streamWriter.Value.Dispose();
      }
    }
  }
}
