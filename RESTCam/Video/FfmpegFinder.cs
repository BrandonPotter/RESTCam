using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace RESTCam.Video
{
    public class FfmpegFinder
    {
        private DirectoryInfo _ffMpegFolder = null;
        public DirectoryInfo FfmpegBinFolder
        {
            get
            {
                if (_ffMpegFolder != null) { return _ffMpegFolder; }
                FileInfo thisAssembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var thisAssemblyFolder = thisAssembly.Directory;
                var ffMpegSubfolder = new DirectoryInfo(Path.Combine(thisAssemblyFolder.FullName, "FFMPEG"));

                if (!ffMpegSubfolder.Exists)
                {
                    ffMpegSubfolder.Create();
                }

                _ffMpegFolder = ffMpegSubfolder;
                return _ffMpegFolder;
            }
        }

        public FileInfo GetFfmpegExePath()
        {
            string exePath = Path.Combine(FfmpegBinFolder.FullName, "ffmpeg.exe");
            if (!System.IO.File.Exists(exePath))
            {
                ExtractFfmpeg();

                if (!System.IO.File.Exists(exePath))
                {
                    throw new Exception("Unexpected error: Could not find FFmpeg exe after resource extraction.");
                }
            }

            return new FileInfo(exePath);
        }

        private void ExtractFfmpeg()
        {
            Stream zipStream;
            zipStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RESTCam.ffmpeg_x64.zip");
            var ziStream = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(zipStream);
            byte[] buffer = new byte[4096];
            ZipEntry nextEntry = ziStream.GetNextEntry();
            while (nextEntry != null)
            {
                string extractPath = Path.Combine(FfmpegBinFolder.FullName, nextEntry.Name);
                using (FileStream streamWriter = File.Create(extractPath))
                {
                    StreamUtils.Copy(ziStream, streamWriter, buffer);
                }
                nextEntry = ziStream.GetNextEntry();
            }
        }
    }
}
