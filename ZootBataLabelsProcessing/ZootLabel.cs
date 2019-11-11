using System.Collections.Generic;
using System.Linq;

namespace ZootBataLabelsProcessing
{
    public class ZootLabel
    {
        // "id";"title";"brand";"price";"categories";"tags"
        public string id { get; set;}
        public string title { get; set; }
        public string brand { get; set; }
        public int price { get; set; }
        public string categories { get; set; }
        public string tags { get; set; }

        public static char[] Delimiter = new[] { ',' };

        public string[] AllTags => tags.Split(Delimiter);
        public string[] AllCategories => categories.Split(Delimiter);

        public IEnumerable<string> AllTextAttributes() => AllTextAttributesRaw().Select(x => x.Trim().ToLower()).Distinct();
               
        private IEnumerable<string> AllTextAttributesRaw()
        {
            yield return title;
            yield return brand;
            foreach (var c in AllCategories) yield return c;
            foreach (var t in AllTags) yield return t;
        }

        public IEnumerable<string> MeaningfulTextAttributes() => MeaningfulTextAttributesRaw().Select(x => x.Trim().ToLower()).Distinct();

        private IEnumerable<string> MeaningfulTextAttributesRaw()
        {
            yield return AllCategories.Last();
            foreach (var t in AllTags) yield return t;
        }
    }
}
