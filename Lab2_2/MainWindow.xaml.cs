using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace Lab2_2
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private List<PointLatLng> _lastMapObjectPoints = new List<PointLatLng>();
		private GMapMarker _lastMarker = null;

		private List<MapObject> _mapObjects = new List<MapObject>();

		private List<Human> _passengersList = new List<Human>();
		public event EventHandler TaxiAborted;

		private object _lock = new object();

		enum MapObjectType
		{
			None = -1,
			Human,
			Car,
			Location,
			Route,
			Area
		}

		public MainWindow()
		{
			InitializeComponent();
			MapObjectName.Text = $"Object {_mapObjects.Count + 1}";
		}

		private void MapLoaded(object sender, RoutedEventArgs e)
		{
			GMaps.Instance.Mode = AccessMode.ServerAndCache;
			Map.MapProvider = YandexMapProvider.Instance;
			Map.MinZoom = 2;
			Map.MaxZoom = 17;
			Map.Zoom = 15;
			Map.Position = new PointLatLng(55.012823, 82.950359);
			Map.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
			Map.CanDragMap = true;
			Map.DragButton = MouseButton.Left;
		}

		private void AddMarker(string title, PointLatLng point)
		{
			MapObject mapObject;
			switch ((MapObjectType)MapObjectSelector.SelectedIndex)
			{
				case MapObjectType.None:
					MessageBox.Show("Choose a map object type.");
					return;
				case MapObjectType.Human:
					mapObject = new Human(title, point);
					break;
				case MapObjectType.Car:
					mapObject = new Car(title, point);
					break;
				case MapObjectType.Location:
					mapObject = new Location(title, point);
					break;
				case MapObjectType.Route:
					_lastMapObjectPoints.Add(point);
					if (_lastMapObjectPoints.Count < 2) return;
					mapObject = new Route(title, _lastMapObjectPoints);
					break;
				case MapObjectType.Area:
					_lastMapObjectPoints.Add(point);
					if (_lastMapObjectPoints.Count < 3) return;
					mapObject = new Area(title, _lastMapObjectPoints);
					break;
				default:
					MessageBox.Show("Unknown map object.");
					return;
			}
			if (_lastMarker != null && _mapObjects.Count != 0 && (mapObject is Route || mapObject is Area))
			{
				Map.Markers.Remove(_lastMarker);
				_mapObjects.RemoveAt(_mapObjects.Count - 1);
			}
			_lastMarker = mapObject.GetMarker();
			Map.Markers.Add(_lastMarker);
			_mapObjects.Add(mapObject);
			MapObjectName.Text = $"Object {_mapObjects.Count + 1}";
		}

		private void Map_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var point = Map.FromLocalToLatLng((int)e.GetPosition(Map).X, (int)e.GetPosition(Map).Y);
			if ((bool) CreateMode.IsChecked)
				AddMarker(MapObjectName.Text, point);
			if ((bool) SearchMode.IsChecked)
			{
				NearbyObjects.Items.Clear();
				List<MapObject> sortedMapObjects = _mapObjects.OrderBy(o => o.GetDistance(point)).ToList();
				for (int i = 0; i < sortedMapObjects.Count; i++)
				{
					KeyValuePair<MapObject, string> keyValuePair = new KeyValuePair<MapObject, string>(sortedMapObjects[i], sortedMapObjects[i].GetTitle());
					NearbyObjects.Items.Add(keyValuePair);
				}
			};
		}

		private void MapObjectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_lastMapObjectPoints.Clear();
			_lastMarker = null;
		}

		private void ClearMap_Click(object sender, RoutedEventArgs e)
		{
			Map.Markers.Clear();
			_lastMapObjectPoints.Clear();
			_mapObjects.Clear();
			NearbyObjects.Items.Clear();
			MapObjectName.Text = $"Object {_mapObjects.Count + 1}";
			_passengersList.Clear();
			TaxiProgress.Value = 0;
			TaxiAborted?.Invoke(this, EventArgs.Empty);
		}

		private void NearbyObjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((ListBox) sender).SelectedIndex > -1)
			{
				var point = ((KeyValuePair<MapObject, string>) ((ListBox) sender).SelectedItem).Key.GetFocus();
				Map.Position = point;
			}
		}

		private void SearchNearbyByName_Click(object sender, RoutedEventArgs e)
		{
			NearbyObjects.Items.Clear();
			PointLatLng point = PointLatLng.Empty;
			MapObject @object = null;
			foreach (var mapObject in _mapObjects)
			{
				if (mapObject.GetTitle() == MapObjectName.Text)
				{
					point = mapObject.GetFocus();
					@object = mapObject;
					break;
				}
			}
			if (point == PointLatLng.Empty)
			{
				MessageBox.Show($"There is no object with the name \"{MapObjectName.Text}\".");
				return;
			}
			List<MapObject> sortedMapObjects = _mapObjects.OrderBy(o => o.GetDistance(point)).ToList();
			sortedMapObjects.Remove(@object);
			for (int i = 0; i < sortedMapObjects.Count; i++)
			{
				KeyValuePair<MapObject, string> keyValuePair = new KeyValuePair<MapObject, string>(sortedMapObjects[i], sortedMapObjects[i].GetTitle());
				NearbyObjects.Items.Add(keyValuePair);
			}
		}

		private void CallTaxi_Click(object sender, RoutedEventArgs e)
		{
			Car currentCar;
			Location destinationLocation = null;
			foreach (var mapObject in _mapObjects)
			{
				if (mapObject is Location location)
				{
					destinationLocation = location;
					break;
				}
			}

			if (destinationLocation == null)
			{
				MessageBox.Show("There is no destination point on the map.");
				return;
			}

			foreach (var mapObject in _mapObjects)
			{
				if (mapObject is Human human)
				{
					human.MoveTo(destinationLocation.GetFocus());
					_passengersList.Add(human);
				}
			}

			if (_passengersList.Count == 0)
			{
				MessageBox.Show("There are no humans on the map.");
				return;
			}

			var carsList = new List<Car>();
			foreach (var mapObject in _mapObjects)
				if (mapObject is Car car) carsList.Add(car);
			if (carsList.Count == 0)
			{
				MessageBox.Show("There are no cars on the map.");
				return;
			}

			var point = _passengersList[0].GetFocus();
			var sortedCarsList = carsList.OrderBy(c => c.GetDistance(point)).ToList();
			currentCar = sortedCarsList[0];

			currentCar.PositionChanged += CarPositionChanged;
			currentCar.Arrived += _passengersList[0].CarArrived;
			_passengersList[0].Seated += currentCar.PassengerSeated;
			currentCar.Arrived += PassengerSeated;
			currentCar.PassengerArrived += PassengerArrivedToDestinationPoint;
			TaxiAborted += currentCar.OnAborted;
			currentCar.Aborted += Car_Aborted;

			currentCar.AddPassenger(_passengersList[0]);
			Map.Markers.Add(currentCar.CreateRouteToDestinationPoint(point));

			Map.Markers.Move(Map.Markers.IndexOf(currentCar.GetMarker()), Map.Markers.Count - 1);
			foreach (var human in _passengersList)
				Map.Markers.Move(Map.Markers.IndexOf(human.GetMarker()), Map.Markers.Count - 1);

			currentCar.StartMove();
		}

		private void Car_Aborted(object sender, EventArgs e)
		{
			if (!(sender is Car car)) return;
			car.Arrived -= PassengerSeated;
			car.PassengerArrived -= PassengerArrivedToDestinationPoint;
			car.PositionChanged -= CarPositionChanged;
			car.Aborted -= Car_Aborted;
		}

		private void PassengerSeated(object sender, EventArgs e)
		{
			if (!(sender is Car car)) return;

			//Map.Markers.Add(car.CreateRouteToDestinationPoint(car.GetPassenger().GetFocus()));
			Map.Markers.Add(car.GetRoute().GetMarker());
			Map.Markers.Move(Map.Markers.IndexOf(car.GetMarker()), Map.Markers.Count - 1);
			foreach (var human in _passengersList)
				Map.Markers.Move(Map.Markers.IndexOf(human.GetMarker()), Map.Markers.Count - 1);
		}

		public void PassengerArrivedToDestinationPoint(object sender, EventArgs e)
		{
			lock (_lock)
			{
				if (!(sender is Car car)) return;
				var passenger = car.GetPassenger();

				car.Arrived -= passenger.CarArrived;
				passenger.Seated -= car.PassengerSeated;

				_passengersList.Remove(passenger);
				_mapObjects.Remove(passenger);
				Map.Markers.Remove(passenger.GetMarker());

				if (_passengersList.Count == 0)
				{
					TaxiAborted?.Invoke(this, EventArgs.Empty);
					MessageBox.Show("Every passenger has arrived to his destination.");
					return;
				}

				var point = car.GetFocus();

				_passengersList.Sort((x, y) => x.GetDistance(point).CompareTo(y.GetDistance(point)));

				car.Arrived += _passengersList[0].CarArrived;
				_passengersList[0].Seated += car.PassengerSeated;

				car.AddPassenger(_passengersList[0]);
				Map.Markers.Add(car.CreateRouteToDestinationPoint(_passengersList[0].GetFocus()));

				Map.Markers.Move(Map.Markers.IndexOf(car.GetMarker()), Map.Markers.Count - 1);
				foreach (var human in _passengersList)
					Map.Markers.Move(Map.Markers.IndexOf(human.GetMarker()), Map.Markers.Count - 1);

				car.StartMove();
			}
		}

		public void CarPositionChanged(object sender, CarPositionChangedArgs e)
		{
			if (!(sender is Car car)) return;
			Map.Position = car.GetFocus();
			TaxiProgress.Maximum = e.Max;
			TaxiProgress.Value = e.Progress;
		}
	}
}