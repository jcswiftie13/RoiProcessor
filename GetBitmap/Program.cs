using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

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
        public Image img;
        public Graphics g;
        private List<Rectangle> recs = new List<Rectangle>();

        public Processor(string data, float height, float width)
        {
            DataParser(data, height, width);
        }

        //DataParser未完成由於目前得到的範例資料還不足以得到整個矩形的座標, 先前有討論過後續可能會給矩形的寬高, 將再視情況開發
        public void DataParser(string data, float height, float width)
        {
            string line;
            Coord tcoord;
            System.IO.StreamReader file = new System.IO.StreamReader(data);
            while ((line = file.ReadLine()) != null)
            {
                Regex pattern = new Regex(@"-?\d+.\d+");
                MatchCollection matches = pattern.Matches(line);
                Rectangle temp = new Rectangle(tcoord = new Coord(Convert.ToSingle(matches[0].Value), Convert.ToSingle(matches[1].Value)), height, width);
                temp.Rotate(Convert.ToSingle(matches[2].Value));
                recs.Add(temp);
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

        private void FillPolygon(Brush brush, Rectangle rec, Rectangle roi)
        {
            PointF[] points = new PointF[4] {
            new PointF(rec.TopLeft.X - roi.TopLeft.X, rec.TopLeft.Y - roi.TopLeft.Y),
            new PointF(rec.BottomLeft.X - roi.TopLeft.X, rec.BottomLeft.Y - roi.TopLeft.Y),
            new PointF(rec.BottomRight.X - roi.TopLeft.X, rec.BottomRight.Y - roi.TopLeft.Y),
            new PointF(rec.TopRight.X - roi.TopLeft.X, rec.TopRight.Y - roi.TopLeft.Y)
            };

            g.FillPolygon(brush, points);
        }
        //GetBitmap的roi部分也還不清楚會是以何格式傳輸
        public void GetBitmap(Rectangle roi)
        {
            img = new Bitmap((int)Math.Ceiling(roi.Width), (int)Math.Ceiling(roi.Height));
            g = Graphics.FromImage(img);
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
            g.Clear(Color.Black);
            SolidBrush brush = new SolidBrush(Color.White);

            for (int i = 0; i < recs.Count; i++)
            {
                if (Out(recs[i], roi)) continue;
                else
                {
                    if (Within(recs[i], roi))
                    {
                        recs.RemoveAt(i);
                    }
                    FillPolygon(brush, recs[i], roi);
                }
            }

            img.Save("ROI.bmp");

        }
    }
    class test
    {
        static void Main()
        {
            Coord roicoord = new Coord(175, 175);
            Rectangle roi = new Rectangle(roicoord, 100, 100);
            Processor p = new Processor("test.txt", 50, 50);
            p.GetBitmap(roi);
        }
    }
}
