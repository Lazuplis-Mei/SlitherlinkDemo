using LazuplisMei.BinarySerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SlitherlinkControl
{

    [Flags]
    public enum EdgeFlags
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8
    }

    public class EdgeObject<T> where T : class
    {
        public T Top { get; set; }
        public T Bottom { get; set; }
        public T Left { get; set; }
        public T Right { get; set; }
        public EdgeFlags EdgeFlags { get; set; }

        /// <summary>
        /// 获得边界对象的数量
        /// </summary>
        public int GetObjectCount()
        {
            int count = 0;
            if (EdgeFlags.HasFlag(EdgeFlags.Top)) count++;
            if (EdgeFlags.HasFlag(EdgeFlags.Bottom)) count++;
            if (EdgeFlags.HasFlag(EdgeFlags.Left)) count++;
            if (EdgeFlags.HasFlag(EdgeFlags.Right)) count++;
            return count;
        }

        /// <summary>
        /// 获得边界上对象的列表
        /// </summary>
        public List<T> GetObjects()
        {
            var result = new List<T>();
            if (EdgeFlags.HasFlag(EdgeFlags.Top))
                result.Add(Top);
            if (EdgeFlags.HasFlag(EdgeFlags.Bottom))
                result.Add(Bottom);
            if (EdgeFlags.HasFlag(EdgeFlags.Left))
                result.Add(Left);
            if (EdgeFlags.HasFlag(EdgeFlags.Right))
                result.Add(Right);
            return result;
        }

        /// <summary>
        /// 添加对象到指定的边界
        /// </summary>
        public void AddObject(T obj, EdgeFlags flag)
        {
            EdgeFlags |= flag;
            switch (flag)
            {
                case EdgeFlags.Top:
                    Top = obj;
                    break;
                case EdgeFlags.Bottom:
                    Bottom = obj;
                    break;
                case EdgeFlags.Left:
                    Left = obj;
                    break;
                case EdgeFlags.Right:
                    Right = obj;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 从指定的边界移除对象
        /// </summary>
        public void RemoveObject(EdgeFlags flag)
        {
            EdgeFlags &= ~flag;
            switch (flag)
            {
                case EdgeFlags.Top:
                    Top = null;
                    break;
                case EdgeFlags.Bottom:
                    Bottom = null;
                    break;
                case EdgeFlags.Left:
                    Left = null;
                    break;
                case EdgeFlags.Right:
                    Right = null;
                    break;
                default:
                    break;
            }
        }

    }

    public class GameBlock
    {
        #region Statics

        public static double BlockSize { get; internal set; }
        public static double HalfCrossSize { get; internal set; }
        public static char DefaultChar { get; set; }
        static GameBlock()
        {
            BlockSize = 40;
            HalfCrossSize = 5;
            DefaultChar = '.';
        }

        #endregion

        #region Properties

        /// <summary>
        /// 该方块所在的行
        /// </summary>
        public int Y { get; }
        /// <summary>
        /// 该方块所在的列
        /// </summary>
        public int X { get; }
        /// <summary>
        /// 方块中的数字，如没有，则为-1
        /// </summary>
        public int Value { get; }
        /// <summary>
        /// 方块中数字的文本形式，如没有，则为<see cref="DefaultChar"/>
        /// </summary>
        public char StringValue => Value == -1 ? DefaultChar : Value.ToString()[0];
        /// <summary>
        /// 边界线段
        /// </summary>
        public EdgeObject<Line> Lines { get; }
        /// <summary>
        /// 边界X标记
        /// </summary>
        public EdgeObject<Path> Crosses { get; }
        /// <summary>
        /// 方块中数字的文本控件
        /// </summary>
        public TextBlock Number { get; }

        #endregion

        #region Constructors

        public GameBlock(int x, int y, TextBlock textBlock)
        {
            X = x;
            Y = y;
            Number = textBlock;
            Value = string.IsNullOrEmpty(textBlock.Text) ? -1 : Convert.ToInt32(textBlock.Text);
            Lines = new EdgeObject<Line>();
            Crosses = new EdgeObject<Path>();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 设置线段的位置
        /// </summary>
        private void SetLinePos(Line line, int dx1, int dy1, int dx2, int dy2)
        {
            if (line != null)
            {
                line.X1 = (X + dx1) * BlockSize;
                line.Y1 = (Y + dy1) * BlockSize;
                line.X2 = (X + dx2) * BlockSize;
                line.Y2 = (Y + dy2) * BlockSize;
            }
        }

        /// <summary>
        /// 设置X标记的位置
        /// </summary>
        private void SetCrossPos(Path shape, double dx, double dy)
        {
            if (shape != null)
            {
                Canvas.SetLeft(shape, (X + dx) * BlockSize - HalfCrossSize);
                Canvas.SetTop(shape, (Y + dy) * BlockSize - HalfCrossSize);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 更新边线和X标记的位置
        /// </summary>
        public void UpdateEdgePos()
        {
            SetLinePos(Lines.Top, 0, 0, 1, 0);
            SetLinePos(Lines.Bottom, 0, 1, 1, 1);
            SetLinePos(Lines.Left, 0, 0, 0, 1);
            SetLinePos(Lines.Right, 1, 0, 1, 1);

            SetCrossPos(Crosses.Top, 0.5, 0);
            SetCrossPos(Crosses.Bottom, 0.5, 1);
            SetCrossPos(Crosses.Left, 0, 0.5);
            SetCrossPos(Crosses.Right, 1, 0.5);
        }

        /// <summary>
        /// 边界上线段的数目是否等于数字
        /// </summary>
        public bool IsCorrect()
        {
            return Value == -1 || Lines.GetObjectCount() == Value;
        }

        /// <summary>
        /// 线和X的数目是否不可能与数字符合
        /// </summary>
        public bool IsPreviewInvaild()
        {
            if (Value == -1) return false;
            int count = Lines.GetObjectCount();
            if (count < Value)
            {
                count = Crosses.GetObjectCount();
                return count + Value > 4;
            }
            return count > Value;
        }

        #endregion

    }

    public struct Step
    {
        /// <summary>
        /// 表示该步骤是添加还是移除
        /// </summary>
        public bool IsAdd { get; set; }
        /// <summary>
        /// 表示该步骤是线段还是x标志
        /// </summary>
        public bool IsLine { get; set; }
        /// <summary>
        /// 表示该步骤操作的边界
        /// </summary>
        public EdgeFlags EdgeFlags { get; set; }
        /// <summary>
        /// 该步骤操作的格子的横坐标(列)
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// 该步骤操作的格子的纵坐标(行)
        /// </summary>
        public int Y { get; set; }
        /// <summary>
        /// 一个操作步骤
        /// </summary>
        [BinaryConstructor]
        public Step(int x, int y, EdgeFlags edge, bool add, bool line)
        {
            IsAdd = add;
            IsLine = line;
            EdgeFlags = edge;
            X = x;
            Y = y;
        }
    }

    public static class DoubleExtension
    {

        /// <summary>
        /// 比较精度
        /// </summary>
        public static double Precision
        {
            get => _Precision;
            set => _Precision = value.RangeLimit(0, double.PositiveInfinity);
        }
        private static double _Precision = 0.01;

        /// <summary>
        /// 两数的差值是否小于比较精度
        /// </summary>
        public static bool AlmostEqual(this double self, double value)
        {
            return Math.Abs(self - value) < Precision;
        }

        /// <summary>
        /// 将数限制到指定范围
        /// </summary>
        public static double RangeLimit(this double self, double min, double max)
        {
            return Math.Max(Math.Min(self, max), min);
        }

    }

    public static class LinesExtension
    {

        /// <summary>
        /// 获得在线段的集合中与指定线段首尾相连的线段数目，并返回第一个相连的线段
        /// </summary>
        public static int GetConnectedLine(this IEnumerable<Line> self, Line line, out Line firstConnectedLine)
        {
            var lines = self.Where(l =>
                    (l.X1.AlmostEqual(line.X1) && l.Y1.AlmostEqual(line.Y1)) ||
                    (l.X2.AlmostEqual(line.X1) && l.Y2.AlmostEqual(line.Y1)) ||
                    (l.X1.AlmostEqual(line.X2) && l.Y1.AlmostEqual(line.Y2)) ||
                    (l.X2.AlmostEqual(line.X2) && l.Y2.AlmostEqual(line.Y2)));
            firstConnectedLine = lines.FirstOrDefault();
            return lines.Count();
        }

        /// <summary>
        /// 在线段的集合中，是否存在大于2根线段与指定线段的一端相连，即线段在此处分叉
        /// </summary>
        public static bool LineCrossed(this IEnumerable<Line> self, Line line)
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