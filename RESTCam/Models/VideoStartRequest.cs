using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTCam.Models
{
    public class VideoStartRequest
    {
        public string RecordingKey { get; set; }
        public bool UseTcpRtsp { get; set; }
        public string Source { get; set; }
        public int MaxDurationSecs { get; set; }
    }
}
