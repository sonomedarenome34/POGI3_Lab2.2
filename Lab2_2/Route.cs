using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace Lab2_2
{
	public class Route : MapObject
	{
		private List<PointLatLng> _pointsList;

		public Route(string title, List<PointLatLng> pointsList) : base(title)
		{
			if (pointsList.Count < 2)
				throw new ArgumentException("List of points must contain at least 2 elements.");

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
			GMapMarker marker = new GMapRoute(_pointsList)
			{
				Shape = new Path()
				{
					Stroke = Brushes.DarkBlue,
					Fill = Brushes.DarkBlue,
					StrokeThickness = 4
				}
			};
			return marker;
		}

		public List<PointLatLng> GetPoints() => _pointsList;
	}
}
