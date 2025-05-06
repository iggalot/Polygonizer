using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Polygonizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int CellSize = 5;

        public MainWindow()
        {
            InitializeComponent();
            DrawScene();
        }

        private void DrawScene()
        {
            // Define some rectangles (with an interior hole)
            var rectangles = new List<Rect>
            {
                new Rect(100, 100, 200, 150), // Outer filled region (boundary)
                new Rect(180, 180, 120, 100), // A rectangle inside the first one (hole)
                new Rect(250, 200, 200, 150), // Another filled region
                new Rect(500, 100, 100, 80),  // Disconnected filled region
                new Rect(520, 130, 50, 50)    // Overlapping filled region inside the last one
            };

            // Trace the filled regions and outer boundary
            var polygons = TraceFilledRegions(rectangles);

            // Draw the rectangles (first) -- this draws the filled region
            foreach (var rect in rectangles)
            {
                var rectShape = new Rectangle
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.LightBlue
                };
                Canvas.SetLeft(rectShape, rect.X);
                Canvas.SetTop(rectShape, rect.Y);
                MainCanvas.Children.Add(rectShape);
            }

            // Draw the polygons (outer boundary in red) -- boundaries are drawn last
            foreach (var points in polygons)
            {
                var poly = new Polygon
                {
                    Stroke = Brushes.Red, // Outer boundary stroke color (for filled regions)
                    StrokeThickness = 3,
                    Fill = Brushes.Transparent,
                    Points = new PointCollection(points)
                };
                MainCanvas.Children.Add(poly);
            }

            // Draw the borders (rectangle edges) last to ensure they are not covered by the fill
            foreach (var rect in rectangles)
            {
                var rectOutline = new Rectangle
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Stroke = Brushes.Black,  // Border color (edges of the original rectangles)
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(rectOutline, rect.X);
                Canvas.SetTop(rectOutline, rect.Y);
                MainCanvas.Children.Add(rectOutline);
            }
        }

        private List<List<Point>> TraceFilledRegions(List<Rect> rectangles)
        {
            // Step 1: Determine the bounding box of the entire area
            Rect bounds = Rect.Empty;
            foreach (var r in rectangles)
                bounds.Union(r);

            int cols = (int)Math.Ceiling(bounds.Width / CellSize) + 2;
            int rows = (int)Math.Ceiling(bounds.Height / CellSize) + 2;

            bool[,] grid = new bool[rows, cols];
            bool[,] visited = new bool[rows, cols];

            // Step 2: Rasterize the filled rectangles into the grid
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

            List<List<Point>> allPolygons = new List<List<Point>>();

            // Step 3: Flood-fill for the filled regions (outer boundary)
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (grid[y, x] && !visited[y, x])
                    {
                        HashSet<(int, int)> filledRegion = FloodFill(grid, visited, x, y, true);
                        List<Point> outerPolygon = TracePerimeter(grid, filledRegion, bounds);
                        if (outerPolygon.Count > 0)
                            allPolygons.Add(outerPolygon);
                    }
                }
            }

            return allPolygons;
        }

        private HashSet<(int, int)> FloodFill(bool[,] grid, bool[,] visited, int startX, int startY, bool isFilled)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);
            Queue<(int, int)> queue = new Queue<(int, int)>();
            HashSet<(int, int)> region = new HashSet<(int, int)>();

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

                    if (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                    {
                        bool condition = isFilled ? grid[ny, nx] : !grid[ny, nx];
                        if (condition && !visited[ny, nx])
                        {
                            visited[ny, nx] = true;
                            queue.Enqueue((nx, ny));
                        }
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
