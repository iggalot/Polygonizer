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

            // Fill grid with rectangle cells
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

            // Flood fill to find all distinct regions and trace their perimeters
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
            var boundaryPoints = new List<Point>();
            var dirs = new (int dx, int dy)[] { (1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };

            // Find the boundary points
            foreach (var (x, y) in region)
            {
                foreach (var (dx, dy) in dirs)
                {
                    int nx = x + dx, ny = y + dy;

                    // Check if the neighbor is out of bounds or not filled
                    if (nx < 0 || ny < 0 || nx >= grid.GetLength(1) || ny >= grid.GetLength(0) || !grid[ny, nx])
                    {
                        // It's an edge pixel, add to boundary points
                        boundaryPoints.Add(new Point(bounds.X + x * CellSize, bounds.Y + y * CellSize));
                        break;
                    }
                }
            }

            // Sort the boundary points in counterclockwise order
            return SortPointsCounterClockwise(boundaryPoints);
        }

        private List<Point> SortPointsCounterClockwise(List<Point> points)
        {
            // Compute the centroid of the points to use as the reference point
            double centroidX = 0, centroidY = 0;
            foreach (var point in points)
            {
                centroidX += point.X;
                centroidY += point.Y;
            }
            centroidX /= points.Count;
            centroidY /= points.Count;

            // Sort points based on the angle relative to the centroid
            points.Sort((p1, p2) =>
            {
                double angle1 = Math.Atan2(p1.Y - centroidY, p1.X - centroidX);
                double angle2 = Math.Atan2(p2.Y - centroidY, p2.X - centroidX);
                return angle1.CompareTo(angle2);
            });

            return points;
        }
    }
}
