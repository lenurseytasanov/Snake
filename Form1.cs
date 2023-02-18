using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using Timer = System.Windows.Forms.Timer;

namespace Snake
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public class Apple
    {
        public Point Position { get; }

        public Apple(List<Point> snakeBody)
        {
            var rand = new Random();
            Position = new Point(rand.Next(20), rand.Next(20));
            while (snakeBody.Contains(Position))
                Position = new Point(rand.Next(20), rand.Next(20));
        }
    }

    public class Snake
    {
        public List<Point> Body { get; }
        public Direction Dir { get; set; }

        public Snake()
        {
            Body = new List<Point>
            {
                new Point(4, 4),
                new Point(5, 4),
                new Point(6, 4)
            };
            Dir = Direction.Right;
        }
    }

    public class MainModel
    {
        public Snake S { get; private set; }
        public Apple A { get; private set; }
        public int Score { get; private set; }
        public int Record { get; private set; }
        public bool IsGamePaused { get; private set; }
        public bool IsGameOver { get; private set; }
        public Timer T { get; }

        public void PauseGame()
        {
            if (!IsGamePaused)
            {
                IsGamePaused = true;
                T.Stop();
            }
            else
            {
                IsGamePaused = false;
                T.Start();
            }

            StateChanged();
        }

        public void EatApple()
        {
            A = new Apple(S.Body);
            Score++;
        }

        private void TickHandler(object sender, EventArgs args)
        {
            var head = S.Body.Last();

            if (head == A.Position) EatApple();
            else S.Body.RemoveAt(0);

            switch (S.Dir)
            {
                case Direction.Up:
                    S.Body.Add(new Point(head.X, head.Y - 1));
                    break;
                case Direction.Down:
                    S.Body.Add(new Point(head.X, head.Y + 1));
                    break;
                case Direction.Left:
                    S.Body.Add(new Point(head.X - 1, head.Y));
                    break;
                case Direction.Right:
                    S.Body.Add(new Point(head.X + 1, head.Y));
                    break;
            }

            if (S.Body.Take(S.Body.Count - 1).Contains(S.Body.Last())
                || S.Body.Last().X < 0 || S.Body.Last().X >= 20
                || S.Body.Last().Y < 0 || S.Body.Last().Y >= 20)
            {
                GameOver();
            }

            if (StateChanged != null) StateChanged();
        }

        public void GameOver()
        {
            T.Stop();
            IsGameOver = true;
            if (Score > Record)
            {
                var writer = new StreamWriter("log.txt", false);
                writer.Write(Score.ToString());
                writer.Close();
                Record = Score;
            }
        }

        public void StartGame()
        {
            try
            {
                var reader = new StreamReader("log.txt");
                Record = int.Parse(reader.ReadLine());
                reader.Close();
            }
            catch (ArgumentNullException)
            {
                Record = 0;
            }
            
            Score = 0;
            IsGamePaused = false;
            IsGameOver = false;
            S = new Snake();
            A = new Apple(S.Body);
            T.Start();
        }

        public MainModel()
        {
            T = new Timer();
            T.Tick += TickHandler;
            T.Interval = 200;
            StartGame();
        }

        public Action StateChanged;
    }

    public partial class Form1 : Form
    {
        private const int CellsCount = 20;
        private const int WinSize = 600;
        private int CellSize = 30;
        private MainModel Model;

        private void PaintHandler(object sender, PaintEventArgs args)
        {
            var g = args.Graphics;
            BackColor = Color.LightGreen;
            g.SmoothingMode = SmoothingMode.HighQuality;

            g.DrawString($"Score: {Model.Score}",
                new Font("Arial", 40, FontStyle.Bold),
                Brushes.LightSeaGreen,
                new PointF(350, 15));
            g.DrawString($"Record: {Model.Record}",
                new Font("Arial", 40, FontStyle.Bold),
                Brushes.PaleVioletRed,
                new PointF(315, 65));
            g.DrawString("Esc - pause game",
                new Font("Arial", 20, FontStyle.Bold),
                Brushes.MediumSeaGreen,
                new PointF(10, 500));
            g.DrawString("R - restart",
                new Font("Arial", 20, FontStyle.Bold),
                Brushes.MediumSeaGreen,
                new PointF(10, 530));
            g.DrawString("˂, ˃, ˄, ˅ - change direction",
                new Font("Arial", 20, FontStyle.Bold),
                Brushes.MediumSeaGreen,
                new PointF(10, 560));

            if (Model.IsGameOver)
            {
                MessageBox.Show(
                    $"GAME OVER\nScore: {Model.Score}\nRecord: {Model.Record}", "",
                    MessageBoxButtons.OK);
                Model.StartGame();
                return;
            }
            g.FillEllipse(
            Brushes.Red,
                (int)(Model.A.Position.X + 0.5) * CellSize,
                (int)(Model.A.Position.Y + 0.5) * CellSize,
            CellSize, CellSize);
            foreach (var segment in Model.S.Body)
            {
                g.FillEllipse(
                Brushes.DarkGreen,
                    (int)(segment.X + 0.5) * CellSize,
                    (int)(segment.Y + 0.5) * CellSize,
                CellSize, CellSize);
            }

            if (Model.IsGamePaused)
            {
                g.FillRectangle(
                    new SolidBrush(Color.FromArgb(150, Color.Black)),
                    0, 0, 600, 600);
                g.DrawString("PAUSE",
                    new Font("Arial", 60, FontStyle.Bold),
                    Brushes.White,
                    new PointF(160, 250));
            }
        }

        private void KeyDownHandler(object sender, KeyEventArgs args)
        {
            var previous = Model.S.Body[Model.S.Body.Count - 2];
            var head = Model.S.Body.Last();
            var delta = new Size(head.X - previous.X, head.Y - previous.Y);
            switch (args.KeyCode)
            {
                case Keys.Up:
                    if (delta != new Size(0, 1)) Model.S.Dir = Direction.Up;
                    break;
                case Keys.Down:
                    if (delta != new Size(0, -1)) Model.S.Dir = Direction.Down;
                    break;
                case Keys.Right:
                    if (delta != new Size(-1, 0)) Model.S.Dir = Direction.Right;
                    break;
                case Keys.Left:
                    if (delta != new Size(1, 0)) Model.S.Dir = Direction.Left;
                    break;
                case Keys.Escape:
                    Model.PauseGame();
                    break;
                case Keys.R:
                    Model.StartGame();
                    break;
            }
        }

        public Form1()
        {
            InitializeComponent();
            ClientSize = new Size(WinSize, WinSize);
            Text = "Snake Game";
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            Model = new MainModel();
            Model.StateChanged = Invalidate;

            Paint += PaintHandler;
            KeyDown += KeyDownHandler;
        }
    }
}
