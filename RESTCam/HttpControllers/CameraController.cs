using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using RESTCam.Models;
using RESTCam.Video;

namespace RESTCam.HttpControllers
{
    public class CameraController : ApiController
    {
        

        [Route("start")]
        [HttpGet]
        [HttpPost]
        public HttpResponseMessage StartRecording([FromUri] VideoStartRequest startRequest)
        {
            if (startRequest == null || string.IsNullOrEmpty(startRequest.Source))
            {
                return
                    GenerateJsonResponse(
                        new
                        {
                            Started = false,
                            StartRequest = startRequest,
                            Error = "No source was specified. A source is required."
                        });
            }

            if (startRequest.MaxDurationSecs == 0)
            {
                startRequest.MaxDurationSecs = (5*60);
            }

            var existingKeys =
                FfmpegInstance.RunningInstances.Where(i => i.StartRequest.RecordingKey == startRequest.RecordingKey);
            foreach (var existingInstance in existingKeys)
            {
                existingInstance.Stop(null);
            }

            FfmpegInstance newInstance = new FfmpegInstance();
            newInstance.Start(startRequest);
            Console.WriteLine("Starting recording: " + Newtonsoft.Json.JsonConvert.SerializeObject(startRequest));

            return GenerateJsonResponse(new {Started = true, StartRequest = startRequest});
        }

        [Route("stop")]
        [HttpGet]
        [HttpPost]
        public HttpResponseMessage StopRecording([FromUri] VideoStopRequest stopRequest)
        {
            if (stopRequest == null)
            {
                stopRequest = new VideoStopRequest();
            }

            if (string.IsNullOrEmpty(stopRequest.FileName))
            {
                stopRequest.FileName = "UnnamedVideo";
            }

            Console.WriteLine("Stopping recording: " + Newtonsoft.Json.JsonConvert.SerializeObject(stopRequest));
            var existingKeys =
                FfmpegInstance.RunningInstances.Where(i => i.StartRequest.RecordingKey == stopRequest.RecordingKey);
            int keysFound = existingKeys.Count();
            foreach (var existingInstance in existingKeys)
            {
                existingInstance.Stop(stopRequest);
            }

            return GenerateJsonResponse(new {Stopped = (keysFound > 0), StoppedCount = keysFound});
        }

        private HttpResponseMessage GenerateJsonResponse(object jsonObject)
        {
            string jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject);
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
