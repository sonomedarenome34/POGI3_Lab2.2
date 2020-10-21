using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Device.Location;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab2_2
{
	public class Human : MapObject
	{
		private PointLatLng _point;
		private PointLatLng _destinationPoint;
		private GMapMarker _humanMarker;
		public event EventHandler Seated;
		public bool IsSeated = false;

		public Human(string title, PointLatLng point) : base(title) => _point = point;

		public override double GetDistance(PointLatLng point)
		{
			GeoCoordinate c1 = new GeoCoordinate(_point.Lat, point.Lng);
			GeoCoordinate c2 = new GeoCoordinate(point.Lat, _point.Lng);
			return c1.GetDistanceTo(c2);
		}

		public override PointLatLng GetFocus() => _point;

		public override GMapMarker GetMarker()
		{
			if (_humanMarker != null)
				return _humanMarker;

			double width = 40;
			double height = 40;
			var tt = new TranslateTransform(height / -2, -(width));
			GMapMarker marker = new GMapMarker(_point)
			{
				Shape = new Image
				{
					Width = width,
					Height = height,
					ToolTip = this.GetTitle(),
					Source = new BitmapImage(new Uri("pack://application:,,,/images/person.png")),
					RenderTransform = tt
				}
			};
			_humanMarker = marker;
			return marker;
		}

		public PointLatLng GetDestinationPoint() => _destinationPoint;

		public void SetPosition(PointLatLng point) => _point = point;

		public void MoveTo(PointLatLng point) => _destinationPoint = point;

		public void CarArrived(object sender, EventArgs e)
		{
			IsSeated = !IsSeated;
			if (IsSeated) Seated?.Invoke(this, EventArgs.Empty);
		}
	}
}
