using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDisplayer
{
    public class ImageDisplayer
    {
        //public static readonly string[] luminance = new string[] { " ", ";", "█" };
        //public static readonly string[] luminance = new string[] { " ", ";", "█", "█", "█" };
        public Config config { get; set; } = Config.Load();
        //public static readonly string[] luminance = new string[] { " ", "'", ".", ",", "-", "~", ":", ";", "=", "+", "!", "*", "#", "$", "@", "█" };
        //character width is half of the height

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
        public Image ImageToImageClass(String imagePath, int width, int height, bool cropimage = false, bool color = true, bool Display = true)
        {
            //Load Bitmap from path.
            Bitmap imageSource = new Bitmap(imagePath);
            return ImageToImageClass(imageSource, width, height, cropimage, color,Display);
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
        public Image ImageToImageClass(Bitmap imageSource, int width, int height, bool cropimage = false, bool color = true, bool Display = true)
        {
            int imageWidth = imageSource.Width;
            int imageHeight = imageSource.Height;
            //Get amount of pixels to check in corelation to the deserved width
            int adjustedHeight = (int)Math.Floor(imageHeight / (imageWidth / (double)width) * config.heightToWidthRatio);
            //Distance between pixel checks (width)
            float adjustedWidthCalc = (float)imageWidth / (float)width;
            //Distance between pixel checks (height)
            float adjustedHeightCalc = (float)imageHeight / (float)(imageHeight / (imageWidth / (float)width)) / config.heightToWidthRatio;
            //brightness fraction
            double fraction = 1 / ((float)255 / config.luminance.Length + 1);
            width--;
            if (cropimage && adjustedHeight > height) adjustedHeight = height - 1;
            //Create 2d color array with deserved size

            Image i = new Image(width, adjustedHeight, new Color[width, adjustedHeight]);
            string print = "";
            for (int h = 0; h < adjustedHeight; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    //Convert pixel to color
                    Color c = new Color(imageSource.GetPixel((int)(adjustedWidthCalc * w), (int)(adjustedHeightCalc * h)));
                    if (Display)
                    {
                        //sw.Stop();
                        //get luminance of color
                        if (!color) print += config.luminance[(int)(c.ToWhiteBlack() * fraction)];
                        else
                        {
                            //Set foreground color for writing
                            ConsoleColor cc = c.GetColor().consoleColor;
                            if (Console.ForegroundColor != cc) Console.ForegroundColor = cc;
                            Console.Write(config.luminance[(int)(c.ToWhiteBlack() * fraction)]);
                        }
                        //sw.Start();
                    }
                    //Add Color to 2d color array
                    i.imageColors[w, h] = c;
                }
                if (Display)
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
            return i;
        }

        public string ImageToString(Bitmap imageSource, int width)
        {
            int imageWidth = imageSource.Width;
            int imageHeight = imageSource.Height;
            int adjustedHeight = (int)Math.Floor(imageHeight / (imageWidth / (double)width) * config.heightToWidthRatio);
            float adjustedWidthCalc = (float)imageWidth / (float)width;
            float adjustedHeightCalc = (float)imageHeight / (float)(imageHeight / (imageWidth / (float)width)) / config.heightToWidthRatio;
            double fraction = 1 / ((double)255 / config.luminance.Length + 1);
            width--;
            String print = "";

            for (int h = 0; h < adjustedHeight; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    //Console.WriteLine(w + ", " + h);
                    Color c = new Color(imageSource.GetPixel((int)(adjustedWidthCalc * w), (int)(adjustedHeightCalc * h)));
                    print += config.luminance[(int)Math.Floor(c.ToWhiteBlack() * fraction)];
                }
                print += "\n";
            }
            return print;
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
            Bitmap output = new Bitmap(image.width * 8, image.height * 16);
            Graphics g = Graphics.FromImage(output);
            //Set background console black
            g.FillRectangle(Brushes.Black, 0, 0, output.Width, output.Height);
            Font font = new Font("Consolas", 10);
            double fraction = 1 / ((double)255 / config.luminance.Length + 1);

            if (color)
            {
                ColorHolder last = Color.white;
                Brush b = new SolidBrush(Color.FromConsoleColor(last).ToColor());
                for (int h = 0; h < image.height; h++)
                {
                    for (int w = 0; w < image.width; w++)
                    {
                        //Console.ForegroundColor = image.imageColors[w, h].GetColor();
                        //Set color of brush
                        ColorHolder cc = image.imageColors[w, h].GetColor();
                        if (cc.consoleColor != last.consoleColor)
                        {
                            b = new SolidBrush(Color.FromConsoleColor(cc).ToColor());
                        }
                        //Write character to Bitmap at right position
                        last = cc;
                        g.DrawString(config.luminance[(int)Math.Floor(image.imageColors[w, h].ToWhiteBlack() * fraction)], font, b, w * 8, h * 16);
                    }
                }
                b.Dispose();
                g.Dispose();
                //Console.ReadLine();
            }
            else
            {
                String print = "";
                Brush b = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255));
                for (int h = 0; h < image.height; h++)
                {
                    for (int w = 0; w < image.width; w++)
                    {
                        //Console.ForegroundColor = image.imageColors[w, h].GetColor();
                        //Set color of brush

                        //Write character to Bitmap at right position
                        print += config.luminance[(int)Math.Floor(image.imageColors[w, h].ToWhiteBlack() * fraction)];
                    }
                    print += System.Environment.NewLine;
                }
                g.DrawString(print, font, b, 0, 0);
            }

            //Save bitmap
            output.Save(destination);
            font.Dispose();
            output.Dispose();
        }

        public void DisplayInConsole(Image image, bool withColor = true)
        {
            //Displays image in console. Obsolete but can still be used if needed.
            double fraction = 1 / ((double)255 / config.luminance.Length + 1);
            if (withColor)
            {
                for (int h = 0; h < image.height; h++)
                {
                    Console.WriteLine();
                    for (int w = 0; w < image.width; w++)
                    {
                        Console.ForegroundColor = image.imageColors[w, h].GetColor().consoleColor;
                        try
                        {
                            Console.Write(config.luminance[(int)Math.Floor(image.imageColors[w, h].ToWhiteBlack() * fraction)]);
                        }
                        catch { }
                    }
                }
            }
            else
            {
                String print = "";
                for (int h = 0; h < image.height; h++)
                {
                    for (int w = 0; w < image.width; w++)
                    {
                        print += config.luminance[(int)Math.Floor(image.imageColors[w, h].ToWhiteBlack() * fraction)];
                    }
                    print += "\n";
                }
                try
                {
                    Console.Write(print.Substring(0, print.Length - 2));
                }
                catch { }

            }

        }
    }
}
