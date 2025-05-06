using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Polygonizer
{
    public partial class MainWindow : Window
    {
        const int CellSize = 5;
        double testY = 170;
        double testX = 530;
        public MainWindow()
        {
            InitializeComponent();
            DrawScene();
        }

        private void DrawScene()
        {
            var rectangles = new List<Rect>
            {
                new Rect(100, 100, 200, 150),
                new Rect(250, 200, 200, 150),
                new Rect(500, 100, 100, 80),
                new Rect(520, 130, 50, 50)
            };

            // Compute bounds
            Rect bounds = Rect.Empty;
            foreach (var r in rectangles)
                bounds.Union(r);

            int cols = (int)Math.Ceiling(bounds.Width / CellSize) + 2;
            int rows = (int)Math.Ceiling(bounds.Height / CellSize) + 2;
            bool[,] grid = new bool[rows, cols];

            foreach (var rect in rectangles)
            {
                int x0 = (int)((rect.X - bounds.X) / CellSize);
                int y0 = (int)((rect.Y - bounds.Y) / CellSize);
                int x1 = (int)Math.Ceiling((rect.Right - bounds.X) / CellSize);
                int y1 = (int)Math.Ceiling((rect.Bottom - bounds.Y) / CellSize);

                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                        grid[y, x] = true;
            }

            // Draw filled rectangles (for visual reference)
            foreach (var rect in rectangles)
            {
                var rectShape = new Rectangle
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Fill = Brushes.LightBlue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rectShape, rect.X);
                Canvas.SetTop(rectShape, rect.Y);
                MainCanvas.Children.Add(rectShape);
            }

            // Trace filled regions
            var regions = TraceFilledRegions(grid);

            // Convert point to grid coordinate

            int cy = (int)((testY - bounds.Y) / CellSize);
            int cx = (int)((testX - bounds.X) / CellSize);

            // Identify the region containing the point
            HashSet<(int, int)> region = null;
            foreach (var r in regions)
            {
                if (r.Contains((cx, cy)))
                {
                    region = r;
                    break;
                }
            }

            if (region == null)
            {
                Title = "Point not in any filled region.";
                return;
            }

            // Draw perimeter of the region
            var polygon = TracePerimeter(grid, region, bounds);
            var polyShape = new Polygon
            {
                Stroke = Brushes.Red,
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Points = new PointCollection(polygon)
            };
            MainCanvas.Children.Add(polyShape);

            // Compute bounds in region for measurement
            double hMin = double.MaxValue, hMax = double.MinValue;
            double vMin = double.MaxValue, vMax = double.MinValue;

            foreach (var (x, y) in region)
            {
                if (y == cy)
                {
                    double xCanvas = bounds.X + x * CellSize;
                    hMin = Math.Min(hMin, xCanvas);
                    hMax = Math.Max(hMax, xCanvas);
                }
                if (x == cx)
                {
                    double yCanvas = bounds.Y + y * CellSize;
                    vMin = Math.Min(vMin, yCanvas);
                    vMax = Math.Max(vMax, yCanvas);
                }
            }

            // Draw horizontal measurement line
            if (hMin <= hMax)
            {
                var hLine = new Line
                {
                    X1 = hMin,
                    X2 = hMax + CellSize,
                    Y1 = testY,
                    Y2 = testY,
                    Stroke = Brushes.Green,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                MainCanvas.Children.Add(hLine);
            }

            // Draw vertical measurement line
            if (vMin <= vMax)
            {
                var vLine = new Line
                {
                    X1 = testX,
                    X2 = testX,
                    Y1 = vMin,
                    Y2 = vMax + CellSize,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                MainCanvas.Children.Add(vLine);
            }

            double width = hMax - hMin + CellSize;
            double height = vMax - vMin + CellSize;
            Title = $"Width at Y={testY}: {width}px, Height at X={testX}: {height}px";
        }

        private List<HashSet<(int, int)>> TraceFilledRegions(bool[,] grid)
        {
            bool[,] visited = new bool[grid.GetLength(0), grid.GetLength(1)];
            List<HashSet<(int, int)>> regions = new List<HashSet<(int, int)>>();

            for (int y = 0; y < grid.GetLength(0); y++)
            {
                for (int x = 0; x < grid.GetLength(1); x++)
                {
                    if (grid[y, x] && !visited[y, x])
                    {
                        regions.Add(FloodFill(grid, visited, x, y));
                    }
                }
            }

            return regions;
        }

        private HashSet<(int, int)> FloodFill(bool[,] grid, bool[,] visited, int startX, int startY)
        {
            var region = new HashSet<(int, int)>();
            var queue = new Queue<(int, int)>();
            int rows = grid.GetLength(0), cols = grid.GetLength(1);

            queue.Enqueue((startX, startY));
            visited[startY, startX] = true;

            int[,] dirs = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                region.Add((x, y));

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dirs[i, 0];
                    int ny = y + dirs[i, 1];

                    if (nx >= 0 && nx < cols && ny >= 0 && ny < rows && grid[ny, nx] && !visited[ny, nx])
                    {
                        visited[ny, nx] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return region;
        }

        private List<Point> TracePerimeter(bool[,] grid, HashSet<(int, int)> region, Rect bounds)
        {
            List<Point> perimeter = new List<Point>();
            int[,] directions = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };

            (int x, int y) = (-1, -1);
            foreach (var cell in region)
            {
                x = cell.Item1;
                y = cell.Item2;
                break;
            }

            if (x == -1) return perimeter;

            int startX = x, startY = y, dir = 0;
            perimeter.Add(ToCanvasPoint(x, y, bounds));

            int cx = x, cy = y;
            do
            {
                int leftDir = (dir + 3) % 4;
                int nx = cx + directions[leftDir, 0];
                int ny = cy + directions[leftDir, 1];

                if (region.Contains((nx, ny)))
                {
                    dir = leftDir;
                    cx = nx;
                    cy = ny;
                    perimeter.Add(ToCanvasPoint(cx, cy, bounds));
                }
                else
                {
                    dir = (dir + 1) % 4;
                }
            }
            while (!(cx == startX && cy == startY && perimeter.Count > 1));

            return perimeter;
        }

        private Point ToCanvasPoint(int x, int y, Rect bounds)
        {
            return new Point(bounds.X + x * CellSize, bounds.Y + y * CellSize);
        }
    }
}
