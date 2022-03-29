using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static _2DSim._2DpMapSim;

namespace _2DSim
{
    public partial class TwoDimensionalSpace : Form
    {
        Dot Dot = new Dot();
        public TwoDimensionalSpace()
        {
            InitializeComponent();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.A || keyData == Keys.S || keyData == Keys.D || keyData == Keys.F || keyData == Keys.Q || keyData == Keys.E)
            {
                if (keyData == Keys.A)
                    Dot.Position = new PointF(Dot.Position.X - 1, Dot.Position.Y);
                if (keyData == Keys.D)
                    Dot.Position = new PointF(Dot.Position.X + 1, Dot.Position.Y);
                if (keyData == Keys.S)
                    Dot.Position = new PointF(Dot.Position.X, Dot.Position.Y + 1);
                if (keyData == Keys.W)
                    Dot.Position = new PointF(Dot.Position.X, Dot.Position.Y - 1);
                if (keyData == Keys.E)
                    Dot.Angle += 0.01;
                if (keyData == Keys.Q)
                    Dot.Angle -= 0.01;
                lastMouseMove = new Point((int)Dot.Position.X, (int)Dot.Position.Y);
            }
            needsToRefresh = true;
            return false;
        }
        private void TwoDimensionalSpace_Load(object sender, EventArgs e)
        {
            environment.gObstructions.Clear(Color.White);
            var doc = netDxf.DxfDocument.Load(@"C:\Users\techboy\Desktop\PositioningSystem\Sim\obstruction path.dxf");
            var userPoly = doc.Splines.ToList()[0].PolygonalVertexes(1000);
            var userPolyWidth = (float)(userPoly.Max(p => p.X) - userPoly.Min(p => p.X));
            var userPolyHeight = (float)(userPoly.Max(p => p.Y) - userPoly.Min(p => p.Y));
            var userPolyX = (float)userPoly.Min(p => p.X);
            var userPolyY = (float)userPoly.Min(p => p.Y);
            float userPolyScale = 10;
            var points = userPoly.Select(p => new PointF(
                ((float)p.X - userPolyX) * userPolyScale + 10,
                ((float)((userPolyHeight - p.Y) - -userPolyY)) * userPolyScale + 10
            )).ToArray();
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddLines(points);
            path.AddRectangle(new RectangleF(0, 0, environment.Width, environment.Height));
            //environment.gObstructions.DrawLines(Pens.Black, points);
            environment.gObstructions.FillPath(Brushes.DarkGray, path);
            environment.RemakeEnvironment();
            environment.Invalidate();
        }

        bool needsToRefresh = false;
        Point lastMouseMove;
        private void environment_MouseMove(object sender, MouseEventArgs e)
        {
            needsToRefresh = true;
            lastMouseMove = e.Location;
        }
        enum LineDirection
        {
            None,
            North,
            East,
            West,
            South,
            NorthEast,
            NorthWest,
            SouthEast,
            SouthWest,
        }
        Point secondLastMouseMove;
        private void timer1_Tick(object sender, EventArgs e2)
        {
            if (!needsToRefresh)
                return;
            needsToRefresh = false;
            Point e = lastMouseMove;
            Dot.Position = new PointF(e.X, e.Y);
            var readings = Dot.TakeSample(environment.Environment);
            environment.gDetected.Clear(Color.Transparent);
            environment.gVolatile.Clear(Color.Transparent);

            // draw sensor line of sight
            for (int i = 0; i < Dot.NumberOfSensors; i++)
            {
                double tho = Dot.Angle + i / (double)Dot.NumberOfSensors * (2 * Math.PI);
                var tri = new PointF[] {
                    new PointF(e.X, e.Y),
                    new PointF((float)(e.X + readings[i] * Math.Cos(tho)),
                    (float)(e.Y + readings[i] * Math.Sin(tho))),
                    new PointF((float)(e.X + readings[i] * Math.Cos(tho + Dot.Sweep)),
                    (float)(e.Y + readings[i] * Math.Sin(tho + Dot.Sweep)))
                    };
                environment.gVolatile.FillPolygon(
                    new SolidBrush(Color.FromArgb(100, Color.Yellow)),
                    tri);
                environment.gVolatile.DrawPolygon(
                    new Pen(Color.FromArgb(255, Color.Yellow)),
                    tri);
            }

            environment.gVolatile.DrawLine(Pens.Green, e.X, e.Y,
    (float)(e.X + 20 * Math.Cos(Dot.Angle)),
    (float)(e.Y + 20 * Math.Sin(Dot.Angle))
    );
            environment.FeedSample(Dot, readings);
            environment.CompileScores();
            for (int y = 0; y < environment.ProbabilityMap.GetLength(1); y++)
                for (int x = 0; x < environment.ProbabilityMap.GetLength(0); x++)
                {
                    if (environment.ProbabilityMap[x, y] == null)
                        continue;
                    var cf = environment.ProbabilityMap[x, y].GroupScore;
                    if (cf > 1)
                        cf = 1;
                    if (cf < 0)
                        cf = 0;
                    int alpha = (int)(cf * 255);
                    Color c = Color.FromArgb(alpha, Color.Red);
                    environment.gDetected.FillRectangle(new SolidBrush(c),
                        x * ProbabilitySample.DW, y * ProbabilitySample.DW,
                        ProbabilitySample.DW, ProbabilitySample.DW);
                }
            // construct the fitting curve now.
            // find out the most certain cell
            double bestCellScore = double.MinValue;

            ProbabilitySample bestCell = null;
            for (int y = 0; y < environment.ProbabilityMap.GetLength(1); y++)
                for (int x = 0; x < environment.ProbabilityMap.GetLength(0); x++)
                {
                    if (environment.ProbabilityMap[x, y].AbsoluteScore > bestCellScore)
                    {
                        bestCellScore = environment.ProbabilityMap[x, y].AbsoluteScore;
                        bestCell = environment.ProbabilityMap[x, y];
                    }
                }
            List<ProbabilitySample> Chain = new List<ProbabilitySample>();
            List<ProbabilitySample> possibleDoubleWall = new List<ProbabilitySample>();
            Chain.Add(bestCell);
            // find the nearest acceptable to the last.
            double DW = ProbabilitySample.DW;
            var finder = Chain.Last();

            var map = environment.ProbabilityMap;
            bool closePath = false;
            for (int i = 0; i < 500; i++)
            {
                // lets find the nearest with at least some cred

                int fxi = (int)(Math.Round(finder.X / ProbabilitySample.DW));
                int fyi = (int)(Math.Round(finder.Y / ProbabilitySample.DW));
                double leastError = double.MaxValue;
                int nearestXi = -1;
                int nearestYi = -1;
                bool found = false;
                for (int dd = 1; dd <= 5 && !found; dd++)
                {
                    // H lines
                    for (int xi = fxi - dd; xi <= fxi + dd && !closePath; xi++)
                    {
                        foreach (var yi in new int[] { fyi - dd, fyi + dd })
                        {
                            if (xi < 0 || yi < 0 || xi >= map.GetLength(0) || yi >= map.GetLength(1))
                                continue;
                            if (i > 100)
                            {
                                if (map[xi, yi] == Chain[0])
                                {
                                    closePath = true;
                                    break;
                                }
                            }
                            if (Chain.Contains(map[xi, yi]))
                                continue;
                            if (possibleDoubleWall.Contains(map[xi, yi]))
                                continue;
                            if (map[xi, yi].AbsoluteScore <= .01)
                                continue;
                            //if (Chain.Count > 1)
                            //{
                            //    var p1 = Chain[Chain.Count - 2];
                            //    var p2 = map[xi, yi];
                            //    var p3 = Chain.Last();
                            //    var p12 = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
                            //    var p23 = Math.Sqrt(Math.Pow(p2.X - p3.X, 2) + Math.Pow(p2.Y - p3.Y, 2));
                            //    var p13 = Math.Sqrt(Math.Pow(p1.X - p3.X, 2) + Math.Pow(p1.Y - p3.Y, 2));
                            //    var th = Math.Acos((p23 * p23 + p13 * p13 - p12 * p12) / (2 * p23 * p13) ) * 180 / Math.PI;
                            //    if (th < 10)
                            //        continue;
                            //}
                            if (Math.Abs(map[xi, yi].AbsoluteScore - finder.AbsoluteScore) < leastError)
                            {
                                leastError = Math.Abs(map[xi, yi].AbsoluteScore - finder.AbsoluteScore);
                                nearestXi = xi;
                                nearestYi = yi;
                                found = true;
                            }
                        }
                    }
                    // V lines
                    for (int yi = fyi - dd + 1; yi <= fyi + dd - 1 && !closePath; yi++)
                    {
                        foreach (var xi in new int[] { fxi - dd, fxi + dd })
                        {
                            if (xi < 0 || yi < 0 || xi >= map.GetLength(0) || yi >= map.GetLength(1))
                                continue; 
                            if (i > 100)
                            {
                                if (map[xi, yi] == Chain[0])
                                {
                                    closePath = true;
                                    break;
                                }
                            }
                            if (Chain.Contains(map[xi, yi]))
                                continue;
                            if (possibleDoubleWall.Contains(map[xi, yi]))
                                continue;
                            if (map[xi, yi].AbsoluteScore <= .01)
                                continue;
                            //if (Chain.Count > 1)
                            //{
                            //    var p1 = Chain[Chain.Count - 2];
                            //    var p2 = map[xi, yi];
                            //    var p3 = Chain.Last();
                            //    var p12 = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
                            //    var p23 = Math.Sqrt(Math.Pow(p2.X - p3.X, 2) + Math.Pow(p2.Y - p3.Y, 2));
                            //    var p13 = Math.Sqrt(Math.Pow(p1.X - p3.X, 2) + Math.Pow(p1.Y - p3.Y, 2));
                            //    var th = Math.Acos((p23 * p23 + p13 * p13 - p12 * p12) / (2 * p23 * p13)) * 180 / Math.PI;
                            //    if (th < 10)
                            //        continue;
                            //} 
                            if (Math.Abs(map[xi, yi].AbsoluteScore - finder.AbsoluteScore) < leastError)
                            {
                                leastError = Math.Abs(map[xi, yi].AbsoluteScore - finder.AbsoluteScore);
                                nearestXi = xi;
                                nearestYi = yi;
                                found = true;
                            }
                        }
                    }

                    
                    if (found || closePath)
                        break;
                }
                if (closePath)
                    break;
                if (!found)
                    ;
                else
                {
                    Chain.Add(map[nearestXi, nearestYi]);
                    if (Chain.Count > 5)
                    {
                        var xdi = (int)(Math.Round(Chain[Chain.Count - 3].X / ProbabilitySample.DW));
                        var ydi = (int)(Math.Round(Chain[Chain.Count - 3].Y / ProbabilitySample.DW));
                        var neighbours = new Point[]
                            {
                                        new Point(xdi + 0, ydi +1),
                                        new Point(xdi + 1, ydi +1),
                                        new Point(xdi + 2, ydi +1),
                                        //new Point(xdi + 0, ydi +0),
                                        new Point(xdi + 1, ydi +0),
                                        new Point(xdi + 2, ydi +0),
                                        new Point(xdi + 0, ydi -1),
                                        new Point(xdi + 1, ydi -1),
                                        new Point(xdi + 2, ydi -1),
                            };
                        foreach (var pn in neighbours)
                        {
                            if (pn.X < 0 || pn.Y < 0 || pn.X >= map.GetLength(0) || pn.Y >= map.GetLength(1))
                                continue;
                            if (!possibleDoubleWall.Contains(map[pn.X, pn.Y]))
                                possibleDoubleWall.Add(map[pn.X, pn.Y]);
                        }

                    }
                    finder = map[nearestXi, nearestYi];
                }
                // we have found the nearest in the ring. 
            }
            var boundaryEstimate = Chain.Select(cell => new PointF((float)(cell.X + DW / 2), (float)(cell.Y + DW / 2))).ToArray();
            if (Chain.Count > 1)
            {
                environment.gDetected.DrawPolygon(Pens.Black, boundaryEstimate);
                for (int i =0; i <boundaryEstimate.Length; i++)
                {
                    //environment.gDetected.DrawString(i.ToString(), new Font("ARIAL", 10), Brushes.Blue, boundaryEstimate[i].X, boundaryEstimate[i].Y);
                }
            }
            secondLastMouseMove = lastMouseMove;
            environment.Invalidate();
        }

        private void beginB_Click(object sender, EventArgs e)
        {
            environment.Reset();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Dot.NumberOfSensors = (int)numericUpDown1.Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            Dot.Sweep = (double)numericUpDown2.Value * Math.PI / 180;
        }
    }
    public class Dot
    {
        public double Range = 1000;
        public PointF Position { get; set; }
        public double Angle { get; set; } = 0;
        public int NumberOfSensors { get; set; } = 10;
        public double Sweep { get; set; }
        public Dot()
        {
            Sweep = 2 * Math.PI / (double)NumberOfSensors / 4;
        }

        public double[] TakeSample(bool[,] environment)
        {
            var readings = new double[NumberOfSensors];
            for (int thi = 0; thi < NumberOfSensors; thi++)
            {
                double r = 0;
                double tho = Angle + (thi / (double)NumberOfSensors) * (2 * Math.PI);
                bool found = false;
                for (; r < Range && !found; r += 1)
                    for (double th = 0; th < Sweep && !found; th += 0.01)
                    {
                        int xi = (int)Math.Round(r * Math.Cos(th + tho) + Position.X);
                        int yi = (int)Math.Round(r * Math.Sin(th + tho) + Position.Y);
                        if (xi < 0 || xi >= environment.GetLength(0) ||
                            yi < 0 || yi >= environment.GetLength(1))
                            break;
                        if (environment[xi, yi])
                        {
                            found = true;
                            break;
                        }
                    }
                readings[thi] = r;
            }
            return readings;
        }
        public double[] TakeFurthestSample(bool[,] environment)
        {
            var readings = new double[NumberOfSensors];
            for (int thi = 0; thi < NumberOfSensors; thi++)
            {
                double r = Range;
                double tho = Angle + (thi / (double)NumberOfSensors) * (2 * Math.PI);
                bool found = false;
                for (; r > 0 && !found; r -= 1)
                    for (double th = 0; th < 2 * Math.PI / (double)NumberOfSensors && !found; th += 0.01)
                    {
                        int xi = (int)Math.Round(r * Math.Cos(th + tho) + Position.X);
                        int yi = (int)Math.Round(r * Math.Sin(th + tho) + Position.Y);
                        if (xi < 0 || xi >= environment.GetLength(0) ||
                            yi < 0 || yi >= environment.GetLength(1))
                            break;
                        if (environment[xi, yi])
                        {
                            found = true;
                            break;
                        }
                    }
                readings[thi] = r;
            }
            return readings;
        }
    }
}
