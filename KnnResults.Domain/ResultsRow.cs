using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ProtoBuf;
using Aglomera;

namespace KnnResults.Domain
{
    public class ResultsRowSetBasedDistance : IDissimilarityMetric<ResultsRow>
    {
        public double Calculate(ResultsRow instance1, ResultsRow instance2)
        {
            var imgsFirst = instance1.GetInvolvedImages();
            var imgsSecond = instance2.GetInvolvedImages();

            var intersect = imgsFirst.Count(x => imgsSecond.Contains(x));
            if (intersect == 1 && imgsSecond.Contains(instance1.Query.ImageId))
                return 1.0;

            var total = imgsFirst.Count + imgsSecond.Count - intersect;

            return ((total - intersect) / (double)total);
        }
    }

    [DebuggerDisplay("Label = {Label}, Corr = {Correlation}")]
    [ProtoContract]
    public class ClusterLabel
    {
        [ProtoMember(1)]
        public string Label { get; set; }
        [ProtoMember(2)]
        public double Correlation { get; set; }
        [ProtoMember(3)]
        public int Count { get; set; }
    }


    [ProtoContract]
    public class ResultsRow : IComparable<ResultsRow>
    {
        [ProtoMember(1)]
        public Patch Query { get; set; }
        [ProtoMember(2)]
        public SearchHit[] Hits { get; set; }
        [ProtoMember(3)]
        public ClusterLabel[] Labels { get; set; }


        private HashSet<int> involvedImages;

        public HashSet<int> GetInvolvedImages()
        {
            involvedImages = involvedImages ??
                             new HashSet<int>(Hits.Select(x => x.Hit.ImageId).Concat(new[] { Query.ImageId }));

            return involvedImages;
        }

        public bool IsRubbish()
        {
            var firstAreVerySmall = Hits.Take(10).All(h => h.Distance < 0.3);
            var maxIsSmall = Hits.Max(x => x.Distance) < 0.95;
            var averageIsSmall = Hits.Average(x => x.Distance) < 0.5;

            return firstAreVerySmall || maxIsSmall || averageIsSmall;

        }


        public void Shrink()
        {
            Hits = Hits.Where(h => h.Hit.ImageId != this.Query.ImageId).ToArray();
            for (int i = 1; i < Hits.Length; i++)
            {
                if (Hits[i].Distance * 0.93 > Hits[i - 1].Distance)
                {
                    this.Hits = this.Hits.Take(i).ToArray();
                    return;
                }
            }
        }

        public override string ToString()
        {
            return $"{nameof(Query)}: {Query.ImageId}/{Query.PatchId}, {nameof(Hits)}: {String.Join(";", Hits.Select(x => x.Hit.ImageId).Distinct())}";
        }

        public string ToString(IDictionary<int, string> imagemapping, IDictionary<int, string> patchmapping)
        {
            var sb = new StringBuilder(capacity: 10000);
            sb.AppendLine("<tr>");

            sb.AppendLine("<td><div><ol>");

            foreach (var label in (Labels ?? new ClusterLabel[0]).Where(x => Math.Abs(x.Correlation) > 0.15))
            {
                sb.AppendLine($"<li>corr ={Math.Abs(label.Correlation).ToString("P1")}; {label.Count.ToString("D2")}x; {label.Label} </li>");
            }
            sb.AppendLine("</ol></div></td>");

            //sb.AppendLine($"<td><div id='container'><img src='images-cropped/{imagemapping[this.Query.ImageId]}' />");
            //RenderPatchStyling(patchmapping, this.Query.PatchId, sb);
            //sb.AppendLine("</div></td>");


            var groupedHits = this.Hits.GroupBy(x => x.Hit.ImageId).OrderBy(x => x.Min(y => y.Distance));
            var lastDistance = this.Hits.Min(x => x.Distance);
            foreach (var groupedHit in groupedHits)
            {
                sb.AppendLine($"<td><div id='container'><img src='images-cropped/{imagemapping[groupedHit.Key]}' />");
                var distance = groupedHit.Min(x => x.Distance);
                sb.AppendLine(distance.ToString("##.000"));
                //if (lastDistance > 0.001)
                //  sb.AppendLine($"( + {(((distance - lastDistance)/lastDistance)*100).ToString("F")}%)");
                //sb.AppendLine($"({AllResults.ReferenceMap[Query.ImageId][groupedHit.Key]}x;REF={AllResults.ReferenceMap[groupedHit.Key].ContainsKey(Query.ImageId)})");
                lastDistance = distance;
                foreach (var location in groupedHit)
                {
                    var patchId = location.Hit.PatchId;
                    RenderPatchStyling(patchmapping, patchId, sb);
                }
                sb.AppendLine("</div></td>");
            }


            sb.AppendLine("</tr>");
            sb.AppendLine(Environment.NewLine);
            return sb.ToString();
        }

        private static void RenderPatchStyling(IDictionary<int, string> patchmapping, int patchId, StringBuilder sb)
        {
            var patch = patchmapping[patchId];
            var parts = patch.Split(new[] { '_', '@' }, StringSplitOptions.RemoveEmptyEntries);
            var size = parts[0];
            var position = int.Parse(parts[1]);
            if (size == "6x8")
            {
                var row = position / 6;
                var column = position % 6;
                sb.AppendLine($"<div class='highlight crop{column}-left crop{row}-top' ></div>");
            }
            else if (size == "3x4")
            {
                var row = 2 * (position / 3);
                var column = 2 * (position % 4);

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        sb.AppendLine($"<div class='highlight crop{column + i}-left crop{row + j}-top'></div>");
                    }
                }
            }
        }

        public bool IsRefBasedRubbish()
        {
            this.Hits = this.Hits.TakeWhile(hit => AllResults.ReferenceMap.ContainsKey(hit.Hit.ImageId) && AllResults.ReferenceMap[hit.Hit.ImageId].ContainsKey(this.Query.ImageId)).ToArray();
            return this.Hits.Length == 0;
        }

        public int CompareTo(ResultsRow other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Query.CompareTo(other.Query);
        }
    }
}