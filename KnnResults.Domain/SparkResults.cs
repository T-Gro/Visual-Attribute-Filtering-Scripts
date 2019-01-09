using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace KnnResults.Domain
{
  public class SparkResults
  {
    private static Regex Parser = new Regex(@"^\(((?<inputId>\d+)\+?)+,List\(((?<outputId>\d+)[\W]{0,2})+\)\)$",RegexOptions.Compiled);

    public static SparkResults Parse(IEnumerable<string> lines)
    {
      var results = new SparkResults();
      foreach (var l in lines)
      {
        var m = Parser.Match(l);
        var input = m.Groups["inputId"].Captures.OfType<Capture>().Select(c => int.Parse(c.Value));
        var output = m.Groups["outputId"].Captures.OfType<Capture>().Select(c => int.Parse(c.Value));
        results.Rules.Add(new MarketBasketRule{Input = input.ToArray(), Output = output.ToArray()});
      }

      return results;
    }

    public void Print(TextWriter tw, Dictionary<int,string> imagemapping, Func<MarketBasketRule, bool> filter)
    {
      tw.WriteLine(@"
<style>
#container {
    position:relative;	
}

img {
  width: 336px;
  height: 414px;
}

th{
	font-size: 40
}

");
      tw.WriteLine("</style>");
      tw.WriteLine("<table>");

      foreach (var rule in Rules.Where(filter))
      {
        tw.WriteLine("  <tr>");
        tw.WriteLine("  <td><h3>WHEN:</h3></td>");
        foreach (var inId in rule.Input)
        {
          tw.WriteLine($"   <td><div id='container'><img src='images-cropped/{imagemapping[inId]}' /></div></td>");
        }
        tw.WriteLine("    <td><h3>-THEN:</h3></td>");
        foreach (var outId in rule.Output)
        {
          tw.WriteLine($"   <td><div id='container'><img src='images-cropped/{imagemapping[outId]}' /></div></td>");
        }
        tw.WriteLine("  </tr>");
      }

      tw.WriteLine("</table>");
    }


    public List<MarketBasketRule> Rules { get; } = new List<MarketBasketRule>();
  }
}