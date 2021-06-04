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

        #region Properties

        /// <summary>
        /// 面板的行数
        /// </summary>
        public int Rows { get; private set; }
        /// <summary>
        /// 面板的列数
        /// </summary>
        public int Columns { get; private set; }
        /// <summary>
        /// 面板数据
        /// </summary>
        public GameBlock[,] Board { get; private set; }
        /// <summary>
        /// 当前游戏是否已经完成
        /// </summary>
        public bool IsFinished { get; private set; }
        /// <summary>
        /// 背景色
        /// </summary>
        public Brush BackColor { get; set; }
        /// <summary>
        /// 线的粗细
        /// </summary>
        public double LineThickness { get; set; }
        /// <summary>
        /// 线的颜色
        /// </summary>
        public Brush LineBrush { get; set; }
        /// <summary>
        /// 点的大小(半径)
        /// </summary>
        public double DotSize { get; set; }
        /// <summary>
        /// 点的颜色
        /// </summary>
        public Brush DotBrush { get; set; }
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
        public Brush CrossBrush { get; set; }
        /// <summary>
        /// X的粗细
        /// </summary>
        public double CrossThickness { get; set; }
        /// <summary>
        /// 错误的颜色
        /// </summary>
        public Brush ErrorBrush { get; set; }
        /// <summary>
        /// 点击识别区域的百分比0~0.5
        /// </summary>
        public double ClickRegionPercent
        {
            get { return _ClickRegionPercent; }
            set { _ClickRegionPercent = value.RangeLimit(0, 0.5); }
        }

        #endregion

        #region Events

        /// <summary>
        /// 当前游戏完成时引发事件
        /// </summary>
        public event Action<SlitherlinkPanel> GameFinished;

        #endregion

        #region Private Fields

        /// <summary>
        /// 一个方格单元的大小
        /// </summary>
        private double _UnitSize;
        /// <summary>
        /// 历史步骤
        /// </summary>
        private readonly Stack<Step> _History;
        /// <summary>
        /// (相对于当前的)未来步骤
        /// </summary>
        private readonly Stack<Step> _Future;
        /// <summary>
        /// [属性字段]点击识别区域的百分比
        /// </summary>
        private double _ClickRegionPercent;

        #endregion

        #region Constructors

        /// <summary>
        /// 数回游戏面板
        /// </summary>
        public SlitherlinkPanel()
        {
            _History = new Stack<Step>();
            _Future = new Stack<Step>();

            Columns = 10;
            Rows = 10;
            BackColor = Brushes.AliceBlue;
            LineThickness = 4;
            LineBrush = Brushes.ForestGreen;
            DotSize = 3;
            DotBrush = Brushes.Black;
            //CrossSize = 10;
            CrossBrush = Brushes.ForestGreen;
            CrossThickness = 1;
            ErrorBrush = Brushes.Red;
            ClickRegionPercent = 0.4;
            
            InitializeComponent();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置面板大小
        /// </summary>
        public void SetBoardSize(int width, int height)
        {
            if (width > 50 || height > 50)
                throw new ArgumentException("board size too large");
            IsFinished = false;
            _History.Clear();
            _Future.Clear();
            Columns = width;
            Rows = height;
            NumberBoard.Children.Clear();
            NumberBoard.Columns = width;
            NumberBoard.Rows = height;
            Board = new GameBlock[Columns, Rows];
            Update();
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
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentException("str is null or whitespace", nameof(str));

            if (Regex.IsMatch(str, @"\d{1,2}x\d{1,2}t0:"))
            {
                LoadSTPPCFormat(str);
            }
            else
            {
                var lines = str.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
                SetBoardSize(lines[0].Length, lines.Length);
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Columns; j++)
                    {
                        var c = lines[i][j];
                        AppendNumber(char.IsDigit(c) ? c.ToString() : string.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// 更新重绘
        /// </summary>
        public virtual void UpdateWithSize(double width, double height)
        {
            _UnitSize = Math.Min(width / Columns, height / Rows);
            GameBlock.BlockSize = _UnitSize;

            GameCanvas.Children.Clear();
            GameCanvas.Width = NumberBoard.Width = _UnitSize * Columns;
            GameCanvas.Height = NumberBoard.Height = _UnitSize * Rows;

            for (int i = 0; i <= Columns; i++)
            {
                for (int j = 0; j <= Rows; j++)
                {
                    if (i < Columns && j < Rows && Board?[i, j] != null)
                    {
                        Board[i, j].UpdateEdgePos();
                        AddToBoard(Board[i, j].Lines.GetObjects());
                        AddToBoard(Board[i, j].Crosses.GetObjects());
                    }
                    AddGridDot(i, j);
                }
            }
        }
        /// <summary>
        /// 更新重绘
        /// </summary>
        public void Update()
        {
            UpdateWithSize(ActualWidth, ActualHeight);
        }

        /// <summary>
        /// 撤销步骤
        /// </summary>
        public void Undo()
        {
            if (_History.Count > 0)
            {
                var step = _History.Pop();
                if (step.IsAdd)
                {
                    if (step.IsLine)
                        BlockRemoveEdge(step.X, step.Y, step.EdgeFlags);
                    else
                        BlockRemoveCross(step.X, step.Y, step.EdgeFlags);
                }
                else
                {
                    if (step.IsLine)
                        BlockAddEdge(step.X, step.Y, step.EdgeFlags);
                    else
                        BlockAddCross(step.X, step.Y, step.EdgeFlags);
                }
                _Future.Push(step);
            }
        }

        /// <summary>
        /// 重做撤销步骤
        /// </summary>
        public void Redo()
        {
            if (_Future.Count > 0)
            {
                var step = _Future.Pop();
                if (step.IsAdd)
                {
                    if (step.IsLine)
                        BlockAddEdge(step.X, step.Y, step.EdgeFlags);
                    else
                        BlockAddCross(step.X, step.Y, step.EdgeFlags);
                }
                else
                {
                    if (step.IsLine)
                        BlockRemoveEdge(step.X, step.Y, step.EdgeFlags);
                    else
                        BlockRemoveCross(step.X, step.Y, step.EdgeFlags);
                }
                _History.Push(step);
            }
        }

        /// <summary>
        /// 添加控件到面板上
        /// </summary>
        public void AddToBoard(IEnumerable<UIElement> element)
        {
            foreach (var item in element)
            {
                AddToBoard(item);
            }
        }
        /// <summary>
        /// 添加控件到面板上
        /// </summary>
        public void AddToBoard(UIElement element)
        {
            if (element != null && !GameCanvas.Children.Contains(element))
            {
                GameCanvas.Children.Add(element);
            }
        }

        /// <summary>
        /// 从面板上移除控件
        /// </summary>
        public void RemoveFromBoard(UIElement element)
        {
            GameCanvas.Children.Remove(element);
        }

        /// <summary>
        /// 未完成时，检查是否已经完成
        /// </summary>
        public void CheckFinshed()
        {
            if (!IsFinished)
            {
                foreach (var block in Board)
                {
                    if (!block.IsCorrect())
                        return;
                }
                if (CheckSingleLoop())
                {
                    IsFinished = true;
                    GameFinished?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// 在格子的指定边上画线，或移除线(如存在)，或移除X(如存在)
        /// </summary>
        public void TryAddEdge(int x, int y, EdgeFlags edge)
        {
            if (x < 0 || y < 0 || x >= Columns || y >= Rows) return;

            if (Board[x, y].Crosses.EdgeFlags.HasFlag(edge))
            {
                BlockRemoveCross(x, y, edge);
                _History.Push(new Step(x, y, edge, false, false));
            }
            else
            {
                if (Board[x, y].Lines.EdgeFlags.HasFlag(edge))
                {
                    BlockRemoveEdge(x, y, edge);
                    _History.Push(new Step(x, y, edge, false, true));
                }
                else
                {
                    BlockAddEdge(x, y, edge);
                    _History.Push(new Step(x, y, edge, true, true));
                }
            }

            _Future.Clear();
        }

        /// <summary>
        /// 在格子的指定边上画X，或移除线(如存在)，或移除X(如存在)
        /// </summary>
        public void TryAddCross(int x, int y, EdgeFlags edge)
        {
            if (x < 0 || y < 0 || x >= Columns || y >= Rows) return;

            if (Board[x, y].Lines.EdgeFlags.HasFlag(edge))
            {
                BlockRemoveEdge(x, y, edge);
                _History.Push(new Step(x, y, edge, false, true));
            }
            else
            {
                if (Board[x, y].Crosses.EdgeFlags.HasFlag(edge))
                {
                    BlockRemoveCross(x, y, edge);
                    _History.Push(new Step(x, y, edge, false, false));
                }
                else
                {
                    BlockAddCross(x, y, edge);
                    _History.Push(new Step(x, y, edge, true, false));
                }
            }

            _Future.Clear();
        }

        /// <summary>
        /// 获得当前盘面，仅包括数字
        /// </summary>
        public virtual string GetBoardString()
        {
            if (Board == null) return string.Empty;

            var str = new StringBuilder();
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    str.Append(Board[x, y].StringValue);
                }
                str.AppendLine();
            }

            return str.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 加载STPPC格式的盘面
        /// </summary>
        private void LoadSTPPCFormat(string str)
        {
            int index = str.IndexOf('x');
            var width = Convert.ToInt32(str.Substring(0, index));
            str = str.Substring(index + 1);

            index = str.IndexOf("t0:");
            var height = Convert.ToInt32(str.Substring(0, index));
            str = str.Substring(index + 3);

            SetBoardSize(width, height);

            foreach (var c in str)
            {
                if (char.IsDigit(c))
                {
                    AppendNumber(c);
                }
                else
                {
                    for (int k = 0; k <= c - 'a'; k++)
                    {
                        AppendNumber(string.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// 在盘面上添加数字
        /// </summary>
        private void AppendNumber(string str)
        {
            var textBlock = new TextBlock
            {
                Text = str,
                FontFamily = FontFamily
            };
            var viewbox = new Viewbox
            {
                Child = textBlock
            };
            int x = NumberBoard.Children.Count % NumberBoard.Columns;
            int y = NumberBoard.Children.Count / NumberBoard.Columns;
            Board[x, y] = new GameBlock(x, y, textBlock);
            NumberBoard.Children.Add(viewbox);
        }
        /// <summary>
        /// 在盘面上添加数字
        /// </summary>
        private void AppendNumber(char c)
        {
            AppendNumber(c.ToString());
        }

        /// <summary>
        /// 添加格点
        /// </summary>
        private void AddGridDot(int i, int j)
        {
            var ellipse = new Ellipse
            {
                Width = 2 * DotSize,
                Height = 2 * DotSize,
                Fill = DotBrush
            };
            Canvas.SetLeft(ellipse, i * _UnitSize - DotSize);
            Canvas.SetTop(ellipse, j * _UnitSize - DotSize);
            Panel.SetZIndex(ellipse, 9999);
            AddToBoard(ellipse);
        }

        /// <summary>
        /// 获得所有线段
        /// </summary>
        private List<Line> GetAllLines()
        {
            return GameCanvas.Children.OfType<Line>().ToList();
        }

        /// <summary>
        /// 如果存在错误，高亮错误并返回<see langword="true"/>
        /// </summary>
        /// <returns></returns>
        private bool TryHighlightError()
        {
            bool hasError = false;


            foreach (var block in Board)
            {
                if (block.IsPreviewInvaild())
                {
                    block.Number.Foreground = ErrorBrush;
                    hasError = true;
                }
                else
                {
                    block.Number.Foreground = Brushes.Black;
                }
            }

            var allLines = GetAllLines();
            foreach (var line in allLines)
            {
                if (allLines.LineCrossed(line))
                {
                    line.Stroke = ErrorBrush;
                    hasError = true;
                }
                else
                {
                    line.Stroke = LineBrush;
                }
            }

            return hasError;
        }

        /// <summary>
        /// 检测是否是单一的回路
        /// </summary>
        private bool CheckSingleLoop()
        {
            var allLines = GetAllLines();

            var line = allLines.First();
            int crossCount = 2;
            while (allLines.Count > 1)
            {
                allLines.Remove(line);
                if (allLines.GetConnectedLine(line, out line) != crossCount)
                    return false;
                crossCount = 1;
            }
            return true;
        }

        /// <summary>
        /// 在指定位置处理点击效果
        /// </summary>
        /// <param name="point">相对于<see cref="GameCanvas"/>的位置</param>
        /// <param name="addMethod">TryAddEdge或TryAddCross</param>
        private void TryAddLineOrCross(Point point, Action<int, int, EdgeFlags> addMethod)
        {
            if (Board == null) return;
            var unitX = point.X / _UnitSize;
            var unitY = point.Y / _UnitSize;
            int x = (int)unitX;
            int y = (int)unitY;
            var marginX = unitX - x;
            var marginY = unitY - y;
            //左边界
            if (marginX < ClickRegionPercent)
            {
                if (marginY < marginX)
                    addMethod(x, y, EdgeFlags.Top);
                else if (marginY + marginX > 1)
                    addMethod(x, y, EdgeFlags.Bottom);
                else
                    addMethod(x, y, EdgeFlags.Left);
            }
            //右边界
            else if (marginX + ClickRegionPercent > 1)
            {
                if (marginY + marginX < 1)
                    addMethod(x, y, EdgeFlags.Top);
                else if (marginY > marginX)
                    addMethod(x, y, EdgeFlags.Bottom);
                else
                    addMethod(x, y, EdgeFlags.Right);
            }
            //上边界
            else if (marginY < ClickRegionPercent)
            {
                if (marginX < marginY)
                    addMethod(x, y, EdgeFlags.Left);
                else if (marginY + marginX > 1)
                    addMethod(x, y, EdgeFlags.Right);
                else
                    addMethod(x, y, EdgeFlags.Top);
            }
            //下边界
            else if (marginY + ClickRegionPercent > 1)
            {
                if (marginX + marginY < 1)
                    addMethod(x, y, EdgeFlags.Left);
                else if (marginX > marginY)
                    addMethod(x, y, EdgeFlags.Right);
                else
                    addMethod(x, y, EdgeFlags.Bottom);
            }
        }

        /// <summary>
        /// 在指定的位置添加线段
        /// </summary>
        private void BlockAddEdge(int x, int y, EdgeFlags edge)
        {
            var line = new Line
            {
                Stroke = LineBrush,
                StrokeThickness = LineThickness
            };
            Board[x, y].Lines.AddObject(line, edge);
            Board[x, y].UpdateEdgePos();
            AddToBoard(line);

            switch (edge)
            {
                case EdgeFlags.Top:
                    if (y - 1 >= 0)
                        Board[x, y - 1].Lines.AddObject(line, EdgeFlags.Bottom);
                    break;
                case EdgeFlags.Bottom:
                    if (y + 1 < Rows)
                        Board[x, y + 1].Lines.AddObject(line, EdgeFlags.Top);
                    break;
                case EdgeFlags.Left:
                    if (x - 1 >= 0)
                        Board[x - 1, y].Lines.AddObject(line, EdgeFlags.Right);
                    break;
                case EdgeFlags.Right:
                    if (x + 1 < Columns)
                        Board[x + 1, y].Lines.AddObject(line, EdgeFlags.Left);
                    break;
                default:
                    break;
            }

            if (!TryHighlightError())
            {
                CheckFinshed();
            }
        }

        /// <summary>
        /// 移除指定的位置的线段
        /// </summary>
        private void BlockRemoveEdge(int x, int y, EdgeFlags edge)
        {
            switch (edge)
            {
                case EdgeFlags.Top:
                    if (y - 1 >= 0)
                        Board[x, y - 1].Lines.RemoveObject(EdgeFlags.Bottom);
                    RemoveFromBoard(Board[x, y].Lines.Top);
                    break;
                case EdgeFlags.Bottom:
                    if (y + 1 < Rows)
                        Board[x, y + 1].Lines.RemoveObject(EdgeFlags.Top);
                    RemoveFromBoard(Board[x, y].Lines.Bottom);
                    break;
                case EdgeFlags.Left:
                    if (x - 1 >= 0)
                        Board[x - 1, y].Lines.RemoveObject(EdgeFlags.Right);
                    RemoveFromBoard(Board[x, y].Lines.Left);
                    break;
                case EdgeFlags.Right:
                    if (x + 1 < Columns)
                        Board[x + 1, y].Lines.RemoveObject(EdgeFlags.Left);
                    RemoveFromBoard(Board[x, y].Lines.Right);
                    break;
                default:
                    break;
            }
            Board[x, y].Lines.RemoveObject(edge);
            if (!TryHighlightError())
            {
                CheckFinshed();
            }
        }

        /// <summary>
        /// 在指定的位置添加X标记
        /// </summary>
        private void BlockAddCross(int x, int y, EdgeFlags edge)
        {
            var path = new Path
            {
                Data = Geometry.Parse(string.Format("M0,0L{0},{0}M0,{0}L{0},0", CrossSize)),
                Width = CrossSize,
                Height = CrossSize,
                Stroke = CrossBrush,
                StrokeThickness = CrossThickness
            };
            Board[x, y].Crosses.AddObject(path, edge);
            Board[x, y].UpdateEdgePos();
            AddToBoard(path);

            switch (edge)
            {
                case EdgeFlags.Top:
                    if (y - 1 >= 0)
                        Board[x, y - 1].Crosses.AddObject(path, EdgeFlags.Bottom);
                    break;
                case EdgeFlags.Bottom:
                    if (y + 1 < Rows)
                        Board[x, y + 1].Crosses.AddObject(path, EdgeFlags.Top);
                    break;
                case EdgeFlags.Left:
                    if (x - 1 >= 0)
                        Board[x - 1, y].Crosses.AddObject(path, EdgeFlags.Right);
                    break;
                case EdgeFlags.Right:
                    if (x + 1 < Columns)
                        Board[x + 1, y].Crosses.AddObject(path, EdgeFlags.Left);
                    break;
                default:
                    break;
            }

            TryHighlightError();
        }

        /// <summary>
        /// 移除指定的位置的X标记
        /// </summary>
        private void BlockRemoveCross(int x, int y, EdgeFlags edge)
        {
            switch (edge)
            {
                case EdgeFlags.Top:
                    if (y - 1 >= 0)
                        Board[x, y - 1].Crosses.RemoveObject(EdgeFlags.Bottom);
                    RemoveFromBoard(Board[x, y].Crosses.Top);
                    break;
                case EdgeFlags.Bottom:
                    if (y + 1 < Rows)
                        Board[x, y + 1].Crosses.RemoveObject(EdgeFlags.Top);
                    RemoveFromBoard(Board[x, y].Crosses.Bottom);
                    break;
                case EdgeFlags.Left:
                    if (x - 1 >= 0)
                        Board[x - 1, y].Crosses.RemoveObject(EdgeFlags.Right);
                    RemoveFromBoard(Board[x, y].Crosses.Left);
                    break;
                case EdgeFlags.Right:
                    if (x + 1 < Columns)
                        Board[x + 1, y].Crosses.RemoveObject(EdgeFlags.Left);
                    RemoveFromBoard(Board[x, y].Crosses.Right);
                    break;
                default:
                    break;
            }

            Board[x, y].Crosses.RemoveObject(edge);

            TryHighlightError();
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// 当尺寸改变时，更新重绘
        /// </summary>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWithSize(e.NewSize.Width, e.NewSize.Height);
        }

        /// <summary>
        /// 当左键点击面板时，尝试添加线段的操作
        /// </summary>
        private void GameBoard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TryAddLineOrCross(e.GetPosition(GameCanvas), TryAddEdge);
        }

        /// <summary>
        /// 当左键点击面板时，尝试添加X标记的操作
        /// </summary>
        private void GameBoard_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TryAddLineOrCross(e.GetPosition(GameCanvas), TryAddCross);
        }

        #endregion

    }

}
