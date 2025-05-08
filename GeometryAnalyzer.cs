using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Polygonizer
{
    public class GeometryAnalyzer
    {
        const bool DEBUG_ON = false;  // switch to display additional debug info 

        private const double tolerance = 0.1;

        string title_str = "";

        public List<(double x, double y)> allCornerPoints = new List<(double x, double y)>();
        public List<(double x, double y)> externalCornerPoints = new List<(double x, double y)>();
        public List<(double x, double y)> internalCornerPoints = new List<(double x, double y)>();

        public double? WidthAtPoint { get; set; } = null;
        public double? HeightAtPoint { get; set; } = null;

        // Define the Islands for our rectagles -- where Islands are independent groupings of overlapping rectangles.
        private List<List<Rect>> Islands = new List<List<Rect>>();
        private List<Geometry> IslandGeometries = new List<Geometry>();
        public List<IslandData> IslandResults { get; private set; } = new List<IslandData>();

        public List<Rect> DataRects = new List<Rect>();

        public GeometryAnalyzer(List<Rect> rects)
        {
            DataRects = rects;
        }

        public void Analyze()
        {
            // Step 1: Group rectangles into connected Islands
            Islands = GroupConnectedRectangles(DataRects);

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

                var pathGeometry = combined.GetFlattenedPathGeometry();
                Point centroid = ComputeCentroidFromGeometry(pathGeometry);
                IslandResults.Add(new IslandData(i, combined.GetArea(), centroid, pathGeometry));
            }

            // todo
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

        public (double? left, double? right, double? up, double? down) FindNearestEdgeDistances(Geometry geometry, Point testPoint, double tolerance = 1e-6)
        {
            if (geometry == null)
            {
               return(null, null, null, null);
            }

            var pathGeometry = geometry.GetFlattenedPathGeometry();

            double? nearestLeft = null;
            double? nearestRight = null;
            double? nearestUp = null;
            double? nearestDown = null;

            if (IsPointOnBoundary(geometry, testPoint))
                return (null, null, null, null);

            foreach (var figure in pathGeometry.Figures)
            {
                Point previous = figure.StartPoint;

                foreach (var segment in figure.Segments)
                {
                    if (segment is PolyLineSegment polylineSegment)
                    {
                        foreach (Point current in polylineSegment.Points)
                        {
                            ProcessSegment(previous, current);
                            previous = current;
                        }
                    }
                    else if (segment is LineSegment lineSegment)
                    {
                        Point current = lineSegment.Point;
                        ProcessSegment(previous, current);
                        previous = current;
                    }
                }
            }

            return (nearestLeft, nearestRight, nearestUp, nearestDown);

            void ProcessSegment(Point start, Point end)
            {
                if (Math.Abs(start.X - end.X) < tolerance)
                {
                    // Vertical segment
                    double x = start.X;
                    double minY = Math.Min(start.Y, end.Y);
                    double maxY = Math.Max(start.Y, end.Y);

                    if (testPoint.Y >= minY && testPoint.Y <= maxY)
                    {
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
                else if (Math.Abs(start.Y - end.Y) < tolerance)
                {
                    // Horizontal segment
                    double y = start.Y;
                    double minX = Math.Min(start.X, end.X);
                    double maxX = Math.Max(start.X, end.X);

                    if (testPoint.X >= minX && testPoint.X <= maxX)
                    {
                        if (y < testPoint.Y)
                        {
                            double dist = testPoint.Y - y;
                            if (nearestUp == null || dist < nearestUp)
                                nearestUp = dist;
                        }
                        else if (y > testPoint.Y)
                        {
                            double dist = y - testPoint.Y;
                            if (nearestDown == null || dist < nearestDown)
                                nearestDown = dist;
                        }
                    }
                }
            }
        }


        // Helper function to check if a point is on any of the boundary segments
        private static bool IsPointOnBoundary(Geometry geometry, Point testPoint)
        {
            var pathGeometry = geometry.GetFlattenedPathGeometry();

            foreach (var figure in pathGeometry.Figures)
            {
                foreach (var segment in figure.Segments)
                {
                    if (segment is PolyLineSegment polylineSegment)
                    {
                        int vertex_count = polylineSegment.Points.Count;

                        for (int i = 0; i < polylineSegment.Points.Count; i++)
                        {
                            Point segmentStart = polylineSegment.Points[i % vertex_count];
                            Point segmentEnd = polylineSegment.Points[(i + 1) % vertex_count];

                            // Check if the point is on the segment using IsPointOnLineSegment logic
                            if (IsPointOnLineSegment(segmentStart, segmentEnd, testPoint))
                            {
                                return true; // The point is on the boundary
                            }
                        }
                    }
                }
            }
            return false; // The point is not on the boundary
        }

        // Helper function to check if the point lies on the line segment
        private static bool IsPointOnLineSegment(Point start, Point end, Point testPoint)
        {
            // Check if the point is within the bounds of the line segment (both X and Y)
            bool isOnSegment = (testPoint.X >= Math.Min(start.X, end.X) && testPoint.X <= Math.Max(start.X, end.X)) &&
                               (testPoint.Y >= Math.Min(start.Y, end.Y) && testPoint.Y <= Math.Max(start.Y, end.Y));

            // Additionally check if the point is collinear with the segment (cross product = 0)
            double crossProduct = (testPoint.Y - start.Y) * (end.X - start.X) - (testPoint.X - start.X) * (end.Y - start.Y);
            return isOnSegment && Math.Abs(crossProduct) < tolerance; // Allow small tolerance for floating-point precision
        }

        /// <summary>
        /// Checks if the given point is inside any of the provided geometries.
        /// Returns the first geometry that contains the point, or null if none do.
        /// </summary>
        public Geometry FindContainingGeometry(List<IslandData> data, Point testPoint, double tolerance = 0.5)
        {
            for (int i = 0; i < data.Count; i++){
                var geometry = data[i].IslandGeometry;
                if (geometry.FillContains(testPoint, tolerance, ToleranceType.Absolute))
                {
                    return geometry;
                }
            }

            return null;
        }

        public void FloodFill(bool[,] grid, bool[,] visited, int x, int y, List<(int x, int y)> region)
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

        public void TraceBoundary(List<(int x, int y)> region, bool[,] grid, Rect bounds)
        {
            foreach (var (x, y) in region)
            {
                ProcessCornerDetection(grid, x, y);
            }
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

                externalCornerPoints = RemoveDuplicates(externalCornerPoints);
                internalCornerPoints = RemoveDuplicates(internalCornerPoints);
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

                public static Point ComputeCentroidFromGeometry(Geometry geometry)
        {
            var pathGeometry = geometry.GetFlattenedPathGeometry();
            double signedArea = 0;
            double centroidX = 0;
            double centroidY = 0;

            foreach (var figure in pathGeometry.Figures)
            {
                List<Point> points = new List<Point>();
                Point start = figure.StartPoint;
                points.Add(start);

                foreach (var segment in figure.Segments)
                {
                    if (segment is PolyLineSegment poly)
                    {
                        points.AddRange(poly.Points);
                    }
                    else if (segment is LineSegment line)
                    {
                        points.Add(line.Point);
                    }
                }

                // Ensure the polygon is closed
                if (points[0] != points[points.Count - 1])
                {
                    points.Add(points[0]);
                }

                for (int i = 0; i < points.Count - 1; i++)
                {
                    double xi = points[i].X;
                    double yi = points[i].Y;
                    double xi1 = points[i + 1].X;
                    double yi1 = points[i + 1].Y;

                    double a = (xi * yi1 - xi1 * yi);
                    signedArea += a;
                    centroidX += (xi + xi1) * a;
                    centroidY += (yi + yi1) * a;
                }
            }

            signedArea *= 0.5;

            if (Math.Abs(signedArea) < 1e-6)
                return new Point(0, 0); // Avoid division by zero

            centroidX /= (6 * signedArea);
            centroidY /= (6 * signedArea);

            return new Point(centroidX, centroidY);
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
    }
}
