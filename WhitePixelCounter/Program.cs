using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace WhitePixelCounter
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Enter search pattern within this directorz, e.g. '3x4*'");
      var files = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), Console.ReadLine() ?? String.Empty);

      var amountsOfWhite = files
        .AsParallel()
        .Select(x => new
      {
        FileName = Path.GetFileNameWithoutExtension(x),
        WhitePercentage = CalculateWhite(x),
        PatchId = x.Substring(x.IndexOf('_') + 1,(x.LastIndexOf('_') - x.IndexOf('_')) - 1),
        ObjectId = x.Substring(x.LastIndexOf('_') + 1, (x.IndexOf('.') - x.LastIndexOf('_')) - 1)
      }).GroupBy(x => x.WhitePercentage / 5).ToList();

      foreach (var group in amountsOfWhite)
      {
        var groupName = $"_percentage-of-white-from-{group.Key * 5}-to-{Math.Min(100,5 + group.Key * 5)}.csv";
        if(File.Exists(groupName))
          File.Delete(groupName);
        using (var sw = File.CreateText(groupName))
        {
          sw.WriteLine("ImageNameInternal,ObjectId,PatchId,PercentageOfWhite");
          foreach (var x in group)
          {
            sw.WriteLine("{0},{1},{2},{3}", x.FileName, x.ObjectId, x.PatchId, x.WhitePercentage);
          }
        }
      }
    }

    private static long counter = 0;

    private static int CalculateWhite(string filename)
    {
      if(Interlocked.Increment(ref counter) % 100 == 0)
        Console.Write(counter);
      unsafe
      {
        using (var bmp = new Bitmap(filename))
        {
          var bitmapData = bmp.LockBits(new Rectangle(2, 2, bmp.Width - 4, bmp.Height - 4), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
          int bytesPerPixel = Image.GetPixelFormatSize(bitmapData.PixelFormat)/8;
          int heightInPixels = bitmapData.Height;
          int widthInBytes = bitmapData.Width*bytesPerPixel;
          byte* ptrFirstPixel = (byte*) bitmapData.Scan0;

          int totalWhitePixels = 0;
          int totalPixels = bitmapData.Height*bitmapData.Width;

          for (int y = 0; y < heightInPixels; y++)
          {
            byte* currentLine = ptrFirstPixel + (y*bitmapData.Stride);
            for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
            {
              int oldBlue = currentLine[x];
              int oldGreen = currentLine[x + 1];
              int oldRed = currentLine[x + 2];

              if (oldBlue > 250 && oldGreen > 250 && oldRed > 250)
                totalWhitePixels++;
            }
          }
          bmp.UnlockBits(bitmapData);

          return (100*totalWhitePixels)/totalPixels;
        }
      }
    }
  }
}
