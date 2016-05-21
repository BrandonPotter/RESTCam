using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace RESTCam
{
    public class Options
    {
        [Option('p', "port", Required = false,
            HelpText = "Port to listen on")]
        public int Port { get; set; }

        [Option("maxdays", Required = false, HelpText = "Delete videos older than (days).")]
        public int MaxDays { get; set; }

        [Option('r', "rootfolder", Required = false, HelpText ="Root folder for video storage and retrieval.")]
        public string RootFolder { get; set; }
    }
}
