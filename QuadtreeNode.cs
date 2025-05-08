using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

public class QuadtreeNode
{
    private const int MaxGeometries = 4;
    private const int MaxDepth = 10;

    public Rect Bounds;
    public List<Geometry> Geometries;
    public QuadtreeNode[] Children;
    public int Depth;

    public QuadtreeNode(Rect bounds, int depth = 0)
    {
        Bounds = bounds;
        Geometries = new List<Geometry>();
        Children = null;
        Depth = depth;
    }

    // Insert a Geometry object into the quadtree
    public void Insert(Geometry geometry)
    {
        if (geometry == null)
            return;

        Rect geometryBounds = geometry.Bounds;

        // If the geometry doesn't intersect with the quadtree node's bounds, return early
        if (!Bounds.IntersectsWith(geometryBounds))
            return;

        // If the current node has enough room, insert the geometry
        if (Children == null && (Geometries.Count < MaxGeometries || Depth >= MaxDepth))
        {
            Geometries.Add(geometry);
            return;
        }

        // Subdivide the node if necessary
        if (Children == null)
            Subdivide();

        // If subdivision is done, insert the geometry into the appropriate child node
        for (int i = 0; i < 4; i++)
        {
            Children[i].Insert(geometry);
        }
    }

    // Subdivide the current node into 4 children
    private void Subdivide()
    {
        Children = new QuadtreeNode[4];

        double halfWidth = Bounds.Width / 2;
        double halfHeight = Bounds.Height / 2;
        double x = Bounds.X;
        double y = Bounds.Y;

        // Create the 4 child nodes
        Children[0] = new QuadtreeNode(new Rect(x, y, halfWidth, halfHeight), Depth + 1); // Top-left
        Children[1] = new QuadtreeNode(new Rect(x + halfWidth, y, halfWidth, halfHeight), Depth + 1); // Top-right
        Children[2] = new QuadtreeNode(new Rect(x, y + halfHeight, halfWidth, halfHeight), Depth + 1); // Bottom-left
        Children[3] = new QuadtreeNode(new Rect(x + halfWidth, y + halfHeight, halfWidth, halfHeight), Depth + 1); // Bottom-right

        // Move the geometries to the appropriate child node
        for (int i = 0; i < Geometries.Count; i++)
        {
            Geometry g = Geometries[i];
            for (int j = 0; j < 4; j++)
            {
                Children[j].Insert(g);
            }
        }

        // Clear the current node's geometries, as they are now in the children
        Geometries.Clear();
    }

    // Find the geometry containing the given point
    public Geometry FindContaining(Point p)
    {
        // If the point is outside the bounds, return null
        if (!Bounds.Contains(p))
            return null;

        // Check if any of the geometries in this node contain the point
        foreach (var g in Geometries)
        {
            if (g.Bounds.Contains(p) && g.FillContains(p)) // Check if the geometry contains the point
                return g;
        }

        // Recursively check the child nodes if the point is within their bounds
        if (Children != null)
        {
            for (int i = 0; i < 4; i++)
            {
                Geometry result = Children[i].FindContaining(p);
                if (result != null)
                    return result;
            }
        }

        return null;
    }
}
