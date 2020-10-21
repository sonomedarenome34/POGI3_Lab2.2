using System;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace Lab2_2
{
	public abstract class MapObject
	{
		private string title;
		private DateTime creationDate;

		protected MapObject(string title)
		{
			this.title = title;
			creationDate = DateTime.Now;
		}

		public string GetTitle() => title;

		public DateTime GetCreationDate() => creationDate;

		public abstract double GetDistance(PointLatLng point);

		public abstract PointLatLng GetFocus();

		public abstract GMapMarker GetMarker();
	}
}
