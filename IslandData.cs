using System.Windows;
using System.Windows.Media;

namespace Polygonizer
{
    public class IslandData
    {

        public int Id;
        public double Area;
        public Point CentroidPt;
        public Geometry IslandGeometry;

        public IslandData(int i, double v, Point centroid, Geometry islandGeometry)
        {
            this.Id = i;
            this.Area = v;
            this.CentroidPt = centroid;
            this.IslandGeometry = islandGeometry;
        }
    }
}
