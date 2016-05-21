using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTCam.Video
{
    public delegate void EncodeStatusEvent(FfMpegEncoder encoder, EncodeStatus status);

    public class FfMpegEncoder
    {
        public event EncodeStatusEvent StatusUpdate;

        public Process FfMpegCmdWrapperProcess { get; private set; }
        public Process FfMpegCoreProcess { get; private set; }

        public void StopFfmpeg()
        {
            if (FfMpegCmdWrapperProcess == null) { return; }
            Stopwatch sw = new Stopwatch();
            Console.WriteLine("Sending close signal to ffmpeg...");
            FfMpegCmdWrapperProcess.StandardInput.Write("q");
            FfMpegCmdWrapperProcess.StandardInput.Flush();
            System.Threading.Thread.Sleep(100);
            FfMpegCmdWrapperProcess.StandardInput.WriteLine("\x3");
            FfMpegCmdWrapperProcess.StandardInput.Flush();
            sw.Start();
            System.Threading.Thread.Sleep(500);
            
            while (FfMpegCmdWrapperProcess.HasExited == false && sw.ElapsedMilliseconds < 10000)
            {
                System.Threading.Thread.Sleep(500);
            }

            if (!FfMpegCmdWrapperProcess.HasExited)
            {
                Console.WriteLine("Ruthlessly killing ffmpeg process since it did not obey.");
                FfMpegCmdWrapperProcess.Kill();
                if (FfMpegCoreProcess != null)
                {
                    if (!FfMpegCoreProcess.HasExited)
                    {
                        FfMpegCoreProcess.Kill();
                    }
                }
                Console.WriteLine("Killed ffmpeg. Sorry ffmpeg.");
            }
        }

        public void Encode(FileInfo ffMpegExe, string encodeArgs)
        {
            FileInfo monitorFile = new FileInfo(Path.Combine(ffMpegExe.Directory.FullName, "FFMpegMonitor_" + Guid.NewGuid().ToString() + ".txt"));

            if (monitorFile.Exists)
            {
                monitorFile.Delete();
            }

            //string ffmpegpath = Environment.SystemDirectory + "\\cmd.exe";
            //string ffmpegargs = "/C \"\"" + ffMpegExe.FullName + "\" " + encodeArgs + "\" 2>" + monitorFile.FullName;

            string ffmpegpath = ffMpegExe.FullName;
            string ffmpegargs = encodeArgs;

            string fullTestCmd = ffmpegpath + " " + ffmpegargs;

            Console.WriteLine(fullTestCmd);

            ProcessStartInfo psi = new ProcessStartInfo(ffmpegpath, ffmpegargs);
            psi.WorkingDirectory = ffMpegExe.Directory.FullName;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.Verb = "runas";

            var procsBefore = Process.GetProcessesByName("ffmpeg");

            var proc = Process.Start(psi);
            FfMpegCmdWrapperProcess = proc;

            System.Threading.Thread.Sleep(1000);
            var procsAfter = Process.GetProcessesByName("ffmpeg");
            if (procsAfter.Count() > procsBefore.Count())
            {
                foreach (var procAfter in procsAfter)
                {
                    if (!procsBefore.Any(pb => pb.Id == procAfter.Id))
                    {
                        Console.WriteLine("Found new Ffmpeg core process: " + procAfter.Id.ToString());
                        FfMpegCoreProcess = procAfter;
                        break;
                    }
                }   
            }

            while (!proc.HasExited)
            {
                System.Threading.Thread.Sleep(1000);

                try
                {
                    var fs = new FileStream(monitorFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    string text = string.Empty;
                    using (var sr = new StreamReader(fs))
                    {
                        text = sr.ReadToEnd();
                    }

                    // frame=  878 fps= 39 q=46.0 size=     971kB time=29.20 bitrate= 272.6kbits/s
                    var matchingLines = text.Split(System.Environment.NewLine[0]).Where(line => string.IsNullOrWhiteSpace(line) == false && line.Trim().StartsWith("frame")).ToList();

                    if (matchingLines.Count == 0) { continue; }

                    string lastLine = matchingLines.Last().Trim();

                    var itemsOfData = lastLine.Split(" "[0], "="[0]).Where(s => string.IsNullOrEmpty(s) == false).Select(s => s.Trim().Replace("=", string.Empty)).ToList();

                    EncodeStatus status = new EncodeStatus();
                    status.Frame = ToNullableInt32(GetValueFromItemData(itemsOfData, "frame"));
                    status.FPS = ToNullableDouble(GetValueFromItemData(itemsOfData, "fps"));
                    status.Size = GetValueFromItemData(itemsOfData, "size");

                    var ts = ToNullableTimeSpan(GetValueFromItemData(itemsOfData, "time"));
                    if (ts.HasValue)
                    {
                        status.Time = ToNullableDouble(ts.Value.TotalSeconds.ToString());
                    }

                    status.Bitrate = GetValueFromItemData(itemsOfData, "bitrate");

                    if (this.StatusUpdate != null)
                    {
                        this.StatusUpdate(this, status);
                    }

                    //Console.WriteLine(lastLine);
                }
                catch { }
            }

            Console.WriteLine("FFmpeg process exited.");
        }

        public static int? ToNullableInt32(string s)
        {
            int i;
            if (Int32.TryParse(s, out i))
            {
                return i;
            }
            return null;
        }

        public static double? ToNullableDouble(string s)
        {
            double i;
            if (Double.TryParse(s, out i))
            {
                return i;
            }
            return null;
        }

        public static TimeSpan? ToNullableTimeSpan(string s)
        {
            TimeSpan ts;
            if (TimeSpan.TryParse(s, out ts))
            {
                return ts;
            }

            return null;
        }

        private string GetValueFromItemData(List<string> items, string targetKey)
        {
            var key = items.FirstOrDefault(i => i.ToUpper() == targetKey.ToUpper());

            if (key == null) { return null; }
            var idx = items.IndexOf(key);

            var valueIdx = idx + 1;

            if (valueIdx >= items.Count)
            {
                return null;
            }

            return items[valueIdx];
        }
    }
}
