using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Polygonizer
{
    public partial class MainWindow : Window
    {
        int testPtX = 300;
        int testPtY = 310;

        const bool DEBUG_ON = false;
        const int CellSize = 2;
        const int ExtraPadding = 0;

        List<(double x, double y)> allCornerPoints = new List<(double x, double y)>();
        List<(double x, double y)> externalCornerPoints = new List<(double x, double y)>();
        List<(double x, double y)> internalCornerPoints = new List<(double x, double y)>();

        // Define the Islands for our rectagles -- where Islands are independent groupings of overlapping rectangles.
        List<List<Rect>> Islands = new List<List<Rect>>();
        private List<Geometry> IslandGeometries = new List<Geometry>();



        /// <summary>
        /// Define our rectangles
        /// </summary>
        List<Rect> rectangles = new List<Rect>
        {
            new Rect(10, 180, 100, 100),

            new Rect(120, 120, 200, 150),
            new Rect(250, 200, 200, 150),
            new Rect(150, 300, 200, 150),

            new Rect(410, 410, 100, 100),


            new Rect(500, 100, 100, 80),  // this is the isolated rectangle
            new Rect(540, 140, 100, 80),  // this is the isolated rectangle

        };

        public MainWindow()
        {
            InitializeComponent();
            DrawScene();

            Point testPoint = new Point(testPtX, testPtY);
            Geometry found = FindContainingGeometry(IslandGeometries, testPoint);


            if (found is PathGeometry pathGeometry)
            {
                var (left, right) = FindNearestVerticalEdgeDistances(pathGeometry, testPoint);
                var (up, down) = FindNearestHorizontalEdgeDistances(pathGeometry, testPoint);

                Console.WriteLine($"Left: {left}, Right: {right}");
                Console.WriteLine($"Up: {up}, Down: {down}");


                // draw a line from the test point to the left edge
                var leftLine = new Line
                {
                    X1 = testPtX,
                    Y1 = testPtY,
                    X2 = testPtX - (left ?? 0),
                    Y2 = testPtY,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };
                MainCanvas.Children.Add(leftLine);

                // draw a line from the test point to the right edge
                var rightLine = new Line
                {
                    X1 = testPtX,
                    Y1 = testPtY,
                    X2 = testPtX + (right ?? 0),
                    Y2 = testPtY,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };
                MainCanvas.Children.Add(rightLine);

                // draw a line from the test point to the top edge
                var topLine = new Line
                {
                    X1 = testPtX,
                    Y1 = testPtY,
                    X2 = testPtX ,
                    Y2 = testPtY - (up ?? 0),
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };
                MainCanvas.Children.Add(topLine);

                // draw a line from the test point to the bottom edge
                var botLine = new Line
                {
                    X1 = testPtX,
                    Y1 = testPtY,
                    X2 = testPtX,
                    Y2 = testPtY + (down ?? 0),
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };
                MainCanvas.Children.Add(botLine);
            }
            else
            {
                Console.WriteLine("Geometry is not a PathGeometry.");
            }



            // draw the test point marker
            var circle = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Black
            };
            Canvas.SetLeft(circle, testPtX - 5);
            Canvas.SetTop(circle, testPtY - 5);
            MainCanvas.Children.Add(circle);
        }

        public static bool IsPathClosed(Geometry geometry)
        {
            var pathGeometry = geometry.GetFlattenedPathGeometry();

            // Iterate over each figure in the path geometry
            foreach (var figure in pathGeometry.Figures)
            {
                // Check if the first and last points are the same to see if the path is closed
                if (figure.Segments.Last() is LineSegment lastSegment)
                {
                    // Compare the first and last point
                    if (figure.StartPoint != lastSegment.Point)
                    {
                        return false; // Path is not closed
                    }
                }
            }

            return true; // Path is closed
        }

        public static (double? left, double? right) FindNearestVerticalEdgeDistances(Geometry geometry, Point testPoint)
        {
            Console.WriteLine("Is path closed? " + IsPathClosed(geometry));
            var pathGeometry = geometry.GetFlattenedPathGeometry(); // Ensures it's a proper PathGeometry
            double? nearestLeft = null;
            double? nearestRight = null;

            foreach (var figure in pathGeometry.Figures)
            {
                Point start = figure.StartPoint;

                int count = 0;
                foreach (var segment in figure.Segments)
                {


                    // Check if the segment is a PolylineSegment
                    if (segment is PolyLineSegment polylineSegment)
                    {
                        int vertex_count = polylineSegment.Points.Count;
                        Console.WriteLine("number of points in polyLine: " + vertex_count);
                        for (int i = 0; i < polylineSegment.Points.Count; i++)
                        {
                            count++;
                            Point segmentStart = polylineSegment.Points[i % vertex_count];
                            Point segmentEnd = polylineSegment.Points[(i + 1) % vertex_count];

                            // Check if the polyline segment is vertical
                            if (Math.Abs(segmentStart.X - segmentEnd.X) < 0.1)
                            {
                                double x = segmentStart.X;

                                // Ensure Y range contains the test point Y
                                double minY = Math.Min(segmentStart.Y, segmentEnd.Y);
                                double maxY = Math.Max(segmentStart.Y, segmentEnd.Y);
                                if (testPoint.Y >= minY && testPoint.Y <= maxY)
                                {
                                    Console.WriteLine($"Segment Y range: {minY} to {maxY}, Test Point Y = {testPoint.Y}");

                                    if (x < testPoint.X)
                                    {
                                        double dist = testPoint.X - x;
                                        if (nearestLeft == null || dist < nearestLeft)
                                            nearestLeft = dist;
                                    }
                                    else if (x > testPoint.X)
                                    {
                                        double dist = x - testPoint.X;
                                        if (nearestRight == null || dist < nearestRight)
                                            nearestRight = dist;
                                    }
                                }
                            }
                        }
                    }

                    start = segment is LineSegment line ? line.Point : start; // Update the start point for the next segment
                }
                Console.WriteLine("count: " + count);
            }

            return (nearestLeft, nearestRight);
        }

        public static (double? up, double? down) FindNearestHorizontalEdgeDistances(Geometry geometry, Point testPoint)
        {
            var pathGeometry = geometry.GetFlattenedPathGeometry(); // Ensures it's a proper PathGeometry
            double? nearestTop = null;
            double? nearestBottom = null;

            foreach (var figure in pathGeometry.Figures)
            {
                Point start = figure.StartPoint;

                int count = 0;
                foreach (var segment in figure.Segments)
                {
                    // Check if the segment is a PolylineSegment
                    if (segment is PolyLineSegment polylineSegment)
                    {
                        int vertex_count = polylineSegment.Points.Count;
                        Console.WriteLine("number of points in polyLine: " + vertex_count);

                        for (int i = 0; i < polylineSegment.Points.Count; i++)
                        {
                            count++;

                            Point segmentStart = polylineSegment.Points[i % vertex_count];
                            Point segmentEnd = polylineSegment.Points[(i + 1) % vertex_count];

                            // Check if the polyline segment is horizontal
                            if (Math.Abs(segmentStart.Y - segmentEnd.Y) < 0.1)
                            {
                                double y = segmentStart.Y;

                                // Ensure Y range contains the test point Y
                                double minX = Math.Min(segmentStart.X, segmentEnd.X);
                                double maxX = Math.Max(segmentStart.X, segmentEnd.X);
                                if (testPoint.X >= minX && testPoint.X <= maxX)
                                {
                                    Console.WriteLine($"Segment X range: {minX} to {maxX}, Test Point X = {testPoint.X}");

                                    if (y < testPoint.Y)
                                    {
                                        double dist = testPoint.Y - y;
                                        if (nearestTop == null || dist < nearestTop)
                                            nearestTop = dist;
                                    }
                                    else if (y > testPoint.Y)
                                    {
                                        double dist = y - testPoint.Y;
                                        if (nearestBottom == null || dist < nearestBottom)
                                            nearestBottom = dist;
                                    }
                                }
                            }
                        }
                    }

                    start = segment is LineSegment line ? line.Point : start; // Update the start point for the next segment
                }
                Console.WriteLine("count: " + count);

            }

            return (nearestTop, nearestBottom);
        }

        /// <summary>
        /// Checks if the given point is inside any of the provided geometries.
        /// Returns the first geometry that contains the point, or null if none do.
        /// </summary>
        public static Geometry FindContainingGeometry(List<Geometry> geometries, Point testPoint, double tolerance = 0.5)
        {
            foreach (var geometry in geometries)
            {
                if (geometry.FillContains(testPoint, tolerance, ToleranceType.Absolute))
                {
                    return geometry;
                }
            }

            return null;
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

            // Step 1: Group rectangles into connected Islands
            Islands = GroupConnectedRectangles(rectangles);

            // Step 2: Color palette
            Brush[] colorPalette = new Brush[]
            {
                Brushes.LightBlue, Brushes.LightGreen, Brushes.LightCoral,
                Brushes.LightGoldenrodYellow, Brushes.LightPink, Brushes.LightSalmon,
                Brushes.LightSeaGreen, Brushes.LightSlateGray
            };

            // Step 3: Process each island
            for (int i = 0; i < Islands.Count; i++)
            {
                var island = Islands[i];
                Geometry combined = new RectangleGeometry(island[0]);
                for (int j = 1; j < island.Count; j++)
                {
                    combined = Geometry.Combine(combined, new RectangleGeometry(island[j]), GeometryCombineMode.Union, null);
                }

                IslandGeometries.Add(combined);

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
            List<List<Rect>> islandGroups = new List<List<Rect>>();
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

                islandGroups.Add(group);
            }

            return islandGroups;
        }

        private bool RectsTouchOrOverlap(Rect a, Rect b)
        {
            return a.IntersectsWith(b) || a.Contains(b) || b.Contains(a);
        }

        private void DrawIslandBoundary(List<Rect> rectangles)
        {
            DrawUnionOutlineWithColors(rectangles);

        }

        /// <summary>
        /// Returns an index for the island group that contains the point(px,py);
        /// </summary>
        /// <param name="px"></param>
        /// <param name="py"></param>
        /// <returns></returns>
        private int GetIslandIndexContainingPoint(double px, double py)
        {
            for (int i = 0; i < Islands.Count; i++)
            {
                foreach (var rect in Islands[i])
                {
                    if (rect.Contains(px, py))
                    {
                        return i; // Found the island
                    }
                }
            }

            return -1; // Not found
        }

        /// <summary>
        /// Returns a list of Rect for the island that contains the point(px,py);
        /// </summary>
        /// <param name="px"></param>
        /// <param name="py"></param>
        /// <returns></returns>
        private List<Rect> GetIslandContainingPoint(double px, double py)
        {
            foreach (var island in Islands)
            {
                foreach (var rect in island)
                {
                    if (rect.Contains(px, py))
                    {
                        return island; // Found the island
                    }
                }
            }

            return null; // No island contains the point
        }

        /// <summary>
        /// Returns the index of the island geometry that contains the given point.
        /// </summary>
        private int GetIslandGeometryIndexContainingPoint(double px, double py)
        {
            Point point = new Point(px, py);
            for (int i = 0; i < IslandGeometries.Count; i++)
            {
                if (IslandGeometries[i].FillContains(point))
                {
                    return i;
                }
            }
            return -1; // No island contains the point
        }

        private List<Rect> GetIslandRectsContainingPoint(double px, double py)
        {
            int index = GetIslandGeometryIndexContainingPoint(px, py);
            if (index >= 0 && index < Islands.Count)
                return Islands[index];
            return null;
        }

        private Rect? GetIslandBoundsContainingPoint(double px, double py)
        {
            int index = GetIslandGeometryIndexContainingPoint(px, py);
            if (index >= 0 && index < IslandGeometries.Count)
                return IslandGeometries[index].Bounds;
            return null;
        }

        /// <summary>
        /// Returns the actual width and height of the island border at the given point,
        /// based on the outermost coordinates of the PathGeometry.
        /// </summary>
        private (double width, double height)? GetIslandGeometrySizeAtPoint(double px, double py)
        {
            Point point = new Point(px, py);

            // Iterate over each island geometry
            for (int i = 0; i < IslandGeometries.Count; i++)
            {
                // Get the current island's geometry
                Geometry geometry = IslandGeometries[i];

                // Check if the point is inside the current geometry
                if (geometry.FillContains(point))
                {
                    // If the point is inside the geometry, calculate its bounds
                    PathGeometry pathGeometry = geometry.GetOutlinedPathGeometry();

                    double minX = double.MaxValue;
                    double maxX = double.MinValue;
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;

                    // Iterate over each figure in the path geometry
                    foreach (PathFigure figure in pathGeometry.Figures)
                    {
                        // Update bounds for the starting point of the figure
                        UpdateBounds(figure.StartPoint, ref minX, ref maxX, ref minY, ref maxY);

                        // Iterate over each segment of the figure and update bounds
                        foreach (PathSegment segment in figure.Segments)
                        {
                            if (segment is PolyLineSegment poly)
                            {
                                foreach (Point p in poly.Points)
                                    UpdateBounds(p, ref minX, ref maxX, ref minY, ref maxY);
                            }
                            else if (segment is LineSegment line)
                            {
                                UpdateBounds(line.Point, ref minX, ref maxX, ref minY, ref maxY);
                            }
                        }
                    }

                    // The width and height are the differences between the min and max coordinates
                    double width = maxX - minX;
                    double height = maxY - minY;

                    return (width, height);
                }
            }

            // If no island contains the point, return null
            return null;
        }

        /// <summary>
        /// Updates the bounding extents from a point.
        /// </summary>
        private void UpdateBounds(Point p, ref double minX, ref double maxX, ref double minY, ref double maxY)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        private void DrawMeasurementLineAtPoint(double px, double py)
        {
            var size = GetIslandGeometrySizeAtPoint(px, py);

            if (size.HasValue)
            {
                double width = size.Value.width;
                double height = size.Value.height;

                // Assuming the selected geometry's bounds are already known
                // You can calculate where to draw the line (we'll use the min/max points from the extents)
                Point startPoint = new Point(px, py); // Start point of measurement
                Point endPoint = new Point(px + width, py); // For width

                // Create the line (for width in this case)
                Line line = new Line
                {
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = endPoint.X,
                    Y2 = endPoint.Y,
                    Stroke = Brushes.Red, // Line color (Red for visibility)
                    StrokeThickness = 2
                };

                // Add the line to the canvas
                MainCanvas.Children.Add(line);

                // Optionally, show the height as a vertical line
                startPoint = new Point(px, py); // Reset to initial point
                endPoint = new Point(px, py + height); // For height

                // Create the vertical line (for height)
                Line verticalLine = new Line
                {
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = endPoint.X,
                    Y2 = endPoint.Y,
                    Stroke = Brushes.Blue, // Line color (Blue for height)
                    StrokeThickness = 2
                };

                // Add the vertical line to the canvas
                MainCanvas.Children.Add(verticalLine);
            }
            else
            {
                MessageBox.Show("No geometry found at this point.");
            }
        }

        /// <summary>
        /// Determines the number of pixels between the island's boundary polygon in the horizontal and vertical directions, passing through point (px, py).
        /// </summary>
        private (double horizontalDistance, double verticalDistance)? GetDistanceToIslandBoundary(double px, double py)
        {
            Point point = new Point(px, py);

            // Step 1: Iterate through the island geometries
            for (int i = 0; i < IslandGeometries.Count; i++)
            {
                var geometry = IslandGeometries[i];

                // Check if the point is inside the island geometry
                if (geometry.FillContains(point))
                {
                    // Step 2: Get the outlined path geometry
                    PathGeometry pathGeometry = geometry.GetOutlinedPathGeometry();

                    // Initialize variables to store the minimum horizontal and vertical distances
                    double minHorizontalDistance = double.MaxValue;
                    double minVerticalDistance = double.MaxValue;

                    // Step 3: Iterate through the segments of the path geometry to calculate distances
                    foreach (PathFigure figure in pathGeometry.Figures)
                    {
                        // Check distances to each segment
                        foreach (PathSegment segment in figure.Segments)
                        {
                            if (segment is LineSegment lineSegment)
                            {
                                // For horizontal distance, compare the X-coordinate of the point with the line's X-coordinate range
                                minHorizontalDistance = Math.Min(minHorizontalDistance, GetDistanceToHorizontalSegment(lineSegment, point));

                                // For vertical distance, compare the Y-coordinate of the point with the line's Y-coordinate range
                                minVerticalDistance = Math.Min(minVerticalDistance, GetDistanceToVerticalSegment(lineSegment, point));
                            }
                            // You can extend for other types of segments like PolyLineSegment if needed
                        }
                    }

                    return (minHorizontalDistance, minVerticalDistance);
                }
            }

            return null; // If no island geometry contains the point
        }

        /// <summary>
        /// Calculates the horizontal distance between the point and the closest point on the given horizontal line segment.
        /// </summary>
        private double GetDistanceToHorizontalSegment(LineSegment lineSegment, Point point)
        {
            // Check if the line is horizontal
            if (lineSegment.Point.Y == point.Y)
            {
                // Distance is the horizontal distance (X-difference)
                return Math.Abs(lineSegment.Point.X - point.X);
            }

            return double.MaxValue; // Return a large value if the line is not horizontal
        }

        /// <summary>
        /// Calculates the vertical distance between the point and the closest point on the given vertical line segment.
        /// </summary>
        private double GetDistanceToVerticalSegment(LineSegment lineSegment, Point point)
        {
            // Check if the line is vertical
            if (lineSegment.Point.X == point.X)
            {
                // Distance is the vertical distance (Y-difference)
                return Math.Abs(lineSegment.Point.Y - point.Y);
            }

            return double.MaxValue; // Return a large value if the line is not vertical
        }

        private (double leftDistance, double rightDistance)? GetDistanceToVerticalEdges(double px, double py)
        {
            Point testPoint = new Point(px, py);

            foreach (var geometry in IslandGeometries)
            {
                // Use more precise point-in-geometry test
                if (!geometry.FillContains(testPoint, 0.5, ToleranceType.Absolute))
                    continue;

                // Geometry found — now check vertical edges
                PathGeometry pathGeometry = geometry.GetOutlinedPathGeometry();

                double? nearestLeftEdge = null;
                double? nearestRightEdge = null;

                foreach (var figure in pathGeometry.Figures)
                {
                    Point previousPoint = figure.StartPoint;

                    foreach (var segment in figure.Segments)
                    {
                        if (segment is LineSegment lineSegment)
                        {
                            Point currentPoint = lineSegment.Point;

                            // Check vertical edge
                            if (Math.Abs(previousPoint.X - currentPoint.X) < 0.1)
                            {
                                double x = previousPoint.X;

                                if (x < px)
                                {
                                    if (!nearestLeftEdge.HasValue || x > nearestLeftEdge.Value)
                                        nearestLeftEdge = x;
                                }
                                else if (x > px)
                                {
                                    if (!nearestRightEdge.HasValue || x < nearestRightEdge.Value)
                                        nearestRightEdge = x;
                                }
                            }

                            previousPoint = currentPoint;
                        }
                    }
                }

                if (nearestLeftEdge.HasValue && nearestRightEdge.HasValue)
                {
                    return (px - nearestLeftEdge.Value, nearestRightEdge.Value - px);
                }

                // If we got here, the point was inside, but vertical edges weren't found
                return null;
            }

            // No geometry contained the point
            return null;
        }



    }
}
