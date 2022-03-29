using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _2DSim
{
    public class pMapSim:Panel
    {
        Image Illustration;
        public Bitmap Obstructions { get; set; }
        public Bitmap Volatile { get; set; }
        public Bitmap Detected { get; set; }
        public Graphics gDetected { get; set; }
        public Graphics gObstructions { get; set; }
        public Graphics gVolatile { get; set; }
        public pMapSim()
        {
            DoubleBuffered = true;
        }
        public void begin()
        {
            Width = Height;
            Illustration = new Bitmap(Width, Height);
            Graphics.FromImage(Illustration).DrawImage(Image.FromFile("back.png"), 0, 0, Width, Height);
            Obstructions = new Bitmap(Width, Height);
            Volatile = new Bitmap(Width, Height);
            Detected = new Bitmap(Width, Height);
            gVolatile = Graphics.FromImage(Volatile);
            gObstructions = Graphics.FromImage(Obstructions);
            gDetected = Graphics.FromImage(Detected);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Illustration == null || Obstructions == null || Volatile == null || Detected == null)
            {
                base.OnPaintBackground(e);
                return;
            }
            e.Graphics.DrawImage(Illustration, 0, 0);
            e.Graphics.DrawImage(Obstructions, 0, 0);
            e.Graphics.DrawImage(Volatile, 0, 0);
            e.Graphics.DrawImage(Detected, 0, 0);
        }
    }
}
