using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Text.RegularExpressions;
using AForge.Video;
using System.Diagnostics;
using AForge.Video.DirectShow;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using FontStyle = System.Drawing.FontStyle;
using System.Windows.Forms;
using Clipboard = System.Windows.Clipboard;
using Microsoft.WindowsAPICodePack.Dialogs;
using ComputerUtils.Videos;
using Microsoft.WindowsAPICodePack.Shell;
using System.Text.Json;
using ComputerUtils.FileManaging;
using ComputerUtils.Camera;

namespace ImageDisplayer
{

    class Program
    {
        static bool CanDraw = true;
        static bool color = true;
        static bool camera = false;
        static int frameCounter = 0;
        static bool save = false;
        static String folder = "";
        static ImageDisplayer d = new ImageDisplayer();
        static CameraManager m = new CameraManager();
        static string inputDir = "C:\\Users\\ComputerElite\\Desktop\\aaaaaaaa\\";
        public static int running = 0;


        static void fun(object fframe)
        {
            Console.WriteLine("converting frame " + fframe + ". " + running + " Threads running");
            Program.running++;
            if (!File.Exists(inputDir + "frame_" + fframe + ".png")) return;
            d.ImageClassToPicture(d.ImageToImageClass(inputDir + "frame_" + fframe + ".png", d.config.videoWidth, d.config.videoHeight, false, color, false), "tmp\\converted_" + fframe + ".png", color);
            Console.WriteLine("Frame " + fframe + " saved");
            Program.running--;
            
        }

        [STAThread]
        static void Main(string[] args)
        {
            d.config.Save();

            // This code is for start of convert and can be removed.
            /*
              int running = 0;
              string ffps = "25";
              int fframe = 1;
              Directory.CreateDirectory("tmp");

              while (true)
              {
                  if (!File.Exists(inputDir + "frame_" + fframe + ".png")) break;
                  if (Program.running < d.config.videoThreads)
                  {
                      Thread t = new Thread(Program.fun);
                      t.Start(fframe);
                      fframe++;
                  }
              }
              if (File.Exists("out.mp4")) File.Delete("out.mp4");
              Process fpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -r " + ffps + "/1 -start_number 0 -i \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\converted_%d.png\" -acodec copy -vcodec copy -c:v libx264 -vf \"fps = " + ffps + ", format = yuv420p\" out.mp4");
              fpr.WaitForExit();
              fpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i out.mp4 -i \"" + inputDir + "audio.wav\" out2.mp4");
              fpr.WaitForExit();

              // O: that's some great code over there
              //VideoManager ma = new VideoManager(inputDir);
              //ma.Play();
              return;
           */
            Console.Title = "Image Displayer main menu";
            Console.WriteLine("1. Display image (grayscale)");
            Console.WriteLine("2. Display image (color)");
            Console.WriteLine("3. Camera feed (grayscale)");
            Console.WriteLine("4. Camera feed (color)");
            Console.Write("Mode: ");
            String mode = Console.ReadLine();
            Console.WriteLine(Console.WindowWidth + ", " + Console.WindowHeight);
            if (mode == "1" || mode == "3") color = false;  
            if (mode == "3" || mode == "4") camera = true;
            if(camera)
            {
                Console.Write("Do you want to save all frames to disk (y/n)?: ");

                // Open directory picker
                if (Console.ReadLine() == "y")
                {
                    CommonOpenFileDialog ofd = new CommonOpenFileDialog();
                    ofd.IsFolderPicker = true;
                    if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        save = true;
                        folder = ofd.FileName;
                    }
                }
                //Get all cameras and print in console
                m.getListCameraUSB();
                Console.Write("Camera: ");
                //Open the camera stream
                m.OpenCamera(Convert.ToInt32(Console.ReadLine()));
                Console.Clear();
                //Set up new frame event
                m.FrameRecievedEvent += DrawFrame;
            } else
            {
                Image i = null;
                Console.Write("Imagepath: ");
                String imagePath = Console.ReadLine().Replace("\"", "");
                if (imagePath == "")
                {
                    //Get image from clipboard
                    BitmapSource source = Clipboard.GetImage();
                    //Convert BitmapSource to Bitmap
                    Bitmap bmp = new Bitmap(source.PixelWidth, source.PixelHeight, PixelFormat.Format32bppPArgb);
                    BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
                    source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                    bmp.UnlockBits(data);

                    //Convert Bitmap to Image
                    i = d.ImageToImageClass(bmp, Console.WindowWidth, Console.WindowHeight, false, color);
                }
                else
                {
                    if(imagePath.EndsWith(".mp4"))
                    {
                        Console.Title = "Preparing video";
                        String fps = "30";
                        ShellFile shellFile = ShellFile.FromFilePath(imagePath);

                        // Get fps of org video from properties for later merge
                        fps = ((double)(shellFile.Properties.System.Video.FrameRate.Value / 1000.0)).ToString();

                        inputDir = AppDomain.CurrentDomain.BaseDirectory + "tmp\\";
                        if (d.config.startFrame == 1)
                        {
                            FileManager.RecreateDirectoryIfExisting(AppDomain.CurrentDomain.BaseDirectory + "tmp");
                            //vid to image sequence
                            Process pr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i \"" + imagePath + "\" -vsync 0 \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\frame_%d.png\"");
                            pr.WaitForExit();
                            if (File.Exists("audio.wav")) File.Delete("audio.wav");

                            // extract audio from vid
                            pr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i \"" + imagePath + "\" audio.wav");
                            pr.WaitForExit();

                            // Set up for video playing
                            File.Copy("audio.wav", inputDir + "\\audio.wav");
                            Console.WriteLine("Creating meta.json");
                            VideoAttributes a = new VideoAttributes(imagePath);
                            File.WriteAllText(inputDir + "\\meta.json", JsonSerializer.Serialize(a));
                        }


                        if(d.config.playVideo)
                        {
                            Console.Title = "Preparing video playback";
                            if(!File.Exists(inputDir + "\\meta.json"))
                            {
                                Console.WriteLine("Creating meta.json");
                                try
                                {
                                    File.Copy("audio.wav", inputDir + "\\audio.wav");
                                } catch { }
                                VideoAttributes a = new VideoAttributes(imagePath);
                                File.WriteAllText(inputDir + "\\meta.json", JsonSerializer.Serialize(a));
                            }
                            Console.WriteLine("Playback will start shortly.");
                            VideoManager m = new VideoManager(inputDir);
                            m.Play(color);
                        } else
                        {
                            ///// Convert
                            Console.Title = "Converting video to ascii";
                            int frame = d.config.startFrame;
                            while (true)
                            {
                                if (!File.Exists(inputDir + "frame_" + frame + ".png")) break;
                                // Start n threads to convert the frames into ConsoleImages
                                if (Program.running < d.config.videoThreads)
                                {
                                    Thread t = new Thread(Program.fun);
                                    t.Start(frame);
                                    frame++;
                                }
                            }
                            if (File.Exists("out.mp4")) File.Delete("out.mp4");

                            // Merge frames into mp4
                            Process ffpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -r " + fps + "/1 -start_number 0 -i \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\converted_%d.png\" -acodec copy -vcodec copy -c:v libx264 -vf \"fps = " + fps + ", format = yuv420p\" out.mp4");
                            ffpr.WaitForExit();

                            if (File.Exists("out2.mp4")) File.Delete("out2.mp4");
                            // Merge mp4 and wav to get a video with audio. This step can probably be combined with the first one
                            ffpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i out.mp4 -i \"" + AppDomain.CurrentDomain.BaseDirectory + "audio.wav\" out2.mp4");
                            ffpr.WaitForExit();
                        }
                        

                    } else
                    {
                        //Convert Bitmap to Image
                        i = d.ImageToImageClass(imagePath, Console.WindowWidth, Console.WindowHeight, false, color);
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Do you want to save the image to disk (y/n)?: ");
                if(Console.ReadLine() == "y")
                {
                    SaveFileDialog s = new SaveFileDialog();
                    s.Filter = "png (*.png)|*.png|jpg (*.jpg)|*.jpg";
                    if(s.ShowDialog() == DialogResult.OK)
                    {
                        //Convert Image to Bitmap (but this time to the thing you see in the console)
                        d.ImageClassToPicture(i, s.FileName, color);
                    }
                }
            }
            //Just wait for input so the window doesn't close
            Console.ReadLine();
        }

        private static void DrawFrame(Bitmap frame)
        {
            if (!CanDraw) return;
            CanDraw = false;
            Image image = null;
            try
            {
                //convert Bitmap to image
                image = d.ImageToImageClass(frame, Console.WindowWidth, Console.WindowHeight, true, color);
            }
            catch
            {
                return;
            }

            if(save)
            {
                //Save image as picture
                frameCounter++;
                d.ImageClassToPicture(image, folder + "\\frame_" + frameCounter + ".png", color);
            }

            // Reset console to top left.
            Console.SetCursorPosition(0, 0);
            CanDraw = true;
        }
    }
}
