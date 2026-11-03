// ==============================================================================
// Golden Implementation: WinUI 3 Smooth Cubic Bézier Spline Connector
// ==============================================================================
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Lexora.Presentation.Examples;

public static class BezierSplineGenerator
{
    public static PathGeometry GenerateSmoothConnectingPath(IReadOnlyList<Point> nodeCenterPoints)
    {
        var geometry = new PathGeometry();
        if (nodeCenterPoints == null || nodeCenterPoints.Count < 2)
        {
            return geometry;
        }

        var figure = new PathFigure
        {
            StartPoint = nodeCenterPoints[0],
            IsClosed = false
        };

        for (int i = 0; i < nodeCenterPoints.Count - 1; i++)
        {
            var p0 = nodeCenterPoints[i];
            var p1 = nodeCenterPoints[i + 1];

            // Use vertical midpoint tangents for a fluid serpentine flow
            double midY = (p0.Y + p1.Y) / 2.0;
            var control1 = new Point(p0.X, midY);
            var control2 = new Point(p1.X, midY);

            var segment = new BezierSegment
            {
                Point1 = control1,
                Point2 = control2,
                Point3 = p1
            };

            figure.Segments.Add(segment);
        }

        geometry.Figures.Add(figure);
        return geometry;
    }
}

