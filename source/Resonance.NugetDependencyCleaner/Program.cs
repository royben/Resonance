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

namespace Resonance.NugetDependencyCleaner
{
    class Program
    {
        private static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F0DEPCLEAN}");

        static void Main(string[] args)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                mutex.ReleaseMutex();
            }
            else
            {
                Environment.Exit(0);
                return;
            }

            Thread.Sleep(1000);

            try
            {
                String projectPath = args[0];
                String configuration = args[1];

                Console.WriteLine($"Executing nuget dependency cleaner for '{Path.GetFileNameWithoutExtension(projectPath)}'...");

                String projectOutput = Path.Combine(projectPath, "bin", configuration);

                List<KeyValuePair<String, String>> dependencies = new List<KeyValuePair<string, string>>();

                for (int i = 2; i < args.Length; i++)
                {
                    dependencies.Add(new KeyValuePair<string, string>(args[i], args[++i]));
                }

                foreach (var dep in dependencies)
                {
                    String targetFramework = String.Empty;

                    if (dep.Key == "net")
                    {
                        targetFramework = ".NETFramework4.6.1";
                    }
                    else if (dep.Key == "core")
                    {
                        targetFramework = "net5.0";
                    }
                    else if (dep.Key == "standard")
                    {
                        targetFramework = ".NETStandard2.0";
                    }
                    else
                    {
                        throw new ArgumentException($"Error processing dependency with target framework '{dep.Key}'.");
                    }

                    RemoveNugetDependency(projectOutput, targetFramework, dep.Value);

                    Console.WriteLine();
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Nuget package modified successfully.");

                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Thread.Sleep(5000);
            }
        }

        private static void RemoveNugetDependency(String projectOutput, String targetFramework, String dependency)
        {
            Console.WriteLine($"Removing '{targetFramework} => {dependency}' from nuget dependencies for '{Path.GetFileName(GetNugetFileFromProjectOutput(projectOutput))}'...");

            var nuget = new NugetPackage(projectOutput);

            var node = nuget.Document.Descendants().Where(x => x.Name.LocalName == "group").First(x => x.Attribute("targetFramework").Value == targetFramework)
                                     .Descendants().Where(x => x.Name.LocalName == "dependency")
                                     .FirstOrDefault(x => x.Attribute("id").Value == dependency);

            if (node != null)
            {
                node.Remove();
                nuget.Commit();
                Console.WriteLine("Dependency removed successfully.");
            }
            else
            {
                Console.WriteLine("The specified dependency was not found on the nuget package.");
            }
        }

        private static String GetNugetFileFromProjectOutput(String projectOutput)
        {
            String file = Directory.GetFiles(projectOutput).LastOrDefault(x => x.EndsWith("nupkg"));

            if (file == null)
            {
                throw new IOException($"Could not locate nuget package in '{projectOutput}'.");
            }

            return file;
        }

        public class NugetPackage
        {
            private Stream _reader;
            private String _entryName;

            public ZipFile ZipFile { get; set; }
            public XDocument Document { get; set; }

            public NugetPackage(String projectOutput)
            {
                String file = GetNugetFileFromProjectOutput(projectOutput);

                ZipFile = new ZipFile(file);
                var entry = ZipFile.Entries.Last(x => x.FileName.EndsWith("nuspec"));
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
