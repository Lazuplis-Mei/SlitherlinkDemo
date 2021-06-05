using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

namespace SlitherlinkDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        readonly Random _random = new Random();
        string[] _loadedGames;
        SaveFileDialog saveFileDlg = new SaveFileDialog();
        OpenFileDialog openFileDlg = new OpenFileDialog();
        private void LoadRandomGame()
        {
            GameBoard.LoadNumbers(_loadedGames?[_random.Next(_loadedGames.Length)]);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("games.txt"))
            {
                _loadedGames = File.ReadAllLines("games.txt");
                LoadRandomGame();
            }
        }

        private void GameBoard_GameFinished(SlitherlinkControl.SlitherlinkPanel obj)
        {
            MessageBox.Show("完成!", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            if (e.Key == Key.N)
            {
                if (ctrl)
                {
                    var inputDlg = new InputDialog();
                    inputDlg.TBInput.Text = GameBoard.GetBoardString();
                    if (inputDlg.ShowDialog()==true)
                    {
                        GameBoard.LoadNumbers(inputDlg.TBInput.Text);
                    }
                }
                else
                {
                    LoadRandomGame();
                }
            }
            else if(e.Key == Key.Z && ctrl)
            {
                GameBoard.Undo();
            }
            else if (e.Key == Key.Y && ctrl)
            {
                GameBoard.Redo();
            }
            else if (e.Key == Key.S && ctrl)
            {
                if (saveFileDlg.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDlg.FileName, GameBoard.GetBoard());
                }
            }
            else if (e.Key == Key.L && ctrl)
            {
                if (openFileDlg.ShowDialog() == true)
                {
                    GameBoard.SetBoard(File.ReadAllBytes(openFileDlg.FileName));
                }
            }
        }
    }

}
