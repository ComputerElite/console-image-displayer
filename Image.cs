using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDisplayer
{
    public class Image
    {

        public int width = 0;
        public int height = 0;
        public Color[,] imageColors = new Color[0, 0];

        public Image(int width, int height, Color[,] imageColors = null)
        {
            this.width = width;
            this.height = height;
            if (imageColors == null) this.imageColors = new Color[width, height];
            else this.imageColors = imageColors;
        }

        public void InvertImage()
        {
            foreach (Color c in imageColors)
            {
                c.Invert();
            }
        }
    }

    public class Color
    {
        public int r { get; set; } = 0;
        public int g { get; set; } = 0;
        public int b { get; set; } = 0;
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

        // Heh this is old normalize code. idk where I used to use it
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

        // Creeate a list of available ConsoleColors with corresponding rgb values
        public static List<ColorHolder> colors { get; set; } = new List<ColorHolder>
        {
            //new ColorHolder(ConsoleColor.Black, new Color(0, 0, 0)),
            //new ColorHolder(ConsoleColor.DarkBlue, new Color(0, 0, 128)),
            //new ColorHolder(ConsoleColor.DarkGreen, new Color(0, 128, 0)),
            //new ColorHolder(ConsoleColor.DarkCyan, new Color(0, 128, 128)),
            //new ColorHolder(ConsoleColor.DarkRed, new Color(128, 0, 0)),
            //new ColorHolder(ConsoleColor.DarkMagenta, new Color(128, 0, 128)),
            //new ColorHolder(ConsoleColor.DarkYellow, new Color(128, 128, 0)),
            //new ColorHolder(ConsoleColor.Gray, new Color(192, 192, 192)),
            //new ColorHolder(ConsoleColor.DarkGray, new Color(128, 128, 128)),
            new ColorHolder(ConsoleColor.Blue, new Color(0, 0, 255)),
            new ColorHolder(ConsoleColor.Green, new Color(0, 255, 0)),
            new ColorHolder(ConsoleColor.Cyan, new Color(0, 255, 255)),
            new ColorHolder(ConsoleColor.Red, new Color(255, 0, 0)),
            new ColorHolder(ConsoleColor.Magenta, new Color(255, 0, 255)),
            new ColorHolder(ConsoleColor.Yellow, new Color(255, 255, 0)),
            new ColorHolder(ConsoleColor.White, new Color(255, 255, 255))
        //new ColorHolder(ConsoleColor.White, new Color(255, 255, 255))
    };

        public static ColorHolder white = new ColorHolder(ConsoleColor.White, new Color(255, 255, 255));

        public static Color FromConsoleColor(ColorHolder c)
        {
            return c.color;
        }

        public Color(int r = 0, int g = 0, int b = 0)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
        int min = 15;
        public ColorHolder GetColor()
        {

            
            // We want color not brightness. So make the biggest one of the values 255 and scale the rest proportionally
            double multiply = 1.0;
            if (255 / (double)r < multiply) multiply = 255 / (double)r;
            if (255 / (double)g < multiply) multiply = 255 / (double)g;
            if (255 / (double)b < multiply) multiply = 255 / (double)b;
            Color normalized = new Color((int)(r * multiply), (int)(g * multiply), (int)(b * multiply));

            // Create the nearest color holder
            ColorHolder nearest = new ColorHolder();
            int nearestValue = normalized.Subtract(nearest.color);
            /*
            if (Math.Abs(r - g) < min && Math.Abs(g - b) < min && Math.Abs(b - r) < min)
            {
                return white;
            }
            */

            for (int i = 0; i < colors.Count; i++)
            {
                // Subtract the colors to get the difference between them
                int subtracted = normalized.Subtract(colors[i].color);
                if (subtracted < nearestValue)
                {
                    // a nearer color has been found. Save it-
                    nearestValue = subtracted;
                    nearest = colors[i];
                }
            }
            return nearest;
        }
        public int ToWhiteBlack()
        {
            return (int)(0.299 * r + 0.587 * g + 0.114 * b);
        }

        public int Subtract(Color b)
        {
            // Return the total distance of red, green and blue
            return Math.Abs(r - b.r) + Math.Abs(g - b.g) + Math.Abs(this.b - b.b);
        }
    }

    public class ColorHolder
    {
        public Color color { get; set; } = new Color(255, 255, 255);
        public ConsoleColor consoleColor { get; set; } = ConsoleColor.White;

        public ColorHolder(ConsoleColor cc, Color c)
        {
            this.color = c;
            this.consoleColor = cc;
        }

        public ColorHolder() { }
    }
}
