using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SlitherlinkControl
{
    /// <summary>
    /// SlitherlinkControl.xaml 的交互逻辑
    /// </summary>
    public partial class SlitherlinkPanel : UserControl
    {
        public int Columns { get; private set; } = 10;
        public int Rows { get; private set; } = 10;
        public GameBlock[,] Board { get; private set; }
        public bool Finished { get; private set; }

        /// <summary>
        /// 背景色
        /// </summary>
        public Brush BackColor { get; set; } = Brushes.AliceBlue;

        /// <summary>
        /// 线的粗细
        /// </summary>
        public double LineThickness { get; set; } = 4;

        /// <summary>
        /// 线的颜色
        /// </summary>
        public Brush LineBrush { get; set; } = Brushes.ForestGreen;

        /// <summary>
        /// 点的大小(半径)
        /// </summary>
        public double DotSize { get; set; } = 3;

        /// <summary>
        /// 点的颜色
        /// </summary>
        public Brush DotBrush { get; set; } = Brushes.Black;

        /// <summary>
        /// X的大小
        /// </summary>
        public double CrossSize
        {
            get => GameBlock.HalfCrossSize * 2;
            set => GameBlock.HalfCrossSize = value / 2;
        }

        /// <summary>
        /// X的颜色
        /// </summary>
        public Brush CrossBrush { get; set; } = Brushes.ForestGreen;

        /// <summary>
        /// X的粗细
        /// </summary>
        public double CrossThickness { get; set; } = 1;

        /// <summary>
        /// 错误的颜色
        /// </summary>
        public Brush ErrorBrush { get; set; } = Brushes.Red;

        private double _cRP = 0.4;

        /// <summary>
        /// 点击边线区域的百分比0~0.5
        /// </summary>
        public double ClickRegionPercent
        {
            get { return _cRP; }
            set { _cRP = Math.Min(Math.Max(0, value), 0.5); }
        }

        public event Action<SlitherlinkPanel> GameFinished;
        
        private double _unitSize;

        private readonly Stack<Step> _history = new Stack<Step>();
        private readonly Stack<Step> _future = new Stack<Step>();

        public SlitherlinkPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置面板大小
        /// </summary>
        public void SetBoardSize(int width, int height)
        {
            Finished = false;
            _history.Clear();
            _future.Clear();
            Columns = width;
            Rows = height;
            NumberBoard.Children.Clear();
            NumberBoard.Columns = width;
            NumberBoard.Rows = height;
            Board = new GameBlock[Columns, Rows];
            UpdateWithSize(ActualWidth, ActualHeight);
        }
        /// <summary>
        /// 设置正方形面板大小
        /// </summary>
        public void SetBoardSize(int size)
        {
            SetBoardSize(size, size);
        }

        /// <summary>
        /// 加载盘面
        /// </summary>
        public void LoadNumbers(sbyte[,] buffer)
        {

            SetBoardSize(buffer.GetLength(1), buffer.GetLength(0));
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    var n = buffer[i, j];
                    AppendNumber(n >= 0 ? n.ToString() : string.Empty);
                }
            }
        }

        /// <summary>
        /// 加载盘面
        /// </summary>
        public virtual void LoadNumbers(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return;

            if (Regex.IsMatch(str, @"\d{1,2}x\d{1,2}t0:"))
            {
                int si = str.IndexOf('x');
                var w = Convert.ToInt32(str.Substring(0, si));
                str = str.Substring(si + 1);

                si = str.IndexOf("t0:");
                var h = Convert.ToInt32(str.Substring(0, si));
                str = str.Substring(si + 3);

                SetBoardSize(w, h);
                foreach (var c in str)
                {
                    var n = c.ToString();
                    if ("0123".Contains(n))
                    {
                        AppendNumber(n);
                    }
                    else
                    {
                        var count = c - 'a';
                        for (int k = 0; k <= count; k++)
                        {
                            AppendNumber(string.Empty);
                        }
                    }
                }
            }
            else
            {
                var lines = str.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                SetBoardSize(lines[0].Length, lines.Length);
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Columns; j++)
                    {
                        var n = lines[i][j].ToString();
                        AppendNumber("0123".Contains(n) ? n : string.Empty);
                    }
                }
            }
        }

        private void AppendNumber(string str)
        {
            var vbox = new Viewbox();
            var text = new TextBlock();
            text.Text = str;
            text.FontFamily = FontFamily;
            vbox.Child = text;
            int i = NumberBoard.Children.Count % NumberBoard.Columns;
            int j = NumberBoard.Children.Count / NumberBoard.Columns;
            Board[i, j] = new GameBlock(i, j, text);
            NumberBoard.Children.Add(vbox);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWithSize(e.NewSize.Width, e.NewSize.Height);
        }

        /// <summary>
        /// 更新重绘
        /// </summary>
        public virtual void UpdateWithSize(double width, double height)
        {
            _unitSize = Math.Min(width / Columns, height / Rows);
            GameBlock.BlockSize = _unitSize;
            GameBoard.Children.Clear();
            GameBoard.Width = NumberBoard.Width = _unitSize * Columns;
            GameBoard.Height = NumberBoard.Height = _unitSize * Rows;
            for (int i = 0; i <= Columns; i++)
            {
                for (int j = 0; j <= Rows; j++)
                {
                    if (i < Columns && j < Rows && Board?[i, j] != null)
                    {
                        AddLineAndCross(i, j);
                    }
                    AddGridDot(i, j);
                }
            }
        }

        private void AddLineAndCross(int i, int j)
        {
            Board[i, j].UpdateEdgePos();
            AddToBoard(Board[i, j].EdgeLine.Top);
            AddToBoard(Board[i, j].EdgeLine.Bottom);
            AddToBoard(Board[i, j].EdgeLine.Left);
            AddToBoard(Board[i, j].EdgeLine.Right);
            AddToBoard(Board[i, j].CrossPath.Top);
            AddToBoard(Board[i, j].CrossPath.Bottom);
            AddToBoard(Board[i, j].CrossPath.Left);
            AddToBoard(Board[i, j].CrossPath.Right);
        }

        private void AddGridDot(int i, int j)
        {
            var ellipse = new Ellipse
            {
                Width = 2 * DotSize,
                Height = 2 * DotSize,
                Fill = DotBrush
            };
            Canvas.SetLeft(ellipse, i * _unitSize - DotSize);
            Canvas.SetTop(ellipse, j * _unitSize - DotSize);
            Panel.SetZIndex(ellipse, 9999);
            AddToBoard(ellipse);
        }

        /// <summary>
        /// 撤销
        /// </summary>
        public void Undo()
        {
            if (_history.Count > 0)
            {
                var step = _history.Pop();
                if (step.IsAdd)
                {
                    if (step.IsLine)
                        BlockRemoveEdge(step.X, step.Y, step.Edge, false);
                    else
                        BlockRemoveCross(step.X, step.Y, step.Edge, false);
                }
                else
                {
                    if (step.IsLine)
                        BlockAddEdge(step.X, step.Y, step.Edge, false);
                    else
                        BlockAddCross(step.X, step.Y, step.Edge, false);
                }
                _future.Push(step);
            }
        }

        /// <summary>
        /// 重做撤销
        /// </summary>
        public void Redo()
        {
            if (_future.Count > 0)
            {
                var step = _future.Pop();
                if (step.IsAdd)
                {
                    if (step.IsLine)
                        BlockAddEdge(step.X, step.Y, step.Edge, false);
                    else
                        BlockAddCross(step.X, step.Y, step.Edge, false);
                }
                else
                {
                    if (step.IsLine)
                        BlockRemoveEdge(step.X, step.Y, step.Edge, false);
                    else
                        BlockRemoveCross(step.X, step.Y, step.Edge, false);
                }
                _history.Push(step);
            }
        }

        /// <summary>
        /// 添加控件到面板上，并高亮错误数字<br/>
        /// 全部完成时引发事件<see cref="GameFinished"/>
        /// </summary>
        public void AddToBoard(UIElement element)
        {
            if (element != null && !GameBoard.Children.Contains(element))
            {
                GameBoard.Children.Add(element);
                if (!Finished)
                {
                    if (element is Line || element is Path)
                    {
                        if (!TryHighlightError())
                        {
                            if (element is Line && CheckFinshed())
                            {
                                GameFinished?.Invoke(this);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public void RemoveFromBoard(UIElement element)
        {
            GameBoard.Children.Remove(element);
            if (!Finished)
            {
                if (element is Line || element is Path)
                {
                    if (!TryHighlightError())
                    {
                        if (element is Line && CheckFinshed())
                        {
                            GameFinished?.Invoke(this);
                            return;
                        }
                    }
                }
            }
        }

        private bool TryHighlightError()
        {
            bool haserror = false;
            HashSet<Line> allLines = new HashSet<Line>();

            foreach (var block in Board)
            {
                if (block.IsPreviewInvaild())
                {
                    block.NumberText.Foreground = ErrorBrush;
                    haserror = true;
                }
                else
                {
                    block.NumberText.Foreground = Brushes.Black;
                }
                if (block.Edge != EdgeFlags.None)
                {
                    if (block.Edge.HasFlag(EdgeFlags.Top))
                        allLines.Add(block.EdgeLine.Top);
                    if (block.Edge.HasFlag(EdgeFlags.Bottom))
                        allLines.Add(block.EdgeLine.Bottom);
                    if (block.Edge.HasFlag(EdgeFlags.Left))
                        allLines.Add(block.EdgeLine.Left);
                    if (block.Edge.HasFlag(EdgeFlags.Right))
                        allLines.Add(block.EdgeLine.Right);
                }
            }

            foreach (var line in allLines)
            {
                if (allLines.LineCrossed(line))
                {
                    line.Stroke = ErrorBrush;
                    haserror = true;
                }
                else
                {
                    line.Stroke = LineBrush;
                }
            }

            return haserror;
        }

        /// <summary>
        /// 检查是否已经完成
        /// </summary>
        public bool CheckFinshed()
        {
            HashSet<Line> allLines = new HashSet<Line>();
            foreach (var block in Board)
            {
                if (!block.IsVaild())
                    return false;
                if (block.Edge != EdgeFlags.None)
                {
                    if (block.Edge.HasFlag(EdgeFlags.Top))
                        allLines.Add(block.EdgeLine.Top);
                    if (block.Edge.HasFlag(EdgeFlags.Bottom))
                        allLines.Add(block.EdgeLine.Bottom);
                    if (block.Edge.HasFlag(EdgeFlags.Left))
                        allLines.Add(block.EdgeLine.Left);
                    if (block.Edge.HasFlag(EdgeFlags.Right))
                        allLines.Add(block.EdgeLine.Right);
                }
            }
            return CheckSingleLoopFinished(allLines);
        }

        private bool CheckSingleLoopFinished(HashSet<Line> allLines)
        {
            Line line = allLines.First();
            int crossCount = 2;
            while (allLines.Count > 1)
            {
                allLines.Remove(line);
                if (allLines.GetConnectedLine(line, out line) != crossCount)
                    return false;
                crossCount = 1;
            }
            return Finished = true;
        }

        private void GameBoard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Board == null) return;
            var point = e.GetPosition(GameBoard);
            var unitX = point.X / _unitSize;
            var unitY = point.Y / _unitSize;
            int x = (int)unitX;
            int y = (int)unitY;
            var marginX = unitX - x;
            var marginY = unitY - y;
            //left side
            if (marginX < ClickRegionPercent)
            {
                if (marginY < marginX)
                    TryAddEdge(x, y, EdgeFlags.Top);
                else if (marginY + marginX > 1)
                    TryAddEdge(x, y, EdgeFlags.Bottom);
                else
                    TryAddEdge(x, y, EdgeFlags.Left);
            }
            //right side
            else if (marginX + ClickRegionPercent > 1)
            {
                if (marginY + marginX < 1)
                    TryAddEdge(x, y, EdgeFlags.Top);
                else if (marginY > marginX)
                    TryAddEdge(x, y, EdgeFlags.Bottom);
                else
                    TryAddEdge(x, y, EdgeFlags.Right);
            }
            //top side
            else if (marginY < ClickRegionPercent)
            {
                if (marginX < marginY)
                    TryAddEdge(x, y, EdgeFlags.Left);
                else if (marginY + marginX > 1)
                    TryAddEdge(x, y, EdgeFlags.Right);
                else
                    TryAddEdge(x, y, EdgeFlags.Top);
            }
            //bottom side
            else if (marginY+ ClickRegionPercent > 1)
            {
                if (marginX + marginY < 1)
                    TryAddEdge(x, y, EdgeFlags.Left);
                else if (marginX > marginY)
                    TryAddEdge(x, y, EdgeFlags.Right);
                else
                    TryAddEdge(x, y, EdgeFlags.Bottom);
            }
        }

        /// <summary>
        /// 在格子的指定边上画线，或移除线(如存在)，或移除X(如存在)
        /// </summary>
        public void TryAddEdge(int x, int y, EdgeFlags edge)
        {
            if (x < 0 || y < 0 || x >= Columns || y >= Rows) return;

            if (Board[x, y].Cross.HasFlag(edge))
            {
                BlockRemoveCross(x, y, edge);
            }
            else
            {
                if (Board[x, y].Edge.HasFlag(edge))
                {
                    BlockRemoveEdge(x, y, edge);
                }
                else
                {
                    BlockAddEdge(x, y, edge);
                }
            }
        }

        private void BlockAddEdge(int x, int y, EdgeFlags edge, bool rem = true)
        {
            var line = new Line();
            switch (edge)
            {
                case EdgeFlags.Top:
                    Board[x, y].EdgeLine.Top = line;
                    if (y - 1 >= 0)
                    {
                        Board[x, y - 1].Edge |= EdgeFlags.Bottom;
                        Board[x, y - 1].EdgeLine.Bottom = line;
                    }
                    break;
                case EdgeFlags.Bottom:
                    Board[x, y].EdgeLine.Bottom = line;
                    if (y + 1 < Rows)
                    {
                        Board[x, y + 1].Edge |= EdgeFlags.Top;
                        Board[x, y + 1].EdgeLine.Top = line;
                    }
                    break;
                case EdgeFlags.Left:
                    Board[x, y].EdgeLine.Left = line;
                    if (x - 1 >= 0)
                    {
                        Board[x - 1, y].Edge |= EdgeFlags.Right;
                        Board[x - 1, y].EdgeLine.Right = line;
                    }
                    break;
                case EdgeFlags.Right:
                    Board[x, y].EdgeLine.Right = line;
                    if (x + 1 < Columns)
                    {
                        Board[x + 1, y].Edge |= EdgeFlags.Left;
                        Board[x + 1, y].EdgeLine.Left = line;
                    }
                    break;
                default:
                    break;
            }
            Board[x, y].Edge |= edge;
            Board[x, y].UpdateEdgePos();
            line.Stroke = LineBrush;
            line.StrokeThickness = LineThickness;
            AddToBoard(line);
            if (rem)
            {
                _history.Push(new Step(x, y, edge, true, true));
                _future.Clear();
            }
        }

        private void BlockRemoveCross(int x, int y, EdgeFlags edge, bool rem = true)
        {
            Board[x, y].Cross &= ~edge;
            if (rem)
            {
                _history.Push(new Step(x, y, edge, false, false));
                _future.Clear();
            }
            switch (edge)
            {
                case EdgeFlags.Top:
                    if (y - 1 >= 0)
                    {
                        Board[x, y - 1].Cross &= ~EdgeFlags.Bottom;
                        Board[x, y - 1].CrossPath.Bottom = null;
                    }
                    RemoveFromBoard(Board[x, y].CrossPath.Top);
                    Board[x, y].CrossPath.Top = null;
                    break;
                case EdgeFlags.Bottom:
                    if (y + 1 < Rows)
                    {
                        Board[x, y + 1].Cross &= ~EdgeFlags.Top;
                        Board[x, y + 1].CrossPath.Top = null;
                    }
                    RemoveFromBoard(Board[x, y].CrossPath.Bottom);
                    Board[x, y].CrossPath.Bottom = null;
                    break;
                case EdgeFlags.Left:
                    if (x - 1 >= 0)
                    {
                        Board[x - 1, y].Cross &= ~EdgeFlags.Right;
                        Board[x - 1, y].CrossPath.Right = null;
                    }
                    RemoveFromBoard(Board[x, y].CrossPath.Left);
                    Board[x, y].CrossPath.Left = null;
                    break;
                case EdgeFlags.Right:
                    if (x + 1 < Columns)
                    {
                        Board[x + 1, y].Cross &= ~EdgeFlags.Left;
                        Board[x + 1, y].CrossPath.Left = null;
                    }
                    RemoveFromBoard(Board[x, y].CrossPath.Right);
                    Board[x, y].CrossPath.Right = null;
                    break;
                default:
                    break;
            }
        }

        private void GameBoard_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Board == null) return;
            var point = e.GetPosition(GameBoard);
            var unitX = point.X / _unitSize;
            var unitY = point.Y / _unitSize;
            int x = (int)unitX;
            int y = (int)unitY;
            var marginX = unitX - x;
            var marginY = unitY - y;
            //left side
            if (marginX < ClickRegionPercent)
            {
                if (marginY < marginX)
                    TryAddCross(x, y, EdgeFlags.Top);
                else if (marginY + marginX > 1)
                    TryAddCross(x, y, EdgeFlags.Bottom);
                else
                    TryAddCross(x, y, EdgeFlags.Left);
            }
            //right side
            else if (marginX + ClickRegionPercent > 1)
            {
                if (marginY + marginX < 1)
                    TryAddCross(x, y, EdgeFlags.Top);
                else if (marginY > marginX)
                    TryAddCross(x, y, EdgeFlags.Bottom);
                else
                    TryAddCross(x, y, EdgeFlags.Right);
            }
            //top side
            else if (marginY < ClickRegionPercent)
            {
                if (marginX < marginY)
                    TryAddCross(x, y, EdgeFlags.Left);
                else if (marginY + marginX > 1)
                    TryAddCross(x, y, EdgeFlags.Right);
                else
                    TryAddCross(x, y, EdgeFlags.Top);
            }
            //bottom side
            else if (marginY + ClickRegionPercent > 1)
            {
                if (marginX + marginY < 1)
                    TryAddCross(x, y, EdgeFlags.Left);
                else if (marginX > marginY)
                    TryAddCross(x, y, EdgeFlags.Right);
                else
                    TryAddCross(x, y, EdgeFlags.Bottom);
            }
        }

        /// <summary>
        /// 在格子的指定边上画X，或移除线(如存在)，或移除X(如存在)
        /// </summary>
        public void TryAddCross(int x, int y, EdgeFlags edge)
        {
            if (x < 0 || y < 0 || x >= Columns || y >= Rows) return;

            if (Board[x, y].Edge.HasFlag(edge))
            {
                BlockRemoveEdge(x, y, edge);
            }
            else
            {
                if (Board[x, y].Cross.HasFlag(edge))
                {
                    BlockRemoveCross(x, y, edge);
                }
                else
                {
                    BlockAddCross(x, y, edge);
                }
            }
        }

        private void BlockAddCross(int x, int y, EdgeFlags edge, bool rem = true)
        {
            var path = new Path();
            path.Data = Geometry.Parse($"M0,0L{CrossSize},{CrossSize}M0,{CrossSize}L{CrossSize},0");
            path.Width = path.Height = CrossSize;
            switch (edge)
            {
                case EdgeFlags.Top:
                    Board[x, y].CrossPath.Top = path;
                    if (y - 1 >= 0)
                    {
                        Board[x, y - 1].Cross |= EdgeFlags.Bottom;
                        Board[x, y - 1].CrossPath.Bottom = path;
                    }
                    break;
                case EdgeFlags.Bottom:
                    Board[x, y].CrossPath.Bottom = path;
                    if (y + 1 < Rows)
                    {
                        Board[x, y + 1].Cross |= EdgeFlags.Top;
                        Board[x, y + 1].CrossPath.Top = path;
                    }
                    break;
                case EdgeFlags.Left:
                    Board[x, y].CrossPath.Left = path;
                    if (x - 1 >= 0)
                    {
                        Board[x - 1, y].Cross |= EdgeFlags.Right;
                        Board[x - 1, y].CrossPath.Right = path;
                    }
                    break;
                case EdgeFlags.Right:
                    Board[x, y].CrossPath.Right = path;
                    if (x + 1 < Columns)
                    {
                        Board[x + 1, y].Cross |= EdgeFlags.Left;
                        Board[x + 1, y].CrossPath.Left = path;
                    }
                    break;
                default:
                    break;
            }
            Board[x, y].Cross |= edge;
            Board[x, y].UpdateEdgePos();
            path.Stroke = CrossBrush;
            path.StrokeThickness = CrossThickness;
            AddToBoard(path);
            if (rem)
            {
                _history.Push(new Step(x, y, edge, true, false));
                _future.Clear();
            }
        }

        private void BlockRemoveEdge(int x, int y, EdgeFlags edge, bool rem = true)
        {
            Board[x, y].Edge &= ~edge;
            if (rem)
            {
                _history.Push(new Step(x, y, edge, false, true));
                _future.Clear();
            }
            switch (edge)
            {
                case EdgeFlags.Top:
                    if (y - 1 >= 0)
                    {
                        Board[x, y - 1].Edge &= ~EdgeFlags.Bottom;
                        Board[x, y - 1].EdgeLine.Bottom = null;
                    }
                    RemoveFromBoard(Board[x, y].EdgeLine.Top);
                    Board[x, y].EdgeLine.Top = null;
                    break;
                case EdgeFlags.Bottom:
                    if (y + 1 < Rows)
                    {
                        Board[x, y + 1].Edge &= ~EdgeFlags.Top;
                        Board[x, y + 1].EdgeLine.Top = null;
                    }
                    RemoveFromBoard(Board[x, y].EdgeLine.Bottom);
                    Board[x, y].EdgeLine.Bottom = null;
                    break;
                case EdgeFlags.Left:
                    if (x - 1 >= 0)
                    {
                        Board[x - 1, y].Edge &= ~EdgeFlags.Right;
                        Board[x - 1, y].EdgeLine.Right = null;
                    }
                    RemoveFromBoard(Board[x, y].EdgeLine.Left);
                    Board[x, y].EdgeLine.Left = null;
                    break;
                case EdgeFlags.Right:
                    if (x + 1 < Columns)
                    {
                        Board[x + 1, y].Edge &= ~EdgeFlags.Left;
                        Board[x + 1, y].EdgeLine.Left = null;
                    }
                    RemoveFromBoard(Board[x, y].EdgeLine.Right);
                    Board[x, y].EdgeLine.Right = null;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 获得当前盘面，仅包括数字
        /// </summary>
        public virtual string GetBoardString()
        {
            if (Board == null) return string.Empty;

            var str = new StringBuilder();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    str.Append(Board[j, i].StringValue);
                }
                str.AppendLine();
            }
            return str.ToString();
        }
    }

}
