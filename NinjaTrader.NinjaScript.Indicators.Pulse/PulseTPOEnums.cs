namespace NinjaTrader.NinjaScript.Indicators.Pulse;

public static class PulseTPOEnums
{
	public enum ColorMode
	{
		SingleColor,
		ColorByLetter,
		ColorBySession
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
