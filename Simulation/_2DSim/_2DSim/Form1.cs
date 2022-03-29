using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _2DSim
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            pMap.begin();
        }

        List<Point> UserObjectPoints = new List<Point>();

        List<netDxf.Vector3> userPoly = null;
        float userPolyHeight = 0;
        float userPolyScale = 1;
        object userPolyAnchor = null;
        object userPolyMouseDown = null;

        void DrawUserPath(Graphics g, float xOffsetBefore, float yOffsetBefore, float xOffset, float yOffset)
        {
            PointF[] points = null;
            points = userPoly.Select(p => new PointF(
                ((float)p.X - xOffsetBefore) * userPolyScale + pMap.Width / 2 + xOffset,
                ((float)(userPolyHeight - p.Y - yOffsetBefore)) * userPolyScale + pMap.Height / 2 + yOffset
            )).ToArray();
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddLines(points);
            path.AddRectangle(new RectangleF(0, 0, pMap.Width, pMap.Height));

            g.FillPath(new SolidBrush(Color.FromArgb(200, Color.DarkGray)), path);
            //g.DrawLines(new Pen(Color.Black, 3), points);
        }
        private void dbpanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (userPoly != null)
            {
                if (userPolyAnchor == null)
                {
                    userPolyAnchor = null;
                    userPolyMouseDown = null;
                }
                else  // scale 
                {
                    userPolyMouseDown = e.Location;
                }
                return;
            }
            UserObjectPoints.Add(e.Location);
            dbpanel_MouseMove(sender, e);
        }

        double[] maxPossibileBins;
        double[] minPossibileBins;
        double rMin = 30;
        double rMax = 0;
        int sensors = 10;
        int scanRes = 40;
        private void dbpanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (userPoly != null)
            {
                if (userPolyAnchor == null)
                {
                    userPolyAnchor = new PointF(e.X - pMap.Width / 2, e.Y - pMap.Height / 2);
                    userPolyMouseDown = null;
                    return;
                }
                if (userPolyMouseDown != null)
                {
                    if ((Point)userPolyMouseDown == e.Location)
                    {
                        pMap.gVolatile.Clear(Color.Transparent); 
                        DrawUserPath(pMap.gObstructions, ((PointF)userPolyAnchor).X, ((PointF)userPolyAnchor).Y, e.X - pMap.Width / 2, e.Y - pMap.Height / 2);
                        pMap.Invalidate();
                        userPolyAnchor = null;
                        userPolyScale = 1;
                        userPoly = null;
                        dbpanel_MouseUp(sender, e);
                    }
                    userPolyMouseDown = null;
                }
                return;
            }
            if (UserObjectPoints.Count > 0)
                pMap.gObstructions.FillPolygon(
                    new SolidBrush(Color.FromArgb(255, Color.DarkGray)),
                    UserObjectPoints.ToArray());


            double[] probabilityBins = new double[sensors * scanRes];
            if (maxPossibileBins == null)
            {
                maxPossibileBins = new double[sensors * scanRes];
                minPossibileBins = new double[sensors * scanRes];
            }
            double[] minPBins = new double[sensors * scanRes];
            double[] maxPBins = new double[sensors * scanRes];

            double fov = 2 * Math.PI / sensors;
            double dth = fov / scanRes;

            for (int thi = 0; thi < scanRes;thi++)
            {
                double thOffset = (double)thi / scanRes * fov ;
                var probabilities = Enumerable.Range(0, sensors).Select(i => ReadSensor(i, thOffset)).ToList();
                for (int si = 0; si < sensors; si++)
                {
                    int binIndex = si * scanRes + thi;
                    probabilityBins[binIndex] += probabilities[si];
                }
            }

            List<PointF> boundary = new List<PointF>();
            List<PointF> minBoundary = new List<PointF>();
            List<PointF> maxBoundary = new List<PointF>();
            for (int i = 0; i < scanRes * sensors; i++)
            {
                double th = i * (2 * Math.PI / (sensors * scanRes)) + fov / 2;
                int minI = i - (scanRes / 2);
                int maxI = i + (scanRes / 2);
                double min = double.MaxValue;
                double max = double.MinValue;
                for (int unsafeMinMaxI = minI; unsafeMinMaxI <= maxI; unsafeMinMaxI++)
                {
                    var minMaxI = unsafeMinMaxI;
                    if (minMaxI < 0)
                        minMaxI += probabilityBins.Length;
                    if (minMaxI >= probabilityBins.Length)
                        minMaxI -= probabilityBins.Length;
                    if (probabilityBins[minMaxI] < min)
                        min = probabilityBins[minMaxI];
                    if (probabilityBins[minMaxI] > max)
                        max = probabilityBins[minMaxI];
                }
                maxPossibileBins[i] = max;
                minPossibileBins[i] = min;
                var x = pMap.Width / 2 + probabilityBins[i] * Math.Cos(th);
                var y = pMap.Width / 2 + probabilityBins[i] * Math.Sin(th);
                var minx = pMap.Width / 2 + min * Math.Cos(th);
                var miny = pMap.Width / 2 + min * Math.Sin(th);
                var maxx = pMap.Width / 2 + max * Math.Cos(th);
                var maxy = pMap.Width / 2 + max * Math.Sin(th);
                boundary.Add(new PointF((float)x, (float)y));
                minBoundary.Add(new PointF((float)minx, (float)miny));
                maxBoundary.Add(new PointF((float)maxx, (float)maxy));
            }

            pMap.gDetected.Clear(Color.Transparent);
            pMap.gDetected.DrawPolygon(new Pen(Color.Black, 2), boundary.ToArray());
            pMap.gDetected.DrawPolygon(new Pen(Color.Red, 2), minBoundary.ToArray());
            pMap.gDetected.DrawPolygon(new Pen(Color.Green, 2), maxBoundary.ToArray());



            //var distances = Enumerable.Range(0, sensors).Select(i => ReadSensor(i, 0)).ToList();
            //List<PointF> boundary = new List<PointF>();
            //int dispResolution = 10;
            //for (int i = 0; i < sensors; i++)
            //{
            //    for (double th0 = 0; th0 <= (2 * Math.PI) / sensors; th0 += (2 * Math.PI) / sensors / dispResolution)
            //    {
            //        double th = th0 + i * (2 * Math.PI / sensors);
            //        var x = pMap.Width / 2 + distances[i] * Math.Cos(th);
            //        var y = pMap.Width / 2 + distances[i] * Math.Sin(th);
            //        boundary.Add(new PointF((float)x, (float)y));
            //    }
            //}

            UserObjectPoints.Clear();
            pMap.gVolatile.Clear(Color.Transparent);
            pMap.Invalidate();
        }

        private void dbpanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (userPoly != null)
            {
                pMap.gVolatile.Clear(Color.Transparent);
                if (userPolyAnchor == null)
                {
                    DrawUserPath(pMap.gVolatile, 0, 0, 0, 0);
                }
                else 
                {
                    if (userPolyMouseDown != null)
                    {
                        float dx = e.X - ((Point)userPolyMouseDown).X;
                        userPolyScale += dx / 100;
                        if (userPolyScale <= 0.0001F)
                            userPolyScale = 0.0001F;
                    }
                    DrawUserPath(pMap.gVolatile, ((PointF)userPolyAnchor).X, ((PointF)userPolyAnchor).Y, e.X - pMap.Width/2, e.Y - pMap.Height/ 2);
                }
                pMap.Invalidate();
                return;
            }

            if (UserObjectPoints.Count > 0)
            {
                UserObjectPoints.Add(e.Location);
                pMap.gVolatile.Clear(Color.Transparent);
                pMap.gVolatile.FillPolygon(
                    new SolidBrush(Color.FromArgb(255, Color.DarkGray)),
                    UserObjectPoints.ToArray());
                pMap.Invalidate();
            }
            else if (minPossibileBins != null)
            {
                double thisTh = Math.Atan2(e.Y - pMap.Width / 2, e.X - pMap.Width / 2);
                if (thisTh < 0)
                    thisTh += Math.PI * 2;
                double fov = 2 * Math.PI / sensors;
                int thisBin = (int)((thisTh - fov / 2) / (2 * Math.PI) * (scanRes * sensors - 1)) ;
                if (thisBin < 0)
                    thisBin += maxPossibileBins.Length;
                pMap.gVolatile.Clear(Color.Transparent);
                pMap.gVolatile.DrawLine(Pens.Black,
                    pMap.Width / 2, pMap.Width / 2,
                    (float)(pMap.Width / 2 + maxPossibileBins[thisBin] * Math.Cos(thisTh)),
                    (float)(pMap.Width / 2 + maxPossibileBins[thisBin] * Math.Sin(thisTh))
                    );
                double conf = (1 - (maxPossibileBins[thisBin] - minPossibileBins[thisBin]) / (rMax - rMin)) * 100;
                pMap.gVolatile.DrawString(conf.ToString("F0") + "%", pMap.Font, Brushes.Black, 4, 4);
                pMap.Invalidate();
            }
        }
        double ReadSensor(int index, double rotation)
        {
            double sensors = 10;
            double samples = 50;
            double thC = 2 * Math.PI / sensors * index + rotation;
            double fov = 2 * Math.PI / sensors;
            double thMin = thC;
            double thMax = thC + fov;
            rMax = pMap.Width / 2.0D;

            double sum = 0;
            int N = 0;
            for (double th = thMin; th <= thMax; th += fov / samples)
            {
                double thisDistance = rMax;
                for (double r = rMin; r < rMax; r += 1)
                {
                    var xc = (int)pMap.Width / 2;
                    var yc = (int)pMap.Width / 2;
                    var x = (int)Math.Round(r * Math.Cos(th));
                    var y = (int)Math.Round(r * Math.Sin(th));
                    if (x + xc >= pMap.Width || x + xc < 0)
                        continue;
                    if (y + yc >= pMap.Width || y + yc < 0)
                        continue;
                    var color = pMap.Obstructions.GetPixel(xc + x, yc + y);
                    if (color.A > 0)
                    {
                        thisDistance = Math.Sqrt(x * x + y * y);
                        break;
                    }
                }
                sum += thisDistance;
                N++;
            }
            sum = sum / N;
            return sum;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            var doc = netDxf.DxfDocument.Load(@"C:\Users\techboy\Desktop\PositioningSystem\Sim\obstruction path.dxf");
            userPoly = doc.Splines.ToList()[0].PolygonalVertexes(1000);
            userPolyHeight = (float)(userPoly.Max(p => p.Y) - userPoly.Min(p => p.Y));
            userPolyScale = 1;
            userPolyAnchor = null;
        }
    }
}
