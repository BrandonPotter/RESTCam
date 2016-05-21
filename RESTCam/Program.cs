using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Owin.Hosting;

namespace RESTCam
{
    class Program
    {
        internal static Options Options { get; set; }
        static void Main(string[] args)
        {
            Options opts = new Options();
            var parser = new Parser(config => config.HelpWriter = Console.Out);
            var result = parser.ParseArguments(args, opts);

            if (!result)
            {
                Console.WriteLine("Invalid arguments");
                System.Threading.Thread.Sleep(5000);
                return;
            }

            Program.Options = opts;

            if (Program.Options.Port <= 0)
            {
                Program.Options.Port = 9000;
            }

            if (string.IsNullOrEmpty(Program.Options.RootFolder))
            {
                FileInfo thisAssembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var thisAssemblyFolder = thisAssembly.Directory;
                var videoSubfolder = new DirectoryInfo(Path.Combine(thisAssemblyFolder.FullName, "Video"));
                Program.Options.RootFolder = videoSubfolder.FullName;
            }

            Console.WriteLine("Using root video folder: " + Program.Options.RootFolder);

            if (Program.Options.MaxDays == 0)
            {
                Console.WriteLine(
                    "--maxdays command line parameter not used. Old video will not be cleaned up automatically.");
            }

            System.Threading.ThreadPool.QueueUserWorkItem((obj) =>
            {
                while (true)
                {
                    try
                    {
                        Video.VideoStorageProvider vsp = new Video.VideoStorageProvider();
                        vsp.Cleanup();
                    }
                    catch
                    {
                    }

                    System.Threading.Thread.Sleep(60000);
                }
                
            });
            

            string baseAddress = $"http://*:{Program.Options.Port}/";
            Console.WriteLine("Starting WebAPI on " + baseAddress); 
            WebApp.Start<Startup>(url: baseAddress);
            Console.WriteLine("WebAPI interface started on " + baseAddress);
            Console.ReadLine();
            Console.WriteLine("Stopping...");
            foreach (var instance in Video.FfmpegInstance.RunningInstances)
            {
                instance.Stop(null);
            }
        }
    }
}
