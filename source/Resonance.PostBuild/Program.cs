using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Resonance.PostBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            RemoveSerialPortDependencyFromNuget(args[0]);
        }

        private static void RemoveSerialPortDependencyFromNuget(String output)
        {
            Thread.Sleep(1000);

            Console.WriteLine("Removing System.IO.SerialPorts from .NET Framework nuget dependencies for Resonance.USB.");
            Console.WriteLine($"Target path: {output}");

            String file = Directory.GetFiles(output).LastOrDefault(x => x.EndsWith("nupkg"));
            ZipFile zip = new ZipFile(file);
            var entry = zip.Entries.First(x => x.FileName == "Resonance.USB.nuspec");

            XDocument doc = null;

            using (var reader = entry.OpenReader())
            {
                doc = XDocument.Load(reader);
                var node = doc.Descendants().Where(x => x.Name.LocalName == "group").First(x => x.Attribute("targetFramework").Value == ".NETFramework4.6.1")
                .Descendants().Where(x => x.Name.LocalName == "dependency")
                .FirstOrDefault(x => x.Attribute("id").Value == "System.IO.Ports");

                if (node != null)
                {
                    node.Remove();
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms);
                zip.UpdateEntry("Resonance.USB.nuspec", ms.ToArray());
            }

            zip.Save();
            zip.Dispose();
        }
    }
}
