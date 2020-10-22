using System;

namespace Lab2_2
{
	public class CarPositionChangedArgs : EventArgs
	{
		public int Max { get; set; }
		public int Progress { get; set; }
	}
}
