using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Polygonizer
{
    public partial class MainWindow : Window
    {
        private bool bFirstLoad = true;

        double testPtX = 0;
        double testPtY = 0;

        const int CellSize = 2;
        const int ExtraPadding = 0;

        GeometryAnalyzer geometryAnalyzer { get; set; }

        /// <summary>
        /// Define our rectangles
        /// </summary>
        public static readonly List<Rect> DefaultRectangles = new List<Rect>
        {
            new Rect(5, 5, 10, 10),
            new Rect(10, 5, 10, 10),
            new Rect(15, 5, 10, 10),
            new Rect(20, 5, 10, 10),
            new Rect(25, 5, 10, 10),
            new Rect(30, 5, 10, 10),
            new Rect(35, 5, 10, 10),
            new Rect(40, 5, 10, 10),
            new Rect(45, 5, 10, 10),
            new Rect(50, 5, 10, 10),
            new Rect(50, 10, 10, 10),
            new Rect(50, 15, 10, 10),
            new Rect(50, 20, 10, 10),
            new Rect(50, 25, 10, 10),
            new Rect(50, 30, 10, 10),
            new Rect(50, 35, 10, 10),
            new Rect(50, 40, 10, 10),
            new Rect(50, 45, 10, 10),


            new Rect(100, 10, 200, 200),

            new Rect(120, 120, 200, 150),
            new Rect(250, 200, 200, 150),
            new Rect(400, 300, 200, 200),

            new Rect(200, 300, 200, 150),
            new Rect(460, 480, 100, 100),


            new Rect(500, 100, 100, 80),
            new Rect(540, 140, 100, 80),
        };

        public List<Rect> rectangles = DefaultRectangles;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                geometryAnalyzer = new GeometryAnalyzer(rectangles);
                geometryAnalyzer.Analyze();
                DrawScene();

                bFirstLoad = false;
            };
        }

        /// <summary>
        /// constructor for taking a list of rectangles and running the algorithm on them
        /// </summary>
        /// <param name="rect"></param>
        public MainWindow(List<Rect> rect) : this()
        {
            this.rectangles = rect;
        }

        private void UpdateUI(Canvas cnv)
        {
            if (!bFirstLoad)
            {
                // Draw the island Boundary
                DrawUnionOutlineWithColors(rectangles);

                // Draw the centroid markers
                foreach(var geom in geometryAnalyzer.IslandResults)
                {
                    DrawCentroidPoints(geom.CentroidPt);
                }
            }
        }

        private void DrawOffsetLine(double? offset, double ptX, double ptY, Brush color, string direction)
        {
            if (offset == null) return;

            double x2 = ptX;
            double y2 = ptY;

            switch (direction.ToLower())
            {
                case "vertical":
                    y2 += offset.Value;
                    break;
                case "horizontal":
                    x2 += offset.Value;
                    break;
                default:
                    throw new ArgumentException("Direction must be 'vertical' or 'horizontal'.");
            }

            var newLine = new Line
            {
                X1 = ptX,
                Y1 = ptY,
                X2 = x2,
                Y2 = y2,
                Stroke = color,
                StrokeThickness = 2
            };

            MainCanvas.Children.Add(newLine);
        }
        private void DrawScene()
        {
            //// Compute bounds with padding
            //Rect bounds = Rect.Empty;
            //foreach (var r in rectangles)
            //    bounds.Union(r);
            //bounds.Inflate(ExtraPadding, ExtraPadding);

            //int cols = (int)Math.Ceiling(bounds.Width / CellSize);
            //int rows = (int)Math.Ceiling(bounds.Height / CellSize);
            //bool[,] grid = new bool[rows, cols];
            //bool[,] visited = new bool[rows, cols];

            //// Fill grid with rectangles
            //foreach (var rect in rectangles)
            //{
            //    int x0 = (int)((rect.X - bounds.X) / CellSize);
            //    int y0 = (int)((rect.Y - bounds.Y) / CellSize);
            //    int x1 = (int)Math.Ceiling((rect.Right - bounds.X) / CellSize);
            //    int y1 = (int)Math.Ceiling((rect.Bottom - bounds.Y) / CellSize);

            //    for (int y = y0; y < y1; y++)
            //        for (int x = x0; x < x1; x++)
            //            grid[y, x] = true;
            //}

            //// Flood fill to find all distinct regions
            //for (int y = 0; y < rows; y++)
            //{
            //    for (int x = 0; x < cols; x++)
            //    {
            //        if (grid[y, x] && !visited[y, x])
            //        {
            //            var region = new List<(int x, int y)>();
            //            geometryAnalyzer.FloodFill(grid, visited, x, y, region);
            //            geometryAnalyzer.TraceBoundary(region, grid, bounds);
            //            DrawCornerPoints(geometryAnalyzer.externalCornerPoints, bounds, Brushes.Green);
            //            DrawCornerPoints(geometryAnalyzer.internalCornerPoints, bounds, Brushes.Red);
            //        }
            //    }
            //}
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
        private void DrawCentroidPoints(Point centroid)
        {
            var circle = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Black
            };
            Canvas.SetLeft(circle, centroid.X - 5);
            Canvas.SetTop(circle, centroid.Y - 5);
            MainCanvas.Children.Add(circle);
        }
        private void DrawUnionOutlineWithColors(List<Rect> rectangles)
        {
            if (rectangles == null || rectangles.Count == 0)
                return;

            // Step 2: Color palette
            Brush[] colorPalette = new Brush[]
            {
                Brushes.LightBlue, Brushes.LightGreen, Brushes.LightCoral,
                Brushes.LightGoldenrodYellow, Brushes.LightPink, Brushes.LightSalmon,
                Brushes.LightSeaGreen, Brushes.LightSlateGray
            };

            // Step 3: Process each island and draw the boundary path
            for (int i = 0; i < geometryAnalyzer.IslandResults.Count; i++)
            {
                Path path = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.5,
                    Fill = colorPalette[i % colorPalette.Length],
                    Data = geometryAnalyzer.IslandResults[i].IslandGeometry
                };

                MainCanvas.Children.Add(path);
            }
        }



        private void MainCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point testPoint = e.GetPosition(MainCanvas);
            testPtX = testPoint.X;
            testPtY = testPoint.Y;

            MainCanvas.Children.Clear();
            // Optional: Only clear measurement overlays instead of full canvas
            DrawScene();

            UpdateUI(MainCanvas);

            Geometry found = geometryAnalyzer.FindContainingGeometry(geometryAnalyzer.IslandResults, testPoint);
            if (found != null)
            {
                var (left, right, up, down) = geometryAnalyzer.FindNearestEdgeDistances(found, testPoint);
                if (left.HasValue && right.HasValue && up.HasValue && down.HasValue)
                {
                    double width = left.Value + right.Value;
                    double height = up.Value + down.Value;

                    UpdateMeasurementLines(testPoint, left, right, up, down);

                    Title = $"At {testPtX:F0}, {testPtY:F0} -- Height: {height:F2}, Width: {width:F2}";
                }
            }
            else
            {
                Title = $"At {testPtX:F0}, {testPtY:F0} -- No island contains this point";
            }

            // Draw marker for test point
            DrawTestPointMarker(testPoint);
        }

        private void UpdateMeasurementLines(Point testPoint, double? left, double? right, double? up, double? down)
        {
            // Clear only the lines
            var linesToClear = MainCanvas.Children.OfType<Line>().ToList();
            foreach (var line in linesToClear)
            {
                MainCanvas.Children.Remove(line);
            }

            // Redraw the necessary lines
            if (left.HasValue) DrawOffsetLine(-left.Value, testPoint.X, testPoint.Y, Brushes.Red, "horizontal");
            if (right.HasValue) DrawOffsetLine(right.Value, testPoint.X, testPoint.Y, Brushes.Red, "horizontal");
            if (up.HasValue) DrawOffsetLine(-up.Value, testPoint.X, testPoint.Y, Brushes.Blue, "vertical");
            if (down.HasValue) DrawOffsetLine(down.Value, testPoint.X, testPoint.Y, Brushes.Blue, "vertical");
        }

        private void DrawTestPointMarker(Point pt)
        {
            Ellipse circle = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Black
            };
            Canvas.SetLeft(circle, pt.X - 5);
            Canvas.SetTop(circle, pt.Y - 5);
            MainCanvas.Children.Add(circle);
        }

    }

}
