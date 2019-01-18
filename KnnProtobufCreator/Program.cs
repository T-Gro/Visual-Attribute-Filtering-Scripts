using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using KnnResults.Domain;

namespace KnnProtobufCreator
{
  class Program
  {
    static void Main(string[] args)
    {

      //SparkMasketBasketParsing();

      Console.WriteLine("Enter name of .bin protobuf file");
      var path = Console.ReadLine();
      var loaded = AllResults.Load(path);
      //ProtobufToCsv(path, loaded);
      //GraphComponentDecomposition(path);
      //ClusterDecomposition.AgglomerativeClustering(loaded);
    }
  }
}
