using Google.Cloud.Vision.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZootBataLabelsProcessing
{
    public class GoogleVisionApiTests
    {
        static void ExtractGoogleLabels(string[] args)
        {
            var client = ImageAnnotatorClient.Create();
            var allfilenames = File.ReadAllLines(@"C:\sir-files\zootBatafilelist.txt");

            var requests = new List<BatchAnnotateImagesRequest> { new BatchAnnotateImagesRequest() };
            foreach (var f in allfilenames)
            {
                if (requests.Last().Requests.Count == 16)
                    requests.Add(new BatchAnnotateImagesRequest());

                requests.Last().Requests.Add(new AnnotateImageRequest
                {
                    Image = Image.FromUri(@"http://herkules.ms.mff.cuni.cz/vadet-merged/images-cropped/images-cropped/" + f),
                    Features = { new Feature { Type = Feature.Types.Type.LabelDetection } }
                });
            }

            var collected = requests.Select((r, i) => {
                Console.Write("Batch " + i);
                var resp = client.BatchAnnotateImages(r);
                var lines = resp.Responses.Select(l => string.Join("|", l.LabelAnnotations.Select(la => la.Score.ToString("0.00") + ":" + la.Description)));
                File.AppendAllLines(@"C:\sir-files\zootGoogleFeaturesTempStorage.txt", lines);
                Console.WriteLine(" ..done");
                return resp;
            }).SelectMany(res => res.Responses).ToList();


            var outputLines = collected
                .Zip(allfilenames, Tuple.Create)
                .Select(l => $"{l.Item2},{string.Join("|", l.Item1.LabelAnnotations.Select(la => la.Score.ToString("0.00") + ":" + la.Description))}")
                .ToArray();

            File.WriteAllLines(@"C:\sir-files\zootBataExtractedGoogleFeatures.txt", outputLines);

            Console.ReadLine();
        }
    }
}
