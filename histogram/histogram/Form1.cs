using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace histogram
{
    public partial class Form1 : Form
    {
        public class Result
        {
            public Bitmap[] histBmp;
            public float[][] histData;
            public Image<Rgb, byte> result;
            public double min;
            public double max;
            public Result()
            {
                histBmp = new Bitmap[3];
                histData = new float[3][];
                for (int i = 0; i < 3; i++)
                {
                    histData[i] = new float[256];
                }
            }
        }

        Image<Rgb, byte> sourceRgbImg8;
        Image<Rgb, UInt16> sourceRgbImg16;
        float[][] oriHistData = new float[3][];
        Bitmap[] oriHistBmp = new Bitmap[3];
        int brightnessNum = 0;
        int contrastNum = 0;
        int depth;
        int oriMin;
        int oriMax;
        Point[] minPt = new Point[1];
        Point[] maxPt = new Point[1];
        public Form1()
        {
            for (int i = 0; i < 3; i++)
                oriHistData[i] = new float[256];
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Mat src = CvInvoke.Imread(ofd.FileName, Emgu.CV.CvEnum.ImreadModes.AnyDepth);
                    //Bitmap[] oriHistBmp = new Bitmap[3];
                    double[] min = new double[1];
                    double[] max = new double[1];
                    src.MinMax(out min, out max, out minPt, out maxPt);
                    oriMin = (int)min[0];
                    oriMax = (int)max[0];
                    switch (src.Depth)
                    {
                        case Emgu.CV.CvEnum.DepthType.Cv8U:
                            {
                                trackBar1.Maximum = 255;
                                trackBar1.Minimum = -255;
                                trackBar2.Maximum = 255;
                                trackBar2.Minimum = -255;
                                depth = 8;
                                sourceRgbImg8 = new Image<Rgb, byte>(ofd.FileName);
                                oriHistData = ComputeHistogram(sourceRgbImg8);
                                oriHistBmp = DrawHistogram(oriHistData);
                                oriHistBmp = DrawMinMax(oriHistBmp, 0, 0);
                                pictureBox1.Image = sourceRgbImg8.ToBitmap();
                                if (checkBox1.Checked)
                                    pictureBox2.Image = oriHistBmp[2];
                                else if (checkBox2.Checked)
                                    pictureBox2.Image = oriHistBmp[1];
                                else
                                    pictureBox2.Image = oriHistBmp[0];
                                label1.Text = oriMin.ToString();
                                label2.Text = oriMax.ToString();
                                break;
                            }
                        case Emgu.CV.CvEnum.DepthType.Cv16U:
                            {
                                trackBar1.Maximum = 65535;
                                trackBar1.Minimum = -65535;
                                trackBar2.Maximum = 65535;
                                trackBar2.Minimum = -65535;
                                depth = 16;
                                sourceRgbImg16 = new Image<Rgb, UInt16>(ofd.FileName);
                                Image<Rgb, byte> tmp = new Image<Rgb, byte>(ofd.FileName);
                                oriHistData = ComputeHistogram(tmp);
                                oriHistBmp = DrawHistogram(oriHistData);
                                oriHistBmp = DrawMinMax(oriHistBmp, 0, 0);
                                pictureBox1.Image = sourceRgbImg16.ToBitmap();
                                if (checkBox1.Checked)
                                    pictureBox2.Image = oriHistBmp[2];
                                else if (checkBox2.Checked)
                                    pictureBox2.Image = oriHistBmp[1];
                                else
                                    pictureBox2.Image = oriHistBmp[0];
                                label1.Text = oriMin.ToString();
                                label2.Text = oriMax.ToString();
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
        }
        private static float[][] ComputeHistogram(Image<Rgb, byte> input)
        {
            float[][] result = new float[3][];
            for (int i = 0; i < 3; i++)
            {
                result[i] = new float[256];
            }
            for (int channel = 0; channel < 3; channel++)
            {
                DenseHistogram hist = new DenseHistogram(256, new RangeF(0, 256));
                hist.Calculate(new Image<Gray, byte>[] { input[channel] }, false, null);
                float[] data = new float[hist.Width * hist.Height];
                Marshal.Copy(hist.DataPointer, data, 0, hist.Width * hist.Height);
                result[channel] = data;
            }
            return result;
        }
        private static Bitmap[] DrawHistogram(float[][] data)
        {
            Bitmap[] histBmp = new Bitmap[3];
            for (int channel = 0; channel < 3; channel++)
            {
                histBmp[channel] = new Bitmap(256, 256);
                Graphics histGraphic = Graphics.FromImage(histBmp[channel]);
                Pen blkPen = new Pen(Color.Black);
                float maxAmount = data[channel].Max();
                for (int r = 0; r < 256; r++)
                {
                    //處理亮度超出max時
                    //if (255 - data[channel][r] > maxAmount)
                    //  maxAmount = 255 - data[channel][r];
                    //y愈下面愈大 Pt1是下面的點Pt2是上面的
                    Point Pt1 = new Point(r, 255);
                    Point Pt2 = new Point(r, (int)Math.Round(255.0 - data[channel][r] / maxAmount * 255.0));                    
                    histGraphic.DrawLine(blkPen, Pt1, Pt2);
                }
            }
            return histBmp;
        }
        private static Bitmap[] DrawMinMax(Bitmap[] histBmp, int brightnessDiv, int contrastDiv)
        {
            Bitmap[] result = histBmp;
            for (int channel = 0; channel < 3; channel++)
            {
                Graphics histGraphic = Graphics.FromImage(result[channel]);
                Point ProjectPt1 = new Point(0 - brightnessDiv + contrastDiv, histBmp[channel].Height);
                Point ProjectPt2 = new Point(255 - brightnessDiv - contrastDiv, 0);
                while (ProjectPt1.X > ProjectPt2.X)
                {
                    ProjectPt1.X--;
                    ProjectPt2.X++;
                }
                switch (channel)
                {
                    case 0:
                        Pen bluePen = new Pen(Color.Blue);
                        histGraphic.DrawLine(bluePen, ProjectPt1, ProjectPt2);
                        //output.histBmp[0] = output.histBmp[channel];
                        break;
                    case 1:
                        Pen greenPen = new Pen(Color.Green);
                        histGraphic.DrawLine(greenPen, ProjectPt1, ProjectPt2);
                        //output.histBmp[1] = output.histBmp[channel];
                        break;
                    case 2:
                        Pen redPen = new Pen(Color.Red);
                        histGraphic.DrawLine(redPen, ProjectPt1, ProjectPt2);
                        //output.histBmp[2] = output.histBmp[channel];
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        private void Change8bit(Image<Rgb, byte> sourceImg, float[][] oriHistData, int brightnessDiv, int contrastDiv, int min, int max, bool changeHistogram, out Result output)
        {
            //fps start
            Stopwatch myStopWatch = new Stopwatch();
            myStopWatch.Start();
            output = new Result();
            Image<Rgb, byte> result = new Image<Rgb, byte>(new Size(sourceImg.Width, sourceImg.Height));

            max -= brightnessDiv;
            min -= brightnessDiv;
            max -= contrastDiv;
            min += contrastDiv;
            int rows = sourceImg.Rows;
            int cols = sourceImg.Cols;
            //計算轉換成圖片的0~255之值
            for (int channel = 0; channel < 3; channel++)
            {
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        double pointValue = sourceImg.Data[r, c, channel];

                        while (max - min < 2)
                        {
                            contrastDiv--;
                            max++;
                            min--;
                        }
                        pointValue = 255.0 / (max - min) * (pointValue - min);
                        //pointValue = Convert.ToInt32((255.0 / (max - min)) * Convert.ToDouble(pointValue - min));
                        if (pointValue > 255)
                        {
                            pointValue = 255;
                        }
                        if (pointValue < 0)
                            pointValue = 0;

                        result.Data[r, c, channel] = (byte)pointValue;
                    }
                }
            }
            //若有要更新才會更新
            if (changeHistogram)
            {
                output.histData = ComputeHistogram(result);
            }
            else
            {
                output.histData = oriHistData;
            }
            output.histBmp = DrawHistogram(output.histData);
            output.histBmp = DrawMinMax(output.histBmp, brightnessDiv, contrastDiv);

            output.result = result;
            output.min = min;
            output.max = max;
            if (output.min < 0)
                output.min = 0;
            if (output.max > 255)
                output.max = 255;
            if (output.min > 255)
                output.min = 255;
            if (output.max < 0)
                output.max = 0;
            //fps end
            myStopWatch.Stop();
            int FPS = 1000 / myStopWatch.Elapsed.Milliseconds;
            myStopWatch.Reset();
            label5.Text = "fps : " + FPS.ToString();
        }

        private void Change16bit(Image<Rgb, UInt16> sourceImg, float[][] oriHistData, int brightnessDiv, int contrastDiv, int min, int max, bool changeHistogram, out Result output)
        {
            ////fps start
            //Stopwatch myStopWatch = new Stopwatch();
            //myStopWatch.Start();     

            output = new Result();
            Image<Rgb, byte> result = new Image<Rgb, byte>(new Size(sourceImg.Width, sourceImg.Height));
            //max -= Convert.ToDouble(brightnessDiv);
            //min -= Convert.ToDouble(brightnessDiv);
            //max -= Convert.ToDouble(contrastDiv);
            //min += Convert.ToDouble(contrastDiv);
            max -= brightnessDiv;
            min -= brightnessDiv;
            max -= contrastDiv;
            min += contrastDiv;
            int rows = sourceImg.Rows;
            int cols = sourceImg.Cols;
            for (int channel = 0; channel < 3; channel++)
            {
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        double pointValue = sourceImg.Data[r, c, channel];

                        while (max - min < 2)
                        {
                            contrastDiv--;
                            max++;
                            min--;
                        }
                        pointValue = 255.0 / (max - min) * (pointValue - min);
                        if (pointValue > 255)
                            pointValue = 255;
                        if (pointValue < 0)
                            pointValue = 0;
                        result.Data[r, c, channel] = (byte)pointValue;

                    }
                }
            }
            //若有要更新才會更新
            if (changeHistogram)
            {
                output.histData = ComputeHistogram(result);
            }
            else
            {
                output.histData = oriHistData;
            }
            output.histBmp = DrawHistogram(output.histData);
            output.histBmp = DrawMinMax(output.histBmp, brightnessDiv >> 8, contrastDiv >> 8);
            output.result = result;
            output.min = min;
            output.max = max;
            if (output.min < 0)
                output.min = 0;
            if (output.max > 65535)
                output.max = 65535;
            if (output.min > 65535)
                output.min = 65535;
            if (output.max < 0)
                output.max = 0;
            //fps end
            //myStopWatch.Stop();
            //int FPS = 1000 / myStopWatch.Elapsed.Milliseconds;
            //myStopWatch.Reset();
            //label5.Text ="fps : "+ FPS.ToString();
        }

        //contrast
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //fps start
            Stopwatch myStopWatch = new Stopwatch();
            myStopWatch.Start();
            bool changeHistogram;
            changeHistogram = checkBox4.Checked;
            contrastNum = trackBar1.Value;
            Result output = new Result();

            if (depth == 8)
                Change8bit(sourceRgbImg8, oriHistData, brightnessNum, contrastNum, oriMin, oriMax, changeHistogram, out output);
            else
                Change16bit(sourceRgbImg16, oriHistData, brightnessNum, contrastNum, oriMin, oriMax, changeHistogram, out output);

            pictureBox1.Image = output.result.ToBitmap();
            if (checkBox1.Checked)
            {
                pictureBox2.Image = output.histBmp[2];
            }
            else if (checkBox2.Checked)
            {
                pictureBox2.Image = output.histBmp[1];
            }
            else
            {
                pictureBox2.Image = output.histBmp[0];
            }

            label1.Text = output.min.ToString();
            label2.Text = output.max.ToString();
            //if (output.min < 0)
            //    label1.Text = "0";
            //else
            //    label1.Text = output.min.ToString();
            //if (output.max < 0)
            //    label2.Text = "0";
            //else
            //    label2.Text = output.max.ToString();
            //fps end
            myStopWatch.Stop();
            int FPS = 1000 / myStopWatch.Elapsed.Milliseconds;
            myStopWatch.Reset();
            label5.Text = "fps : " + FPS.ToString();
        }

        //brightness
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            //fps start
            Stopwatch myStopWatch = new Stopwatch();
            myStopWatch.Start();
            bool changeHistogram;
            changeHistogram = checkBox4.Checked;
            brightnessNum = trackBar2.Value;
            Result output = new Result();
            if (depth == 8)
                Change8bit(sourceRgbImg8, oriHistData, brightnessNum, contrastNum, oriMin, oriMax, changeHistogram, out output);
            else
                Change16bit(sourceRgbImg16, oriHistData, brightnessNum, contrastNum, oriMin, oriMax, changeHistogram, out output);

            pictureBox1.Image = output.result.ToBitmap();
            if (checkBox1.Checked)
                pictureBox2.Image = output.histBmp[2];
            else if (checkBox2.Checked)
                pictureBox2.Image = output.histBmp[1];
            else
                pictureBox2.Image = output.histBmp[0];

            label1.Text = output.min.ToString();
            label2.Text = output.max.ToString();

            //if (output.min < 0)
            //    label1.Text = "0";
            //else
            //    label1.Text = output.min.ToString();
            //if (output.max < 0)
            //    label2.Text = "0";
            //else
            //    label2.Text = output.max.ToString();

            //fps end
            myStopWatch.Stop();
            int FPS = 1000 / myStopWatch.Elapsed.Milliseconds;
            myStopWatch.Reset();
            label5.Text = "fps : " + FPS.ToString();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                pictureBox2.Image = oriHistBmp[2];
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                pictureBox2.Image = oriHistBmp[1];
        }
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
                pictureBox2.Image = oriHistBmp[0];
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox4.Checked)
            {
                if (checkBox1.Checked)
                    pictureBox2.Image = oriHistBmp[2];
                if (checkBox2.Checked)
                    pictureBox2.Image = oriHistBmp[1];
                if (checkBox3.Checked)
                    pictureBox2.Image = oriHistBmp[0];
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}