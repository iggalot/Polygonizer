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
                    }
                }
            }

            DrawIslandBoundaries();
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

        private void DrawIslandBoundaries()
        {
            var rects = MainCanvas.Children.OfType<Rectangle>().ToList();
            var visited = new HashSet<Rectangle>();
            var islands = new List<List<Rect>>();

            foreach (var rect in rects)
            {
                if (visited.Contains(rect)) continue;

                var queue = new Queue<Rectangle>();
                var island = new List<Rect>();
                queue.Enqueue(rect);
                visited.Add(rect);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var r = new Rect(Canvas.GetLeft(current), Canvas.GetTop(current), current.Width, current.Height);
                    island.Add(r);

                    foreach (var neighbor in rects)
                    {
                        if (visited.Contains(neighbor)) continue;

                        var nr = new Rect(Canvas.GetLeft(neighbor), Canvas.GetTop(neighbor), neighbor.Width, neighbor.Height);
                        if (r.IntersectsWith(nr) || RectsTouch(r, nr))
                        {
                            queue.Enqueue(neighbor);
                            visited.Add(neighbor);
                        }
                    }
                }

                islands.Add(island);
            }

            foreach (var island in islands)
            {
                var polygon = TraceIslandBoundary(island);
                if (polygon != null)
                {
                    polygon.Stroke = Brushes.Red;
                    polygon.StrokeThickness = 2;
                    polygon.Fill = Brushes.Transparent;
                    MainCanvas.Children.Add(polygon);
                }
            }
        }

        private Polygon TraceIslandBoundary(List<Rect> rects)
        {
            var edgeMap = new Dictionary<(Point, Point), int>(new EdgeComparer());

            foreach (var r in rects)
            {
                var edges = new[]
                {
            (new Point(r.Left, r.Top), new Point(r.Right, r.Top)),     // top
            (new Point(r.Right, r.Top), new Point(r.Right, r.Bottom)), // right
            (new Point(r.Right, r.Bottom), new Point(r.Left, r.Bottom)), // bottom
            (new Point(r.Left, r.Bottom), new Point(r.Left, r.Top))    // left
        };

                foreach (var edge in edges)
                {
                    var normalized = NormalizeEdge(edge.Item1, edge.Item2);
                    if (!edgeMap.ContainsKey(normalized))
                        edgeMap[normalized] = 1;
                    else
                        edgeMap[normalized]++;
                }
            }

            // Only edges with a count of 1 are boundary edges
            var outerEdges = edgeMap.Where(e => e.Value == 1).Select(e => e.Key).ToList();
            if (outerEdges.Count == 0) return null;

            // Chain edges into a polygon
            var boundaryPoints = ChainBoundaryEdges(outerEdges);
            if (boundaryPoints.Count < 3) return null;

            var poly = new Polygon
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
            foreach (var pt in boundaryPoints)
                poly.Points.Add(pt);

            return poly;
        }

        private List<Point> ChainBoundaryEdges(List<(Point, Point)> edges)
        {
            var lookup = new Dictionary<Point, List<Point>>();

            foreach (var (a, b) in edges)
            {
                if (!lookup.ContainsKey(a)) lookup[a] = new List<Point>();
                if (!lookup.ContainsKey(b)) lookup[b] = new List<Point>();
                lookup[a].Add(b);
                lookup[b].Add(a);
            }

            var start = edges[0].Item1;
            var current = start;
            var previous = new Point(double.NaN, double.NaN);
            var result = new List<Point> { current };

            while (true)
            {
                var neighbors = lookup[current];
                Point next = neighbors.FirstOrDefault(p => !p.Equals(previous));
                if (next == start || next == default) break;

                result.Add(next);
                previous = current;
                current = next;
            }

            return result;
        }

        private (Point, Point) NormalizeEdge(Point a, Point b)
        {
            return a.X < b.X || a.Y < b.Y ? (a, b) : (b, a);
        }

        private bool RectsTouch(Rect a, Rect b)
        {
            return (a.Right == b.Left || a.Left == b.Right) && a.Bottom > b.Top && a.Top < b.Bottom
                || (a.Bottom == b.Top || a.Top == b.Bottom) && a.Right > b.Left && a.Left < b.Right;
        }

        private class EdgeComparer : IEqualityComparer<(Point, Point)>
        {
            public bool Equals((Point, Point) e1, (Point, Point) e2)
            {
                return (e1.Item1 == e2.Item1 && e1.Item2 == e2.Item2) ||
                       (e1.Item1 == e2.Item2 && e1.Item2 == e2.Item1);
            }

            public int GetHashCode((Point, Point) edge)
            {
                unchecked
                {
                    int h1 = edge.Item1.GetHashCode();
                    int h2 = edge.Item2.GetHashCode();
                    return h1 ^ h2;
                }
            }
        }

    }
}
