using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Linq;

namespace Polygonizer
{
    public partial class MainWindow : Window
    {
        const int CellSize = 5;
        const int Padding = 1;

        public MainWindow()
        {
            InitializeComponent();
            DrawScene();
        }

        private void DrawScene()
        {
            var rectangles = new List<Rect>
            {
                new Rect(10, 10, 100, 100),
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
                        DrawIslandBoundary(region, grid, bounds);
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
            var boundaryPoints = new List<Point>();
            List<(double x, double y)> allCornerPoints = new List<(double x, double y)>();

            List<(double x, double y)> externalCornerPoints = new List<(double x, double y)>();
            List<(double x, double y)> internalCornerPoints = new List<(double x, double y)>();

            // Identify corner points based on the corrected definition
            foreach (var (x, y) in region)
            {
                // Check if the current pixel has two adjacent filled neighbors (horizontal + vertical)
                bool hasTop = GetSafe(grid, x, y - 1);
                bool hasBottom = GetSafe(grid, x, y + 1);
                bool hasLeft = GetSafe(grid, x - 1, y);
                bool hasRight = GetSafe(grid, x + 1, y);

                int neighborFilledCount = 0;
                if (hasTop) neighborFilledCount++;
                if (hasBottom) neighborFilledCount++;
                if (hasLeft) neighborFilledCount++;
                if (hasRight) neighborFilledCount++;

                // Check diagonal neighbors for empty space (outside the filled region)
                bool topLeftEmpty = !GetSafe(grid, x - 1, y - 1);
                bool topRightEmpty = !GetSafe(grid, x + 1, y - 1);
                bool bottomLeftEmpty = !GetSafe(grid, x - 1, y + 1);
                bool bottomRightEmpty = !GetSafe(grid, x + 1, y + 1);

                // Define the corner condition: two adjacent filled pixels (horizontal + vertical)
                // with an empty diagonal neighbor
                bool isCorner = false;

                // For external corners
                if (!hasLeft && !hasTop && hasRight && hasBottom && topLeftEmpty)
                {
                    allCornerPoints.Add((x - 1, y - 1));
                    externalCornerPoints.Add((x - 1, y - 1));
                }
                if (!hasRight && !hasTop && hasLeft && hasBottom && topRightEmpty)
                {
                    allCornerPoints.Add((x + 1, y - 1));
                    externalCornerPoints.Add((x + 1, y - 1));
                }
                if (!hasRight && !hasBottom && hasLeft && hasTop && bottomRightEmpty)
                {
                    allCornerPoints.Add((x + 1, y + 1));
                    externalCornerPoints.Add((x + 1, y + 1));
                }
                if (!hasLeft && !hasBottom && hasRight && hasTop && bottomLeftEmpty)
                {
                    allCornerPoints.Add((x - 1, y + 1));
                    externalCornerPoints.Add((x - 1, y + 1));
                }

                // For internal corners
                if (!hasLeft && !topLeftEmpty)
                {
                    allCornerPoints.Add((x - 1, y));
                    internalCornerPoints.Add((x - 1, y));
                }
                if (!hasTop && !topLeftEmpty)
                {
                    allCornerPoints.Add((x, y - 1));
                    internalCornerPoints.Add((x, y - 1));
                }
                if (!hasRight && !topRightEmpty)
                {
                    allCornerPoints.Add((x + 1, y));
                    internalCornerPoints.Add((x + 1, y));
                }
                if (!hasTop && !topRightEmpty)
                {
                    allCornerPoints.Add((x, y - 1));
                    internalCornerPoints.Add((x, y - 1));
                }
                if (!hasRight && !bottomRightEmpty)
                {
                    allCornerPoints.Add((x + 1, y));
                    internalCornerPoints.Add((x + 1, y));
                }
                if (!hasBottom && !bottomRightEmpty)
                {
                    allCornerPoints.Add((x, y + 1));
                    internalCornerPoints.Add((x, y + 1));
                }
                if (!hasLeft && !bottomLeftEmpty)
                {
                    allCornerPoints.Add((x - 1, y));
                    internalCornerPoints.Add((x - 1, y));
                }
                if (!hasBottom && !bottomLeftEmpty)
                {
                    allCornerPoints.Add((x, y + 1));
                    internalCornerPoints.Add((x, y + 1));
                }

                externalCornerPoints = RemoveDuplicates(externalCornerPoints);
                internalCornerPoints = RemoveDuplicates(internalCornerPoints);

                // Visualization of corner points on the canvas
                foreach (var corner in externalCornerPoints)
                {
                    boundaryPoints.Add(new Point(bounds.X + corner.x * CellSize, bounds.Y + corner.y * CellSize));

                    // Add a large circle at the corner point for visualization
                    var cornerCircle = new Ellipse
                    {
                        Width = 10,   // Larger size
                        Height = 10,  // Larger size
                        Fill = Brushes.Green
                    };
                    Canvas.SetLeft(cornerCircle, bounds.X + corner.x * CellSize - 5);  // Center circle
                    Canvas.SetTop(cornerCircle, bounds.Y + corner.y * CellSize - 5);   // Center circle
                    MainCanvas.Children.Add(cornerCircle);
                }

                foreach (var corner in internalCornerPoints)
                {
                    boundaryPoints.Add(new Point(bounds.X + corner.x * CellSize, bounds.Y + corner.y * CellSize));

                    // Add a large circle at the corner point for visualization
                    var cornerCircle = new Ellipse
                    {
                        Width = 10,   // Larger size
                        Height = 10,  // Larger size
                        Fill = Brushes.Red
                    };
                    Canvas.SetLeft(cornerCircle, bounds.X + corner.x * CellSize - 5);  // Center circle
                    Canvas.SetTop(cornerCircle, bounds.Y + corner.y * CellSize - 5);   // Center circle
                    MainCanvas.Children.Add(cornerCircle);
                }
            }

            // Start tracing from one of the corner points
            if (externalCornerPoints.Count > 0)
            {
                TraceRectangleBoundary(externalCornerPoints[0], grid, bounds);
            }
        }

        private void TraceRectangleBoundary((double x, double y) startCorner, bool[,] grid, Rect bounds)
        {
            var current = startCorner;
            var boundaryPoints = new List<Point>();

            // March around the boundary until we close the loop
            do
            {
                boundaryPoints.Add(new Point(bounds.X + current.x * CellSize, bounds.Y + current.y * CellSize));

                // Check the 4 adjacent directions (right, down, left, up)
                var next = GetNextBoundaryPoint(current, grid);
                current = next;

            } while (current != startCorner);

            // Optionally, visualize the boundary
            foreach (var pt in boundaryPoints)
            {
                var pointEllipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = Brushes.Black
                };
                Canvas.SetLeft(pointEllipse, pt.X - 2.5);
                Canvas.SetTop(pointEllipse, pt.Y - 2.5);
                MainCanvas.Children.Add(pointEllipse);
            }
        }

        private (double x, double y) GetNextBoundaryPoint((double x, double y) current, bool[,] grid)
        {
            // Implement logic to find the next boundary point here
            return current; // Placeholder to be filled with boundary logic
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

        private void DrawIslandBoundary(List<(int x, int y)> region, bool[,] grid, Rect bounds)
        {
            var boundaryPoints = new List<Point>();

            // A simple method to check if a grid point is a boundary (an edge of the island)
            bool IsBoundary(int x, int y)
            {
                return (x < 0 || y < 0 || x >= grid.GetLength(1) || y >= grid.GetLength(0) || !grid[y, x]);
            }

            // Loop through each point in the region to find the boundary
            foreach (var (x, y) in region)
            {
                // Check if any of the 4 adjacent points are out of bounds or not part of the island
                if (IsBoundary(x - 1, y) || IsBoundary(x + 1, y) || IsBoundary(x, y - 1) || IsBoundary(x, y + 1))
                {
                    // This point is part of the boundary, add it to the boundary points list
                    boundaryPoints.Add(new Point(bounds.X + x * CellSize, bounds.Y + y * CellSize));
                }
            }

            // For debugging: Draw a line connecting all boundary points
            if (boundaryPoints.Count > 1)
            {
                var polyline = new Polyline
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2,
                    Points = new PointCollection(boundaryPoints)
                };
                MainCanvas.Children.Add(polyline);
            }

            // Optionally, visualize the boundary points for debugging
            foreach (var point in boundaryPoints)
            {
                var pointEllipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(pointEllipse, point.X - 2.5);  // Center circle
                Canvas.SetTop(pointEllipse, point.Y - 2.5);   // Center circle
                MainCanvas.Children.Add(pointEllipse);
            }
        }

        // Helper function to get the first external corner from the region
        private (double x, double y)? GetFirstExternalCorner(List<(int x, int y)> region, bool[,] grid)
        {
            foreach (var (x, y) in region)
            {
                bool hasTop = GetSafe(grid, x, y - 1);
                bool hasBottom = GetSafe(grid, x, y + 1);
                bool hasLeft = GetSafe(grid, x - 1, y);
                bool hasRight = GetSafe(grid, x + 1, y);

                // Check for external corners
                if (!hasLeft && !hasTop && hasRight && hasBottom)
                {
                    return (x - 1, y - 1); // external corner at top-left
                }
                if (!hasRight && !hasTop && hasLeft && hasBottom)
                {
                    return (x + 1, y - 1); // external corner at top-right
                }
                if (!hasRight && !hasBottom && hasLeft && hasTop)
                {
                    return (x + 1, y + 1); // external corner at bottom-right
                }
                if (!hasLeft && !hasBottom && hasRight && hasTop)
                {
                    return (x - 1, y + 1); // external corner at bottom-left
                }
            }
            return null; // No external corner found
        }

        private (double x, double y) GetNextBoundaryPoint((double x, double y) current, Point direction, bool[,] grid)
        {
            int x = (int)current.x + (int)direction.X;
            int y = (int)current.y + (int)direction.Y;

            // Check if the next point is valid (part of the filled region)
            if (GetSafe(grid, x, y))
            {
                return (x, y);
            }
            else
            {
                return current; // No valid next point found, stay at the current position
            }
        }
    }
}
