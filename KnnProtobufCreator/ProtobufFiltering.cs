using System;
using System.IO;
using KnnResults.Domain;

namespace KnnProtobufCreator
{
  class ProtobufFiltering
  {
    private static void RenderData()
    {
      var protobufFolder = @"G:\siret\zoot\protobuf";
      foreach (var file in Directory.GetFiles(protobufFolder, "*conv4*.bin"))
      {
        Console.WriteLine(file + " started");
        var loadedFile = AllResults.Load(file);

        var newName = Filter(loadedFile, file);
        Print(newName, loadedFile);

        Console.WriteLine($"{file} is done now");
      }
    }

    private static void Print(string newName, AllResults loadedFile)
    {
      using (var sw = new StreamWriter(newName.Replace(".bin", ".html"), append: false))
      {
        loadedFile.Render(sw);
      }
    }

    private static string Filter(AllResults loadedFile, string file)
    {
      for (int i = 0; i < 31; i++)
      {
        Console.WriteLine($"iteration {i} starting");
        loadedFile.CrossReferenceFilter();
        loadedFile.RefBasedShrink();
      }

      var newName = file.Replace(".bin", "-refShrink" + 30 + ".bin");
      loadedFile.Save(newName);
      return newName;
    }
  }
}