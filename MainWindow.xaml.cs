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
                new Rect(10, 120, 100, 100),

                new Rect(10, 10, 100, 100),
                new Rect(100, 100, 200, 150),
                new Rect(250, 200, 200, 150),
                new Rect(500, 100, 100, 80),  // this is the isolated rectangle
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

        private List<(Point A, Point B)> ExtractOuterEdges(List<Rect> rects)
        {
            var edgeSet = new HashSet<(Point, Point)>();

            foreach (var rect in rects)
            {
                var p1 = new Point(rect.X, rect.Y);
                var p2 = new Point(rect.X + rect.Width, rect.Y);
                var p3 = new Point(rect.X + rect.Width, rect.Y + rect.Height);
                var p4 = new Point(rect.X, rect.Y + rect.Height);

                var edges = new List<(Point, Point)>
        {
            (p1, p2), (p2, p3), (p3, p4), (p4, p1)
        };

                foreach (var edge in edges)
                {
                    var reverse = (edge.Item2, edge.Item1);
                    if (edgeSet.Contains(reverse))
                        edgeSet.Remove(reverse); // shared internal edge
                    else
                        edgeSet.Add(edge);
                }
            }

            return edgeSet.ToList(); // only outer edges remain
        }

        private static List<Point> TraceBoundary(Dictionary<(Point, Point), int> edgeCounts)
        {
            // Get only outer edges
            var outerEdges = edgeCounts
                .Where(kvp => kvp.Value == 1)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            if (outerEdges.Count == 0)
                return new List<Point>();

            // Start from the top-left most point
            var startEdge = outerEdges.OrderBy(e => e.Item1.Y).ThenBy(e => e.Item1.X).First();
            var path = new List<Point> { startEdge.Item1, startEdge.Item2 };
            outerEdges.Remove(startEdge);

            Point current = startEdge.Item2;
            Point previous = startEdge.Item1;

            while (true)
            {
                var candidates = outerEdges
                    .Where(e => e.Item1 == current || e.Item2 == current)
                    .ToList();

                if (candidates.Count == 0)
                    break;

                // Choose next edge based on left-turn preference (optional)
                (Point, Point) nextEdge = candidates
                    .OrderBy(e =>
                    {
                        Point other = e.Item1 == current ? e.Item2 : e.Item1;
                        return GetAngle(previous, current, other);
                    }).First();

                outerEdges.Remove(nextEdge);

                Point nextPoint = nextEdge.Item1 == current ? nextEdge.Item2 : nextEdge.Item1;
                path.Add(nextPoint);
                previous = current;
                current = nextPoint;

                if (nextPoint == path[0])
                    break;
            }

            return path;
        }

        private static double GetAngle(Point from, Point center, Point to)
        {
            Vector v1 = Point.Subtract(center, from);
            Vector v2 = Point.Subtract(to, center);
            return Vector.AngleBetween(v1, v2);
        }

        private static (Point, Point) NormalizeEdge(Point a, Point b)
        {
            return a.X < b.X || (a.X == b.X && a.Y < b.Y) ? (a, b) : (b, a);
        }

        private static Dictionary<(Point, Point), int> CountEdges(List<Rect> rectangles)
        {
            var edgeCounts = new Dictionary<(Point, Point), int>();

            foreach (var rect in rectangles)
            {
                var p1 = new Point(rect.X, rect.Y);
                var p2 = new Point(rect.X + rect.Width, rect.Y);
                var p3 = new Point(rect.X + rect.Width, rect.Y + rect.Height);
                var p4 = new Point(rect.X, rect.Y + rect.Height);

                var edges = new[]
                {
            NormalizeEdge(p1, p2),
            NormalizeEdge(p2, p3),
            NormalizeEdge(p3, p4),
            NormalizeEdge(p4, p1)
        };

                foreach (var edge in edges)
                {
                    if (edgeCounts.ContainsKey(edge))
                        edgeCounts[edge]++;
                    else
                        edgeCounts[edge] = 1;
                }
            }

            return edgeCounts;
        }


        private static Dictionary<(Point, Point), int> GetEdgeCounts(List<Rect> rectangles)
        {
            var edgeCounts = new Dictionary<(Point, Point), int>();

            foreach (var rect in rectangles)
            {
                var p1 = new Point(rect.X, rect.Y);
                var p2 = new Point(rect.X + rect.Width, rect.Y);
                var p3 = new Point(rect.X + rect.Width, rect.Y + rect.Height);
                var p4 = new Point(rect.X, rect.Y + rect.Height);

                var edges = new[]
                {
            NormalizeEdge(p1, p2),
            NormalizeEdge(p2, p3),
            NormalizeEdge(p3, p4),
            NormalizeEdge(p4, p1)
        };

                foreach (var edge in edges)
                {
                    if (edgeCounts.ContainsKey(edge))
                        edgeCounts[edge]++;
                    else
                        edgeCounts[edge] = 1;
                }
            }

            return edgeCounts;
        }

        private static Dictionary<Point, List<Point>> BuildEdgeGraph(Dictionary<(Point, Point), int> edgeCounts)
        {
            var graph = new Dictionary<Point, List<Point>>();

            foreach (var kvp in edgeCounts)
            {
                if (kvp.Value == 1)
                {
                    var (a, b) = kvp.Key;
                    if (!graph.ContainsKey(a)) graph[a] = new List<Point>();
                    if (!graph.ContainsKey(b)) graph[b] = new List<Point>();
                    graph[a].Add(b);
                    graph[b].Add(a);
                }
            }

            return graph;
        }

        private static List<Point> TracePerimeter(Dictionary<Point, List<Point>> graph)
        {
            Point start = graph.Keys.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            var path = new List<Point> { start };
            var visitedEdges = new HashSet<(Point, Point)>();

            Point current = start;
            Point? previous = null;

            while (true)
            {
                var neighbors = graph[current];

                Point? next = null;
                double bestAngle = double.MaxValue;

                foreach (var neighbor in neighbors)
                {
                    var edge = NormalizeEdge(current, neighbor);
                    if (visitedEdges.Contains(edge)) continue;

                    double angle = GetAngle(previous, current, neighbor);
                    if (angle < bestAngle)
                    {
                        bestAngle = angle;
                        next = neighbor;
                    }
                }

                if (next == null || next == start) break;

                visitedEdges.Add(NormalizeEdge(current, next.Value));
                path.Add(next.Value);
                previous = current;
                current = next.Value;
            }

            return path;
        }

        private static double GetAngle(Point? prev, Point curr, Point next)
        {
            Vector v1 = prev.HasValue ? Point.Subtract(curr, prev.Value) : new Vector(1, 0);
            Vector v2 = Point.Subtract(next, curr);
            double angle = Vector.AngleBetween(v1, v2);
            return (angle + 360) % 360;
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

            ////List<Point> perimeter_pts = new List<Point>();
            ////perimeter_pts = TracePerimeter(rectangles);

            //Dictionary<int, Rect> rect_dict = new Dictionary<int, Rect>();

            //int rect_count = 0;
            //foreach (var rect in rectangles)
            //{
            //    rect_dict.Add(rect_count, rect);
            //    rect_count += 1;
            //    Console.WriteLine("Rectangle " + rect_count + ": " + rect);
            //}

            //if (rect_dict.Count == 0)
            //{
            //    return;
            //}

            //List<Point> boundary_pts = new List<Point>();
            //boundary_pts.Add(new Point(rect_dict[0].X, rect_dict[0].Y));

            //for (int i = 0; i < rect_dict.Count; i++)
            //{
            //    Rect rect = rect_dict[i];
            //    Point p1 = new Point(rect.X, rect.Y);
            //    Point p2 = new Point(rect.X + rect.Width, rect.Y);
            //    Point p3 = new Point(rect.X + rect.Width, rect.Y + rect.Height);
            //    Point p4 = new Point(rect.X, rect.Y + rect.Height);

            //    Dictionary<int, Point> intersect_pts = new Dictionary<int, Point>();

            //    // find all intersections with other rectangles of the edges
            //    for (int j = i+1; j < rect_dict.Count; j++)
            //    {
            //        Rect rect2 = rect_dict[j];
            //        Point p1_r2 = new Point(rect2.X, rect2.Y);
            //        Point p2_r2 = new Point(rect2.X + rect2.Width, rect2.Y);
            //        Point p3_r2 = new Point(rect2.X + rect2.Width, rect2.Y + rect2.Height);
            //        Point p4_r2 = new Point(rect2.X, rect2.Y + rect2.Height);

            //        Point intersection;
            //        if(FindNearestIntersection(rectangles, p1, p2, out intersection))
            //        {
            //            intersect_pts.Add(j, intersection);
            //            Console.WriteLine("Intersection between top edge of rect " + i + " and top edge of rect " + j + ": " + intersection);
            //        } else if (FindNearestIntersection(rectangles, p2, p3, out intersection))
            //            {
            //            intersect_pts.Add(j,intersection);
            //            Console.WriteLine("Intersection between right edge of rect " + i + " and right edge of rect " + j + ": " + intersection);
            //        }
            //        else if (FindNearestIntersection(rectangles, p3, p4, out intersection))

            //        {
            //            intersect_pts.Add(j, intersection);
            //            Console.WriteLine("Intersection between bottom edge of rect " + i + " and bottom edge of rect " + j + ": " + intersection);
            //        }
            //        else if (FindNearestIntersection(rectangles, p4, p1, out intersection))
            //        {
            //            intersect_pts.Add(j, intersection);
            //            Console.WriteLine("Intersection between left edge of rect " + i + " and left edge of rect " + j + ": " + intersection);
            //        }
            //    }
            //    if(intersect_pts.Count == 0)
            //    {
            //        boundary_pts.Add(p2);
            //    } else if (intersect_pts.Count == 1)
            //    {
            //        boundary_pts.Add(intersect_pts.Values.First());
            //    } else
            //    {
            //        // find nearest intersection to p1
            //        double min_dist = double.MaxValue;
            //        int min_dist_index = -1;
            //        foreach (var kvp in intersect_pts)
            //        {
            //            double dist = Math.Sqrt(Math.Pow(kvp.Value.X - p1.X, 2) + Math.Pow(kvp.Value.Y - p1.Y, 2));
            //            if (dist < min_dist)
            //            {
            //                min_dist = dist;
            //                min_dist_index = kvp.Key;
            //            }
            //        }
            //        boundary_pts.Add(intersect_pts[min_dist_index]);
            //    }
            //}

            //var rect_bounds = rect.Value;
            //var rect_corners = new List<(double x, double y)>();
            //rect_corners.Add((rect_bounds.X, rect_bounds.Y));

        }

        // Normalized edge with custom equality
        private struct Edge : IEquatable<Edge>
        {
            public Point A;
            public Point B;

            public Edge(Point a, Point b)
            {
                if (a.X < b.X || (a.X == b.X && a.Y < b.Y))
                {
                    A = a;
                    B = b;
                }
                else
                {
                    A = b;
                    B = a;
                }
            }

            public bool Equals(Edge other) => A == other.A && B == other.B;
            public override bool Equals(object obj) => obj is Edge e && Equals(e);
            public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
        }

        // Main function: finds the nearest intersection point with any rectangle edge
        // Updated to use List<Rect>
        public static bool FindNearestIntersection(List<Rect> rectangles, Point segStart, Point segEnd, out Point nearestIntersection)
        {
            nearestIntersection = new Point();
            double minDistance = double.MaxValue;
            bool found = false;

            foreach (var rect in rectangles)
            {
                Point topLeft = new Point(rect.X, rect.Y);
                Point topRight = new Point(rect.X + rect.Width, rect.Y);
                Point bottomRight = new Point(rect.X + rect.Width, rect.Y + rect.Height);
                Point bottomLeft = new Point(rect.X, rect.Y + rect.Height);

                Point[][] edges = new Point[][]
                {
                new[] { topLeft, topRight },       // Top
                new[] { topRight, bottomRight },   // Right
                new[] { bottomRight, bottomLeft }, // Bottom
                new[] { bottomLeft, topLeft }      // Left
                };

                foreach (var edge in edges)
                {
                    if (TryGetHVIntersection(segStart, segEnd, edge[0], edge[1], out Point intersection))
                    {
                        double dist = GetDistance(segStart, intersection);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestIntersection = intersection;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        // Returns the Euclidean distance between two points
        public static double GetDistance(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // Checks if the point is inside or on the edge of the axis-aligned rectangle defined by rectPt1 and rectPt2
        public static bool IsPointInsideRectangle(Point rectPt1, Point rectPt2, Point testPt)
        {
            double minX = Math.Min(rectPt1.X, rectPt2.X);
            double maxX = Math.Max(rectPt1.X, rectPt2.X);
            double minY = Math.Min(rectPt1.Y, rectPt2.Y);
            double maxY = Math.Max(rectPt1.Y, rectPt2.Y);

            return testPt.X >= minX && testPt.X <= maxX &&
                   testPt.Y >= minY && testPt.Y <= maxY;
        }


        // Assumes segments are either horizontal or vertical.
        public static bool TryGetHVIntersection(Point a1, Point a2, Point b1, Point b2, out Point intersection)
        {
            intersection = new Point();

            // Identify orientation of segments
            bool aIsHorizontal = Math.Abs(a1.Y - a2.Y) < 1e-10;
            bool bIsHorizontal = Math.Abs(b1.Y - b2.Y) < 1e-10;

            // Only check perpendicular lines (horizontal vs vertical)
            if (aIsHorizontal == bIsHorizontal)
                return false;

            Point hStart, hEnd, vStart, vEnd;

            if (aIsHorizontal)
            {
                hStart = a1; hEnd = a2;
                vStart = b1; vEnd = b2;
            }
            else
            {
                hStart = b1; hEnd = b2;
                vStart = a1; vEnd = a2;
            }

            // Normalize points
            if (hStart.X > hEnd.X)
            {
                var temp = hStart;
                hStart = hEnd;
                hEnd = temp;
            }

            if (vStart.Y > vEnd.Y)
            {
                var temp = vStart;
                vStart = vEnd;
                vEnd = temp;
            }

            // Check if the vertical line crosses the horizontal one
            if (vStart.X >= hStart.X && vStart.X <= hEnd.X &&
                hStart.Y >= vStart.Y && hStart.Y <= vEnd.Y)
            {
                intersection = new Point(vStart.X, hStart.Y);
                return true;
            }

            return false;
        }
    }
}
