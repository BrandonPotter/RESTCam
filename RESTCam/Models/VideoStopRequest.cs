using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTCam.Models
{
    public class VideoStopRequest
    {
        public string RecordingKey { get; set; }
        public string FileName { get; set; }
    }
}
