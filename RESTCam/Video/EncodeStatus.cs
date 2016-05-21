using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTCam.Video
{
    public class EncodeStatus
    {
        public int? Frame { get; set; }
        public double? FPS { get; set; }
        public string Size { get; set; }
        public double? Time { get; set; }
        public string Bitrate { get; set; }
    }
}
