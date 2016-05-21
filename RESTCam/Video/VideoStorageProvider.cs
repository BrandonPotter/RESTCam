using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTCam.Video
{
    public class VideoStorageProvider
    {
        private DirectoryInfo _vRoot = null;
        public DirectoryInfo VideoRootFolder
        {
            get
            {
                if (_vRoot != null) { return _vRoot; }
                DirectoryInfo videoSubfolder = new DirectoryInfo(Program.Options.RootFolder);

                if (!videoSubfolder.Exists)
                {
                    videoSubfolder.Create();
                }

                _vRoot = videoSubfolder;
                return _vRoot;
            }
        }

        private static DateTime? _lastCleanup;
        public void Cleanup()
        {
            if (Program.Options.MaxDays > 0)
            {
                if (_lastCleanup.HasValue && DateTime.Now.Subtract(_lastCleanup.Value).TotalMinutes < 10)
                {
                    return;
                }

                _lastCleanup = DateTime.Now;

                DateTime olderThanDate = DateTime.Now.AddDays(0 - Program.Options.MaxDays);
                Console.WriteLine("Cleaning up files older than " + olderThanDate.ToString() + " in folder " +
                                  VideoRootFolder.FullName);
                var files = VideoRootFolder.GetFiles("*.*", SearchOption.AllDirectories);
                int cleanedFileCount = 0;
                foreach (var file in files)
                {
                    if (file.LastWriteTime < olderThanDate)
                    {
                        Console.WriteLine("Cleaning up old file " + file.Name + " from " + file.LastWriteTime.ToString());
                        try
                        {
                            file.Delete();
                            cleanedFileCount += 1;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Could not delete file " + file.Name + ": " + ex.Message);
                        }
                    }
                }

                Console.WriteLine("Cleanup complete, files cleaned: " + cleanedFileCount.ToString());
            }
        }
    }
}
