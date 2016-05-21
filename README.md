# RESTCam
A C# console/service application that pulls and records streams from multiple, concurrent IP cameras or any FFmpeg-compatible source, controlled via WebAPI HTTP interface. Useful for triggering recordings via webhooks. Uses FFmpeg to encode videos to MPEG4.

Starting RESTCam
-------
Start `RESTcam.exe` with the following optional parameters:

- `--port`: Port for the web server to listen on, i.e. `--port 1234`
- `--maxdays`: Max days to keep video, i.e. `--maxdays 30` will delete video older than 30 days. Cleanup runs about every 10 minutes.
- `--rootfolder`: The root folder for video storage, i.e. `--rootfolder "C:\MyRootVideoFolder"` By default RESTCam creates a folder called "Video" where it is launched from.

Strong Opinions
-------
RESTCam comes with the following strong opinions about how things should go down:

- **Every recording session has a maximum recording time limit.** Defaults to 5 minutes, but it's a parameter called `MaxDurationSecs`. There are better solutions for long-running video recording/surveillance; this is meant for quick start/stop recording.
- **Unless you specify differently, your video is named `UnnamedVideo`**
- **Every recording is suffixed with an index number, and will not overwrite.** If you specify a filename of `MyVideo`, you will actually see `MyVideo_0000.mp4` as the first file on disk.
- **Everything is MPEG4.**
- **File names are specified at the end, not the beginning.** I use this library to record production line processes. Most of the time, I'm capturing something where I don't know what to name it until the end of the process (when a barcode finally gets scanned, etc). A random file name is generated at the beginning of recording.

Starting Recording
-------
Assuming your IP address is `192.168.1.100` and RESTCam is running on default port (`9000`), here are some example URLs to start a recording:

**Record the stream from `rtsp://some.camera/axis-media/media.amp` for a maximum of 60 seconds**

`http://192.168.1.100:9000/start?source=rtsp://my-camera-ip-address/axis-media/media.amp&MaxDurationSecs=60`

**Record the stream from `rtsp://some.camera/axis-media/media.amp` for a maximum of 60 seconds, using TCP RTSP (recommended for some Axis cameras)**

`http://192.168.1.100:9000/start?source=rtsp://my-camera-ip-address/axis-media/media.amp&MaxDurationSecs=60&UseTcpRtsp=true`

Stopping Recording
------
**Stop the current recording and name it `MyVideo`**

`http://192.168.1.100:9000/stop?FileName=MyVideo`

Multiple Recordings at the Same Time
------
By default, RESTCam assumes you are working with one recording at a time. If you want to work with, say, 3 IP cameras at a time, you will need to tell RESTCam which stream you mean with a `RecordingKey` parameter in the start and stop query strings.

For example:

**Start Recording Camera 1**
`http://192.168.1.100:9000/start?source=rtsp://my-camera-1-ip-address/axis-media/media.amp&MaxDurationSecs=60&RecordingKey=Cam1`

**Start Recording Camera 2**
`http://192.168.1.100:9000/start?source=rtsp://my-camera-2-ip-address/axis-media/media.amp&MaxDurationSecs=60&RecordingKey=Cam2`

**Start Recording Camera 3**
`http://192.168.1.100:9000/start?source=rtsp://my-camera-3-ip-address/axis-media/media.amp&MaxDurationSecs=60&RecordingKey=Cam3`

**Stop Camera 1**
`http://192.168.1.100:9000/stop?FileName=MyVideoFromCam1&RecordingKey=Cam1`

**Stop Camera 2**
`http://192.168.1.100:9000/stop?FileName=MyVideoFromCam2&RecordingKey=Cam2`

**Stop Camera 3**
`http://192.168.1.100:9000/stop?FileName=MyVideoFromCam3&RecordingKey=Cam3`

If multiple streams are in progress and `/stop` is called with no `RecordingKey`, all currently active streams are stopped.
