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

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("1. Display image (grayscale)");
            Console.WriteLine("2. Display image (color)");
            Console.WriteLine("3. Camera feed (grayscale)");
            Console.WriteLine("4. Camera feed (color)");
            Console.Write("Mode: ");
            String mode = Console.ReadLine();
            if (mode == "1" || mode == "3") color = false;
            if (mode == "3" || mode == "4") camera = true;
            if(color)
            {
                String read = "";
                Console.Write("grayscale trigger variation control (0-255; threadshold for grayscale activation of color; leave empty for default): ");
                if ((read = Console.ReadLine()) != "") minVar = Convert.ToInt32(read);
                Console.Write("Intercolor variation control (0-255; threadshold for determiting color of pixel; leave empty for default): ");
                if ((read = Console.ReadLine()) != "") maxVar = Convert.ToInt32(read);
            }
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
                    i = d.ImageToImageClass(bmp, Console.WindowWidth, Console.WindowHeight, false, color, maxVar, minVar);
                }
                else
                {
                    //experimental
                    if(imagePath.EndsWith(".mp4"))
                    {
                        String fps = "25";
                        if (Directory.Exists("tmp")) Directory.Delete("tmp", true);
                        Directory.CreateDirectory("tmp");
                        //vid to image sequence
                        Process pr = Process.Start("ffmpeg.exe", "-i \"" + imagePath + "\" -vsync 0 \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\frame_%d.png\"");
                        pr.WaitForExit();
                        if (File.Exists("audio.mp3")) File.Delete("audio.mp3");
                        pr = Process.Start("ffmpeg.exe", "-i \"" + imagePath + "\" audio.mp3");
                        pr.WaitForExit();
                        int frame = 1;
                        while (true)
                        {
                            if (!File.Exists("tmp\\frame_" + frame + ".png")) break;
                            d.ImageClassToPicture(d.ImageToImageClass("tmp\\frame_" + frame + ".png", Console.WindowWidth, Console.WindowHeight, false, color, maxVar, minVar, false), "tmp\\converted_" + frame + ".png", color);
                            Console.WriteLine("tmp\\conveted_" + frame + ".png saved");
                            frame++;
                        }
                        if (File.Exists("out.mp4")) File.Delete("out.mp4");
                        pr = Process.Start("ffmpeg.exe", "-r " + fps + "/1 -start_number 0 -i \"" + AppDomain.CurrentDomain.BaseDirectory + "tmp\\converted_%d.png\" -acodec copy -vcodec copy -c:v libx264 -vf \"fps = " + fps + ", format = yuv420p\" out.mp4");
                        pr.WaitForExit();
                    } else
                    {
                        //Convert Bitmap to Image
                        i = d.ImageToImageClass(imagePath, Console.WindowWidth, Console.WindowHeight, false, color, maxVar, minVar);
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
                image = d.ImageToImageClass(frame, Console.WindowWidth, Console.WindowHeight, true, color, maxVar, minVar);
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

    public class CameraManager
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice;
        private VideoCapabilities[] snapshotCapabilities;
        private ArrayList listCamera = new ArrayList();
        public static string usbcamera;
        public Bitmap lastFrame = new Bitmap(1, 1);
        public delegate void FrameRecieved(Bitmap frame);
        public event FrameRecieved FrameRecievedEvent;
        //public string pathFolder = Application.StartupPath + @"\ImageCapture\";

        public void getListCameraUSB()
        {
            //get input devices
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            int i = 0;
            if (videoDevices.Count != 0)
            {
                // add all devices to combo
                foreach (FilterInfo device in videoDevices)
                {
                    //Write all cameras in console
                    Console.WriteLine(i + ". " + device.Name);
                    i++;
                }
            }
            else
            {
                Console.WriteLine("No DirectShow devices found");
            }
        }

        public void OpenCamera(int index)
        {
            try
            {
                usbcamera = index.ToString();
                //Get all cameras
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count != 0)
                {
                    // add all devices to combo
                    foreach (FilterInfo device in videoDevices)
                    {
                        listCamera.Add(device.Name);
                    }
                }
                else
                {
                   Console.WriteLine("No Camera devices found");
                }
                //get camera
                videoDevice = new VideoCaptureDevice(videoDevices[Convert.ToInt32(usbcamera)].MonikerString);
                snapshotCapabilities = videoDevice.SnapshotCapabilities;
                if (snapshotCapabilities.Length == 0)
                {
                    //MessageBox.Show("Camera Capture Not supported");
                }
                //Start camera and set up frame event
                videoDevice.Start();
                videoDevice.NewFrame += Display;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }



        private void Display(object sender, NewFrameEventArgs eventArgs)
        {
            //Fire fram event
            FrameRecievedEvent(eventArgs.Frame);
        }
    }

    public class ImageDisplayer
    {
        public static readonly string[] luminance = new string[] { " ", "'", ".", ",", "-", "~", ":", ";", "=", "+", "!", "*", "#", "$", "@", "???" };
        //character width is half of the height
        public static readonly float heightToWidthRatio = 0.5f;

        /// <summary>
        /// Converts a Image file to a imageclass
        /// </summary>
        /// <param name="imagePath">SourceImage path</param>
        /// <param name="width">desired width</param>
        /// <param name="height">desired height</param>
        /// <param name="cropimage">crop the image at the desired height?</param>
        /// <param name="color">want color? set this to true</param>
        /// <param name="maxVariation">color similarity (0 to 255)</param>
        /// <param name="minVariation">grayscale trigger (0 to 255)</param>
        /// <param name="Display">want to display in console? Set this to true</param>
        /// <returns>converted image</returns>
        public Image ImageToImageClass(String imagePath, int width, int height, bool cropimage = false, bool color = true, int maxVariation = 75, int minVariation = 20, bool Display = true)
        {
            //Load Bitmap from path.
            Bitmap imageSource = new Bitmap(imagePath);
            return ImageToImageClass(imageSource, width, height, cropimage, color, maxVariation, minVariation, Display);
        }

        /// <summary>
        /// Converts a Bitmap to a imageclass
        /// </summary>
        /// <param name="imageSource">Bitmap as input</param>
        /// <param name="width">desired width</param>
        /// <param name="height">desired height</param>
        /// <param name="cropimage">crop the image at the desired height?</param>
        /// <param name="color">want color? set this to true</param>
        /// <param name="maxVariation">color similarity (0 to 255)</param>
        /// <param name="minVariation">grayscale trigger (0 to 255)</param>
        /// <param name="Display">want to display in console? Set this to true</param>
        /// <returns>converted image</returns>
        public Image ImageToImageClass(Bitmap imageSource, int width, int height, bool cropimage = false, bool color = true, int maxVariation = 75, int minVariation = 20, bool Display = true)
        {
            int imageWidth = imageSource.Width;
            int imageHeight = imageSource.Height;
            //Get amount of pixels to check in corelation to the deserved width
            int adjustedHeight = (int)Math.Floor(imageHeight / (imageWidth / (double)width) * heightToWidthRatio);
            //Distance between pixel checks (width)
            float adjustedWidthCalc = (float)imageWidth / (float)width;
            //Distance between pixel checks (height)
            float adjustedHeightCalc = (float)imageHeight / (float)(imageHeight / (imageWidth / (float)width)) / heightToWidthRatio;
            //brightness fraction
            double fraction = 1 / ((double)255 / luminance.Length + 1);
            width--;
            if (cropimage && adjustedHeight > height) adjustedHeight = height - 1;
            //Create 2d color array with deserved size
            Color[,] image = new Color[width, adjustedHeight];
            String print = "";

            for (int h = 0; h < adjustedHeight; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    //Console.WriteLine(w + ", " + h);
                    //Convert pixel to color
                    Color c = new Color(imageSource.GetPixel((int)(adjustedWidthCalc * w), (int)(adjustedHeightCalc * h)));
                    //Set values for color determiting
                    c.minVariation = minVariation;
                    c.variation = maxVariation;
                    if(Display)
                    {
                        //get luminance of color
                        if (!color) print += luminance[(int)Math.Floor(c.ToWhiteBlack() * fraction)];
                        else
                        {
                            //Set foreground color for writing
                            Console.ForegroundColor = c.GetColor();
                            try
                            {
                                //Write luminance of color
                                Console.Write(luminance[(int)Math.Floor(c.ToWhiteBlack() * fraction)]);
                            }
                            catch { }
                        }
                    }
                    //Add Color to 2d color array
                    image[w, h] = c;
                }
                if(Display)
                {
                    if (!color) print += "\n";
                    else Console.WriteLine();
                }
            }
            if (Display)
            {
                if (!color) Console.Write(print);
            }
            //Create image
             return new Image(width, adjustedHeight, image);
        }

        /// <summary>
        /// Saves a image in console format
        /// </summary>
        /// <param name="image">input image class</param>
        /// <param name="destination">save location</param>
        /// <param name="color">want color? set this to true</param>
        public void ImageClassToPicture(Image image, String destination, bool color = true)
        {
            //Set up output Bitmap
            Bitmap output = new Bitmap(image.width * 16, image.height * 32);
            Graphics g = Graphics.FromImage(output);
            //Set background console black
            g.FillRectangle(Brushes.Black, 0, 0, output.Width, output.Height);
            Font font = new Font("Consolas", 20);
            double fraction = 1 / ((double)255 / luminance.Length + 1);
            for (int h = 0; h < image.height; h++)
            {
                for (int w = 0; w < image.width; w++)
                {
                    //Console.ForegroundColor = image.imageColors[w, h].GetColor();
                    //Set color of brush
                    Brush b = new SolidBrush(color ? Color.FromConsoleColor(image.imageColors[w, h].GetColor()).ToColor() : System.Drawing.Color.FromArgb(255, 255, 255));
                    //Write character to Bitmap at right position
                    g.DrawString(luminance[(int)Math.Floor(image.imageColors[w, h].ToWhiteBlack() * fraction)], font, b, w * 16, h * 32);
                }
            }
            //Save bitmap
            output.Save(destination);
        }

        public void DisplayInConsole(Image image, bool withColor = true)
        {
            //Displays image in console. Obsolete but can still be used if needed.
            double fraction = 1 / ((double)255 / luminance.Length + 1);
            if(withColor)
            {
                for (int h = 0; h < image.height; h++)
                {
                    Console.WriteLine();
                    for (int w = 0; w < image.width; w++)
                    {
                        Console.ForegroundColor = image.imageColors[w, h].GetColor();
                        try
                        {
                            Console.Write(luminance[(int)Math.Floor(image.imageColors[w, h].ToWhiteBlack() * fraction)]);
                        } catch { }
                    }
                }
            } else
            {
                String print = "";
                for (int h = 0; h < image.height; h++)
                {
                    for (int w = 0; w < image.width; w++)
                    {
                        print += luminance[(int)Math.Floor(image.imageColors[w, h].ToWhiteBlack() * fraction)];
                    }
                    print += "\n";
                }
                try
                {
                    Console.Write(print.Substring(0, print.Length - 2));
                } catch { }
                
            }
            
        }
    }

    public class Image
    {
        
        public int width = 0;
        public int height = 0;
        public Color[,] imageColors = new Color[0,0];

        public Image(int width, int height, Color[,] imageColors = null)
        {
            this.width = width;
            this.height = height;
            if (imageColors == null) this.imageColors = new Color[width, height];
            else this.imageColors = imageColors;
        }

        public void SetMaxVariation(int variation)
        {
            foreach(Color c in imageColors)
            {
                c.variation = variation;
            }
        }

        public void SetMinVariation(int variation)
        {
            foreach (Color c in imageColors)
            {
                c.minVariation = variation;
            }
        }

        public void InvertImage()
        {
            foreach (Color c in imageColors)
            {
                c.Invert();
            }
        }

        //Presets
        public void SetMinColors()
        {
            
            SetMinVariation(0);
            SetMaxVariation(50);
        }

        //Presets
        public void SetDefault()
        {
            SetMaxVariation(new Color().variation);
            SetMinVariation(new Color().minVariation);
        }
    }

    public class Color
    {
        public int r { get; set; } = 0;
        public int g { get; set; } = 0;
        public int b { get; set; } = 0;
        public int variation = 75;
        public int minVariation = 20;
        //private double littleVariation = 0.25;
        public Color(System.Drawing.Color color)
        {
            this.r = (int)color.R;
            this.g = (int)color.G;
            this.b = (int)color.B;
        }

        public Color Invert()
        {
            r = 255 - r;
            g = 255 - g;
            b = 255 - b;
            return this;
        }

        public System.Drawing.Color ToColorAdjusted()
        {
            //Normalise color to max values
            int[] colors = new int[] { r, g, b };
            Array.Sort(colors);
            double rr = 1;
            double gr = 1;
            double br = 1;
            if (colors[0] != 0)
            {
                rr = (double)r / colors[2];
                gr = (double)g / colors[2];
                br = (double)b / colors[2];
            }
            System.Drawing.Color c = System.Drawing.Color.FromArgb((int)Math.Floor(rr * 255), (int)Math.Floor(gr * 255), (int)Math.Floor(br * 255));
            //Console.WriteLine(c);
            return c;
        }

        public System.Drawing.Color ToColor()
        {
            //convert to System.Drawing.Color
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public static Color FromConsoleColor(ConsoleColor c)
        {
            //ConsoleColor to Color
            switch(c)
            {
                case ConsoleColor.Black:
                    return new Color(0, 0, 0);
                case ConsoleColor.DarkBlue:
                    return new Color(0, 0, 128);
                case ConsoleColor.DarkGreen:
                    return new Color(0, 128, 0);
                case ConsoleColor.DarkCyan:
                    return new Color(0, 128, 128);
                case ConsoleColor.DarkRed:
                    return new Color(128, 0, 0);
                case ConsoleColor.DarkMagenta:
                    return new Color(128, 0, 128);
                case ConsoleColor.DarkYellow:
                    return new Color(128, 128, 0);
                case ConsoleColor.Gray:
                    return new Color(192, 192, 192);
                case ConsoleColor.DarkGray:
                    return new Color(128, 128, 128);
                case ConsoleColor.Blue:
                    return new Color(0, 0, 255);
                case ConsoleColor.Green:
                    return new Color(0, 255, 0);
                case ConsoleColor.Cyan:
                    return new Color(0, 255, 255);
                case ConsoleColor.Red:
                    return new Color(255, 0, 0);
                case ConsoleColor.Magenta:
                    return new Color(255, 0, 255);
                case ConsoleColor.Yellow:
                    return new Color(255, 255, 0);
                case ConsoleColor.White:
                    return new Color(255, 255, 255);
            }
            return new Color(0, 0, 0);
        }

        public Color(int r = 0, int g = 0, int b = 0)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public ConsoleColor GetColor()
        {
            //int[] colors = new int[] { r, b, g };
            //Array.Sort(colors);
            //float rr = 1;
            //float gr = 1;
            //float br = 1;
            //if(colors[0] != 0)
            //{
            //    rr = (float)r / colors[2];
            //    gr = (float)g / colors[2];
            //    br = (float)b / colors[2];
            //}
            //Console.WriteLine(rr + ", " + gr + ", " + br);

            //Determite color
            //variation defines the similarity a color has
            //minVariation the trigger for grayscale
            if (IsBiggest(r, g, b))
            {
                if (Math.Abs(Math.Abs(r - b) - Math.Abs(r - g)) < minVariation)
                {
                    if (r < 50) return ConsoleColor.Black;
                    if (r < 125) return ConsoleColor.DarkGray;
                    if (r < 175) return ConsoleColor.Gray;
                    return ConsoleColor.White;
                }
                if (Math.Abs(r - g) < variation) return ConsoleColor.Yellow;
                if (Math.Abs(r - b) < variation) return ConsoleColor.DarkMagenta;
                if (r < 100) return ConsoleColor.DarkRed;
                return ConsoleColor.Red;
            } else if(IsBiggest(g, r, b))
            {
                if (Math.Abs(Math.Abs(g - b) - Math.Abs(g - r)) < minVariation)
                {
                    if (g < 50) return ConsoleColor.Black;
                    if (g < 125) return ConsoleColor.DarkGray;
                    if (g < 175) return ConsoleColor.Gray;
                    return ConsoleColor.White;
                }
                if (Math.Abs(g - r) < variation) return ConsoleColor.Yellow;
                if (Math.Abs(g - b) < variation) return ConsoleColor.Cyan;
                if (g < 100) return ConsoleColor.DarkGreen;
                return ConsoleColor.Green;
            } else if(IsBiggest(b, r, g))
            {
                if (Math.Abs(Math.Abs(b - r) - Math.Abs(b - g)) < minVariation)
                {
                    if (b < 50) return ConsoleColor.Black;
                    if (b < 125) return ConsoleColor.DarkGray;
                    if (b < 175) return ConsoleColor.Gray;
                    return ConsoleColor.White;
                }
                if (Math.Abs(b - r) < variation) return ConsoleColor.DarkMagenta;
                if (Math.Abs(b - g) < variation) return ConsoleColor.Cyan;
                if (b < 100) return ConsoleColor.DarkBlue;
                return ConsoleColor.Blue;
            } else if(b == r && b == g && g == r || Math.Abs(r - g) < minVariation && Math.Abs(b - g) < minVariation && Math.Abs(b - r) < minVariation)
            {
                if (r < 50) return ConsoleColor.Black;
                if (r < 125) return ConsoleColor.DarkGray;
                if (r < 175) return ConsoleColor.Gray;
                return ConsoleColor.White;
            }
            return ConsoleColor.Black;
        }

        private bool IsBiggest(float check, float other1, float other2)
        {
            return check > other1 && check > other2;
        }
        public int ToWhiteBlack()
        {
            return (r + g + b) / 3;
        }
    }
}
