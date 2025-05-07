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
                        TraceBoundary(region, grid, bounds);
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
            List<(double x, double y)> all_corner_points = new List<(double x, double y)>();

            List<(double x, double y)> external_corner_points = new List<(double x, double y)>();
            List<(double x, double y)> internal_corner_points = new List<(double x, double y)>();


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


                // for external corners
                if (!hasLeft && !hasTop && hasRight && hasBottom && topLeftEmpty)
                {
                    all_corner_points.Add((x - 1, y - 1));
                    external_corner_points.Add((x - 1, y - 1));
                }
                if (!hasRight && !hasTop && hasLeft && hasBottom && topRightEmpty)
                {
                    all_corner_points.Add((x + 1, y - 1));
                    external_corner_points.Add((x + 1, y - 1));
                }
                if (!hasRight && !hasBottom && hasLeft && hasTop && bottomRightEmpty)
                {
                    all_corner_points.Add((x + 1, y + 1));
                    external_corner_points.Add((x + 1, y + 1));
                }
                if (!hasLeft && !hasBottom && hasRight && hasTop && bottomLeftEmpty && external_corner_points.Count < 4)
                {
                    all_corner_points.Add((x - 1, y + 1));
                    external_corner_points.Add((x - 1, y + 1));
                }

                // for internal corners
                if (!hasLeft && !topLeftEmpty)
                {
                    all_corner_points.Add((x - 1, y));
                    internal_corner_points.Add((x - 1, y));

                }
                if (!hasTop && !topLeftEmpty)
                {
                    all_corner_points.Add((x, y - 1));
                    internal_corner_points.Add((x, y - 1));
                }
                if (!hasRight && !topRightEmpty)
                {
                    all_corner_points.Add((x + 1, y));
                    internal_corner_points.Add((x + 1, y));
                }
                if (!hasTop && !topRightEmpty)
                {
                    all_corner_points.Add((x, y - 1));
                    internal_corner_points.Add((x, y - 1));
                }
                if (!hasRight && !bottomRightEmpty)
                {
                    all_corner_points.Add((x + 1, y));
                    internal_corner_points.Add((x + 1, y));
                }
                if (!hasBottom && !bottomRightEmpty)
                {
                    all_corner_points.Add((x, y + 1));
                    internal_corner_points.Add((x, y + 1));
                }
                if (!hasLeft && !bottomLeftEmpty)
                {
                    all_corner_points.Add((x - 1, y));
                    internal_corner_points.Add((x - 1, y));
                }
                if (!hasBottom && !bottomLeftEmpty)
                {
                    all_corner_points.Add((x, y + 1));
                    internal_corner_points.Add((x, y + 1));
                }

                all_corner_points = RemoveDuplicates(external_corner_points);

                external_corner_points = RemoveDuplicates(external_corner_points);
                internal_corner_points = RemoveDuplicates(internal_corner_points);



                //|| 
                //    (!hasLeft && !hasBottom && hasRight && hasTop && bottomLeftEmpty) || 
                //    (!hasRight && !hasBottom && hasLeft && hasTop) || 
                //    (!hasRight && !hasTop && hasLeft && hasBottom))

                //if ((hasLeft && hasBottom && bottomRightEmpty) || (hasRight && hasTop && topLeftEmpty) ||
                //    (hasLeft && hasTop && bottomLeftEmpty) || (hasRight && hasBottom && topRightEmpty))
                //{
                //    isCorner = true;
                //}

                foreach (var corner in external_corner_points)
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

                foreach (var corner in internal_corner_points)
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


                //if (isCorner)
                //{
                //    boundaryPoints.Add(new Point(bounds.X + x * CellSize, bounds.Y + y * CellSize));

                //    // Add a large circle at the corner point for visualization
                //    var cornerCircle = new Ellipse
                //    {
                //        Width = 10,   // Larger size
                //        Height = 10,  // Larger size
                //        Fill = Brushes.Green
                //    };
                //    Canvas.SetLeft(cornerCircle, bounds.X + x * CellSize - 5);  // Center circle
                //    Canvas.SetTop(cornerCircle, bounds.Y + y * CellSize - 5);   // Center circle
                //    MainCanvas.Children.Add(cornerCircle);
                //}
            }

            // Now you can further connect these corner points if necessary.
        }

        private bool GetSafe(bool[,] grid, int x, int y)
        {
            return x >= 0 && y >= 0 && y < grid.GetLength(0) && x < grid.GetLength(1) && grid[y, x];
        }


        /// This agorithm finds corner nodes "3" based on a filled parameter "1" and an empty parameter "0".  A parameter "2" is added to any "0"
        /// that is horizontally or vertically adjacent to a "2", and then iterates to 
        //    using System;

        //class Program
        //    {
        //        static void Main()
        //        {
        //            // Input grid representing the rectangle pattern
        //            int[,] grid = {
        //            { 0, 0, 0, 0, 0 },
        //            { 0, 1, 0, 1, 0 },
        //            { 0, 0, 1, 0, 0 },
        //            { 0, 1, 0, 1, 0 },
        //            { 0, 0, 0, 0, 0 }
        //        };

        //            // Output grid to store the processed result
        //            int[,] outputGrid = new int[grid.GetLength(0), grid.GetLength(1)];

        //            // Step 1: Identify all cells adjacent to a "1" (including diagonals) and mark them as "2"
        //            for (int i = 0; i < grid.GetLength(0); i++)
        //            {
        //                for (int j = 0; j < grid.GetLength(1); j++)
        //                {
        //                    if (grid[i, j] == 1)
        //                    {
        //                        // Check adjacent cells and mark them as "2"
        //                        for (int dx = -1; dx <= 1; dx++)
        //                        {
        //                            for (int dy = -1; dy <= 1; dy++)
        //                            {
        //                                int nx = i + dx;
        //                                int ny = j + dy;

        //                                if (nx >= 0 && ny >= 0 && nx < grid.GetLength(0) && ny < grid.GetLength(1) && grid[nx, ny] == 0)
        //                                {
        //                                    outputGrid[nx, ny] = 2; // Mark as border
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //            // Step 2: Identify corner cells (those that are diagonal to at least two "1"s) and mark them as "3"
        //            for (int i = 0; i < grid.GetLength(0); i++)
        //            {
        //                for (int j = 0; j < grid.GetLength(1); j++)
        //                {
        //                    if (outputGrid[i, j] == 2) // Only check cells marked as border
        //                    {
        //                        int countDiagonal = 0;
        //                        // Check diagonals to count how many "1"s are adjacent diagonally
        //                        if (i > 0 && j > 0 && grid[i - 1, j - 1] == 1) countDiagonal++; // Top-left
        //                        if (i > 0 && j < grid.GetLength(1) - 1 && grid[i - 1, j + 1] == 1) countDiagonal++; // Top-right
        //                        if (i < grid.GetLength(0) - 1 && j > 0 && grid[i + 1, j - 1] == 1) countDiagonal++; // Bottom-left
        //                        if (i < grid.GetLength(0) - 1 && j < grid.GetLength(1) - 1 && grid[i + 1, j + 1] == 1) countDiagonal++; // Bottom-right

        //                        // If two diagonal "1"s are found, it's a corner
        //                        if (countDiagonal >= 2)
        //                        {
        //                            outputGrid[i, j] = 3; // Mark as corner
        //                        }
        //                    }
        //                }
        //            }

        //            // Step 3: Display the output grid
        //            PrintGrid(outputGrid);
        //        }

        //        // Method to print the grid
        //        static void PrintGrid(int[,] grid)
        //        {
        //            for (int i = 0; i < grid.GetLength(0); i++)
        //            {
        //                for (int j = 0; j < grid.GetLength(1); j++)
        //                {
        //                    Console.Write(grid[i, j] + " ");
        //                }
        //                Console.WriteLine();
        //            }
        //        }
        //    }



        public static List<(double x, double y)> RemoveDuplicates(List<(double x, double y)> corner_points)
        {
            HashSet<(double x, double y)> uniquePoints = new HashSet<(double x, double y)>();
            List<(double x, double y)> result = new List<(double x, double y)>();

            foreach (var point in corner_points)
            {
                if (uniquePoints.Add(point)) // Add returns false if already exists
                {
                    result.Add(point);
                }
            }

            return result;
        }
    }
}