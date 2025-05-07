using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace Polygonizer
{
    public partial class MainWindow : Window
    {
        const int CellSize = 5;
        const int Padding = 2;

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

            // Compute bounds with padding
            Rect bounds = Rect.Empty;
            foreach (var r in rectangles)
                bounds.Union(r);
            bounds.Inflate(Padding, Padding);

            int cols = (int)Math.Ceiling(bounds.Width / CellSize);
            int rows = (int)Math.Ceiling(bounds.Height / CellSize);
            bool[,] grid = new bool[rows, cols];
            bool[,] visited = new bool[rows, cols];

            // Fill grid
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

            // Draw filled rectangles
            foreach (var rect in rectangles)
            {
                var fill = new Rectangle
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Fill = Brushes.LightBlue
                };
                Canvas.SetLeft(fill, rect.X);
                Canvas.SetTop(fill, rect.Y);
                MainCanvas.Children.Add(fill);
            }

            // Flood fill to find all distinct regions
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (grid[y, x] && !visited[y, x])
                    {
                        var region = new List<(int x, int y)>();
                        FloodFill(grid, visited, x, y, region);
                        var polygon = TracePerimeter(region, grid, bounds);
                        if (polygon.Count > 1)
                        {
                            var poly = new Polygon
                            {
                                Stroke = Brushes.Red,
                                StrokeThickness = 1,
                                Fill = Brushes.Transparent,
                                Points = new PointCollection(polygon)
                            };
                            MainCanvas.Children.Add(poly);
                        }
                    }
                }
            }

            // Optional: black outlines on individual rectangles
            foreach (var rect in rectangles)
            {
                var outline = new Rectangle
                {
                    Width = rect.Width + 1,
                    Height = rect.Height + 1,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(outline, rect.X - 0.5);
                Canvas.SetTop(outline, rect.Y - 0.5);
                MainCanvas.Children.Add(outline);
            }
        }

        private void FloodFill(bool[,] grid, bool[,] visited, int x, int y, List<(int x, int y)> region)
        {
            int rows = grid.GetLength(0), cols = grid.GetLength(1);
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((x, y));

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                if (cx < 0 || cy < 0 || cx >= cols || cy >= rows) continue;
                if (!grid[cy, cx] || visited[cy, cx]) continue;

                visited[cy, cx] = true;
                region.Add((cx, cy));

                queue.Enqueue((cx + 1, cy));
                queue.Enqueue((cx - 1, cy));
                queue.Enqueue((cx, cy + 1));
                queue.Enqueue((cx, cy - 1));
            }
        }

        private List<Point> TracePerimeter(List<(int x, int y)> region, bool[,] grid, Rect bounds)
        {
            var hash = new HashSet<(int, int)>(region);
            var points = new List<Point>();
            var dirs = new (int dx, int dy)[] { (1, 0), (0, 1), (-1, 0), (0, -1) };

            var start = region.Find(cell => IsEdge(grid, cell.x, cell.y));
            int x = start.x, y = start.y, dir = 0;
            var startState = (x, y, dir);
            bool first = true;

            Point ToPoint(int gx, int gy) => new Point(bounds.X + gx * CellSize, bounds.Y + gy * CellSize);

            while (first || (x, y, dir) != startState)
            {
                first = false;
                points.Add(ToPoint(x, y));

                int leftDir = (dir + 3) % 4;
                int lx = x + dirs[leftDir].dx;
                int ly = y + dirs[leftDir].dy;

                if (hash.Contains((lx, ly)))
                {
                    dir = leftDir;
                    x = lx;
                    y = ly;
                }
                else
                {
                    int fx = x + dirs[dir].dx;
                    int fy = y + dirs[dir].dy;

                    if (hash.Contains((fx, fy)))
                    {
                        x = fx;
                        y = fy;
                    }
                    else
                    {
                        dir = (dir + 1) % 4;
                    }
                }

                if (points.Count > 10000) break;
            }

            points.Add(points[0]); // Close loop
            return points;
        }

        private bool IsEdge(bool[,] grid, int x, int y)
        {
            return !GetSafe(grid, x - 1, y) || !GetSafe(grid, x + 1, y) ||
                   !GetSafe(grid, x, y - 1) || !GetSafe(grid, x, y + 1);
        }

        private bool GetSafe(bool[,] grid, int x, int y)
        {
            return x >= 0 && y >= 0 && y < grid.GetLength(0) && x < grid.GetLength(1) && grid[y, x];
        }
    }
}
