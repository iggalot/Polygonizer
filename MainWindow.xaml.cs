using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Polygonizer
{
    public partial class MainWindow : Window
    {
        const bool DEBUG_ON = true;
        const int CellSize = 2;
        const int ExtraPadding = 0;

        List<(double x, double y)> allCornerPoints = new List<(double x, double y)>();

        List<(double x, double y)> externalCornerPoints = new List<(double x, double y)>();
        List<(double x, double y)> internalCornerPoints = new List<(double x, double y)>();

        /// <summary>
        /// Define our rectangles
        /// </summary>
        List<Rect> rectangles = new List<Rect>
        {
            new Rect(10, 180, 100, 100),

            new Rect(410, 410, 100, 100),
            new Rect(120, 120, 200, 150),
            new Rect(250, 200, 200, 150),
            new Rect(150, 300, 200, 150),

            new Rect(500, 100, 100, 80),  // this is the isolated rectangle
            new Rect(540, 140, 100, 80),  // this is the isolated rectangle

        };

        public MainWindow()
        {
            InitializeComponent();
            DrawScene();
        }

        private void DrawScene()
        {
            // Compute bounds with padding
            Rect bounds = Rect.Empty;
            foreach (var r in rectangles)
                bounds.Union(r);
            bounds.Inflate(ExtraPadding, ExtraPadding);

            int cols = (int)Math.Ceiling(bounds.Width / CellSize);
            int rows = (int)Math.Ceiling(bounds.Height / CellSize);
            bool[,] grid = new bool[rows, cols];
            bool[,] visited = new bool[rows, cols];

            // Fill grid with rectangles
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
                        TraceBoundary(region, grid, bounds);
                        DrawIslandBoundary(rectangles);
                    }
                }
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

        private void TraceBoundary(List<(int x, int y)> region, bool[,] grid, Rect bounds)
        {
            foreach (var (x, y) in region)
            {
                ProcessCornerDetection(grid, x, y);
            }

            externalCornerPoints = RemoveDuplicates(externalCornerPoints);
            internalCornerPoints = RemoveDuplicates(internalCornerPoints);

            DrawCornerPoints(externalCornerPoints, bounds, Brushes.Green);
            DrawCornerPoints(internalCornerPoints, bounds, Brushes.Red);
        }

        private void ProcessCornerDetection(bool[,] grid, int x, int y)
        {
            bool hasTop = GetSafe(grid, x, y - 1);
            bool hasBottom = GetSafe(grid, x, y + 1);
            bool hasLeft = GetSafe(grid, x - 1, y);
            bool hasRight = GetSafe(grid, x + 1, y);

            bool topLeftEmpty = !GetSafe(grid, x - 1, y - 1);
            bool topRightEmpty = !GetSafe(grid, x + 1, y - 1);
            bool bottomLeftEmpty = !GetSafe(grid, x - 1, y + 1);
            bool bottomRightEmpty = !GetSafe(grid, x + 1, y + 1);

            if (DEBUG_ON)
            {
                AddExternalCorners(x, y, hasTop, hasBottom, hasLeft, hasRight, topLeftEmpty, topRightEmpty, bottomLeftEmpty, bottomRightEmpty);
                AddInternalCorners(x, y, hasTop, hasBottom, hasLeft, hasRight, topLeftEmpty, topRightEmpty, bottomLeftEmpty, bottomRightEmpty);
            }
        }

        private void AddExternalCorners(int x, int y, bool hasTop, bool hasBottom, bool hasLeft, bool hasRight,
                                        bool topLeftEmpty, bool topRightEmpty, bool bottomLeftEmpty, bool bottomRightEmpty)
        {
            if (!hasLeft && !hasTop && hasRight && hasBottom && topLeftEmpty)
                AddCornerPoint(x - 1, y - 1, externalCornerPoints);

            if (!hasRight && !hasTop && hasLeft && hasBottom && topRightEmpty)
                AddCornerPoint(x + 1, y - 1, externalCornerPoints);

            if (!hasRight && !hasBottom && hasLeft && hasTop && bottomRightEmpty)
                AddCornerPoint(x + 1, y + 1, externalCornerPoints);

            if (!hasLeft && !hasBottom && hasRight && hasTop && bottomLeftEmpty)
                AddCornerPoint(x - 1, y + 1, externalCornerPoints);
        }

        private void AddInternalCorners(int x, int y, bool hasTop, bool hasBottom, bool hasLeft, bool hasRight,
                                        bool topLeftEmpty, bool topRightEmpty, bool bottomLeftEmpty, bool bottomRightEmpty)
        {
            if (!hasLeft && !topLeftEmpty) AddCornerPoint(x - 1, y, internalCornerPoints);
            if (!hasTop && !topLeftEmpty) AddCornerPoint(x, y - 1, internalCornerPoints);
            if (!hasRight && !topRightEmpty) AddCornerPoint(x + 1, y, internalCornerPoints);
            if (!hasTop && !topRightEmpty) AddCornerPoint(x, y - 1, internalCornerPoints);
            if (!hasRight && !bottomRightEmpty) AddCornerPoint(x + 1, y, internalCornerPoints);
            if (!hasBottom && !bottomRightEmpty) AddCornerPoint(x, y + 1, internalCornerPoints);
            if (!hasLeft && !bottomLeftEmpty) AddCornerPoint(x - 1, y, internalCornerPoints);
            if (!hasBottom && !bottomLeftEmpty) AddCornerPoint(x, y + 1, internalCornerPoints);
        }

        private void AddCornerPoint(double x, double y, List<(double x, double y)> cornerList)
        {
            allCornerPoints.Add((x, y));
            cornerList.Add((x, y));
        }

        private void DrawCornerPoints(List<(double x, double y)> corners, Rect bounds, Brush color)
        {
            foreach (var (x, y) in corners)
            {
                var point = new Point(bounds.X + x * CellSize, bounds.Y + y * CellSize);
                var circle = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = color
                };
                Canvas.SetLeft(circle, point.X - 5);
                Canvas.SetTop(circle, point.Y - 5);
                MainCanvas.Children.Add(circle);
            }
        }



        private bool GetSafe(bool[,] grid, int x, int y)
        {
            return x >= 0 && y >= 0 && y < grid.GetLength(0) && x < grid.GetLength(1) && grid[y, x];
        }

        public static List<(double x, double y)> RemoveDuplicates(List<(double x, double y)> cornerPoints)
        {
            HashSet<(double x, double y)> uniquePoints = new HashSet<(double x, double y)>();
            List<(double x, double y)> result = new List<(double x, double y)>();

            foreach (var point in cornerPoints)
            {
                if (uniquePoints.Add(point)) // Add returns false if already exists
                {
                    result.Add(point);
                }
            }

            return result;
        }

        private void DrawUnionOutlineWithColors(List<Rect> rectangles)
        {
            if (rectangles == null || rectangles.Count == 0)
                return;

            // Step 1: Group rectangles into connected islands
            List<List<Rect>> islands = GroupConnectedRectangles(rectangles);

            // Step 2: Color palette
            Brush[] colorPalette = new Brush[]
            {
                Brushes.LightBlue, Brushes.LightGreen, Brushes.LightCoral,
                Brushes.LightGoldenrodYellow, Brushes.LightPink, Brushes.LightSalmon,
                Brushes.LightSeaGreen, Brushes.LightSlateGray
            };

            // Step 3: Process each island
            for (int i = 0; i < islands.Count; i++)
            {
                var island = islands[i];
                Geometry combined = new RectangleGeometry(island[0]);
                for (int j = 1; j < island.Count; j++)
                {
                    combined = Geometry.Combine(combined, new RectangleGeometry(island[j]), GeometryCombineMode.Union, null);
                }

                Path path = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.5,
                    Fill = colorPalette[i % colorPalette.Length],
                    Data = combined
                };

                MainCanvas.Children.Add(path);
            }
        }

        private List<List<Rect>> GroupConnectedRectangles(List<Rect> rectangles)
        {
            List<List<Rect>> groups = new List<List<Rect>>();
            HashSet<int> visited = new HashSet<int>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                if (visited.Contains(i)) continue;

                List<Rect> group = new List<Rect>();
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(i);
                visited.Add(i);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    group.Add(rectangles[current]);

                    for (int j = 0; j < rectangles.Count; j++)
                    {
                        if (!visited.Contains(j) && RectsTouchOrOverlap(rectangles[current], rectangles[j]))
                        {
                            queue.Enqueue(j);
                            visited.Add(j);
                        }
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        private bool RectsTouchOrOverlap(Rect a, Rect b)
        {
            return a.IntersectsWith(b) || a.Contains(b) || b.Contains(a);
        }

        private void DrawIslandBoundary(List<Rect> rectangles)
        {
            DrawUnionOutlineWithColors(rectangles);

        }
    }
}
