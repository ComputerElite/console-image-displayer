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
        static int minVar = 10;
        static int maxVar = 75;
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
            d.ImageClassToPicture(d.ImageToImageClass(inputDir + "frame_" + fframe + ".png", 640, 360, false, color, false), "tmp\\converted_" + fframe + ".png", color);
            Console.WriteLine("Frame " + fframe + " saved");
            Program.running--;
            
        }

        [STAThread]
        static void Main(string[] args)
        {
          /*
            int running = 0;
            string ffps = "25";
            int fframe = 1;
            Directory.CreateDirectory("tmp");
            
            while (true)
            {
                if (!File.Exists(inputDir + "frame_" + fframe + ".png")) break;
                if (Program.running < 12)
                {
                    Console.WriteLine(Program.running);
                    Thread t = new Thread(Program.fun);
                    t.Start(fframe);
                    fframe++;
                }
                Thread.Sleep(100);
            }
            if (File.Exists("out.mp4")) File.Delete("out.mp4");
            Process fpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -r " + ffps + "/1 -start_number 0 -i \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\converted_%d.png\" -acodec copy -vcodec copy -c:v libx264 -vf \"fps = " + ffps + ", format = yuv420p\" out.mp4");
            fpr.WaitForExit();
            fpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i out.mp4 -i \"" + inputDir + "audio.wav\" out2.mp4");
            fpr.WaitForExit();
            
            
            //VideoManager ma = new VideoManager(inputDir);
            //ma.Play();
            return;
         */
            Console.WriteLine("1. Display image (grayscale)");
            Console.WriteLine("2. Display image (color)");
            Console.WriteLine("3. Camera feed (grayscale)");
            Console.WriteLine("4. Camera feed (color)");
            Console.Write("Mode: ");
            String mode = Console.ReadLine();
            if (mode == "1" || mode == "3") color = false;  
            if (mode == "3" || mode == "4") camera = true;
            if(camera)
            {
                Console.Write("Do you want to save all frames to disk (y/n)?: ");
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
                    //Write any text
                    //Bitmap b = new Bitmap(200, 100);
                    //Graphics g = Graphics.FromImage(b);
                    //g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    //g.DrawString("I can write\nanything", new Font("Arial", 20), Brushes.White, 0, 0);
                    //d.ImageToImageClass(b, Console.WindowWidth, Console.WindowHeight, false, color, maxVar, minVar);
                    //Console.ReadLine();

                    //return;

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
                    //experimental
                    if(imagePath.EndsWith(".mp4"))
                    {
                        String fps = "30";
                        ShellFile shellFile = ShellFile.FromFilePath(imagePath);
                        fps = ((double)(shellFile.Properties.System.Video.FrameRate.Value / 1000.0)).ToString();


                        if (d.config.startFrame == 1) FileManager.RecreateDirectoryIfExisting(AppDomain.CurrentDomain.BaseDirectory + "tmp");
                        FileManager.CreateDirectoryIfNotExisting(AppDomain.CurrentDomain.BaseDirectory + "tmp");
                        //vid to image sequence
                        Process pr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i \"" + imagePath + "\" -vsync 0 \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\frame_%d.png\"");
                        pr.WaitForExit();
                        if (File.Exists("audio.mp3")) File.Delete("audio.mp3");
                        pr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i \"" + imagePath + "\" audio.mp3");
                        pr.WaitForExit();
                        
                        int frame = d.config.startFrame;
                        while (true)
                        {
                            if (!File.Exists(inputDir + "frame_" + frame + ".png")) break;
                            if (Program.running < 12)
                            {
                                Console.WriteLine(Program.running);
                                Thread t = new Thread(Program.fun);
                                t.Start(frame);
                                frame++;
                            }
                            Thread.Sleep(100);
                        }
                        if (File.Exists("out.mp4")) File.Delete("out.mp4");
                        Process ffpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -r " + fps + "/1 -start_number 0 -i \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\converted_%d.png\" -acodec copy -vcodec copy -c:v libx264 -vf \"fps = " + fps + ", format = yuv420p\" out.mp4");
                        ffpr.WaitForExit();
                        ffpr = Process.Start("ffmpeg.exe", "-hwaccel cuda -i out.mp4 -i \"" + inputDir + "audio.wav\" out2.mp4");
                        ffpr.WaitForExit();

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
                //image.SetMaxVariation(maxVar);
                //image.SetMinVariation(minVar);
                //image.InvertImage();
                //d.DisplayInConsole(image, color);
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

            //image.SetMaxVariation(maxVar);
            //image.SetMinVariation(minVar);
            //image.InvertImage();
            Console.SetCursorPosition(0, 0);
            //d.DisplayInConsole(image, color);

            CanDraw = true;
        }
    }
}
