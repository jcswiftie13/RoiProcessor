using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace RoiProcessor
{
    public class Coord
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Coord(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
    public class Rectangle
    {
        public float Height { get; set; }
        public float Width { get; set; }
        public float Rotation { get; private set; }
        public Coord Center { get; private set; }
        public Coord TopLeft { get; private set; }
        public Coord TopRight { get; private set; }
        public Coord BottomLeft { get; private set; }
        public Coord BottomRight { get; private set; }

        public Rectangle(Coord origin, float height, float width)
        {
            Height = height;
            Width = width;
            Center = origin;

            BottomLeft = new Coord(Center.X - Width / 2, Center.Y + Height / 2);
            BottomRight = new Coord(Center.X + Width / 2, Center.Y + Height / 2);
            TopLeft = new Coord(Center.X - Width / 2, Center.Y - Height / 2);
            TopRight = new Coord(Center.X + Width / 2, Center.Y - Height / 2);
        }

        private void Move(Coord c)
        {

            MoveCorners(new Coord((c.X - Center.X), (c.Y - Center.Y)));
            Center.X = Center.X + (c.X - Center.X);
            Center.Y = Center.Y + (c.Y - Center.Y);
        }

        private void MoveCorners(Coord c)
        {
            BottomRight.X = (BottomRight.X + c.X);
            BottomRight.Y = (BottomRight.Y + c.Y);

            BottomLeft.X = (BottomLeft.X + c.X);
            BottomLeft.Y = (BottomLeft.Y + c.Y);

            TopRight.X = (TopRight.X + c.X);
            TopRight.Y = (TopRight.Y + c.Y);

            TopLeft.X = (TopLeft.X + c.X);
            TopLeft.Y = (TopLeft.Y + c.Y);
        }

        public void Rotate(float rotation)
        {
            Coord temp = new Coord(Center.X, Center.Y);
            Move(new Coord(0, 0));

            BottomRight = RotateCorrner(BottomRight, rotation);
            TopRight = RotateCorrner(TopRight, rotation);
            BottomLeft = RotateCorrner(BottomLeft, rotation);
            TopLeft = RotateCorrner(TopLeft, rotation);

            Move(temp);
        }

        Coord RotateCorrner(Coord p, float rotation)
        {
            Coord temp = new Coord(p.X, p.Y);
            p.X = Convert.ToSingle(temp.X * Math.Cos(rotation) - temp.Y * Math.Sin(rotation));
            p.Y = Convert.ToSingle(temp.Y * Math.Cos(rotation) + temp.X * Math.Sin(rotation));

            return p;
        }
    }

    public class Processor
    {
        public float scale;
        private List<Rectangle> recs = new List<Rectangle>();

        public Processor(string data, float height, float width, float dpi)
        {
            DataParser(data, height, width, dpi);
        }

        public void DataParser(string data, float height, float width, float dpi)
        {
            string[] lines;
            Coord tcoord;
            Regex pattern = new Regex(@"-?\d+.\d+");
            Regex nparts = new Regex("(?<=\"NumberOfParts\":)\\d+");
            scale = Convert.ToSingle(0.1 * 0.393700787 * dpi);

            lines = System.IO.File.ReadAllLines(data);

            for (int i = 7; i < 8; i++)
            {
                Match match = nparts.Match(lines[i]);
                MatchCollection matches = pattern.Matches(lines[i]);

                for (int j = 0; j < Convert.ToInt32(match.Value); j++)
                {
                    Rectangle temp = new Rectangle(tcoord = new Coord(Convert.ToSingle(matches[j * 4 + 0].Value) * scale, Convert.ToSingle(matches[j * 4 + 1].Value) * scale), height * scale, width * scale);
                    temp.Rotate(Convert.ToSingle(matches[j * 4 + 2].Value));
                    recs.Add(temp);
                }
            }
        }

        public bool Out(Rectangle rec, Rectangle roi)
        {
            Coord l1 = roi.TopLeft;
            Coord r1 = roi.BottomRight;
            Coord l2 = rec.TopLeft;
            Coord r2 = rec.BottomRight;

            if (l1.X >= r2.X || l2.X >= r1.X) return true;
            if (r1.Y <= r2.Y || l2.Y >= r1.Y) return true;
            return false;
        }

        public bool Within(Rectangle rec, Rectangle roi)
        {
            Coord l1 = roi.TopLeft;
            Coord r1 = roi.BottomRight;
            Coord l2 = rec.TopLeft;
            Coord r2 = rec.BottomRight;

            if (l1.X <= l2.X && r1.X >= r2.X && l1.Y >= l2.Y && r1.Y <= r2.Y) return true;
            return false;
        }

        private void FillPolygon(Graphics g, Brush brush, Rectangle rec, Rectangle roi)
        {
            PointF[] points = new PointF[4] {
            new PointF(rec.TopLeft.X - roi.TopLeft.X, rec.TopLeft.Y - roi.TopLeft.Y),
            new PointF(rec.BottomLeft.X - roi.TopLeft.X, rec.BottomLeft.Y - roi.TopLeft.Y),
            new PointF(rec.BottomRight.X - roi.TopLeft.X, rec.BottomRight.Y - roi.TopLeft.Y),
            new PointF(rec.TopRight.X - roi.TopLeft.X, rec.TopRight.Y - roi.TopLeft.Y)
            };

            g.FillPolygon(brush, points);
        }

        public Bitmap GetBitmap(Rectangle roi)
        {
            roi = new Rectangle(new Coord(roi.Center.X * scale, roi.Center.Y * scale), roi.Height * scale, roi.Width * scale);

            Image img = new Bitmap((int)Math.Ceiling(roi.Width), (int)Math.Ceiling(roi.Height));
            Graphics g = Graphics.FromImage(img);
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
            g.Clear(Color.Black);
            SolidBrush brush = new SolidBrush(Color.White);

            for (int i = 0; i < recs.Count; i++)
            {
                if (Out(recs[i], roi)) continue;
                else
                {
                    FillPolygon(g, brush, recs[i], roi);
                }
            }
            using (System.IO.MemoryStream oMS = new System.IO.MemoryStream())
            {
                img.Save(oMS, System.Drawing.Imaging.ImageFormat.Jpeg);

                using (System.IO.FileStream oFS = System.IO.File.Open($"test/ROI{roi.Center.X}_{roi.Center.Y}.bmp", System.IO.FileMode.OpenOrCreate))
                {
                    oFS.Write(oMS.ToArray(), 0, oMS.ToArray().Length); 
                }
            }
            //img.Save($"test/ROI{roi.Center.X}_{roi.Center.Y}.bmp");
            return (Bitmap)img;
        }
    }
    class test
    {
        static void Main()
        {
            int roiHeight = 50;
            int roiWidth = 50;
            int picHeight = 300;
            int picWidth = 300;
            float recHeight = 19;
            float recWidth = 4.2f;
            float mm = 0.01f;

            int dpi = Convert.ToInt32(1 / (0.393700787 * 0.1 / (1 / mm)));

            List<int> timeList = new List<int>();
            Rectangle roi = new Rectangle(new Coord(roiWidth/2, roiHeight/2), roiHeight, roiWidth);
            Processor p = new Processor("test.txt", recHeight, recWidth, dpi);

            var watch = Stopwatch.StartNew();

            Thread[] threads = new Thread[picWidth / roiWidth * picHeight / roiHeight];

            for (int j = 0; j < picHeight / roiHeight; j++)
            {
                for (int i = 0; i < picWidth/roiWidth; i++)
                {
                    threads[j * picWidth / roiWidth + i] = new Thread(() => p.GetBitmap(roi));
                    threads[j * picWidth / roiWidth + i].Start();
                    threads[j * picWidth / roiWidth + i].Join();
                    //p.GetBitmap(roi);
                    roi.Center.X += roiWidth;
                }
                roi.Center.Y += roi.Height;
                roi.Center.X = roiWidth / 2;
            }
            watch.Stop();
            var elapsed = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsed);
        }
    }
}
