#region Using declarations
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
	public static class PulseVPEnums
	{
		public enum ColorMode
		{
			AllLettersSameColor,
			EachLetterDifferentColor
		}

		public enum SessionType
		{
			RTHAndETH,
			Day,
			Week,
			Month
		}

		public enum TradingHours
		{
			RTH,
			ETH,
			Day,
			Week,
			Month,
			None
		}
	}
}
