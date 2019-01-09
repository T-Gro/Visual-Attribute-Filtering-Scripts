using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace KnnResults.Domain
{
  public class MarketBasketRule
  {
    public int[] Input { get; set; }
    public int[] Output { get; set; }
  }


  [ProtoContract]
  public class AllResults
  {
    public static Dictionary<int, Dictionary<int, int>> ReferenceMap;

    [ProtoMember(1)]
    public List<ResultsRow> Rows { get; set; } 
    [ProtoMember(2)]
    public Dictionary<string,int> ImageEncoding { get; set; }
    [ProtoMember(3)]
    public Dictionary<string,int> PatchEncoding { get; set; }

    public AllResults()
    {
      Rows = new List<ResultsRow>(capacity:1200000);
      ImageEncoding = new Dictionary<string, int>(capacity:40000);
      PatchEncoding = new Dictionary<string, int>(capacity:60);
    }

    public void CrossReferenceFilter()
    {
      ReferenceMap =
        Rows
          .GroupBy(x => x.Query.ImageId)
          .Select(g => new
          {
            g.Key,
            Friends = g
              .SelectMany(x => x.Hits)
              .GroupBy(x => x.Hit.ImageId)
              .Select(h => new {h.Key, Count = h.Count()})
              .ToDictionary(x => x.Key, x => x.Count)
          }).ToDictionary(x => x.Key, x => x.Friends);
    }

    public void RefBasedShrink()
    {
      var removed = this.Rows.RemoveAll(r => r.IsRefBasedRubbish());
      Console.WriteLine(removed + " rows removed alltogether");
    }

    public void Save(string filename)
    {
      using (var file = File.Create(filename))
      {
        Serializer.Serialize(file, this);
      }
    }

    public void Render(TextWriter sw)
    {
      sw.WriteLine(@"
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

.highlight {
    position:absolute;
    width: 56px;
    height:52px;
	border: 2px solid red;    
}
.crop0-left{	
    left:0px;
}
.crop1-left{	
    left:56px;
}
.crop2-left{	
    left:112px;
}
.crop3-left{	
    left:168px;
}
.crop4-left{	
    left:224px;
}
.crop5-left{	
    left:280px;
}
.crop0-top{	
    top:0px;
}
.crop1-top{	
    top:52px;
}
.crop2-top{	
    top:103px;
}
.crop3-top{	
    top:155px;
}
.crop4-top{	
    top:207px;
}
.crop5-top{	
    top:259px;
}
.crop6-top{	
    top:310px;
}
.crop7-top{	
    top:362px;
}");
//      td:first-child{
//	border:5px solid lime;
//}


      sw.WriteLine("</style>");
      sw.WriteLine("<table>");
      var names = ImageEncoding.ToDictionary(x => x.Value, x => x.Key);
      var patches = PatchEncoding.ToDictionary(x => x.Value, x => x.Key);
      foreach (var row in Rows.Take(3000))
      {
        sw.WriteLine(row.ToString(names, patches));
      }
      sw.WriteLine("</table>");
    }

    public static AllResults Load(string filename)
    {
      using (var file = File.OpenRead(filename))
      {
        return Serializer.Deserialize<AllResults>(file);
      }
    }

    public void ToCsv(TextWriter tw)
    {
      foreach (var rr in Rows)
      {
        tw.WriteLine(String.Join(";", rr.GetInvolvedImages()));
      }
    }
  }
}