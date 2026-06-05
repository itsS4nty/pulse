using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
public class PulseFootprintPro : Indicator
{
	private class FootprintLevel
	{
		public double Price;

		public long BidVolume;

		public long AskVolume;

		public long TotalVolume => BidVolume + AskVolume;

		public long Delta => AskVolume - BidVolume;
	}

	private class FootprintBar
	{
		public int BarIndex;

		public DateTime BarTime;

		public double BarHigh = double.MinValue;

		public double BarLow = double.MaxValue;

		public double BarOpen;

		public double BarClose;

		public Dictionary<double, FootprintLevel> Levels = new Dictionary<double, FootprintLevel>();

		public long TotalVolume;

		public long TotalDelta;

		public long TotalAskVolume;

		public long TotalBidVolume;

		public double POCPrice;

		public long POCVolume;

		public bool IsLoaded;

		public bool RenderCacheDirty = true;

		public readonly List<FootprintLevel> SortedLevelsByPriceDesc = new List<FootprintLevel>(128);

		public readonly List<FootprintLevel> SortedLevelsByVolumeDesc = new List<FootprintLevel>(128);

		public readonly HashSet<double> ValueAreaLevels = new HashSet<double>();

		public FootprintLevel CachedPOCLevel;

		public FootprintLevel CachedMaxDeltaLevel;

		public long CachedMaxVolume = 1L;

		public long CachedMaxAbsDelta = 1L;
	}

	private class StackedImbalance
	{
		public int StartBarIndex;

		public double TopPrice;

		public double BottomPrice;

		public bool IsPositive;

		public bool IsTouched;
	}

	private Dictionary<int, FootprintBar> footprintBars = new Dictionary<int, FootprintBar>();

	private readonly object dataLock = new object();

	private SolidColorBrush buyBrushDx;

	private SolidColorBrush sellBrushDx;

	private SolidColorBrush neutralBrushDx;

	private SolidColorBrush pocBrushDx;

	private SolidColorBrush textBrushDx;

	private SolidColorBrush tableBgBrushDx;

	private SolidColorBrush tableCellBgBrushDx;

	private SolidColorBrush tempBrushDx;

	private TextFormat textFormat;

	private TextFormat smallTextFormat;

	private TextFormat textFormatRight;

	private TextFormat smallTextFormatRight;

	private TextFormat tableTextFormat;

	private TextFormat tableTextCenterFormat;

	private double tickSize;

	private int maxBarsToStore = 500;

	private bool historicalDataLoaded;

	private HashSet<int> loadedBars = new HashSet<int>();

	private double lastTradePrice = double.NaN;

	private int lastTradeDirection;

	private double currentBid = double.NaN;

	private double currentAsk = double.NaN;

	private bool hideCandles = true;

	private bool showBottomTable = true;

	private int footprintFontSize = 10;

	private int tableFontSize = 11;

	private int minRowHeightPx = 10;

	private int bottomTableHeightPx = 90;

	private Brush positiveDeltaColor = (Brush)(object)Brushes.BlueViolet;

	private Brush negativeDeltaColor = (Brush)(object)Brushes.White;

	private Brush positiveVolumeColor = (Brush)(object)Brushes.BlueViolet;

	private Brush negativeVolumeColor = (Brush)(object)Brushes.White;

	private Brush pocColor = (Brush)(object)Brushes.Gold;

	private Brush candleUpColor = (Brush)(object)Brushes.BlueViolet;

	private Brush candleDownColor = (Brush)(object)Brushes.White;

	private Brush stackedImbalancePositiveColor = (Brush)(object)Brushes.Cyan;

	private Brush stackedImbalanceNegativeColor = (Brush)(object)Brushes.Red;

	private Brush textColor = (Brush)(object)Brushes.White;

	private Brush deltaBarPositiveColor = (Brush)(object)Brushes.BlueViolet;

	private Brush deltaBarNegativeColor = (Brush)(object)Brushes.White;

	private bool showStackedImbalances;

	private int stackedImbalanceMinLevels = 3;

	private bool stackedImbalanceIgnoreZeroValues;

	private int stackedImbalanceRatioPercent = 300;

	private int stackedImbalanceMinVolume = 30;

	private List<StackedImbalance> activeStackedImbalances = new List<StackedImbalance>();

	private readonly List<(int barIdx, FootprintBar fpBar)> renderBarsScratch = new List<(int, FootprintBar)>(512);

	private readonly List<(int barIdx, FootprintBar fpBar)> bottomTableBarsScratch = new List<(int, FootprintBar)>(512);

	private SolidColorBrush buyBrush;

	private SolidColorBrush sellBrush;

	private SolidColorBrush neutralBrush;

	private SolidColorBrush pocBrush;

	private const bool DEBUG_MODE = false;

	private static readonly string[] BottomTableRowNames = new string[5] { "Delta", "Delta %", "Total", "AskVolume", "BidVolume" };

	[NinjaScriptProperty]
	[Display(Name = "Hide Candles", Order = 1, GroupName = "Display")]
	public bool HideCandles
	{
		get
		{
			return hideCandles;
		}
		set
		{
			hideCandles = value;
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Bottom Table", Order = 2, GroupName = "Display")]
	public bool ShowBottomTable
	{
		get
		{
			return showBottomTable;
		}
		set
		{
			showBottomTable = value;
		}
	}

	[NinjaScriptProperty]
	[Range(6, 16)]
	[Display(Name = "Font Size", Order = 3, GroupName = "Display")]
	public int FootprintFontSize
	{
		get
		{
			return footprintFontSize;
		}
		set
		{
			footprintFontSize = Math.Max(6, Math.Min(16, value));
		}
	}

	[NinjaScriptProperty]
	[Range(8, 24)]
	[Display(Name = "Table Font Size", Order = 4, GroupName = "Display")]
	public int TableFontSize
	{
		get
		{
			return tableFontSize;
		}
		set
		{
			tableFontSize = Math.Max(8, Math.Min(24, value));
		}
	}

	[NinjaScriptProperty]
	[Range(1, 1000)]
	[Display(Name = "Stacked Imbalance Min Volume (DISABLED)", Order = 8, GroupName = "Display")]
	[ReadOnly(true)]
	public int StackedImbalanceMinVolume
	{
		get
		{
			return 30;
		}
		set
		{
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Stacked Imbalances (DISABLED)", Order = 4, GroupName = "Display")]
	[ReadOnly(true)]
	public bool ShowStackedImbalances
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[NinjaScriptProperty]
	[Range(2, 10)]
	[Display(Name = "Min Stacked Levels (DISABLED)", Order = 5, GroupName = "Display")]
	[ReadOnly(true)]
	public int StackedImbalanceMinLevels
	{
		get
		{
			return 3;
		}
		set
		{
		}
	}

	[NinjaScriptProperty]
	[Range(100, 1000)]
	[Display(Name = "Stacked Imbalance Ratio (DISABLED)", Order = 7, GroupName = "Display")]
	[ReadOnly(true)]
	public int StackedImbalanceRatioPercent
	{
		get
		{
			return 300;
		}
		set
		{
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Stacked Imbalance Ignore Zero (DISABLED)", Order = 6, GroupName = "Display")]
	[ReadOnly(true)]
	public bool StackedImbalanceIgnoreZeroValues
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[XmlIgnore]
	[Display(Name = "Positive Delta Color", Order = 1, GroupName = "Colors")]
	public Brush PositiveDeltaColor
	{
		get
		{
			return positiveDeltaColor;
		}
		set
		{
			positiveDeltaColor = value;
		}
	}

	[Browsable(false)]
	public string PositiveDeltaColorSerializable
	{
		get
		{
			return Serialize.BrushToString(positiveDeltaColor);
		}
		set
		{
			positiveDeltaColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Negative Delta Color", Order = 2, GroupName = "Colors")]
	public Brush NegativeDeltaColor
	{
		get
		{
			return negativeDeltaColor;
		}
		set
		{
			negativeDeltaColor = value;
		}
	}

	[Browsable(false)]
	public string NegativeDeltaColorSerializable
	{
		get
		{
			return Serialize.BrushToString(negativeDeltaColor);
		}
		set
		{
			negativeDeltaColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Positive Volume Color", Order = 3, GroupName = "Colors")]
	public Brush PositiveVolumeColor
	{
		get
		{
			return positiveVolumeColor;
		}
		set
		{
			positiveVolumeColor = value;
		}
	}

	[Browsable(false)]
	public string PositiveVolumeColorSerializable
	{
		get
		{
			return Serialize.BrushToString(positiveVolumeColor);
		}
		set
		{
			positiveVolumeColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Negative Volume Color", Order = 4, GroupName = "Colors")]
	public Brush NegativeVolumeColor
	{
		get
		{
			return negativeVolumeColor;
		}
		set
		{
			negativeVolumeColor = value;
		}
	}

	[Browsable(false)]
	public string NegativeVolumeColorSerializable
	{
		get
		{
			return Serialize.BrushToString(negativeVolumeColor);
		}
		set
		{
			negativeVolumeColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "POC Color", Order = 5, GroupName = "Colors")]
	public Brush POCColor
	{
		get
		{
			return pocColor;
		}
		set
		{
			pocColor = value;
		}
	}

	[Browsable(false)]
	public string POCColorSerializable
	{
		get
		{
			return Serialize.BrushToString(pocColor);
		}
		set
		{
			pocColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Candle Up Color", Order = 6, GroupName = "Colors")]
	public Brush CandleUpColor
	{
		get
		{
			return candleUpColor;
		}
		set
		{
			candleUpColor = value;
		}
	}

	[Browsable(false)]
	public string CandleUpColorSerializable
	{
		get
		{
			return Serialize.BrushToString(candleUpColor);
		}
		set
		{
			candleUpColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Candle Down Color", Order = 7, GroupName = "Colors")]
	public Brush CandleDownColor
	{
		get
		{
			return candleDownColor;
		}
		set
		{
			candleDownColor = value;
		}
	}

	[Browsable(false)]
	public string CandleDownColorSerializable
	{
		get
		{
			return Serialize.BrushToString(candleDownColor);
		}
		set
		{
			candleDownColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Stacked Imbalance + Color (DISABLED)", Order = 8, GroupName = "Colors")]
	[ReadOnly(true)]
	public Brush StackedImbalancePositiveColor
	{
		get
		{
			return (Brush)(object)Brushes.Cyan;
		}
		set
		{
		}
	}

	[Browsable(false)]
	public string StackedImbalancePositiveColorSerializable
	{
		get
		{
			return Serialize.BrushToString(stackedImbalancePositiveColor);
		}
		set
		{
			stackedImbalancePositiveColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Stacked Imbalance - Color (DISABLED)", Order = 9, GroupName = "Colors")]
	[ReadOnly(true)]
	public Brush StackedImbalanceNegativeColor
	{
		get
		{
			return (Brush)(object)Brushes.Red;
		}
		set
		{
		}
	}

	[Browsable(false)]
	public string StackedImbalanceNegativeColorSerializable
	{
		get
		{
			return Serialize.BrushToString(stackedImbalanceNegativeColor);
		}
		set
		{
			stackedImbalanceNegativeColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Text Color", Order = 10, GroupName = "Colors")]
	public Brush TextColor
	{
		get
		{
			return textColor;
		}
		set
		{
			textColor = value;
		}
	}

	[Browsable(false)]
	public string TextColorSerializable
	{
		get
		{
			return Serialize.BrushToString(textColor);
		}
		set
		{
			textColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Delta Bar Positive Color", Order = 11, GroupName = "Colors")]
	public Brush DeltaBarPositiveColor
	{
		get
		{
			return deltaBarPositiveColor;
		}
		set
		{
			deltaBarPositiveColor = value;
		}
	}

	[Browsable(false)]
	public string DeltaBarPositiveColorSerializable
	{
		get
		{
			return Serialize.BrushToString(deltaBarPositiveColor);
		}
		set
		{
			deltaBarPositiveColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Delta Bar Negative Color", Order = 12, GroupName = "Colors")]
	public Brush DeltaBarNegativeColor
	{
		get
		{
			return deltaBarNegativeColor;
		}
		set
		{
			deltaBarNegativeColor = value;
		}
	}

	[Browsable(false)]
	public string DeltaBarNegativeColorSerializable
	{
		get
		{
			return Serialize.BrushToString(deltaBarNegativeColor);
		}
		set
		{
			deltaBarNegativeColor = Serialize.StringToBrush(value);
		}
	}

	public PulseFootprintPro()
	{
	}

	protected override void OnStateChange()
	{
		if (State == State.SetDefaults)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			Description = "Pulse Footprint Pro - Footprint with historical data (no Tick Replay required)";
			Name = "PulseFootprintPro";
			Calculate = Calculate.OnEachTick;
			IsOverlay = true;
			DisplayInDataBox = false;
			DrawOnPricePanel = true;
			PaintPriceMarkers = false;
			IsSuspendedWhileInactive = false;
			buyBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, (byte)0, (byte)180, byte.MaxValue));
			sellBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, (byte)80, (byte)80));
			neutralBrush = new SolidColorBrush(Color.FromArgb((byte)180, (byte)180, (byte)180, (byte)180));
			pocBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)0));
			hideCandles = true;
			showBottomTable = true;
			footprintFontSize = 10;
			tableFontSize = 11;
			minRowHeightPx = 10;
			bottomTableHeightPx = 60;
		}
		else if (State == State.Configure)
		{
			AddDataSeries((BarsPeriodType)0, 1);
		}
		else if (State == State.DataLoaded)
		{
			tickSize = Instrument.MasterInstrument.TickSize;
		}
		else if (State == State.Terminated)
		{
			DisposeResources();
		}
	}

	protected override void OnBarUpdate()
	{
		if (CurrentBars[0] < 1 || (BarsArray.Length > 1 && CurrentBars[1] < 1))
		{
			return;
		}
		if (BarsInProgress == 0)
		{
			if (hideCandles)
			{
				BarBrushes[0] = (Brush)(object)Brushes.Transparent;
				CandleOutlineBrushes[0] = (Brush)(object)Brushes.Transparent;
			}
			EnsureFootprintBar(CurrentBar);
			if (!historicalDataLoaded && State == State.Realtime && CurrentBar > 10)
			{
				LoadHistoricalFootprintData();
			}
		}
		else
		{
			_ = BarsInProgress;
			_ = 1;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		if ((int)marketDataUpdate.MarketDataType == 1)
		{
			currentBid = marketDataUpdate.Price;
		}
		else if ((int)marketDataUpdate.MarketDataType == 0)
		{
			currentAsk = marketDataUpdate.Price;
		}
		else if ((int)marketDataUpdate.MarketDataType == 2 && CurrentBar >= 1)
		{
			double price = marketDataUpdate.Price;
			long volume = marketDataUpdate.Volume;
			bool isBuy = false;
			bool isSell = false;
			if (!double.IsNaN(currentAsk) && price >= currentAsk)
			{
				isBuy = true;
				lastTradeDirection = 1;
			}
			else if (!double.IsNaN(currentBid) && price <= currentBid)
			{
				isSell = true;
				lastTradeDirection = -1;
			}
			else if (lastTradeDirection > 0)
			{
				isBuy = true;
			}
			else
			{
				isSell = true;
			}
			lastTradePrice = price;
			AccumulateTickData(CurrentBar, price, volume, isBuy, isSell);
		}
	}

	private void EnsureFootprintBar(int barIndex)
	{
		lock (dataLock)
		{
			if (!footprintBars.TryGetValue(barIndex, out var value))
			{
				double val = Math.Round(High[0] / tickSize) * tickSize;
				double val2 = Math.Round(Low[0] / tickSize) * tickSize;
				double num = Close[0];
				double val3 = num + 100.0 * tickSize;
				double val4 = num - 100.0 * tickSize;
				val = Math.Min(val, val3);
				val2 = Math.Max(val2, val4);
				footprintBars[barIndex] = new FootprintBar
				{
					BarIndex = barIndex,
					BarTime = Time[0],
					BarOpen = Open[0],
					BarHigh = val,
					BarLow = val2,
					BarClose = Close[0],
					IsLoaded = false
				};
			}
			else
			{
				if (value.Levels.Count == 0)
				{
					double val5 = Math.Round(High[0] / tickSize) * tickSize;
					double val6 = Math.Round(Low[0] / tickSize) * tickSize;
					double num2 = Close[0];
					double val7 = num2 + 100.0 * tickSize;
					double val8 = num2 - 100.0 * tickSize;
					val5 = Math.Min(val5, val7);
					val6 = Math.Max(val6, val8);
					double barHigh = value.BarHigh;
					double barLow = value.BarLow;
					value.BarHigh = Math.Max(value.BarHigh, val5);
					value.BarLow = Math.Min(value.BarLow, val6);
				}
				value.BarClose = Close[0];
			}
		}
	}

	private void ProcessRealtimeTick()
	{
		if (BarsArray.Length < 2 || CurrentBars[1] < 1)
		{
			return;
		}
		int barIndex = CurrentBars[0];
		double num = Closes[1][0];
		long volume = (long)Volumes[1][0];
		bool isBuy = false;
		bool isSell = false;
		if (!double.IsNaN(lastTradePrice))
		{
			if (num > lastTradePrice)
			{
				isBuy = true;
				lastTradeDirection = 1;
			}
			else if (num < lastTradePrice)
			{
				isSell = true;
				lastTradeDirection = -1;
			}
			else if (lastTradeDirection > 0)
			{
				isBuy = true;
			}
			else
			{
				isSell = true;
			}
		}
		else
		{
			isSell = true;
		}
		lastTradePrice = num;
		AccumulateTickData(barIndex, num, volume, isBuy, isSell);
	}

	private void AccumulateTickData(int barIndex, double price, long volume, bool isBuy, bool isSell)
	{
		double num = Math.Round(price / tickSize) * tickSize;
		lock (dataLock)
		{
			if (!footprintBars.TryGetValue(barIndex, out var value))
			{
				value = new FootprintBar
				{
					BarIndex = barIndex,
					BarHigh = num,
					BarLow = num
				};
				footprintBars[barIndex] = value;
			}
			value.BarHigh = Math.Max(value.BarHigh, num);
			value.BarLow = Math.Min(value.BarLow, num);
			if (!value.Levels.TryGetValue(num, out var value2))
			{
				value2 = new FootprintLevel
				{
					Price = num
				};
				value.Levels[num] = value2;
			}
			if (isBuy)
			{
				value2.AskVolume += volume;
				value.TotalAskVolume += volume;
				value.TotalDelta += volume;
			}
			else if (isSell)
			{
				value2.BidVolume += volume;
				value.TotalBidVolume += volume;
				value.TotalDelta -= volume;
			}
			value.TotalVolume += volume;
			if (value2.TotalVolume > value.POCVolume)
			{
				value.POCVolume = value2.TotalVolume;
				value.POCPrice = num;
			}
			value.RenderCacheDirty = true;
		}
	}

	private void LoadHistoricalFootprintData()
	{
		if (historicalDataLoaded || CurrentBar < 2)
		{
			return;
		}
		int currentBar = CurrentBar;
		List<(DateTime, int, double, double, double, double)> list = new List<(DateTime, int, double, double, double, double)>();
		for (int i = 0; i <= currentBar; i++)
		{
			try
			{
				list.Add((Bars.GetTime(i), i, Bars.GetOpen(i), Bars.GetHigh(i), Bars.GetLow(i), Bars.GetClose(i)));
			}
			catch
			{
				break;
			}
		}
		if (list.Count < 2)
		{
			return;
		}
		List<(DateTime, int, double, double, double, double)> list2 = new List<(DateTime, int, double, double, double, double)>(list);
		list2.Sort(((DateTime time, int idx, double open, double high, double low, double close) a, (DateTime time, int idx, double open, double high, double low, double close) b) => a.time.CompareTo(b.time));
		int count = list2.Count;
		DateTime[] sortedBarTimes = new DateTime[count];
		int[] sortedBarIndices = new int[count];
		Dictionary<int, (DateTime time, int idx, double open, double high, double low, double close)> barOHLC = new Dictionary<int, (DateTime, int, double, double, double, double)>(count);
		for (int num = 0; num < count; num++)
		{
			(DateTime, int, double, double, double, double) value = list2[num];
			sortedBarTimes[num] = value.Item1;
			sortedBarIndices[num] = value.Item2;
			barOHLC[value.Item2] = value;
		}
		DateTime dateTime = sortedBarTimes[0];
		DateTime dateTime2 = sortedBarTimes[count - 1];
		try
		{
			new BarsRequest(Instrument, dateTime, dateTime2)
			{
				BarsPeriod = new BarsPeriod
				{
					BarsPeriodType = (BarsPeriodType)0,
					Value = 1
				},
				TradingHours = TradingHours.Get("Default 24 x 7")
			}.Request((Action<BarsRequest, ErrorCode, string>)delegate(BarsRequest bars, ErrorCode errorCode, string errorMessage)
			{
				if ((int)errorCode != 0 || bars == null || bars.Bars == null || bars.Bars.Count == 0)
				{
					return;
				}
				double num2 = double.NaN;
				int num3 = 0;
				int num4 = 0;
				DateTime dateTime3;
				try
				{
					dateTime3 = Bars.GetTime(CurrentBar);
				}
				catch
				{
					dateTime3 = Globals.MinDate;
				}
				lock (dataLock)
				{
					for (int j = 0; j < bars.Bars.Count; j++)
					{
						DateTime time = bars.Bars.GetTime(j);
						double close = bars.Bars.GetClose(j);
						long volume = bars.Bars.GetVolume(j);
						int num5 = FindBarForTick(sortedBarTimes, sortedBarIndices, time);
						if (num5 >= 0 && barOHLC.TryGetValue(num5, out (DateTime, int, double, double, double, double) value2) && (!(dateTime3 != Globals.MinDate) || !(time >= dateTime3)))
						{
							bool isBuy = false;
							bool isSell = false;
							if (!double.IsNaN(num2))
							{
								if (close > num2)
								{
									isBuy = true;
									num3 = 1;
								}
								else if (close < num2)
								{
									isSell = true;
									num3 = -1;
								}
								else
								{
									isBuy = num3 > 0;
									isSell = num3 <= 0;
								}
							}
							else
							{
								isSell = true;
							}
							num2 = close;
							if (!footprintBars.TryGetValue(num5, out var _))
							{
								double val = Math.Round(value2.Item4 / tickSize) * tickSize;
								double val2 = Math.Round(value2.Item5 / tickSize) * tickSize;
								double item = value2.Item6;
								double val3 = item + 100.0 * tickSize;
								double val4 = item - 100.0 * tickSize;
								val = Math.Min(val, val3);
								val2 = Math.Max(val2, val4);
								footprintBars[num5] = new FootprintBar
								{
									BarIndex = num5,
									BarTime = value2.Item1,
									BarOpen = value2.Item3,
									BarHigh = val,
									BarLow = val2,
									BarClose = value2.Item6
								};
							}
							AccumulateTickDataInternal(num5, close, volume, isBuy, isSell);
							num4++;
						}
					}
					foreach (KeyValuePair<int, FootprintBar> footprintBar in footprintBars)
					{
						footprintBar.Value.IsLoaded = true;
					}
				}
			});
		}
		catch (Exception)
		{
		}
		historicalDataLoaded = true;
	}

	private int FindBarForTick(DateTime[] barTimes, int[] barIndices, DateTime tickTime)
	{
		if (barTimes == null || barIndices == null || barTimes.Length == 0 || barTimes.Length != barIndices.Length)
		{
			return -1;
		}
		int num = 0;
		int num2 = barTimes.Length - 1;
		while (num <= num2)
		{
			int num3 = num + (num2 - num >> 1);
			if (tickTime <= barTimes[num3])
			{
				num2 = num3 - 1;
			}
			else
			{
				num = num3 + 1;
			}
		}
		if (num < 0 || num >= barIndices.Length)
		{
			return -1;
		}
		return barIndices[num];
	}

	private void AccumulateTickDataInternal(int barIndex, double price, long volume, bool isBuy, bool isSell)
	{
		if (footprintBars.TryGetValue(barIndex, out var value))
		{
			double num = Math.Round(price / tickSize) * tickSize;
			if (!value.Levels.TryGetValue(num, out var value2))
			{
				value2 = new FootprintLevel
				{
					Price = num
				};
				value.Levels[num] = value2;
			}
			if (isBuy)
			{
				value2.AskVolume += volume;
				value.TotalAskVolume += volume;
			}
			else if (isSell)
			{
				value2.BidVolume += volume;
				value.TotalBidVolume += volume;
			}
			value.TotalVolume += volume;
			value.TotalDelta = value.TotalAskVolume - value.TotalBidVolume;
			if (num > value.BarHigh)
			{
				value.BarHigh = num;
			}
			if (num < value.BarLow)
			{
				value.BarLow = num;
			}
			value.BarClose = num;
			if (value2.TotalVolume > value.POCVolume)
			{
				value.POCVolume = value2.TotalVolume;
				value.POCPrice = num;
			}
			value.RenderCacheDirty = true;
		}
	}

	private void LoadBarHistoricalData(int barIndex)
	{
		if (barIndex < 1 || barIndex >= Bars.Count)
		{
			return;
		}
		DateTime time = Bars.GetTime(CurrentBar);
		if (Bars.GetTime(barIndex) >= time.AddMinutes(-5.0))
		{
			return;
		}
		int capturedBarIndex = barIndex;
		loadedBars.Add(capturedBarIndex);
		DateTime time2 = Bars.GetTime(capturedBarIndex);
		DateTime dateTime = Bars.GetTime(capturedBarIndex - 1).AddSeconds(1.0);
		try
		{
			new BarsRequest(Instrument, dateTime, time2)
			{
				BarsPeriod = new BarsPeriod
				{
					BarsPeriodType = (BarsPeriodType)0,
					Value = 1
				},
				TradingHours = TradingHours.Get("Default 24 x 7")
			}.Request((Action<BarsRequest, ErrorCode, string>)delegate(BarsRequest bars, ErrorCode errorCode, string errorMessage)
			{
				if ((int)errorCode != 0 || bars == null || bars.Bars == null)
				{
					return;
				}
				lock (dataLock)
				{
					if (footprintBars.ContainsKey(capturedBarIndex))
					{
						double high = Bars.GetHigh(capturedBarIndex);
						double low = Bars.GetLow(capturedBarIndex);
						double val = Math.Round(high / tickSize) * tickSize;
						double val2 = Math.Round(low / tickSize) * tickSize;
						double close = Bars.GetClose(capturedBarIndex);
						double val3 = close + 100.0 * tickSize;
						double val4 = close - 100.0 * tickSize;
						val = Math.Min(val, val3);
						val2 = Math.Max(val2, val4);
						footprintBars[capturedBarIndex] = new FootprintBar
						{
							BarIndex = capturedBarIndex,
							BarTime = Bars.GetTime(capturedBarIndex),
							BarOpen = Bars.GetOpen(capturedBarIndex),
							BarHigh = val,
							BarLow = val2,
							BarClose = Bars.GetClose(capturedBarIndex)
						};
					}
				}
				double num = double.NaN;
				int num2 = 0;
				for (int i = 0; i < bars.Bars.Count; i++)
				{
					double close2 = bars.Bars.GetClose(i);
					long volume = bars.Bars.GetVolume(i);
					bool isBuy = false;
					bool isSell = false;
					if (!double.IsNaN(num))
					{
						if (close2 > num)
						{
							isBuy = true;
							num2 = 1;
						}
						else if (close2 < num)
						{
							isSell = true;
							num2 = -1;
						}
						else
						{
							isBuy = num2 > 0;
							isSell = num2 <= 0;
						}
					}
					else
					{
						isSell = true;
					}
					num = close2;
					AccumulateTickData(capturedBarIndex, close2, volume, isBuy, isSell);
				}
				lock (dataLock)
				{
					if (footprintBars.TryGetValue(capturedBarIndex, out var value))
					{
						value.IsLoaded = true;
						value.BarOpen = Bars.GetOpen(capturedBarIndex);
						value.BarHigh = Bars.GetHigh(capturedBarIndex);
						value.BarLow = Bars.GetLow(capturedBarIndex);
						value.BarClose = Bars.GetClose(capturedBarIndex);
					}
				}
			});
		}
		catch (Exception)
		{
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		OnRender(chartControl, chartScale);
		if (Bars == null || ChartControl == null || ChartBars == null || RenderTarget == null)
		{
			return;
		}
		CreateResources();
		try
		{
			int fromIndex = ChartBars.FromIndex;
			int toIndex = ChartBars.ToIndex;
			renderBarsScratch.Clear();
			lock (dataLock)
			{
				for (int i = fromIndex; i <= toIndex && i >= 0; i++)
				{
					if (i <= CurrentBar && footprintBars.TryGetValue(i, out var value))
					{
						EnsureRenderCache(value);
						renderBarsScratch.Add((i, value));
					}
				}
			}
			for (int j = 0; j < renderBarsScratch.Count; j++)
			{
				RenderFootprintBar(chartControl, chartScale, renderBarsScratch[j].barIdx, renderBarsScratch[j].fpBar);
			}
			RenderActiveStackedImbalances(chartControl, chartScale, fromIndex, toIndex);
			if (showBottomTable)
			{
				RenderBottomTable(chartControl, chartScale, fromIndex, toIndex);
			}
		}
		catch (Exception)
		{
		}
	}

	private void EnsureRenderCache(FootprintBar fpBar)
	{
		if (fpBar == null || !fpBar.RenderCacheDirty)
		{
			return;
		}
		List<FootprintLevel> sortedLevelsByPriceDesc = fpBar.SortedLevelsByPriceDesc;
		sortedLevelsByPriceDesc.Clear();
		foreach (FootprintLevel value in fpBar.Levels.Values)
		{
			if (value.TotalVolume > 0)
			{
				sortedLevelsByPriceDesc.Add(value);
			}
		}
		sortedLevelsByPriceDesc.Sort((FootprintLevel a, FootprintLevel b) => b.Price.CompareTo(a.Price));
		long num = 0L;
		long num2 = 0L;
		FootprintLevel cachedPOCLevel = null;
		FootprintLevel cachedMaxDeltaLevel = null;
		for (int num3 = 0; num3 < sortedLevelsByPriceDesc.Count; num3++)
		{
			FootprintLevel footprintLevel = sortedLevelsByPriceDesc[num3];
			long totalVolume = footprintLevel.TotalVolume;
			long num4 = Math.Abs(footprintLevel.Delta);
			if (totalVolume > num)
			{
				num = totalVolume;
				cachedPOCLevel = footprintLevel;
			}
			if (num4 > num2)
			{
				num2 = num4;
				cachedMaxDeltaLevel = footprintLevel;
			}
		}
		fpBar.CachedMaxVolume = Math.Max(1L, num);
		fpBar.CachedMaxAbsDelta = Math.Max(1L, num2);
		fpBar.CachedPOCLevel = cachedPOCLevel;
		fpBar.CachedMaxDeltaLevel = cachedMaxDeltaLevel;
		fpBar.ValueAreaLevels.Clear();
		if (fpBar.TotalVolume > 0 && sortedLevelsByPriceDesc.Count > 0)
		{
			List<FootprintLevel> sortedLevelsByVolumeDesc = fpBar.SortedLevelsByVolumeDesc;
			sortedLevelsByVolumeDesc.Clear();
			sortedLevelsByVolumeDesc.AddRange(sortedLevelsByPriceDesc);
			sortedLevelsByVolumeDesc.Sort((FootprintLevel a, FootprintLevel b) => b.TotalVolume.CompareTo(a.TotalVolume));
			long num5 = (long)((double)fpBar.TotalVolume * 0.7);
			long num6 = 0L;
			for (int num7 = 0; num7 < sortedLevelsByVolumeDesc.Count; num7++)
			{
				FootprintLevel footprintLevel2 = sortedLevelsByVolumeDesc[num7];
				fpBar.ValueAreaLevels.Add(footprintLevel2.Price);
				num6 += footprintLevel2.TotalVolume;
				if (num6 >= num5)
				{
					break;
				}
			}
		}
		fpBar.RenderCacheDirty = false;
	}

	private void RenderFootprintBar(ChartControl chartControl, ChartScale chartScale, int barIdx, FootprintBar fpBar)
	{
		bool num = fpBar.Levels.Count > 0;
		float num2 = chartControl.GetXByBarIndex(ChartBars, barIdx);
		float candleWidth;
		float candleX;
		if (!num)
		{
			candleWidth = Math.Max(2f, (float)chartControl.BarWidth * 0.04f);
			candleX = num2;
			RenderCandle(chartScale, barIdx, fpBar, candleX, candleWidth);
			return;
		}
		List<FootprintLevel> sortedLevelsByPriceDesc = fpBar.SortedLevelsByPriceDesc;
		if (sortedLevelsByPriceDesc.Count == 0)
		{
			candleWidth = Math.Max(2f, (float)chartControl.BarWidth * 0.04f);
			candleX = num2;
			RenderCandle(chartScale, barIdx, fpBar, candleX, candleWidth);
			return;
		}
		float num3;
		if (barIdx <= ChartBars.FromIndex)
		{
			num3 = ((barIdx >= ChartBars.ToIndex) ? ((float)chartControl.BarWidth * 4f) : Math.Abs((float)chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - num2));
		}
		else
		{
			float num4 = chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
			num3 = Math.Abs(num2 - num4);
		}
		candleWidth = Math.Max(2f, num3 * 0.04f);
		Math.Max((num3 - candleWidth - 4f) / 2f, 10f);
		float num5 = num2 - num3 / 2f;
		float num6 = num2 + num3 / 2f;
		float num7 = num5 + 1f;
		float num8 = num2 - candleWidth / 2f - 1f;
		candleX = num2;
		float num9 = num2 + candleWidth / 2f + 1f;
		float num10 = num6 - 1f;
		float num11 = num8 - num7;
		float num12 = num10 - num9;
		bool flag = num11 >= 15f;
		FootprintLevel cachedPOCLevel = fpBar.CachedPOCLevel;
		FootprintLevel cachedMaxDeltaLevel = fpBar.CachedMaxDeltaLevel;
		long val = Math.Max(1L, fpBar.CachedMaxVolume);
		long val2 = Math.Max(1L, fpBar.CachedMaxAbsDelta);
		HashSet<double> valueAreaLevels = fpBar.ValueAreaLevels;
		Color4 val3 = BrushToColor4(positiveDeltaColor, 1f);
		Color4 val4 = BrushToColor4(negativeDeltaColor, 1f);
		Color4 val5 = BrushToColor4(positiveVolumeColor, 1f);
		Color4 val6 = BrushToColor4(pocColor, 1f);
		RectangleF val8 = default(RectangleF);
		RectangleF val13 = default(RectangleF);
		Color4 val14 = default(Color4);
		RectangleF val15 = default(RectangleF);
		Color4 val17 = default(Color4);
		RectangleF val18 = default(RectangleF);
		Color4 val20 = default(Color4);
		RectangleF val21 = default(RectangleF);
		RectangleF val24 = default(RectangleF);
		for (int i = 0; i < sortedLevelsByPriceDesc.Count; i++)
		{
			FootprintLevel footprintLevel = sortedLevelsByPriceDesc[i];
			if (footprintLevel.TotalVolume == 0L)
			{
				continue;
			}
			float num13 = chartScale.GetYByValue(footprintLevel.Price);
			float num14 = Math.Abs((float)chartScale.GetYByValue(footprintLevel.Price - tickSize) - num13);
			if (num14 < 2f)
			{
				continue;
			}
			bool flag2 = cachedPOCLevel != null && Math.Abs(footprintLevel.Price - cachedPOCLevel.Price) < tickSize / 2.0;
			bool flag3 = cachedMaxDeltaLevel != null && Math.Abs(footprintLevel.Price - cachedMaxDeltaLevel.Price) < tickSize / 2.0;
			if (footprintLevel.Delta != 0L || footprintLevel.TotalVolume > 0)
			{
				string text = ((footprintLevel.Delta >= 0) ? $"{footprintLevel.Delta}" : footprintLevel.Delta.ToString());
				float num15 = (float)Math.Abs(footprintLevel.Delta) / (float)Math.Max(1L, val2);
				float num16 = Math.Max(num11 * 0.1f, num15 * num11);
				float num17 = num8 - num16;
				if (Math.Abs(footprintLevel.Delta) > 0)
				{
					float num18 = 0.3f + num15 * 0.7f;
					Color4 val7 = ((footprintLevel.Delta >= 0) ? new Color4(val3.Red, val3.Green, val3.Blue, num18) : new Color4(val4.Red, val4.Green, val4.Blue, num18));
					val8 = new RectangleF(num17, num13 - num14 / 2f, num16, num14);
					if (tempBrushDx != null)
					{
						tempBrushDx.Color = val7;
						RenderTarget.FillRectangle(val8, (Brush)(object)tempBrushDx);
					}
					else
					{
						SolidColorBrush val9 = new SolidColorBrush(RenderTarget, val7);
						try
						{
							RenderTarget.FillRectangle(val8, (Brush)(object)val9);
						}
						finally
						{
							((IDisposable)val9)?.Dispose();
						}
					}
					if (flag3)
					{
						Color4 val10 = val6;
						if (tempBrushDx != null)
						{
							tempBrushDx.Color = val10;
							RenderTarget.DrawRectangle(val8, (Brush)(object)tempBrushDx, 2f);
						}
						else
						{
							SolidColorBrush val11 = new SolidColorBrush(RenderTarget, val10);
							try
							{
								RenderTarget.DrawRectangle(val8, (Brush)(object)val11, 2f);
							}
							finally
							{
								((IDisposable)val11)?.Dispose();
							}
						}
					}
				}
				if (flag && num14 >= 6f)
				{
					TextFormat val12 = ((num14 < 10f) ? smallTextFormatRight : textFormatRight);
					if (val12 != null && textBrushDx != null)
					{
						val13 = new RectangleF(num7 + 1f, num13 - num14 / 2f, Math.Max(1f, num11 - 2f), num14);
						RenderTarget.DrawText(text, val12, val13, (Brush)(object)textBrushDx);
					}
				}
			}
			if (footprintLevel.TotalVolume <= 0)
			{
				continue;
			}
			string text2 = footprintLevel.TotalVolume.ToString();
			float num19 = (float)footprintLevel.TotalVolume / (float)Math.Max(1L, val);
			float num20 = num19 * num12 * 0.9f;
			float num21 = 0.3f + num19 * 0.6f;
			if (flag2)
			{
				val14 = new Color4(val6.Red, val6.Green, val6.Blue, 0.9f);
				val15 = new RectangleF(num9, num13 - num14 / 2f, num20, num14);
				if (tempBrushDx != null)
				{
					tempBrushDx.Color = val14;
					RenderTarget.FillRectangle(val15, (Brush)(object)tempBrushDx);
				}
				else
				{
					SolidColorBrush val16 = new SolidColorBrush(RenderTarget, val14);
					try
					{
						RenderTarget.FillRectangle(val15, (Brush)(object)val16);
					}
					finally
					{
						((IDisposable)val16)?.Dispose();
					}
				}
			}
			else if (valueAreaLevels.Contains(footprintLevel.Price))
			{
				val17 = new Color4(val5.Red, val5.Green, val5.Blue, num21);
				val18 = new RectangleF(num9, num13 - num14 / 2f, num20, num14);
				if (tempBrushDx != null)
				{
					tempBrushDx.Color = val17;
					RenderTarget.FillRectangle(val18, (Brush)(object)tempBrushDx);
				}
				else
				{
					SolidColorBrush val19 = new SolidColorBrush(RenderTarget, val17);
					try
					{
						RenderTarget.FillRectangle(val18, (Brush)(object)val19);
					}
					finally
					{
						((IDisposable)val19)?.Dispose();
					}
				}
			}
			else
			{
				val20 = new Color4(0.5f, 0.5f, 0.5f, num21);
				val21 = new RectangleF(num9, num13 - num14 / 2f, num20, num14);
				if (tempBrushDx != null)
				{
					tempBrushDx.Color = val20;
					RenderTarget.FillRectangle(val21, (Brush)(object)tempBrushDx);
				}
				else
				{
					SolidColorBrush val22 = new SolidColorBrush(RenderTarget, val20);
					try
					{
						RenderTarget.FillRectangle(val21, (Brush)(object)val22);
					}
					finally
					{
						((IDisposable)val22)?.Dispose();
					}
				}
			}
			if (flag && num14 >= 6f)
			{
				TextFormat val23 = ((num14 < 10f) ? smallTextFormat : textFormat);
				if (val23 != null && textBrushDx != null)
				{
					val24 = new RectangleF(num9 + 1f, num13 - num14 / 2f, Math.Max(1f, num12 - 2f), num14);
					RenderTarget.DrawText(text2, val23, val24, (Brush)(object)textBrushDx);
				}
			}
		}
		RenderCandle(chartScale, barIdx, fpBar, candleX, candleWidth);
	}

	private void RenderStackedImbalances(ChartScale chartScale, FootprintBar fpBar, float deltaStartX, float deltaEndX, float volumeStartX, float volumeEndX)
	{
		if (fpBar.Levels.Count >= stackedImbalanceMinLevels && !activeStackedImbalances.Any((StackedImbalance si) => si.StartBarIndex == fpBar.BarIndex))
		{
			List<FootprintLevel> sortedLevels = fpBar.Levels.Values.OrderByDescending((FootprintLevel l) => l.Price).ToList();
			DetectStackedImbalances(sortedLevels, fpBar.BarIndex);
		}
	}

	private void DetectStackedImbalances(List<FootprintLevel> sortedLevels, int barIndex)
	{
		Dictionary<double, FootprintLevel> dictionary = sortedLevels.ToDictionary((FootprintLevel l) => l.Price, (FootprintLevel l) => l);
		List<double> list = dictionary.Keys.OrderBy((double p) => p).ToList();
		List<double> list2 = new List<double>();
		List<double> list3 = new List<double>();
		foreach (double item in list)
		{
			FootprintLevel footprintLevel = dictionary[item];
			long bidVolume = footprintLevel.BidVolume;
			long askVolume = footprintLevel.AskVolume;
			if (stackedImbalanceIgnoreZeroValues && (bidVolume == 0L || askVolume == 0L))
			{
				if (list2.Count >= stackedImbalanceMinLevels)
				{
					RegisterStackedImbalance(list2, barIndex, isAskImbalance: true);
				}
				if (list3.Count >= stackedImbalanceMinLevels)
				{
					RegisterStackedImbalance(list3, barIndex, isAskImbalance: false);
				}
				list2.Clear();
				list3.Clear();
				continue;
			}
			if (Math.Max(bidVolume, askVolume) < stackedImbalanceMinVolume)
			{
				if (list2.Count >= stackedImbalanceMinLevels)
				{
					RegisterStackedImbalance(list2, barIndex, isAskImbalance: true);
				}
				if (list3.Count >= stackedImbalanceMinLevels)
				{
					RegisterStackedImbalance(list3, barIndex, isAskImbalance: false);
				}
				list2.Clear();
				list3.Clear();
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			if (askVolume > 0 && bidVolume == 0L)
			{
				flag = true;
			}
			else if (bidVolume > 0 && askVolume == 0L)
			{
				flag2 = true;
			}
			else if (bidVolume > 0 && askVolume > 0)
			{
				double num = (double)askVolume * 100.0 / (double)bidVolume;
				double num2 = (double)bidVolume * 100.0 / (double)askVolume;
				if (num >= (double)stackedImbalanceRatioPercent)
				{
					flag = true;
				}
				else if (num2 >= (double)stackedImbalanceRatioPercent)
				{
					flag2 = true;
				}
			}
			if (flag)
			{
				list2.Add(item);
				if (list3.Count >= stackedImbalanceMinLevels)
				{
					RegisterStackedImbalance(list3, barIndex, isAskImbalance: false);
				}
				list3.Clear();
				continue;
			}
			if (flag2)
			{
				list3.Add(item);
				if (list2.Count >= stackedImbalanceMinLevels)
				{
					RegisterStackedImbalance(list2, barIndex, isAskImbalance: true);
				}
				list2.Clear();
				continue;
			}
			if (list2.Count >= stackedImbalanceMinLevels)
			{
				RegisterStackedImbalance(list2, barIndex, isAskImbalance: true);
			}
			if (list3.Count >= stackedImbalanceMinLevels)
			{
				RegisterStackedImbalance(list3, barIndex, isAskImbalance: false);
			}
			list2.Clear();
			list3.Clear();
		}
		if (list2.Count >= stackedImbalanceMinLevels)
		{
			RegisterStackedImbalance(list2, barIndex, isAskImbalance: true);
		}
		if (list3.Count >= stackedImbalanceMinLevels)
		{
			RegisterStackedImbalance(list3, barIndex, isAskImbalance: false);
		}
	}

	private void RegisterStackedImbalance(List<double> priceStack, int barIndex, bool isAskImbalance)
	{
		if (priceStack != null && priceStack.Count >= stackedImbalanceMinLevels)
		{
			double topPrice = priceStack.Max();
			double bottomPrice = priceStack.Min();
			activeStackedImbalances.Add(new StackedImbalance
			{
				StartBarIndex = barIndex,
				TopPrice = topPrice,
				BottomPrice = bottomPrice,
				IsPositive = isAskImbalance,
				IsTouched = false
			});
		}
	}

	private void RenderActiveStackedImbalances(ChartControl chartControl, ChartScale chartScale, int firstBar, int lastBar)
	{
	}

	private void DrawStackedImbalanceRect(ChartScale chartScale, double topPrice, double bottomPrice, float left, float right, bool isPositive)
	{
		float num = chartScale.GetYByValue(topPrice);
		float num2 = chartScale.GetYByValue(bottomPrice - tickSize);
		Color4 val = (isPositive ? BrushToColor4(stackedImbalancePositiveColor, 0.25f) : BrushToColor4(stackedImbalanceNegativeColor, 0.25f));
		Color4 val2 = (isPositive ? BrushToColor4(stackedImbalancePositiveColor, 0.8f) : BrushToColor4(stackedImbalanceNegativeColor, 0.8f));
		RectangleF val3 = default(RectangleF);
		val3 = new RectangleF(left, num, right - left, num2 - num);
		SolidColorBrush val4 = new SolidColorBrush(RenderTarget, val);
		try
		{
			SolidColorBrush val5 = new SolidColorBrush(RenderTarget, val2);
			try
			{
				RenderTarget.FillRectangle(val3, (Brush)(object)val4);
				RenderTarget.DrawRectangle(val3, (Brush)(object)val5, 1.5f);
			}
			finally
			{
				((IDisposable)val5)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val4)?.Dispose();
		}
	}

	private void RenderCandle(ChartScale chartScale, int barIdx, FootprintBar fpBar, float candleX, float candleWidth)
	{
		if (fpBar.BarHigh <= fpBar.BarLow)
		{
			return;
		}
		double num = ((fpBar.BarOpen > 0.0) ? fpBar.BarOpen : Bars.GetOpen(barIdx));
		double barHigh = fpBar.BarHigh;
		double barLow = fpBar.BarLow;
		double num2 = ((fpBar.BarClose > 0.0) ? fpBar.BarClose : Bars.GetClose(barIdx));
		float val = chartScale.GetYByValue(num);
		float num3 = chartScale.GetYByValue(barHigh);
		float num4 = chartScale.GetYByValue(barLow);
		float val2 = chartScale.GetYByValue(num2);
		Color4 val3 = ((num2 >= num) ? BrushToColor4(candleUpColor, 1f) : BrushToColor4(candleDownColor, 1f));
		if (tempBrushDx != null)
		{
			tempBrushDx.Color = val3;
			RenderTarget.DrawLine(new Vector2(candleX, num3), new Vector2(candleX, num4), (Brush)(object)tempBrushDx, 1f);
			float num5 = Math.Min(val, val2);
			float num6 = Math.Max(val, val2);
			float num7 = Math.Max(1f, num6 - num5);
			RectangleF val4 = default(RectangleF);
			val4 = new RectangleF(candleX - candleWidth / 2f, num5, candleWidth, num7);
			RenderTarget.FillRectangle(val4, (Brush)(object)tempBrushDx);
			return;
		}
		SolidColorBrush val5 = new SolidColorBrush(RenderTarget, val3);
		try
		{
			RenderTarget.DrawLine(new Vector2(candleX, num3), new Vector2(candleX, num4), (Brush)(object)val5, 1f);
			float num8 = Math.Min(val, val2);
			float num9 = Math.Max(val, val2);
			float num10 = Math.Max(1f, num9 - num8);
			RectangleF val6 = default(RectangleF);
			val6 = new RectangleF(candleX - candleWidth / 2f, num8, candleWidth, num10);
			RenderTarget.FillRectangle(val6, (Brush)(object)val5);
		}
		finally
		{
			((IDisposable)val5)?.Dispose();
		}
	}

	private void RenderBottomTable(ChartControl chartControl, ChartScale chartScale, int firstBar, int lastBar)
	{
		int num = BottomTableRowNames.Length;
		if (num <= 0)
		{
			return;
		}
		float num2 = Math.Max((float)tableFontSize + 6f, minRowHeightPx);
		float num3 = Math.Max(bottomTableHeightPx, num2 * (float)num);
		float num4 = (float)ChartPanel.H - num3;
		float num5 = num3 / (float)num;
		float num6 = 70f;
		float num7 = (float)ChartPanel.W - num6;
		RectangleF val = default(RectangleF);
		val = new RectangleF(num7, num4, num6, num3);
		if (tableBgBrushDx != null)
		{
			RenderTarget.FillRectangle(val, (Brush)(object)tableBgBrushDx);
		}
		RenderTarget.DrawRectangle(val, (Brush)(object)neutralBrushDx, 1f);
		RectangleF val2 = default(RectangleF);
		for (int i = 0; i < num; i++)
		{
			if (tableTextFormat != null)
			{
				val2 = new RectangleF(num7 + 2f, num4 + (float)i * num5, num6 - 2f, num5);
				RenderTarget.DrawText(BottomTableRowNames[i], tableTextFormat, val2, (Brush)(object)neutralBrushDx);
			}
		}
		long num8 = 1L;
		bottomTableBarsScratch.Clear();
		float num9 = 60f;
		lock (dataLock)
		{
			for (int j = firstBar; j <= lastBar && j >= 0; j++)
			{
				if (j <= CurrentBar && footprintBars.TryGetValue(j, out var value) && value.Levels.Count != 0)
				{
					bottomTableBarsScratch.Add((j, value));
					num8 = Math.Max(num8, Math.Abs(value.TotalDelta));
				}
			}
		}
		RectangleF val3 = default(RectangleF);
		RectangleF val5 = default(RectangleF);
		RectangleF val6 = default(RectangleF);
		RectangleF val7 = default(RectangleF);
		RectangleF val8 = default(RectangleF);
		RectangleF val9 = default(RectangleF);
		RectangleF val10 = default(RectangleF);
		for (int k = 0; k < bottomTableBarsScratch.Count; k++)
		{
			int item = bottomTableBarsScratch[k].barIdx;
			FootprintBar item2 = bottomTableBarsScratch[k].fpBar;
			float num10 = chartControl.GetXByBarIndex(ChartBars, item);
			float num11 = (float)chartControl.BarWidth;
			if (item < ChartBars.ToIndex)
			{
				num11 = Math.Abs((float)chartControl.GetXByBarIndex(ChartBars, item + 1) - num10);
			}
			else if (item > ChartBars.FromIndex)
			{
				float num12 = chartControl.GetXByBarIndex(ChartBars, item - 1);
				num11 = Math.Abs(num10 - num12);
			}
			float num13 = num11;
			float num14 = num10 - num11 / 2f;
			float num15 = num7 - 1f;
			if (num14 + num13 > num15)
			{
				num13 = Math.Max(0f, num15 - num14);
			}
			if (!(num13 < 8f))
			{
				if (item2.TotalDelta != 0L)
				{
					float num16 = (float)((double)Math.Abs(item2.TotalDelta) / (double)num8 * (double)num9);
					float num17 = Math.Min(num13 * 0.6f, 20f);
					float num18 = num10 - num17 / 2f;
					float num19 = num4 - num16 - 2f;
					val3 = new RectangleF(num18, num19, num17, num16);
					SolidColorBrush val4 = ((item2.TotalDelta > 0) ? buyBrushDx : sellBrushDx);
					RenderTarget.FillRectangle(val3, (Brush)(object)val4);
					RenderTarget.DrawRectangle(val3, (Brush)(object)val4, 1f);
				}
				val5 = new RectangleF(num14, num4, num13, num3);
				if (tableCellBgBrushDx != null)
				{
					RenderTarget.FillRectangle(val5, (Brush)(object)tableCellBgBrushDx);
				}
				RenderTarget.DrawRectangle(val5, (Brush)(object)neutralBrushDx, 1f);
				for (int l = 1; l < num; l++)
				{
					float num20 = num4 + (float)l * num5;
					RenderTarget.DrawLine(new Vector2(num14, num20), new Vector2(num14 + num13, num20), (Brush)(object)neutralBrushDx, 1f);
				}
				if (tableTextCenterFormat != null)
				{
					val6 = new RectangleF(num14, num4 + 0f * num5, num13, num5);
					val7 = new RectangleF(num14, num4 + 1f * num5, num13, num5);
					val8 = new RectangleF(num14, num4 + 2f * num5, num13, num5);
					val9 = new RectangleF(num14, num4 + 3f * num5, num13, num5);
					val10 = new RectangleF(num14, num4 + 4f * num5, num13, num5);
					double num21 = ((item2.TotalVolume > 0) ? ((double)item2.TotalDelta * 100.0 / (double)item2.TotalVolume) : 0.0);
					string text = num21.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%";
					SolidColorBrush val11 = ((num21 > 0.0) ? buyBrushDx : ((num21 < 0.0) ? sellBrushDx : neutralBrushDx));
					RenderTarget.DrawText(item2.TotalDelta.ToString(CultureInfo.InvariantCulture), tableTextCenterFormat, val6, (Brush)(object)((item2.TotalDelta >= 0) ? buyBrushDx : sellBrushDx));
					RenderTarget.DrawText(text, tableTextCenterFormat, val7, (Brush)(object)val11);
					RenderTarget.DrawText(item2.TotalVolume.ToString(CultureInfo.InvariantCulture), tableTextCenterFormat, val8, (Brush)(object)neutralBrushDx);
					RenderTarget.DrawText(item2.TotalAskVolume.ToString(CultureInfo.InvariantCulture), tableTextCenterFormat, val9, (Brush)(object)neutralBrushDx);
					RenderTarget.DrawText(item2.TotalBidVolume.ToString(CultureInfo.InvariantCulture), tableTextCenterFormat, val10, (Brush)(object)neutralBrushDx);
				}
			}
		}
	}

	private void CreateResources()
	{
		if (RenderTarget == null)
		{
			return;
		}
		Color4 val = BrushToColor4(deltaBarPositiveColor, 1f);
		if (buyBrushDx == null || buyBrushDx.Color != val)
		{
			if (buyBrushDx != null)
			{
				((DisposeBase)buyBrushDx).Dispose();
				buyBrushDx = null;
			}
			buyBrushDx = new SolidColorBrush(RenderTarget, val);
		}
		Color4 val2 = BrushToColor4(deltaBarNegativeColor, 1f);
		if (sellBrushDx == null || sellBrushDx.Color != val2)
		{
			if (sellBrushDx != null)
			{
				((DisposeBase)sellBrushDx).Dispose();
				sellBrushDx = null;
			}
			sellBrushDx = new SolidColorBrush(RenderTarget, val2);
		}
		if (neutralBrushDx == null)
		{
			neutralBrushDx = new SolidColorBrush(RenderTarget, new Color4(0.7f, 0.7f, 0.7f, 1f));
		}
		if (pocBrushDx == null)
		{
			pocBrushDx = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 0f, 1f));
		}
		Color4 val3 = BrushToColor4(textColor, 1f);
		if (textBrushDx == null || textBrushDx.Color != val3)
		{
			if (textBrushDx != null)
			{
				((DisposeBase)textBrushDx).Dispose();
				textBrushDx = null;
			}
			textBrushDx = new SolidColorBrush(RenderTarget, val3);
		}
		if (tableBgBrushDx == null)
		{
			tableBgBrushDx = new SolidColorBrush(RenderTarget, new Color4(0.1f, 0.15f, 0.25f, 0.95f));
		}
		if (tableCellBgBrushDx == null)
		{
			tableCellBgBrushDx = new SolidColorBrush(RenderTarget, new Color4(0.05f, 0.1f, 0.2f, 0.9f));
		}
		if (tempBrushDx == null)
		{
			tempBrushDx = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 1f));
		}
		if (textFormat == null || textFormat.FontSize != (float)footprintFontSize)
		{
			if (textFormat != null)
			{
				((DisposeBase)textFormat).Dispose();
				textFormat = null;
			}
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Consolas", (float)footprintFontSize);
			textFormat.TextAlignment = (TextAlignment)0;
		}
		if (smallTextFormat == null || smallTextFormat.FontSize != (float)(footprintFontSize - 2))
		{
			if (smallTextFormat != null)
			{
				((DisposeBase)smallTextFormat).Dispose();
				smallTextFormat = null;
			}
			smallTextFormat = new TextFormat(Globals.DirectWriteFactory, "Consolas", (float)(footprintFontSize - 2));
			smallTextFormat.TextAlignment = (TextAlignment)0;
		}
		if (textFormatRight == null || textFormatRight.FontSize != (float)footprintFontSize)
		{
			if (textFormatRight != null)
			{
				((DisposeBase)textFormatRight).Dispose();
				textFormatRight = null;
			}
			textFormatRight = new TextFormat(Globals.DirectWriteFactory, "Consolas", (float)footprintFontSize);
			textFormatRight.TextAlignment = (TextAlignment)1;
		}
		if (smallTextFormatRight == null || smallTextFormatRight.FontSize != (float)(footprintFontSize - 2))
		{
			if (smallTextFormatRight != null)
			{
				((DisposeBase)smallTextFormatRight).Dispose();
				smallTextFormatRight = null;
			}
			smallTextFormatRight = new TextFormat(Globals.DirectWriteFactory, "Consolas", (float)(footprintFontSize - 2));
			smallTextFormatRight.TextAlignment = (TextAlignment)1;
		}
		if (tableTextFormat == null || tableTextFormat.FontSize != (float)tableFontSize)
		{
			if (tableTextFormat != null)
			{
				((DisposeBase)tableTextFormat).Dispose();
				tableTextFormat = null;
			}
			tableTextFormat = new TextFormat(Globals.DirectWriteFactory, "Consolas", (float)tableFontSize);
			tableTextFormat.TextAlignment = (TextAlignment)0;
		}
		if (tableTextCenterFormat == null || tableTextCenterFormat.FontSize != (float)tableFontSize)
		{
			if (tableTextCenterFormat != null)
			{
				((DisposeBase)tableTextCenterFormat).Dispose();
				tableTextCenterFormat = null;
			}
			tableTextCenterFormat = new TextFormat(Globals.DirectWriteFactory, "Consolas", (float)tableFontSize);
			tableTextCenterFormat.TextAlignment = (TextAlignment)2;
		}
	}

	private void DisposeResources()
	{
		if (buyBrushDx != null)
		{
			((DisposeBase)buyBrushDx).Dispose();
			buyBrushDx = null;
		}
		if (sellBrushDx != null)
		{
			((DisposeBase)sellBrushDx).Dispose();
			sellBrushDx = null;
		}
		if (neutralBrushDx != null)
		{
			((DisposeBase)neutralBrushDx).Dispose();
			neutralBrushDx = null;
		}
		if (pocBrushDx != null)
		{
			((DisposeBase)pocBrushDx).Dispose();
			pocBrushDx = null;
		}
		if (textBrushDx != null)
		{
			((DisposeBase)textBrushDx).Dispose();
			textBrushDx = null;
		}
		if (tableBgBrushDx != null)
		{
			((DisposeBase)tableBgBrushDx).Dispose();
			tableBgBrushDx = null;
		}
		if (tableCellBgBrushDx != null)
		{
			((DisposeBase)tableCellBgBrushDx).Dispose();
			tableCellBgBrushDx = null;
		}
		if (tempBrushDx != null)
		{
			((DisposeBase)tempBrushDx).Dispose();
			tempBrushDx = null;
		}
		if (textFormat != null)
		{
			((DisposeBase)textFormat).Dispose();
			textFormat = null;
		}
		if (smallTextFormat != null)
		{
			((DisposeBase)smallTextFormat).Dispose();
			smallTextFormat = null;
		}
		if (textFormatRight != null)
		{
			((DisposeBase)textFormatRight).Dispose();
			textFormatRight = null;
		}
		if (smallTextFormatRight != null)
		{
			((DisposeBase)smallTextFormatRight).Dispose();
			smallTextFormatRight = null;
		}
		if (tableTextFormat != null)
		{
			((DisposeBase)tableTextFormat).Dispose();
			tableTextFormat = null;
		}
		if (tableTextCenterFormat != null)
		{
			((DisposeBase)tableTextCenterFormat).Dispose();
			tableTextCenterFormat = null;
		}
	}

	private Color4 BrushToColor4(Brush brush, float alpha = 0.7f)
	{
		SolidColorBrush val = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
		if (val != null)
		{
			Color color = val.Color;
			return new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, alpha);
		}
		return new Color4(0.5f, 0.5f, 0.5f, alpha);
	}

	private HashSet<double> CalculateValueArea(FootprintBar fpBar, double vaPercent)
	{
		HashSet<double> hashSet = new HashSet<double>();
		if (fpBar.Levels.Count == 0)
		{
			return hashSet;
		}
		long num = (long)((double)fpBar.TotalVolume * vaPercent);
		List<FootprintLevel> list = fpBar.Levels.Values.OrderByDescending((FootprintLevel l) => l.TotalVolume).ToList();
		long num2 = 0L;
		foreach (FootprintLevel item in list)
		{
			hashSet.Add(item.Price);
			num2 += item.TotalVolume;
			if (num2 >= num)
			{
				break;
			}
		}
		return hashSet;
	}

	public override void OnRenderTargetChanged()
	{
		DisposeResources();
	}
}
}
