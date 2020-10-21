using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace Lab2_2
{
	public class Area : MapObject
	{
		private List<PointLatLng> _pointsList;

		public Area(string title, List<PointLatLng> pointsList) : base(title)
		{
			if (pointsList.Count < 3)
				throw new ArgumentException("List of points must contain at least 3 elements.");

			_pointsList = new List<PointLatLng>();
			foreach (var point in pointsList)
				_pointsList.Add(point);
		}

		public override double GetDistance(PointLatLng point)
		{
			GeoCoordinate c1 = new GeoCoordinate(_pointsList[0].Lat, point.Lng);
			GeoCoordinate c2 = new GeoCoordinate(point.Lat, _pointsList[0].Lng);
			return c1.GetDistanceTo(c2);
		}

		public override PointLatLng GetFocus() => _pointsList[0];

		public override GMapMarker GetMarker()
		{
			GMapMarker marker = new GMapPolygon(_pointsList)
			{
				Shape = new Path
				{
					Stroke = Brushes.Black,
					Fill = Brushes.MediumSlateBlue,
					Opacity = 0.6
				}
			};
			return marker;
		}
	}
}
