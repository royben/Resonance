using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Resonance.PostBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            try
            {
                String configuration = args[0];
                String output = Path.GetFullPath($@"../Resonance\bin\{configuration}");
                String usbOutput = Path.GetFullPath($@"../Resonance.USB\bin\{configuration}");

                Remove_Usb_Serial_Port_Dependency(usbOutput);
                //AddNugetReadMeMarkup(output); //Not supported ?
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private static void AddNugetReadMeMarkup(params String[] outputs)
        {
            Console.WriteLine("Adding README.md file as nuget documentation.");

            foreach (var output in outputs)
            {
                var nuget = new NugetPackage(output);
                nuget.ZipFile.AddFile("../../README.md", "/");

                var documentationElement = new XElement("documentation");
                documentationElement.SetAttributeValue("xmlns", null);

                documentationElement.SetAttributeValue("src", "README.md");

                nuget.Document.Descendants().First(x => x.Name.LocalName == "metadata")
                    .Add(documentationElement);

                nuget.Commit();
            }

            //< documentation src = "documentation.md" />
        }

        private static void Remove_Usb_Serial_Port_Dependency(String projectOutput)
        {
            Console.WriteLine("Removing System.IO.SerialPorts from .NET Framework nuget dependencies for Resonance.USB.");
            Console.WriteLine($"Target path: {projectOutput}");

            var nuget = new NugetPackage(projectOutput);

            var node = nuget.Document.Descendants().Where(x => x.Name.LocalName == "group").First(x => x.Attribute("targetFramework").Value == ".NETFramework4.6.1")
                                     .Descendants().Where(x => x.Name.LocalName == "dependency")
                                     .FirstOrDefault(x => x.Attribute("id").Value == "System.IO.Ports");

            if (node != null)
            {
                node.Remove();
            }

            nuget.Commit();
        }

        public class NugetPackage
        {
            private Stream _reader;
            private String _entryName;

            public ZipFile ZipFile { get; set; }
            public XDocument Document { get; set; }

            public NugetPackage(String projectOutput)
            {
                String file = Directory.GetFiles(projectOutput).LastOrDefault(x => x.EndsWith("nupkg"));

                ZipFile = new ZipFile(file);
                var entry = ZipFile.Entries.First(x => x.FileName.EndsWith("nuspec"));
                _entryName = entry.FileName;

                _reader = entry.OpenReader();
                Document = XDocument.Load(_reader);
            }

            public void Commit()
            {
                _reader.Dispose();

                using (MemoryStream ms = new MemoryStream())
                {
                    Document.Save(ms);
                    ZipFile.UpdateEntry(_entryName, ms.ToArray());
                }

                ZipFile.Save();
                ZipFile.Dispose();
            }
        }
    }
}
