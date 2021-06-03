using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SlitherlinkControl
{

    public enum EdgeFlags
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8
    }

    public class GameBlock
    {
        internal static double BlockSize;
        internal static double HalfCrossSize = 5;
        public int Row { get; }
        public int Column { get; }
        public int Value { get; }
        public string StringValue => Value == -1 ? "." : Value.ToString();
        public EdgeFlags Edge { get; set; }
        public (Line Top, Line Bottom, Line Left, Line Right) EdgeLine;

        public EdgeFlags Cross { get; set; }
        public (Path Top, Path Bottom, Path Left, Path Right) CrossPath;

        public TextBlock NumberText { get; }

        /// <summary>
        /// 一个格子，当没有数字时，<paramref name="str"/>为<see cref="string.Empty"/>
        /// </summary>
        public GameBlock(int column, int row, TextBlock text)
        {
            Column = column;
            Row = row;
            NumberText = text;
            Value = string.IsNullOrEmpty(text.Text) ? -1 : Convert.ToInt32(text.Text);
        }

        public GameBlock()
        {

        }

        private void SetLinePos(Line l, int dx1, int dy1, int dx2, int dy2)
        {
            if (l != null)
            {
                l.X1 = (Column + dx1) * BlockSize;
                l.Y1 = (Row + dy1) * BlockSize;
                l.X2 = (Column + dx2) * BlockSize;
                l.Y2 = (Row + dy2) * BlockSize;
            }
        }

        private void SetCrossPos(Shape x, double dx, double dy)
        {
            if (x != null)
            {
                Canvas.SetLeft(x, (Column + dx) * BlockSize - HalfCrossSize);
                Canvas.SetTop(x, (Row + dy) * BlockSize - HalfCrossSize);
            }
        }

        /// <summary>
        /// 更新边线和X的位置
        /// </summary>
        public void UpdateEdgePos()
        {
            SetLinePos(EdgeLine.Top, 0, 0, 1, 0);
            SetLinePos(EdgeLine.Bottom, 0, 1, 1, 1);
            SetLinePos(EdgeLine.Left, 0, 0, 0, 1);
            SetLinePos(EdgeLine.Right, 1, 0, 1, 1);

            SetCrossPos(CrossPath.Top, 0.5, 0);
            SetCrossPos(CrossPath.Bottom, 0.5, 1);
            SetCrossPos(CrossPath.Left, 0, 0.5);
            SetCrossPos(CrossPath.Right, 1, 0.5);
        }

        /// <summary>
        /// 线的数目是否与数字符合
        /// </summary>
        public bool IsVaild()
        {
            if (Value == -1) return true;
            int count = 0;
            if (Edge.HasFlag(EdgeFlags.Top)) count++;
            if (Edge.HasFlag(EdgeFlags.Bottom)) count++;
            if (Edge.HasFlag(EdgeFlags.Left)) count++;
            if (Edge.HasFlag(EdgeFlags.Right)) count++;
            return count == Value;
        }

        /// <summary>
        /// 线和X的数目是否不可能与数字符合
        /// </summary>
        public bool IsPreviewInvaild()
        {
            if (Value == -1) return false;
            int count = 0;
            if (Edge.HasFlag(EdgeFlags.Top)) count++;
            if (Edge.HasFlag(EdgeFlags.Bottom)) count++;
            if (Edge.HasFlag(EdgeFlags.Left)) count++;
            if (Edge.HasFlag(EdgeFlags.Right)) count++;
            if (count < Value)
            {
                count = 0;
                if (Cross.HasFlag(EdgeFlags.Top)) count++;
                if (Cross.HasFlag(EdgeFlags.Bottom)) count++;
                if (Cross.HasFlag(EdgeFlags.Left)) count++;
                if (Cross.HasFlag(EdgeFlags.Right)) count++;
                return count + Value > 4;
            }
            return count > Value;
        }

    }

    public class Step
    {
        public bool IsAdd { get; }
        public bool IsLine { get; }
        public EdgeFlags Edge { get; }
        public int X { get; }
        public int Y { get; }

        public Step(int x, int y, EdgeFlags edge, bool add, bool line)
        {
            IsAdd = add;
            IsLine = line;
            Edge = edge;
            X = x;
            Y = y;
        }
    }

    public static class DoubleExtension
    {
        private static double _precision = 0.01;

        public static double Precision
        {
            get => _precision;
            set => _precision = Math.Max(0, value);
        }

        public static bool AlmostEqual(this double self, double value)
        {
            return Math.Abs(self - value) < Precision;
        }
    }

    public static class LinesExtension
    {
        public static int GetConnectedLine(this ICollection<Line> self, Line line, out Line firstConnectedLine)
        {
            var lines = self.Where(l =>
                    (l.X1.AlmostEqual(line.X1) && l.Y1.AlmostEqual(line.Y1)) ||
                    (l.X2.AlmostEqual(line.X1) && l.Y2.AlmostEqual(line.Y1)) ||
                    (l.X1.AlmostEqual(line.X2) && l.Y1.AlmostEqual(line.Y2)) ||
                    (l.X2.AlmostEqual(line.X2) && l.Y2.AlmostEqual(line.Y2)));
            firstConnectedLine = lines.FirstOrDefault();
            return lines.Count();
        }

        public static bool LineCrossed(this ICollection<Line> self, Line line)
        {
            var lines = self.Where(l =>
                    (l.X1.AlmostEqual(line.X1) && l.Y1.AlmostEqual(line.Y1)) ||
                    (l.X2.AlmostEqual(line.X1) && l.Y2.AlmostEqual(line.Y1)));
            if (lines.Count() > 2) return true;
            lines = self.Where(l =>
                    (l.X1.AlmostEqual(line.X2) && l.Y1.AlmostEqual(line.Y2)) ||
                    (l.X2.AlmostEqual(line.X2) && l.Y2.AlmostEqual(line.Y2)));
            return lines.Count() > 2;
        }
    }
}