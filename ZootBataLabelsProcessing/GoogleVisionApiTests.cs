using Google.Cloud.Vision.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ZootBataLabelsProcessing
{
    [TestFixture]
    public class GoogleVisionApiTests
    {
        [TestCase(
            @"C:\sir-files\vbs-filelist.txt",
            @"http://herkules.ms.mff.cuni.cz/vadet-merged/images-cropped/vbs-images/",
            @"C:\sir-files\vsb-google-features.html")]
        public void ExtractGoogleLabels(string filelistFilepath, string imageHostingBaseUrl, string outputPath)
        {
            var client = ImageAnnotatorClient.Create();
            var allfilenames = File.ReadAllLines(filelistFilepath);

            var requests = new List<BatchAnnotateImagesRequest> { new BatchAnnotateImagesRequest() };
            foreach (var f in allfilenames)
            {
                if (requests.Last().Requests.Count == 16)
                    requests.Add(new BatchAnnotateImagesRequest());

                requests.Last().Requests.Add(new AnnotateImageRequest
                {
                    Image = Image.FromUri(imageHostingBaseUrl + f),
                    Features =
                    {
                        new Feature { Type = Feature.Types.Type.LabelDetection },
                        new Feature{Type = Feature.Types.Type.TextDetection},
                        new Feature{Type = Feature.Types.Type.ObjectLocalization},
                        new Feature{Type = Feature.Types.Type.FaceDetection}
                    }
                });
            }

            var collected = requests
                .Select((r, i) => client.BatchAnnotateImages(r))
                .SelectMany(res => res.Responses)
                .ToList();


            File.WriteAllText(outputPath, @"
<html>
<table border='1px solid black'>
<tr>
<th>Image</th>
<th>Labels</th>
<th>Text</th>
<th>Faces</th>
<th>Objects</th>
</tr>");

            var outputLines = collected
                .Zip(allfilenames, Tuple.Create)
                .Select(l => $@"
<tr>
    <td><a href='{imageHostingBaseUrl + l.Item2}'><img src='{imageHostingBaseUrl + l.Item2}' width='300px'/></a></td>
    <td><ol> {l.Item1.LabelAnnotations.Select(la => "<li>" + la.Description + "  (" + la.Score.ToString("0.00") + ")</li>" ).Tie()} </ol></td>
    <td><ol> {l.Item1.TextAnnotations.Select(ta => "<li>" + ta.Description + "</li>").Tie()} </ol></td>
    <td><ol> {l.Item1.FaceAnnotations.Select(fa => "<li>" + GetFaceAttributes(fa) +"  (" + fa.DetectionConfidence.ToString("0.00") + ")</li>").Tie()} </ol></td>
    <td><ol> {l.Item1.LocalizedObjectAnnotations.Select(loa => "<li>" + loa.Name + "  (" + loa.Score.ToString("0.00") + "  "+ loa.BoundingPoly.Vertices.Dump() +")</li>").Tie()} </ol></td>
</tr>")
                
                .ToArray();

            File.AppendAllLines(outputPath, outputLines);
            File.AppendAllText(outputPath, "</table></html>");

            Assert.Pass();
        }

        private string GetFaceAttributes(FaceAnnotation fa)
        {
            var props = fa
                .GetType()
                .GetProperties()
                .Where(prop => prop.PropertyType == typeof(Likelihood))
                .Select(p => new {p.Name, Likelihood = (Likelihood) p.GetValue(fa)})
                .Where(x => x.Likelihood > Likelihood.Unlikely)
                .ToDictionary(x => x.Name, x => x.Likelihood.ToString());
          
            return  new
            {
                Properties = props, fa.PanAngle, fa.TiltAngle, fa.RollAngle
            }.Dump();
        }

    }
}
