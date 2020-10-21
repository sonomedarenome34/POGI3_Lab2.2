using System;

namespace Lab2_2
{
	public class CarPositionChanged : EventArgs
	{
		public int Max { get; set; }
		public int Progress { get; set; }
	}
}
