using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RESTCam.Models;

namespace RESTCam.Video
{
    public class FfmpegInstance
    {
        private static List<FfmpegInstance> _runningInstances = new List<FfmpegInstance>();
        public static IEnumerable<FfmpegInstance> RunningInstances
        {
            get { return _runningInstances.ToArray(); }
        }

        private FileInfo _ffMpegPath = null;
        public FfmpegInstance()
        {
            _ffMpegPath = new FfmpegFinder().GetFfmpegExePath();
        }

        public DateTime? StartedAt { get; private set; }

        public TimeSpan? Duration
        {
            get
            {
                if (StartedAt.HasValue && StoppedAt.HasValue)
                {
                    return StoppedAt.Value.Subtract(StartedAt.Value);
                }
                else if (StartedAt.HasValue)
                {
                    return DateTime.Now.Subtract(StartedAt.Value);
                }
                else
                {
                    return null;
                }
            }
        }

        public DateTime? StoppedAt { get; private set; }
        public VideoStartRequest StartRequest { get; set; }
        public string RecordingPath { get; set; }

        private FfMpegEncoder _encoder = null;
        public void Start(VideoStartRequest startRequest)
        {
            StartRequest = startRequest;
            _encoder = new FfMpegEncoder();
            _runningInstances.Add(this);
            string outFilePath = Path.Combine(new Video.VideoStorageProvider().VideoRootFolder.FullName,
                Guid.NewGuid().ToString().Replace("-", "") + ".mp4");
            RecordingPath = outFilePath;
            string inputArgs = $"-i {startRequest.Source}";
            string outputArgs = $"-f mp4 -vcodec mpeg4 -r 30 -t {startRequest.MaxDurationSecs} {outFilePath}";

            if (startRequest.UseTcpRtsp)
            {
                inputArgs = $"-rtsp_transport tcp {inputArgs}";
            }

            string encodeArgs = $"{inputArgs} {outputArgs}";

            System.Threading.ThreadPool.QueueUserWorkItem((obj) =>
            {
                try
                {
                    _encoder.Encode(_ffMpegPath, encodeArgs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error encoding from start request " +
                                      Newtonsoft.Json.JsonConvert.SerializeObject(StartRequest) + ": " + ex.Message);
                }
            });

            Stopwatch swFileCreate = new Stopwatch();
            swFileCreate.Start();
            while (swFileCreate.ElapsedMilliseconds < 10000)
            {
                if (System.IO.File.Exists(RecordingPath))
                {
                    break;
                }
                System.Threading.Thread.Sleep(500);
            }

            if (!System.IO.File.Exists(RecordingPath))
            {
                Console.WriteLine("Warning: Recording file not created after 10 seconds for request " +
                                  Newtonsoft.Json.JsonConvert.SerializeObject(StartRequest));
            }
        }

        public void Stop(VideoStopRequest stopRequest)
        {
            _runningInstances.Remove(this);
            _encoder.StopFfmpeg();
            if (!string.IsNullOrEmpty(RecordingPath))
            {
                if (!System.IO.File.Exists(RecordingPath))
                {
                    Console.WriteLine("Recording path " + RecordingPath + " not found.");
                }
                else
                {
                    if (!string.IsNullOrEmpty(stopRequest.FileName))
                    {
                        int fileIndex = 0;
                        while (System.IO.File.Exists(GenerateVideoFilePath(stopRequest.FileName, fileIndex)))
                        {
                            fileIndex += 1;
                        }

                        var finalFile = GenerateVideoFilePath(stopRequest.FileName, fileIndex);
                        Console.WriteLine("Renaming " + RecordingPath + " to " + finalFile);
                        PatientlyRenameFile(RecordingPath, finalFile, 0);
                        
                    }
                }
            }
        }

        private void PatientlyRenameFile(string fromFile, string toFile, int tries)
        {
            try
            {
                System.IO.File.Move(fromFile, toFile);
            }
            catch (System.IO.IOException ioEx)
            {
                if (tries > 10)
                {
                    Console.WriteLine($"Rename failed from {fromFile} to {toFile}. {ioEx.Message}. Falling back to copy instead.");
                    System.IO.File.Copy(fromFile, toFile);
                    Console.WriteLine($"Copy complete from {fromFile} to {toFile}");
                    return;
                }

                System.Threading.Thread.Sleep(1000);
                PatientlyRenameFile(fromFile, toFile, tries + 1);
            }
        }

        private string GenerateVideoFilePath(string baseFileName, int recordingIndex)
        {
            VideoStorageProvider vsp = new VideoStorageProvider();
            var filePath = Path.Combine(vsp.VideoRootFolder.FullName, $"{baseFileName}_{recordingIndex.ToString().PadLeft(4, '0')}.mp4");
            return filePath;
        }
    }
}
