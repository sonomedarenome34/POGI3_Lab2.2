using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GMap.NET.MapProviders;
using System.Windows;
using System.Numerics;

namespace Lab2_2
{
	public class Car : MapObject
	{
		private PointLatLng _point;
		private Route _route;
		private Human _passenger;
		private GMapMarker _carMarker;
		public event EventHandler Arrived;
		public event EventHandler PassengerArrived;
		public event EventHandler<CarPositionChangedArgs> PositionChanged;
		private Thread _moveThread;
		private List<PointLatLng> lerpPoints;

		public Car(string title, PointLatLng point) : base(title)
		{
			_point = point;
			lerpPoints = new List<PointLatLng>();
		}

		public override double GetDistance(PointLatLng point)
		{
			GeoCoordinate c1 = new GeoCoordinate(_point.Lat, point.Lng);
			GeoCoordinate c2 = new GeoCoordinate(point.Lat, _point.Lng);
			return c1.GetDistanceTo(c2);
		}

		public override PointLatLng GetFocus() => _point;

		public override GMapMarker GetMarker()
		{
			if (_carMarker != null)
				return _carMarker;
			double width = 40;
			double height = 40;
			var tt = new TranslateTransform(height / -2, width / -1.5);
			GMapMarker marker = new GMapMarker(_point)
			{
				Shape = new Image
				{
					Width = width,
					Height = height,
					ToolTip = this.GetTitle(),
					Source = new BitmapImage(new Uri("pack://application:,,,/images/car.png")),
					RenderTransform = tt
				}
			};
			_carMarker = marker;
			return marker;
		}

		public GMapMarker CreateRouteToDestinationPoint(PointLatLng endPoint)
		{
			RoutingProvider routingProvider = GMapProviders.OpenStreetMap;
			MapRoute route = routingProvider.GetRoute(_point, endPoint, false, false, 15);
			List<PointLatLng> routePoints = route.Points;
			_route = new Route("Car Route", routePoints);

			double minDistance = 0.00001;

			lerpPoints.Clear();

			for (int i = 0; i < routePoints.Count - 1; i++)
			{
				var vector1 = new Vector2((float)routePoints[i].Lat, (float)routePoints[i].Lng);
				var vector2 = new Vector2((float)routePoints[i + 1].Lat, (float)routePoints[i + 1].Lng);
				double distance = Vector2.Distance(vector1, vector2);
				if (distance > minDistance)
				{
					double aPoints = distance / minDistance;
					for (int j = 0; j < aPoints; j++)
					{
						var t = Vector2.Lerp(vector1, vector2, (float)(j / aPoints));
						lerpPoints.Add(new PointLatLng(t.X, t.Y));
					}
				}
			}

			return _route.GetMarker();
		}

		public void StartMove()
		{
			_moveThread?.Abort();
			_moveThread = new Thread(MoveByRoute);
			_moveThread.Start();
		}

		public void AddPassenger(Human passenger) => _passenger = passenger;

		public Human GetPassenger() => _passenger;

		public Route GetRoute() => _route;

		private void MoveByRoute()
		{
			lock (lerpPoints)
			{
				double cAngle = 0;

				for (int i = 0; i < lerpPoints.Count; i++)
				{
					var point = lerpPoints[i];

					if (Application.Current == null)
					{
						_moveThread?.Abort();
						return;
					}

					Application.Current.Dispatcher.Invoke(() =>
					{
						//if (i < lerpPoints.Count - 10)
						//{
						//	var nextPoint = lerpPoints[i + 10];
						//	double latDiff = nextPoint.Lat - point.Lat;
						//	double lngDiff = nextPoint.Lng - point.Lng;
						//	double angle = Math.Atan2(lngDiff, latDiff) * 180.0 / Math.PI - 90.0;

						//	if (Math.Abs(angle - cAngle) > 11)
						//	{
						//		cAngle = angle;
						//		_carMarker.Shape.RenderTransform = new RotateTransform { Angle = angle/*, CenterX = 20, CenterY = 26.67*/ };
						//	}
						//}

						_point = point;
						_carMarker.Position = point;
						if (_passenger.IsSeated)
						{
							_passenger.SetPosition(point);
							_passenger.GetMarker().Position = point;
						}
						PositionChanged?.Invoke(this, new CarPositionChangedArgs { Max = lerpPoints.Count, Progress = i });
					});

					Thread.Sleep(1);
				}

				if (_moveThread.IsAlive)
					Application.Current.Dispatcher.Invoke(() => Arrived?.Invoke(this, EventArgs.Empty));

				if (!_passenger.IsSeated)
					Application.Current.Dispatcher.Invoke(() => PassengerArrived?.Invoke(this, EventArgs.Empty));
			}
		}

		public void PassengerSeated(object sender, EventArgs e)
		{
			if (!(sender is Human passenger))
				return;
			_passenger = passenger;

			if (Application.Current == null)
			{
				_moveThread?.Abort();
				return;
			}

			Application.Current.Dispatcher.Invoke(() =>
			{
				CreateRouteToDestinationPoint(passenger.GetDestinationPoint());
				StartMove();
			});
		}

		public void MapCleared(object sender, EventArgs e) => _moveThread?.Abort();
	}
}