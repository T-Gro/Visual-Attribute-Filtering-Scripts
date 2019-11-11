using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargeFileFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceFile = File.ReadLines(args[0]);
            var whitelistItems = new HashSet<string>(File.ReadAllLines(args[1]), StringComparer.OrdinalIgnoreCase);
            using (var sw = new StreamWriter(args[0].Replace(".","-filtered.")))
            {
                foreach (var line in sourceFile)
                {
                    var keyIdx = line.IndexOf(';');
                    var key = line.Substring(0, keyIdx);
                    if(whitelistItems.Contains(key))
                        sw.WriteLine(line);
                }
            }

        }
    }
}
