
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows.Media;
using ImageDisplayer;
using System.Media;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ComputerUtils.Videos
{
    public class VideoManager
    {
        public VideoAttributes attributes = new VideoAttributes();
        public DateTime StartTime = DateTime.Now;
        public bool playing = false;
        public String videoFolderPath = "";
        public Thread t;

        public String ytURL = "";
        public String name = "";

        public delegate void VideoProcessed(VideoProcessedEventArgs args);
        public event VideoProcessed VideoProcessedEvent;

        public VideoManager(String videoFolderPath)
        {
            this.videoFolderPath = videoFolderPath;
            this.attributes = JsonSerializer.Deserialize<VideoAttributes>(File.ReadAllText(videoFolderPath + "\\meta.json"));
        }

        public VideoManager()
        {

        }

        public void Download(String YouTubeURL, String name, String VideoBaseDir)
        {
            this.videoFolderPath = VideoBaseDir;
            this.ytURL = YouTubeURL;
            this.name = name;
            Thread t = new Thread(Download);
            t.Start();
        }

        private void Download()
        {
            if (Directory.Exists(videoFolderPath + "\\" + name)) Directory.Delete(videoFolderPath + "\\" + name, true);
            Directory.CreateDirectory(videoFolderPath + "\\" + name);
            Process p = Process.Start("youtube-dl.exe", "--output \"" + videoFolderPath + "\\" + name + "\\video.mp4\" " + this.ytURL);
            p.WaitForExit();
            String file = "";
            foreach (String s in Directory.GetFiles(videoFolderPath + "\\" + name))
            {
                if (Path.GetFileName(s).StartsWith("video."))
                {
                    file = s;
                    break;
                }
            }
            p = Process.Start("ffmpeg.exe", "-i \"" + file + "\" -start_number 0 -vsync 0 \"" + videoFolderPath + "\\" + name + "\\frame_%d.png\"");
            p.WaitForExit();
            p = Process.Start("ffmpeg.exe", "-i \"" + file + "\" \"" + videoFolderPath + "\\" + name + "\\audio.mp3\"");
            p.WaitForExit();
            VideoAttributes a = new VideoAttributes(file);
            File.WriteAllText(videoFolderPath + "\\" + name + "\\meta.json", JsonSerializer.Serialize(a));
            VideoProcessedEvent(new VideoProcessedEventArgs(name, a.frames, a.length, a.fPS, a.success));
        }

        public void Play()
        {
            playing = true;
            t = new Thread(VideoPlayerThread);
            t.Start();
        }

        public int playedFrames = 0;

        private void VideoPlayerThread()
        {
            StartTime = DateTime.Now;
            ImageDisplayer.ImageDisplayer d = new ImageDisplayer.ImageDisplayer();
            Process pr = null;
            if (!File.Exists(videoFolderPath + "\\audio.wav"))
            {
                pr = Process.Start("ffmpeg.exe", "-i \"" + videoFolderPath + "\\audio.mp3\" \"" + videoFolderPath + "\\audio.wav\"");
                pr.WaitForExit();
            }
            SoundPlayer p = new SoundPlayer(videoFolderPath + "\\audio.wav");
            p.Load();
            p.Play();
            int width = Console.WindowWidth;
            int height = Console.WindowHeight;
            List<DateTime> frameTimes = new List<DateTime>();
            frameTimes.Add(DateTime.Now);
            int i = 0;
            while (playing)
            {
                int frame = (int)Math.Floor((DateTime.Now - StartTime).TotalSeconds * this.attributes.fPS) + this.attributes.start;
                if (frame > this.attributes.frames)
                {
                    playing = false;
                    break;
                }
                if(i == 0)
                {
                    width = Console.WindowWidth;
                    height = Console.WindowHeight;
                }
                Bitmap bmp = new Bitmap(videoFolderPath + "\\frame_" + frame + ".png");
                DateTime t = DateTime.Now;
                d.ImageToImageClass(bmp, width, height, true, true, true);
                Console.SetCursorPosition(0, 0);
                Console.Write((playedFrames / (DateTime.Now - frameTimes[0]).TotalSeconds) + " fps");
                if (frameTimes.Count > 20)
                {
                    frameTimes.RemoveAt(0);
                    playedFrames--;
                }
                frameTimes.Add(DateTime.Now);
                playedFrames++;
                i++;
                if (i == 5) i = 0;
                //Console.Clear();
                //int wait = (1000 / Program._client.Count) - (t - DateTime.Now).Milliseconds;
                //Program.Commands.Log("sent message " + client, 0);
            }
        }
    }

    public class VideoProcessedEventArgs : EventArgs
    {
        public String name = "";
        public int frames = 0;
        public int start = 0;
        public double length = 0.0;
        public double fPS = 0.0;
        public bool success { get; set; } = true;

        public VideoProcessedEventArgs(String name, int frames, double length, double fPS, bool success)
        {
            this.name = name;
            this.frames = frames;
            this.length = length;
            this.fPS = fPS;
            this.success = success;
        }
    }

    public class VideoAttributes
    {
        public double fPS { get; set; } = 0.0;
        public double length { get; set; } = 0.0;
        public bool success { get; set; } = true;
        public int frames { get; set; } = 0;
        public int start { get; set; } = 0;
        public String name { get; set; } = "";

        public VideoAttributes()
        {

        }

        public VideoAttributes(double fPS, double length, int frames, string name)
        {
            this.fPS = fPS;
            this.length = length;
            this.frames = frames;
        }

        public VideoAttributes(String file)
        {
            try
            {
                ShellFile shellFile = ShellFile.FromFilePath(file);
                try
                {
                    this.fPS = (double)(shellFile.Properties.System.Video.FrameRate.Value / 1000.0);
                }
                catch { }
                try
                {
                    this.length = (double)(shellFile.Properties.System.Media.Duration.Value / 10000000);
                }
                catch { }
                try
                {
                    this.frames = (int)Math.Floor(this.fPS * this.length);
                }
                catch { }
            }
            catch
            {
                this.success = false;
            }
        }
    }
}