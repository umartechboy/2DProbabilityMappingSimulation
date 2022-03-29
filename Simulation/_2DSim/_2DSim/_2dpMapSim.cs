using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _2DSim
{
    public class _2DpMapSim:Panel
    {
        Image Illustration;
        public Bitmap Obstructions { get; set; }
        public Bitmap Volatile { get; set; }
        public Bitmap Detected { get; set; }
        public Graphics gDetected { get; set; }
        public Graphics gObstructions { get; set; }
        public Graphics gVolatile { get; set; }
        public _2DpMapSim()
        {
            DoubleBuffered = true;
            SizeChanged += _2DpMapSim_SizeChanged;
        }

        private void _2DpMapSim_SizeChanged(object sender, EventArgs e)
        {
            begin();
        }

        public void begin()
        {
            Illustration = new Bitmap(Width, Height);
            //Graphics.FromImage(Illustration).DrawImage(Image.FromFile("back.png"), 0, 0, Width, Height);
            Obstructions = new Bitmap(Width, Height);
            Volatile = new Bitmap(Width, Height);
            Detected = new Bitmap(Width, Height);
            gVolatile = Graphics.FromImage(Volatile);
            gObstructions = Graphics.FromImage(Obstructions);
            gDetected = Graphics.FromImage(Detected);
        }
        public enum ProbabilityAlgorithm
        {
            MoreSamplesTheBetter,
            MoreSensorReferencesTheBetter
        }
        ProbabilityAlgorithm AlgorithmToUse = ProbabilityAlgorithm.MoreSensorReferencesTheBetter;
        public class ProbabilitySample
        {
            public static int DW { get; } = 10;
            public double X { get; private set; }
            public double Y { get; private set; }
            public ProbabilitySample(double x, double y)
            {
                X = x;
                Y = y;
            }
            /// <summary>
            /// Should be in the range [0, 1]
            /// </summary>
            public double GroupScore { get; set; }
            /// <summary>
            /// Can be in any scale
            /// </summary>
            public virtual double AbsoluteScore { get; }

            virtual public void RegisterPresence(Dot dot)
            {
                throw new NotImplementedException();
            }

            public virtual void RegisterAbsence(Dot dot)
            {
                //throw new NotImplementedException();
            }
        }
        public class SamplesCountProbabilitySample : ProbabilitySample
        {
            public int Samples { get; set; } = 0;
            public override double AbsoluteScore => Samples;
            public SamplesCountProbabilitySample(double x, double y) : base(x, y)
            {
            }
            public override void RegisterPresence(Dot dot)
            {
                Samples++;
            }
            public override void RegisterAbsence(Dot dot)
            {
                Samples -= 1;
            }

            public static void CompileScores(ProbabilitySample[,] ProbabilityMap_)
            {
                var ProbabilityMap = new SamplesCountProbabilitySample[ProbabilityMap_.GetLength(0), ProbabilityMap_.GetLength(1)];
                double maxScore = double.MinValue;
                for (int y = 0; y < ProbabilityMap.GetLength(1); y++)
                    for (int x = 0; x < ProbabilityMap.GetLength(0); x++)
                    {
                        ProbabilityMap[x, y] = (SamplesCountProbabilitySample)ProbabilityMap_[x, y];
                        if (ProbabilityMap[x, y] == null)
                            continue;
                        double score = ProbabilityMap[x, y].Samples;
                        if (score > maxScore)
                            maxScore = score;
                    }
                for (int y = 0; y < ProbabilityMap.GetLength(1); y++)
                    for (int x = 0; x < ProbabilityMap.GetLength(0); x++)
                    {
                        if (ProbabilityMap[x, y] == null)
                            continue;
                        ProbabilityMap[x, y].GroupScore = ProbabilityMap[x, y].Samples / maxScore;
                    }
            }
        }
        public class SensorDiversityProbabilitySample : ProbabilitySample
        {
            public static int CountBinsCount { get; } = 10;
            public double[] AnisotrpicSamplesCountBins { get; private set; }
            public override double AbsoluteScore { get { return AnisotrpicSamplesCountBins.Sum(b => b > 0 ? 1 : 0) / (double)AnisotrpicSamplesCountBins.Length * 4; } }
            public SensorDiversityProbabilitySample(double x, double y) : base(x, y)
            {
                AnisotrpicSamplesCountBins = new double[CountBinsCount];
            }
            public double ComputeScore()
            {
                return AnisotrpicSamplesCountBins.Average();
            }
            public override void RegisterPresence(Dot dot)
            {
                double angle = Math.Atan2(dot.Position.Y - Y, dot.Position.X - X);
                if (angle < 0)
                    angle += 2 * Math.PI;
                int angleI = (int)Math.Round(angle / (2 * Math.PI) * (CountBinsCount - 1));
                AnisotrpicSamplesCountBins[angleI]++;
            }
            public override void RegisterAbsence(Dot dot)
            {
                for (int i = 0; i < AnisotrpicSamplesCountBins.Length; i++)
                    AnisotrpicSamplesCountBins[i] -= 1F;
            }

            internal static void CompileScores(ProbabilitySample[,] ProbabilityMap_)
            {
                var ProbabilityMap = new SensorDiversityProbabilitySample[ProbabilityMap_.GetLength(0), ProbabilityMap_.GetLength(1)];
                for (int y = 0; y < ProbabilityMap.GetLength(1); y++)
                    for (int x = 0; x < ProbabilityMap.GetLength(0); x++)
                    {
                        ProbabilityMap[x, y] = (SensorDiversityProbabilitySample)ProbabilityMap_[x, y];
                        if (ProbabilityMap[x, y] == null)
                            continue;
                        var bins = ProbabilityMap[x, y].AnisotrpicSamplesCountBins;
                        ProbabilityMap[x, y].GroupScore = ProbabilityMap[x, y].AbsoluteScore;
                    }
            }
            public override string ToString()
            {
                return "X = " + X.ToString("F2") + ", Y = " + Y.ToString("F2") + ", AS = " + AbsoluteScore.ToString("F2") + ", GS = " + GroupScore.ToString("F2");
            }
        }
        public void CompileScores()
        {
            if (AlgorithmToUse == ProbabilityAlgorithm.MoreSamplesTheBetter)
            {
                SamplesCountProbabilitySample.CompileScores(ProbabilityMap);
            }
            else if (AlgorithmToUse == ProbabilityAlgorithm.MoreSensorReferencesTheBetter)
            {
                SensorDiversityProbabilitySample.CompileScores(ProbabilityMap);
            }
        }
        public ProbabilitySample[,] ProbabilityMap;
        public bool[,] Environment;
        public void RemakeEnvironment()
        {
            Environment = new bool[Obstructions.Width, Obstructions.Height];
            for (int y = 0; y < Obstructions.Height; y++)
            {
                for (int x = 0; x < Obstructions.Width; x++)
                {
                    Environment[x, y] = Obstructions.GetPixel(x, y).R < 250;
                }
            }
            ProbabilityMap = new ProbabilitySample[Obstructions.Width / ProbabilitySample.DW + 1, Obstructions.Height / ProbabilitySample.DW + 1];
        }
        public void FeedSample(Dot dot, double[] readings)
        {
            var thisMap = new bool[ProbabilityMap.GetLength(0), ProbabilityMap.GetLength(1)];
            var thisNegMap = new bool[ProbabilityMap.GetLength(0), ProbabilityMap.GetLength(1)];

            for (int thi = 0; thi < dot.NumberOfSensors; thi++)
            {
                double tho = dot.Angle + (thi / (double)dot.NumberOfSensors) * (2 * Math.PI);
                double negSweepClearance = 5 * Math.PI / 180;
                for (double th = negSweepClearance / 2; th < dot.Sweep - negSweepClearance; th += 0.01)
                {
                    for (float r = 0; r < readings[thi]; r += 1)
                    {
                        int xi = ((int)Math.Round(r * Math.Cos(th + tho) + dot.Position.X)) / ProbabilitySample.DW;
                        int yi = ((int)Math.Round(r * Math.Sin(th + tho) + dot.Position.Y)) / ProbabilitySample.DW;
                        if (xi < 0 || xi >= ProbabilityMap.GetLength(0) ||
                            yi < 0 || yi >= ProbabilityMap.GetLength(1))
                            break;
                        thisNegMap[xi, yi] = true;
                    }
                }
            }

            for (int thi = 0; thi < dot.NumberOfSensors; thi++)
            {
                double tho = dot.Angle + (thi / (double)dot.NumberOfSensors) * (2 * Math.PI);
                for (double th = 0; th < dot.Sweep; th += 0.01)
                {
                    int xi = ((int)Math.Round(readings[thi] * Math.Cos(th + tho) + dot.Position.X)) / ProbabilitySample.DW;
                    int yi = ((int)Math.Round(readings[thi] * Math.Sin(th + tho) + dot.Position.Y)) / ProbabilitySample.DW;
                    if (xi < 0 || xi >= ProbabilityMap.GetLength(0) ||
                        yi < 0 || yi >= ProbabilityMap.GetLength(1))
                        break;
                    thisMap[xi, yi] = true;
                    thisNegMap[xi, yi] = false;

                }
            }
            for (int y = 0; y < thisMap.GetLength(1); y++)
                for (int x = 0; x < thisMap.GetLength(0); x++)
                {
                    if (ProbabilityMap[x, y] == null)
                    {
                        if (AlgorithmToUse == ProbabilityAlgorithm.MoreSamplesTheBetter)
                            ProbabilityMap[x, y] = new SamplesCountProbabilitySample(x * ProbabilitySample.DW, y * ProbabilitySample.DW);
                        else if (AlgorithmToUse == ProbabilityAlgorithm.MoreSensorReferencesTheBetter)
                            ProbabilityMap[x, y] = new SensorDiversityProbabilitySample(x * ProbabilitySample.DW, y * ProbabilitySample.DW);
                    }
                    if (thisMap[x, y])
                    {
                        ProbabilityMap[x, y].RegisterPresence(dot);
                        //ProbabilityMap[x, y].AnisotrpicSamplesCountBins[angleI] =
                        //    ProbabilityMap[x, y].AnisotrpicSamplesCountBins[angleI] * (1 - f) + f;
                    }
                    else
                    {
                        if (thisNegMap[x, y])
                        {
                            ProbabilityMap[x, y].RegisterAbsence(dot);
                        }
                    }
                }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
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

        internal void Reset()
        {
            gDetected.Clear(Color.Transparent);
            gVolatile.Clear(Color.Transparent);
            //gObstructions.Clear(Color.Transparent);
            RemakeEnvironment();
            Invalidate();
        }
    }
}
