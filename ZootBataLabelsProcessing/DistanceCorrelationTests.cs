using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;

namespace ZootBataLabelsProcessing
{
    [TestFixture]
    public class DistanceCorrelationTests
    {
        [Test]
        public void PointBisectionForPerfectMatch()
        {
            var rnd = new Random();
            var labels = ZootLabelProcessingTests.AllRecords;
            var socks =
                (from l in labels
                let isSock = l.AllTags.Contains("socks")
                select Tuple.Create(l, (float)(isSock ? rnd.NextDouble()/4 : rnd.NextDouble()+0.2)))
                .ToList();

            var corrFactorSocks = socks.PointBiserialCorrelation(l => l.title.Contains("pono"));
            var corrFactorKalhoty = socks.PointBiserialCorrelation(l => l.title.Contains("kalhoty"));
            var corrFactorSada = socks.PointBiserialCorrelation(l => l.title.Contains("Sada"));
            var corrFactorWinter = socks.PointBiserialCorrelation(l => l.AllTags.Contains("winter"));

            Console.WriteLine($"socks = {corrFactorSocks}, kalhoty = {corrFactorKalhoty}, sada = {corrFactorSada}, winter = {corrFactorWinter}");
        }
    }
}
