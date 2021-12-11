using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageDisplayer
{
    public class Config
    {
        public string[] luminance { get; set; } = new string[] { " ", ";", ";", "█", "█" };
        public float heightToWidthRatio { get; set; } = 0.5f;
        public int startFrame { get; set; } = 1;
        public int videoWidth { get; set; } = 640;
        public int videoHeight { get; set; } = 360;
        public int videoThreads { get; set; } = 12;
        public bool playVideo { get; set; } = false;

        public static Config Load()
        {
            if (!File.Exists("config.json")) File.WriteAllText("config.json", JsonSerializer.Serialize(new Config()));
            return JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
        }

        public void Save()
        {
            File.WriteAllText("config.json", JsonSerializer.Serialize(this));
        }
    }
}
