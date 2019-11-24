using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NormalizeViaReferenceFile
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var delimiter = new char[] { ';' };


            var oldFiles = Directory.GetFiles(Environment.CurrentDirectory, "*2017*");


            foreach (var of in oldFiles)
            {
                var binMaximas = new float[10000];
                var lineNo = 0;

                foreach (var line in File.ReadLines(of))
                {
                    lineNo++;
                    var parts = line.Split(delimiter);
                    for (int i = 1; i < parts.Length; i++)
                    {
                        try
                        {
                            binMaximas[i] = Math.Max(binMaximas[i], float.Parse(parts[i]));
                        }
                        catch (Exception ex)
                        {                        
                            Console.WriteLine($"LineNo = {lineNo}; Index {i} = {parts[i]}");
                            continue;
                        }
                        
                    }
                }

                var newFileName = of.Replace("-2017.csv", "-bata2019.csv");
                using(var sw = new StreamWriter(newFileName.Replace(".csv","-normalized.csv")))
                {
                    foreach (var line in File.ReadLines(newFileName))
                    {
                        var parts = line.Split(delimiter);
                        sw.Write(parts[0]);
                        sw.Write(';');
                        for (int i = 1; i < parts.Length; i++)
                        {
                            var item = float.Parse(parts[i]);
                            var normalized = item == 0.0 ? item : (item / binMaximas[i]);
                            sw.Write(normalized.ToString("F3"));
                            if (i == parts.Length - 1)
                                sw.WriteLine();
                            else
                                sw.Write(';');
                        }
                    }
                }

                Console.WriteLine($"{of} : {String.Join(";", binMaximas.Select(f => f.ToString("F2")))}");
            }

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
