using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators.Pulse;

[CategoryOrder("Session", 1)]
[CategoryOrder("POC", 2)]
[CategoryOrder("Virgin POC", 3)]
[CategoryOrder("Price Histogram", 4)]
[CategoryOrder("Volume Histogram", 5)]
[CategoryOrder("Data", 6)]
public class PulseVolumeProfileLite : Indicator
{
	public class MPBar
	{
		public Brush Color;

		public TimeSpan StartTime;

		public TimeSpan EndTime;

		public double Close;

		public double High;

		public double Low;

		public double Open;

		public string Letter;

		public string Tag;

		public MPBar(Brush _color, TimeSpan _starttime, TimeSpan _endtime, double _close, double _high, double _low, double _open, string _letter, string _tag)
		{
			Color = _color;
			StartTime = _starttime;
			EndTime = _endtime;
			Close = _close;
			High = _high;
			Low = _low;
			Open = _open;
			Letter = _letter;
			Tag = _tag;
		}
	}

	public class MPHelper
	{
		public bool IBCompleted;

		public bool OpenRangeCompleted;

		public bool VAHCompleted;

		public bool VALCompleted;

		public bool POCCompleted;

		public bool VirginVAHCompleted;

		public bool VirginVALCompleted;

		public bool VirginPOCCompleted;

		public DateTime BeginDate;

		public DateTime EndDate;

		public double Close;

		public double High;

		public double HighestHigh = double.MinValue;

		public double IBHigh = double.MinValue;

		public double IBLow = double.MaxValue;

		public double Low;

		public double LowestLow = double.MaxValue;

		public double Open;

		public double OpenRangeHigh = double.MinValue;

		public double OpenRangeLow = double.MaxValue;

		public double POCPrice;

		public double POCVAHPrice;

		public double POCVALPrice;

		public int CloseBarEndBarsAgo;

		public int CurrentIndex;

		public int EndBar;

		public int FirstBar;

		public int Index;

		public int LastBar;

		public int LetterIndex;

		public int MaxVolume;

		public int POCStartBar;

		public int POCEndBar;

		public int StartBar;

		public int TotalTPOCount;

		public int VirginPOCStartBar;

		public List<MPBar> MPBars = new List<MPBar>(89);

		public List<string> TagsArrows = new List<string>(2);

		public List<string> TagsIB = new List<string>(13);

		public List<string> TagsLines = new List<string>(21);

		public List<string> TagsPOC = new List<string>(13);

		public List<string> TagsPriceHistogram = new List<string>(377);

		public List<string> TagsTPO = new List<string>(2584);

		public List<string> TagsVolumeHistogram = new List<string>(377);

		public Dictionary<string, List<TPOLetter>> TPOLetters = new Dictionary<string, List<TPOLetter>>(StringComparer.Ordinal);

		public Dictionary<string, int> VolumeHistogram = new Dictionary<string, int>(StringComparer.Ordinal);

		public string CurrentIndexString;

		public string ID;

		public TimeSpan BeginTime;

		public TimeSpan EndTime;

		public TimeSpan IBEndTime;

		public TimeSpan OpenRangeEndTime;

		public PulseVPEnums.TradingHours TradingHours;

		public MPHelper(int _CurrentIndex, string _CurrentIndexString, PulseVPEnums.TradingHours _TradingHours, DateTime _BeginDate, int _FirstBar, int _StarttBar, double _Open, TimeSpan _BeginTime, TimeSpan _EndTime)
		{
			CurrentIndex = _CurrentIndex;
			CurrentIndexString = _CurrentIndexString;
			TradingHours = _TradingHours;
			BeginDate = _BeginDate;
			FirstBar = _FirstBar;
			StartBar = _StarttBar;
			Open = _Open;
			BeginTime = _BeginTime;
			EndTime = _EndTime;
			ID = string.Format("{0} {1}/{2}-{3}", new object[4]
			{
				TradingHours,
				BeginDate.Month,
				BeginDate.Day.ToString(),
				BeginDate.DayOfWeek.ToString().Substring(0, 3)
			});
			TagsArrows.Add("ArrowOpen" + CurrentIndexString);
			TagsArrows.Add("ArrowClose" + CurrentIndexString);
			string[] array = new string[14]
			{
				"IB100", "IB150U", "IB150L", "IB200U", "IB200L", "IB300U", "IB300L", "OpenRange", "POCVAH", "POC",
				"POCVAL", "VPOCVAH", "VPOC", "VPOCVAL"
			};
			foreach (string text in array)
			{
				TagsIB.Add(text + CurrentIndexString);
			}
		}
	}

	public class TPOLetter
	{
		public string Letter;

		public Brush LetterColor;

		public TPOLetter(Brush _LetterColor, string _Letter)
		{
			LetterColor = _LetterColor;
			Letter = _Letter;
		}
	}

	private List<MPHelper> alMPHelper;

	private List<MPHelper> alMPHelperDay;

	private List<MPHelper> alMPHelperWeek;

	private List<MPHelper> alMPHelperMonth;

	private int barsPerDay;

	private int letterIndex;

	private int mPHelperCurrentIndex;

	private int newVolumePerPlotRange;

	private int numberOfPlots;

	private int pOCIndex;

	private int previousMonth;

	private int previousWeek;

	private int ticksPerPlotRange;

	private int tPOCount;

	private int tPOsAbove;

	private int tPOsBelow;

	private int vA;

	private double ceiling;

	private double floor;

	private double highDecimal;

	private double keyHigh;

	private double keyLow;

	private double lowDecimal;

	private double tickSize_x_TicksPerPlot;

	private double vol;

	private bool invalidPeriodType;

	private bool isBusy;

	private bool refreshTPO;

	private Brush candleOutlineColor;

	private Brush downColor;

	private Brush upColor;

	private MPBar currentTPOBar;

	private MPHelper currentTPOSession;

	private MPHelper currentTPOSessionDay;

	private MPHelper currentTPOSessionWeek;

	private MPHelper currentTPOSessionMonth;

	private DateTimeFormatInfo dateTimeFormatInfo;

	private bool isPrimaryOneMinuteChart;

	private bool isPrimaryVolumeChart;

	private bool hasSecondaryDataSeries;

	private int lastDebugBar = -1;

	private PulseVPEnums.TradingHours tradingHours;

	private Brush[] TPOLetterColors;

	private TimeZoneInfo easternTimeZone;

	private DateTime cachedRthOpenDate = DateTime.MinValue;

	private int cachedRthOpenAbsoluteBar = -1;

	private readonly Dictionary<double, string> priceKeyCache = new Dictionary<double, string>(8192);

	private TimeSpan ts1000 = new TimeSpan(1, 0, 0, 0);

	private SimpleFont windings3Font;

	private const string closeArrowString = "\uf085";

	private const string openArrowString = "\uf086";

	private const string tPOLetters = "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

	public override string DisplayName
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Invalid comparison between Unknown and I4
			if ((int)((NinjaScript)this).State != 4 && (int)((NinjaScript)this).State != 5 && (int)((NinjaScript)this).State != 7)
			{
				return "Pulse Market Profile Lite (TPO) v1.0";
			}
			return $"Pulse Market Profile Lite (TPO) v1.0 ({((NinjaScriptBase)this).Instrument.FullName} ({((NinjaScriptBase)this).BarsPeriod.Value} {((NinjaScriptBase)this).BarsPeriod.BarsPeriodType}))";
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Draw POC", Order = 1, GroupName = "POC")]
	public bool DrawPOCBool { get; set; }

	[Browsable(false)]
	public bool DrawPriceHistogramBool { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Draw Virgin POC", Order = 1, GroupName = "Virgin POC")]
	public bool DrawVirginPOCBool { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Draw Volume Histogram Bool", Order = 1, GroupName = "Volume Histogram")]
	public bool DrawVolumeHistogramBool { get; set; }

	[Browsable(false)]
	public DateTime ETHBeginTime { get; set; }

	[Browsable(false)]
	public DateTime ETHEndTime { get; set; }

	[Browsable(false)]
	public bool PlotETHBool { get; set; }

	[Browsable(false)]
	public PulseVPEnums.SessionType MPSessionType { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "POC Stroke", Order = 2, GroupName = "POC")]
	public Stroke POCStroke { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VAH Stroke", Order = 3, GroupName = "POC")]
	public Stroke POCVAHStroke { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VAL Stroke", Order = 4, GroupName = "POC")]
	public Stroke POCVALStroke { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Brush PriceHistogramBorderColor { get; set; }

	[Browsable(false)]
	public string PriceHistogramBorderColorSerializable
	{
		get
		{
			return Serialize.BrushToString(PriceHistogramBorderColor);
		}
		set
		{
			PriceHistogramBorderColor = Serialize.StringToBrush(value);
		}
	}

	[Browsable(false)]
	public int PriceHistogramOpacity { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Brush PriceHistogramVAColor { get; set; }

	[Browsable(false)]
	public string PriceHistogramVAColorSerializable
	{
		get
		{
			return Serialize.BrushToString(PriceHistogramVAColor);
		}
		set
		{
			PriceHistogramVAColor = Serialize.StringToBrush(value);
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Brush PriceHistogramVAHColor { get; set; }

	[Browsable(false)]
	public string PriceHistogramVAHColorSerializable
	{
		get
		{
			return Serialize.BrushToString(PriceHistogramVAHColor);
		}
		set
		{
			PriceHistogramVAHColor = Serialize.StringToBrush(value);
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Brush PriceHistogramVALColor { get; set; }

	[Browsable(false)]
	public string PriceHistogramVALColorSerializable
	{
		get
		{
			return Serialize.BrushToString(PriceHistogramVALColor);
		}
		set
		{
			PriceHistogramVALColor = Serialize.StringToBrush(value);
		}
	}

	[Browsable(false)]
	public DateTime RTHBeginTime { get; set; }

	[Browsable(false)]
	public DateTime RTHEndTime { get; set; }

	[Browsable(false)]
	public bool ShowBars { get; set; }

	[Browsable(false)]
	public int TPOSize { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Value Area Size", Order = 2, GroupName = "POC", Description = "Value Area Size in percentage.")]
	public int ValueAreaSize { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Virgin POC Stroke", Order = 3, GroupName = "Virgin POC")]
	public Stroke VirginPOCStroke { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Virgin POC VAH Stroke", Order = 4, GroupName = "Virgin POC")]
	public Stroke VirginPOCVAHStroke { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Virgin POC VAL Stroke", Order = 5, GroupName = "Virgin POC")]
	public Stroke VirginPOCVALStroke { get; set; }

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Volume Histogram Border Color", Order = 5, GroupName = "Volume Histogram")]
	public Brush VolumeHistogramBorderColor { get; set; }

	[Browsable(false)]
	public string VolumeHistogramBorderColorSerializable
	{
		get
		{
			return Serialize.BrushToString(VolumeHistogramBorderColor);
		}
		set
		{
			VolumeHistogramBorderColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Volume Histogram Stroke", Order = 2, GroupName = "Volume Histogram")]
	public Stroke VolumeHistogramStroke { get; set; }

	public PulseVolumeProfileLite()
	{
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Invalid comparison between Unknown and I4
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Expected O, but got Unknown
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Expected O, but got Unknown
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Expected O, but got Unknown
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Invalid comparison between Unknown and I4
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Invalid comparison between Unknown and I4
		//IL_048b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0491: Invalid comparison between Unknown and I4
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Invalid comparison between Unknown and I4
		//IL_051b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0521: Invalid comparison between Unknown and I4
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Invalid comparison between Unknown and I4
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Invalid comparison between Unknown and I4
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Invalid comparison between Unknown and I4
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ef: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Pulse Volume Profile Lite - Professional Market Profile with Volume Analysis - Pulse Suite";
			((NinjaScriptBase)this).Name = "Pulse Market Profile Lite (TPO)";
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			DrawPOCBool = true;
			DrawPriceHistogramBool = true;
			DrawVirginPOCBool = true;
			DrawVolumeHistogramBool = true;
			ETHBeginTime = DateTime.Parse("16:16");
			ETHEndTime = DateTime.Parse("9:29");
			MPSessionType = PulseVPEnums.SessionType.RTHAndETH;
			PlotETHBool = false;
			POCStroke = new Stroke((Brush)(object)Brushes.Orange, (DashStyleHelper)0, 5f);
			POCVAHStroke = new Stroke((Brush)(object)Brushes.Black, (DashStyleHelper)0, 2f);
			POCVALStroke = new Stroke((Brush)(object)Brushes.Black, (DashStyleHelper)0, 2f);
			PriceHistogramVAHColor = (Brush)(object)Brushes.LightCoral;
			PriceHistogramVAColor = (Brush)(object)Brushes.Yellow;
			PriceHistogramVALColor = (Brush)(object)Brushes.LightGreen;
			PriceHistogramBorderColor = (Brush)(object)Brushes.Black;
			PriceHistogramOpacity = 80;
			RTHBeginTime = DateTime.Parse("9:30");
			RTHEndTime = DateTime.Parse("16:15");
			ShowBars = true;
			TPOSize = 30;
			ValueAreaSize = 68;
			POCVAHStroke = new Stroke((Brush)(object)Brushes.Black, (DashStyleHelper)0, 2f);
			VirginPOCStroke = new Stroke((Brush)(object)Brushes.Orange, (DashStyleHelper)1, 5f);
			VirginPOCVAHStroke = new Stroke((Brush)(object)Brushes.Black, (DashStyleHelper)1, 2f);
			VirginPOCVALStroke = new Stroke((Brush)(object)Brushes.Black, (DashStyleHelper)1, 2f);
			VolumeHistogramBorderColor = (Brush)(object)Brushes.Black;
			VolumeHistogramStroke = new Stroke((Brush)(object)Brushes.MediumSeaGreen, (DashStyleHelper)0, 34f, 30);
			((NinjaScript)this).Print((object)"Pulse Volume Profile Lite: Professional Market Profile initialized - Pulse Suite");
			return;
		}
		if ((int)((NinjaScript)this).State == 2)
		{
			bool flag = (int)((NinjaScriptBase)this).Bars.BarsPeriod.BarsPeriodType == 4 && ((NinjaScriptBase)this).Bars.BarsPeriod.Value == 1;
			bool flag2 = (int)((NinjaScriptBase)this).Bars.BarsPeriod.BarsPeriodType == 1;
			if (!flag && !flag2)
			{
				((NinjaScript)this).Print((object)$"Pulse Volume Profile Lite: Adding 1-minute series for {((NinjaScriptBase)this).Bars.BarsPeriod.BarsPeriodType} {((NinjaScriptBase)this).Bars.BarsPeriod.Value} chart");
				((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)4, 1);
			}
			else if (flag)
			{
				((NinjaScript)this).Print((object)"Pulse Volume Profile Lite: Already on 1-minute chart, no additional series needed");
			}
			else if (flag2)
			{
				((NinjaScript)this).Print((object)$"Pulse Volume Profile Lite: Volume chart detected ({((NinjaScriptBase)this).Bars.BarsPeriod.Value} volume), using primary series directly");
			}
			return;
		}
		if ((int)((NinjaScript)this).State == 4)
		{
			((IndicatorRenderBase)this).SetZOrder(500);
			dateTimeFormatInfo = new CultureInfo("en-US", useUserOverride: false).DateTimeFormat;
			isPrimaryOneMinuteChart = (int)((NinjaScriptBase)this).Bars.BarsPeriod.BarsPeriodType == 4 && ((NinjaScriptBase)this).Bars.BarsPeriod.Value == 1;
			isPrimaryVolumeChart = (int)((NinjaScriptBase)this).Bars.BarsPeriod.BarsPeriodType == 1;
			hasSecondaryDataSeries = ((NinjaScriptBase)this).BarsArray.Length > 1;
			alMPHelper = new List<MPHelper>(3660);
			alMPHelperDay = new List<MPHelper>(3660);
			alMPHelperMonth = new List<MPHelper>(120);
			alMPHelperWeek = new List<MPHelper>(530);
			if ((int)((NinjaScriptBase)this).Bars.BarsPeriod.BarsPeriodType == 4)
			{
				barsPerDay = 1440 / ((NinjaScriptBase)this).BarsPeriod.Value;
			}
			else
			{
				barsPerDay = 1440;
			}
			dateTimeFormatInfo = DateTimeFormatInfo.CurrentInfo;
			invalidPeriodType = false;
			letterIndex = 25;
			mPHelperCurrentIndex = 0;
			tradingHours = PulseVPEnums.TradingHours.None;
			vol = 0.0;
			windings3Font = new SimpleFont("Wingdings 3", 8);
			TPOLetterColors = (Brush[])(object)new Brush[13]
			{
				(Brush)Brushes.Black,
				(Brush)Brushes.CadetBlue,
				(Brush)Brushes.Brown,
				(Brush)Brushes.Navy,
				(Brush)Brushes.Goldenrod,
				(Brush)Brushes.Purple,
				(Brush)Brushes.Peru,
				(Brush)Brushes.SlateGray,
				(Brush)Brushes.DarkRed,
				(Brush)Brushes.Olive,
				(Brush)Brushes.Blue,
				(Brush)Brushes.IndianRed,
				(Brush)Brushes.Green
			};
			try
			{
				easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
				return;
			}
			catch
			{
				easternTimeZone = null;
				return;
			}
		}
		if ((int)((NinjaScript)this).State == 5)
		{
			if (((IndicatorRenderBase)this).ChartControl != null)
			{
				downColor = ((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.DownBrush;
				upColor = ((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.UpBrush;
				if (!ShowBars)
				{
					((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.DownBrush = (Brush)(object)Brushes.Transparent;
					((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.UpBrush = (Brush)(object)Brushes.Transparent;
				}
				CheckInstrumentAndBarType();
			}
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			if (((IndicatorRenderBase)this).ChartControl != null && (object)((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.DownBrush == Brushes.Transparent)
			{
				((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.DownBrush = downColor;
				((IndicatorRenderBase)this).ChartBars.Properties.ChartStyle.UpBrush = upColor;
			}
			priceKeyCache.Clear();
		}
	}

	private void CalculatePOC(MPHelper helper)
	{
		if (mPHelperCurrentIndex <= 0 || !refreshTPO)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter in helper.TPOLetters)
		{
			num = Math.Max(num, tPOLetter.Value.Count);
		}
		if (num <= 0)
		{
			return;
		}
		double num2 = (helper.HighestHigh + helper.LowestLow) / 2.0;
		double num3 = double.MaxValue;
		foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter2 in helper.TPOLetters)
		{
			if (num == tPOLetter2.Value.Count && TryParsePriceKey(tPOLetter2.Key, out var value))
			{
				double num4 = Math.Abs(num2 - value);
				if (num4 < num3)
				{
					num3 = num4;
					helper.POCPrice = value;
				}
			}
		}
	}

	private void CalculateVAHAndVAL(MPHelper helper)
	{
		if (mPHelperCurrentIndex <= 0 || !refreshTPO)
		{
			return;
		}
		vA = 0;
		foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter in helper.TPOLetters)
		{
			vA += tPOLetter.Value.Count;
		}
		vA = vA * ValueAreaSize / 100;
		string cachedPriceKey = GetCachedPriceKey(helper.POCPrice);
		if (!helper.TPOLetters.TryGetValue(cachedPriceKey, out var value))
		{
			return;
		}
		tPOCount = value.Count;
		keyHigh = helper.POCPrice + tickSize_x_TicksPerPlot;
		keyLow = helper.POCPrice - tickSize_x_TicksPerPlot;
		string cachedPriceKey2 = GetCachedPriceKey(keyHigh);
		string cachedPriceKey3 = GetCachedPriceKey(keyLow);
		List<TPOLetter> value2;
		bool flag = helper.TPOLetters.TryGetValue(cachedPriceKey2, out value2);
		List<TPOLetter> value3;
		bool flag2 = helper.TPOLetters.TryGetValue(cachedPriceKey3, out value3);
		while (flag || flag2)
		{
			tPOsAbove = (flag ? value2.Count : 0);
			tPOsBelow = (flag2 ? value3.Count : 0);
			if (tPOsAbove > tPOsBelow)
			{
				tPOCount += tPOsAbove;
				helper.POCVAHPrice = keyHigh + tickSize_x_TicksPerPlot;
				keyHigh += tickSize_x_TicksPerPlot;
				cachedPriceKey2 = GetCachedPriceKey(keyHigh);
			}
			else if (tPOsAbove < tPOsBelow)
			{
				tPOCount += tPOsBelow;
				helper.POCVALPrice = keyLow;
				keyLow -= tickSize_x_TicksPerPlot;
				cachedPriceKey3 = GetCachedPriceKey(keyLow);
			}
			else
			{
				tPOCount += tPOsAbove + tPOsBelow;
				helper.POCVAHPrice = keyHigh + tickSize_x_TicksPerPlot;
				helper.POCVALPrice = keyLow;
				keyHigh += tickSize_x_TicksPerPlot;
				cachedPriceKey2 = GetCachedPriceKey(keyHigh);
				keyLow -= tickSize_x_TicksPerPlot;
				cachedPriceKey3 = GetCachedPriceKey(keyLow);
			}
			if (tPOCount < vA)
			{
				flag = helper.TPOLetters.TryGetValue(cachedPriceKey2, out value2);
				flag2 = helper.TPOLetters.TryGetValue(cachedPriceKey3, out value3);
				continue;
			}
			break;
		}
	}

	private void CheckInstrumentAndBarType()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected I4, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Invalid comparison between Unknown and I4
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		if ((int)((IndicatorRenderBase)this).ChartBars.Properties.ChartStyleType != 1 && (int)((IndicatorRenderBase)this).ChartBars.Properties.ChartStyleType != 3)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "errormsg3", "This Indicator works best with CandleStick and OHLC charts.", TextPosition.BottomLeft, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, new SimpleFont("Tahoma", 12), ((IndicatorRenderBase)this).ChartControl.Properties.AxisPen.Brush, (Brush)(object)Brushes.Transparent, 100);
		}
		InstrumentType instrumentType = ((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType;
		switch ((int)instrumentType)
		{
		default:
			if ((int)instrumentType == 99)
			{
				ticksPerPlotRange = 1;
			}
			else
			{
				ticksPerPlotRange = 1;
			}
			break;
		case 4:
			ticksPerPlotRange = 2;
			break;
		case 0:
			ticksPerPlotRange = 1;
			break;
		case 2:
			ticksPerPlotRange = 10;
			break;
		case 3:
			ticksPerPlotRange = 1;
			break;
		case 1:
			if (((NinjaScriptBase)this).High[0] < 89.0)
			{
				ticksPerPlotRange = 1;
			}
			else if (((NinjaScriptBase)this).High[0] < 377.0)
			{
				ticksPerPlotRange = 2;
			}
			else if (((NinjaScriptBase)this).High[0] < 610.0)
			{
				ticksPerPlotRange = 3;
			}
			else
			{
				ticksPerPlotRange = 5;
			}
			break;
		}
		tickSize_x_TicksPerPlot = ((NinjaScriptBase)this).TickSize * (double)ticksPerPlotRange;
	}

	private MPBar CreateTPOBar(TimeSpan begintime, TimeSpan endtime)
	{
		if (letterIndex >= "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Length - 1)
		{
			letterIndex = -1;
		}
		letterIndex++;
		endtime = ((endtime > ts1000) ? endtime.Subtract(ts1000) : endtime);
		MPBar result = new MPBar((Brush)(object)Brushes.Transparent, begintime, endtime, ((NinjaScriptBase)this).Close[0], ((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).Low[0], ((NinjaScriptBase)this).Open[0], "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Substring(letterIndex, 1), "");
		vol = 0.0;
		return result;
	}

	private MPHelper CreateTPOSession(List<MPHelper> alMPHelper, PulseVPEnums.TradingHours tradingHours)
	{
		mPHelperCurrentIndex++;
		DateTime dateTime = ((NinjaScriptBase)this).Time[0];
		int currentBar = ((NinjaScriptBase)this).CurrentBar;
		MPHelper mPHelper = new MPHelper(mPHelperCurrentIndex, tradingHours.ToString().Substring(0, 2) + mPHelperCurrentIndex, tradingHours, dateTime.Date, currentBar, currentBar, ((NinjaScriptBase)this).Open[0], (tradingHours == PulseVPEnums.TradingHours.RTH) ? RTHBeginTime.TimeOfDay : ETHBeginTime.TimeOfDay, (tradingHours == PulseVPEnums.TradingHours.RTH) ? RTHEndTime.TimeOfDay : ETHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0)));
		switch (tradingHours)
		{
		case PulseVPEnums.TradingHours.Day:
			mPHelper.BeginTime = RTHBeginTime.TimeOfDay;
			mPHelper.EndTime = ETHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0));
			break;
		case PulseVPEnums.TradingHours.Week:
			mPHelper.BeginTime = RTHBeginTime.TimeOfDay;
			mPHelper.EndTime = ETHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0));
			break;
		case PulseVPEnums.TradingHours.Month:
			mPHelper.BeginTime = RTHBeginTime.TimeOfDay;
			mPHelper.EndTime = ETHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0));
			break;
		}
		mPHelper.Index = alMPHelper.Count;
		mPHelper.LetterIndex = 99;
		mPHelper.POCEndBar = 0;
		return mPHelper;
	}

	private void CalculateAuxiliaryValues()
	{
		lowDecimal = ((NinjaScriptBase)this).Low[0] % 1.0;
		floor = (double)decimal.Truncate((decimal)((NinjaScriptBase)this).Low[0]) + lowDecimal - lowDecimal % tickSize_x_TicksPerPlot;
		highDecimal = ((NinjaScriptBase)this).High[0] % 1.0;
		ceiling = (double)decimal.Truncate((decimal)((NinjaScriptBase)this).High[0]) + highDecimal - highDecimal % tickSize_x_TicksPerPlot;
		numberOfPlots = (int)((ceiling - floor) / tickSize_x_TicksPerPlot + 1.0);
		if (numberOfPlots < 1)
		{
			numberOfPlots = 1;
		}
		newVolumePerPlotRange = (int)Math.Round(1.0 * Math.Abs(((NinjaScriptBase)this).Volume[0] - vol) / (double)numberOfPlots);
		vol = ((NinjaScriptBase)this).Volume[0];
	}

	private void DrawPOC(MPHelper helper)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		if (DrawPOCBool && helper.POCStartBar >= 0)
		{
			Draw.Line((NinjaScriptBase)(object)this, "POC" + helper.CurrentIndexString, ((NinjaScriptBase)this).IsAutoScale, helper.POCStartBar, helper.POCPrice, (helper.POCStartBar >= helper.POCEndBar) ? helper.POCEndBar : 0, helper.POCPrice, POCStroke.Brush, POCStroke.DashStyleHelper, (int)POCStroke.Width);
			Draw.Line((NinjaScriptBase)(object)this, "POCVAH" + helper.CurrentIndexString, ((NinjaScriptBase)this).IsAutoScale, helper.POCStartBar, helper.POCVAHPrice, (helper.POCStartBar >= helper.POCEndBar) ? helper.POCEndBar : 0, helper.POCVAHPrice, POCVAHStroke.Brush, POCVAHStroke.DashStyleHelper, (int)POCVAHStroke.Width);
			Draw.Line((NinjaScriptBase)(object)this, "POCVAL" + helper.CurrentIndexString, ((NinjaScriptBase)this).IsAutoScale, helper.POCStartBar, helper.POCVALPrice, (helper.POCStartBar >= helper.POCEndBar) ? helper.POCEndBar : 0, helper.POCVALPrice, POCVALStroke.Brush, POCVALStroke.DashStyleHelper, (int)POCVALStroke.Width);
			helper.VirginPOCStartBar = ((NinjaScriptBase)this).CurrentBar;
		}
	}

	private void DrawPriceHistogram(MPHelper helper)
	{
		int num = ((helper.TradingHours == PulseVPEnums.TradingHours.RTH) ? (((NinjaScriptBase)this).CurrentBar - helper.StartBar) : (((NinjaScriptBase)this).CurrentBar - helper.FirstBar));
		if (num >= ((NinjaScriptBase)this).CurrentBar)
		{
			num = ((NinjaScriptBase)this).CurrentBar - 1;
		}
		if (num < 0)
		{
			num = 0;
		}
		if (num < 0 || num >= ((NinjaScriptBase)this).CurrentBar)
		{
			return;
		}
		double value;
		if (DrawPriceHistogramBool && refreshTPO)
		{
			string text = "PH" + helper.CurrentIndexString;
			{
				foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter in helper.TPOLetters)
				{
					if (TryParsePriceKey(tPOLetter.Key, out value) && !double.IsNaN(value) && !(value <= 0.0) && tPOLetter.Value != null && tPOLetter.Value.Count != 0)
					{
						int count = tPOLetter.Value.Count;
						Brush areaBrush;
						if (value != helper.POCPrice)
						{
							areaBrush = ((value >= helper.POCVAHPrice) ? PriceHistogramVAHColor : ((!(value < helper.POCVALPrice)) ? PriceHistogramVAColor : PriceHistogramVALColor));
						}
						else
						{
							areaBrush = PriceHistogramVAColor;
							int val = num - count * 4 - 1;
							helper.POCStartBar = Math.Max(0, Math.Min(val, ((NinjaScriptBase)this).CurrentBar - 1));
						}
						int num2 = num - count * 4 + 1;
						if (num2 < 0)
						{
							num2 = 0;
						}
						if (num2 >= ((NinjaScriptBase)this).CurrentBar)
						{
							num2 = ((NinjaScriptBase)this).CurrentBar - 1;
						}
						// num2 = num - count*4 + 1 is always < num (count >= 1), so the rectangle spans num2..num; guard must be num2 < num, not num < num2
						if (num >= 0 && num < ((NinjaScriptBase)this).CurrentBar && num2 >= 0 && num2 < ((NinjaScriptBase)this).CurrentBar && num2 < num)
						{
							string text2 = text + tPOLetter.Key;
							Draw.Rectangle((NinjaScriptBase)(object)this, text2, ((NinjaScriptBase)this).IsAutoScale, num, value, num2, value + tickSize_x_TicksPerPlot, PriceHistogramBorderColor, areaBrush, PriceHistogramOpacity).OutlineStroke.Width = 1f;
							helper.TagsPriceHistogram.Add(text2);
						}
					}
				}
				return;
			}
		}
		foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter2 in helper.TPOLetters)
		{
			if (TryParsePriceKey(tPOLetter2.Key, out value) && tPOLetter2.Value != null && tPOLetter2.Value.Count != 0)
			{
				int count2 = tPOLetter2.Value.Count;
				if (value == helper.POCPrice)
				{
					int val2 = num - count2 * 4 - 1;
					helper.POCStartBar = Math.Max(0, Math.Min(val2, ((NinjaScriptBase)this).CurrentBar - 1));
					break;
				}
			}
		}
	}

	private void DrawVirginPOC(List<MPHelper> alMPHelper)
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		if (!DrawVirginPOCBool)
		{
			return;
		}
		foreach (MPHelper item in alMPHelper)
		{
			if (!item.POCCompleted || item.VirginPOCStartBar <= 0)
			{
				continue;
			}
			int num = ((NinjaScriptBase)this).CurrentBar - item.VirginPOCStartBar;
			if (num >= 0 && num <= ((NinjaScriptBase)this).CurrentBar && !double.IsNaN(item.POCPrice) && !double.IsNaN(item.POCVAHPrice) && !double.IsNaN(item.POCVALPrice) && (!item.VirginVAHCompleted || !item.VirginVALCompleted || !item.VirginPOCCompleted))
			{
				if (!item.VirginVAHCompleted)
				{
					Draw.Line((NinjaScriptBase)(object)this, "VPOCVAH" + item.CurrentIndexString, ((NinjaScriptBase)this).IsAutoScale, num, item.POCVAHPrice, 0, item.POCVAHPrice, VirginPOCVAHStroke.Brush, VirginPOCVAHStroke.DashStyleHelper, (int)VirginPOCVAHStroke.Width);
					item.VirginVAHCompleted = item.POCVAHPrice >= ((NinjaScriptBase)this).Low[0] && item.POCVAHPrice <= ((NinjaScriptBase)this).High[0];
				}
				if (!item.VirginPOCCompleted)
				{
					Draw.Line((NinjaScriptBase)(object)this, "VPOC" + item.CurrentIndexString, ((NinjaScriptBase)this).IsAutoScale, num, item.POCPrice, 0, item.POCPrice, VirginPOCStroke.Brush, VirginPOCStroke.DashStyleHelper, (int)VirginPOCStroke.Width);
					item.VirginPOCCompleted = item.POCPrice >= ((NinjaScriptBase)this).Low[0] && item.POCPrice <= ((NinjaScriptBase)this).High[0];
				}
				if (!item.VirginVALCompleted)
				{
					Draw.Line((NinjaScriptBase)(object)this, "VPOCVAL" + item.CurrentIndexString, ((NinjaScriptBase)this).IsAutoScale, num, item.POCVALPrice, 0, item.POCVALPrice, VirginPOCVALStroke.Brush, VirginPOCVALStroke.DashStyleHelper, (int)VirginPOCVALStroke.Width);
					item.VirginVALCompleted = item.POCVALPrice >= ((NinjaScriptBase)this).Low[0] && item.POCVALPrice <= ((NinjaScriptBase)this).High[0];
				}
			}
		}
	}

	private void DrawVolumeHistogram(MPHelper helper)
	{
		if (!DrawVolumeHistogramBool || !refreshTPO)
		{
			return;
		}
		DateTime currentBarTime = ((NinjaScriptBase)this).Time[0];
		int rthOpenBarsAgo = GetRthOpenBarsAgo(helper, currentBarTime);
		if (rthOpenBarsAgo < 0 || rthOpenBarsAgo >= ((NinjaScriptBase)this).CurrentBar || helper.MaxVolume <= 0)
		{
			return;
		}
		double num = 1.0 / (double)helper.MaxVolume;
		string text = "VH" + helper.CurrentIndexString;
		foreach (KeyValuePair<string, int> item in helper.VolumeHistogram)
		{
			if (TryParsePriceKey(item.Key, out var value) && !double.IsNaN(value) && !(value <= 0.0))
			{
				double num2 = (double)item.Value * num;
				int num3 = (int)Math.Round((double)VolumeHistogramStroke.Width * num2);
				int num4 = rthOpenBarsAgo;
				int num5 = Math.Max(0, rthOpenBarsAgo - num3);
				if (num4 >= ((NinjaScriptBase)this).CurrentBar)
				{
					num4 = ((NinjaScriptBase)this).CurrentBar - 1;
				}
				if (num5 >= ((NinjaScriptBase)this).CurrentBar)
				{
					num5 = ((NinjaScriptBase)this).CurrentBar - 1;
				}
				// num5 = Max(0, num4 - num3) is always <= num4, so the bar spans num5..num4; guard must be num5 < num4, not num4 < num5
				if (num4 >= 0 && num4 < ((NinjaScriptBase)this).CurrentBar && num5 >= 0 && num5 < ((NinjaScriptBase)this).CurrentBar && num5 < num4)
				{
					Draw.Rectangle((NinjaScriptBase)(object)this, text + item.Key, ((NinjaScriptBase)this).IsAutoScale, num4, value, num5, value + tickSize_x_TicksPerPlot, VolumeHistogramBorderColor, VolumeHistogramStroke.Brush, VolumeHistogramStroke.Opacity).OutlineStroke.Width = 1f;
				}
			}
		}
	}

	private int GetRthOpenBarsAgo(MPHelper helper, DateTime currentBarTime)
	{
		if (cachedRthOpenDate == currentBarTime.Date && cachedRthOpenAbsoluteBar >= 0 && cachedRthOpenAbsoluteBar <= ((NinjaScriptBase)this).CurrentBar)
		{
			return ((NinjaScriptBase)this).CurrentBar - cachedRthOpenAbsoluteBar;
		}
		int num = ((helper != null) ? (((NinjaScriptBase)this).CurrentBar - helper.StartBar) : (-1));
		int num2 = -1;
		int num3 = Math.Min(((NinjaScriptBase)this).CurrentBar, 2000);
		if (easternTimeZone != null)
		{
			for (int i = 0; i <= num3; i++)
			{
				DateTime dateTime = ((NinjaScriptBase)this).Time[i];
				if (dateTime.Date < currentBarTime.Date)
				{
					break;
				}
				if (!(dateTime.Date != currentBarTime.Date))
				{
					DateTime dateTime2;
					try
					{
						dateTime2 = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, easternTimeZone);
					}
					catch
					{
						continue;
					}
					DateTime dateTime3 = new DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day, 9, 30, 0);
					if (Math.Abs((dateTime2 - dateTime3).TotalMinutes) <= 10.0)
					{
						num2 = i;
						break;
					}
				}
			}
		}
		if (num2 < 0)
		{
			num2 = num;
		}
		if (num2 >= 0 && num2 <= ((NinjaScriptBase)this).CurrentBar)
		{
			cachedRthOpenDate = currentBarTime.Date;
			cachedRthOpenAbsoluteBar = ((NinjaScriptBase)this).CurrentBar - num2;
		}
		return num2;
	}

	private static string PriceToKey(double price)
	{
		return price.ToString("0.##########", CultureInfo.InvariantCulture);
	}

	private string GetCachedPriceKey(double price)
	{
		double num = Math.Round(price, 10);
		if (!priceKeyCache.TryGetValue(num, out var value))
		{
			value = PriceToKey(num);
			priceKeyCache[num] = value;
		}
		return value;
	}

	private static bool TryParsePriceKey(string key, out double value)
	{
		if (double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
		{
			return true;
		}
		return double.TryParse(key, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
	}

	protected override void OnBarUpdate()
	{
		if (invalidPeriodType || ((NinjaScriptBase)this).BarsInProgress != 0 || ((NinjaScriptBase)this).CurrentBar < 100)
		{
			return;
		}
		try
		{
			((NinjaScriptBase)this).CandleOutlineBrush = (Brush)(ShowBars ? ((object)candleOutlineColor) : ((object)Brushes.Transparent));
			DateTime dateTime = ((NinjaScriptBase)this).Time[0];
			int weekOfYear = dateTimeFormatInfo.Calendar.GetWeekOfYear(dateTime.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
			if (TimeBetweenExclusive(dateTime.TimeOfDay, RTHBeginTime.TimeOfDay, RTHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0))))
			{
				if (tradingHours != PulseVPEnums.TradingHours.RTH)
				{
					if (mPHelperCurrentIndex > 0 && currentTPOSession != null)
					{
						currentTPOSession.POCCompleted = true;
					}
					tradingHours = PulseVPEnums.TradingHours.RTH;
					letterIndex = 99;
					currentTPOSession = CreateTPOSession(alMPHelper, PulseVPEnums.TradingHours.RTH);
					currentTPOBar = CreateTPOBar(RTHBeginTime.TimeOfDay, RTHBeginTime.TimeOfDay.Add(new TimeSpan(0, TPOSize, 0)));
					currentTPOSession.MPBars.Add(currentTPOBar);
					alMPHelper.Add(currentTPOSession);
					if (mPHelperCurrentIndex > 0 && currentTPOSessionDay != null)
					{
						currentTPOSessionDay.POCCompleted = true;
					}
					currentTPOSessionDay = CreateTPOSession(alMPHelperDay, PulseVPEnums.TradingHours.Day);
					currentTPOSessionDay.MPBars.Add(currentTPOBar);
					alMPHelperDay.Add(currentTPOSessionDay);
					if (weekOfYear != previousWeek)
					{
						if (mPHelperCurrentIndex > 0 && currentTPOSessionWeek != null)
						{
							currentTPOSessionWeek.POCCompleted = true;
						}
						currentTPOSessionWeek = CreateTPOSession(alMPHelperWeek, PulseVPEnums.TradingHours.Week);
						currentTPOSessionWeek.MPBars.Add(currentTPOBar);
						alMPHelperWeek.Add(currentTPOSessionWeek);
						previousWeek = weekOfYear;
					}
					if (dateTime.Month != previousMonth)
					{
						if (mPHelperCurrentIndex > 0 && currentTPOSessionMonth != null)
						{
							currentTPOSessionMonth.POCCompleted = true;
						}
						currentTPOSessionMonth = CreateTPOSession(alMPHelperMonth, PulseVPEnums.TradingHours.Month);
						currentTPOSessionMonth.MPBars.Add(currentTPOBar);
						alMPHelperMonth.Add(currentTPOSessionMonth);
						previousMonth = dateTime.Month;
					}
				}
			}
			else if (TimeBetweenExclusive(dateTime.TimeOfDay, ETHBeginTime.TimeOfDay, ETHEndTime.TimeOfDay) && tradingHours != PulseVPEnums.TradingHours.ETH)
			{
				if (mPHelperCurrentIndex > 0 && currentTPOSession != null)
				{
					currentTPOSession.POCCompleted = true;
				}
				tradingHours = PulseVPEnums.TradingHours.ETH;
				letterIndex = 99;
				currentTPOSession = CreateTPOSession(alMPHelper, PulseVPEnums.TradingHours.ETH);
				currentTPOBar = CreateTPOBar(ETHBeginTime.TimeOfDay, ETHBeginTime.TimeOfDay.Add(new TimeSpan(0, TPOSize, 0)));
				currentTPOSession.MPBars.Add(currentTPOBar);
				alMPHelper.Add(currentTPOSession);
				if (weekOfYear != previousWeek)
				{
					if (mPHelperCurrentIndex > 0 && currentTPOSessionWeek != null)
					{
						currentTPOSessionWeek.POCCompleted = true;
					}
					currentTPOSessionWeek = CreateTPOSession(alMPHelperWeek, PulseVPEnums.TradingHours.Week);
					currentTPOSessionWeek.MPBars.Add(currentTPOBar);
					alMPHelperWeek.Add(currentTPOSessionWeek);
					previousWeek = weekOfYear;
				}
				if (dateTime.Month != previousMonth)
				{
					if (mPHelperCurrentIndex > 0 && currentTPOSessionMonth != null)
					{
						currentTPOSessionMonth.POCCompleted = true;
					}
					currentTPOSessionMonth = CreateTPOSession(alMPHelperMonth, PulseVPEnums.TradingHours.Month);
					currentTPOSessionMonth.MPBars.Add(currentTPOBar);
					alMPHelperMonth.Add(currentTPOSessionMonth);
					previousMonth = dateTime.Month;
				}
			}
			if (!TimeBetweenExclusiveBar() && mPHelperCurrentIndex > 0)
			{
				currentTPOBar = CreateTPOBar(currentTPOBar.EndTime, currentTPOBar.EndTime.Add(new TimeSpan(0, TPOSize, 0)));
				currentTPOSession.MPBars.Add(currentTPOBar);
				if (currentTPOSessionDay != null)
				{
					currentTPOSessionDay.MPBars.Add(currentTPOBar);
				}
				currentTPOSessionWeek.MPBars.Add(currentTPOBar);
				currentTPOSessionMonth.MPBars.Add(currentTPOBar);
			}
			CalculateAuxiliaryValues();
			UpdateCurrentTPOSession(currentTPOSession);
			UpdateCurrentTPOBar(currentTPOBar);
			UpdateTPOLetters(currentTPOSession);
			UpdateVolumeHistogram(currentTPOSession);
			CalculatePOC(currentTPOSession);
			CalculateVAHAndVAL(currentTPOSession);
			if (!isBusy && (MPSessionType != PulseVPEnums.SessionType.RTHAndETH || currentTPOSession.TradingHours != PulseVPEnums.TradingHours.ETH || PlotETHBool))
			{
				if (MPSessionType == PulseVPEnums.SessionType.RTHAndETH && alMPHelper.Count > 1)
				{
					DrawPriceHistogram(currentTPOSession);
					DrawVolumeHistogram(currentTPOSession);
					DrawPOC(currentTPOSession);
					DrawVirginPOC(alMPHelper);
				}
				else if (MPSessionType == PulseVPEnums.SessionType.Day && alMPHelperDay.Count > 1)
				{
					DrawPriceHistogram(currentTPOSessionDay);
					DrawVolumeHistogram(currentTPOSessionDay);
					DrawPOC(currentTPOSessionDay);
					DrawVirginPOC(alMPHelperDay);
				}
				else if (MPSessionType == PulseVPEnums.SessionType.Week && alMPHelperWeek.Count > 1)
				{
					DrawPriceHistogram(currentTPOSessionWeek);
					DrawVolumeHistogram(currentTPOSessionWeek);
					DrawPOC(currentTPOSessionWeek);
					DrawVirginPOC(alMPHelperWeek);
				}
				refreshTPO = false;
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)$"Pulse Volume Profile Lite ERROR: {ex.Message} at Bar={((NinjaScriptBase)this).CurrentBar}, BarsInProgress={((NinjaScriptBase)this).BarsInProgress}");
		}
	}

	private bool TimeBetween(TimeSpan tsInputTime, TimeSpan tsStartTime, TimeSpan tsEndTime)
	{
		if (tsStartTime < tsEndTime)
		{
			if (tsInputTime >= tsStartTime)
			{
				return tsInputTime < tsEndTime;
			}
			return false;
		}
		if (!(tsInputTime >= tsStartTime) || !(tsInputTime <= new TimeSpan(23, 59, 59)))
		{
			if (tsInputTime >= new TimeSpan(0, 0, 0))
			{
				return tsInputTime < tsEndTime;
			}
			return false;
		}
		return true;
	}

	private bool TimeBetweenExclusive(TimeSpan tsInputTime, TimeSpan tsStartTime, TimeSpan tsEndTime)
	{
		if (tsStartTime < tsEndTime)
		{
			if (tsInputTime > tsStartTime)
			{
				return tsInputTime <= tsEndTime;
			}
			return false;
		}
		if (!(tsInputTime > tsStartTime) || !(tsInputTime <= new TimeSpan(23, 59, 59)))
		{
			if (tsInputTime >= new TimeSpan(0, 0, 0))
			{
				return tsInputTime < tsEndTime;
			}
			return false;
		}
		return true;
	}

	private bool TimeBetweenExclusiveBar()
	{
		DateTime dateTime = ((NinjaScriptBase)this).Time[0];
		if (currentTPOBar.StartTime < currentTPOBar.EndTime)
		{
			if (dateTime.TimeOfDay > currentTPOBar.StartTime)
			{
				return dateTime.TimeOfDay <= currentTPOBar.EndTime;
			}
			return false;
		}
		if (!(dateTime.TimeOfDay > currentTPOBar.StartTime) || !(dateTime.TimeOfDay <= new TimeSpan(23, 59, 59)))
		{
			if (dateTime.TimeOfDay >= new TimeSpan(0, 0, 0))
			{
				return dateTime.TimeOfDay < currentTPOBar.EndTime;
			}
			return false;
		}
		return true;
	}

	private bool TimeBetweenInclusive(TimeSpan tsInputTime, TimeSpan tsStartTime, TimeSpan tsEndTime)
	{
		if (tsStartTime < tsEndTime)
		{
			if (tsInputTime >= tsStartTime)
			{
				return tsInputTime <= tsEndTime;
			}
			return false;
		}
		if (!(tsInputTime >= tsStartTime) || !(tsInputTime <= new TimeSpan(23, 59, 59)))
		{
			if (tsInputTime >= new TimeSpan(0, 0, 0))
			{
				return tsInputTime <= tsEndTime;
			}
			return false;
		}
		return true;
	}

	private void UpdateCurrentTPOBar(MPBar bar)
	{
		bar.Close = ((NinjaScriptBase)this).Close[0];
		bar.High = Math.Max(((NinjaScriptBase)this).High[0], currentTPOBar.High);
		bar.Low = Math.Min(((NinjaScriptBase)this).Low[0], currentTPOBar.Low);
	}

	private void UpdateCurrentTPOSession(MPHelper helper)
	{
		helper.Close = ((NinjaScriptBase)this).Close[0];
		helper.HighestHigh = Math.Max(((NinjaScriptBase)this).High[0], helper.HighestHigh);
		helper.LowestLow = Math.Min(((NinjaScriptBase)this).Low[0], helper.LowestLow);
	}

	private void UpdateTPOLetters(MPHelper helper)
	{
		MPBar mPBar = helper.MPBars[helper.MPBars.Count - 1];
		string letter = mPBar.Letter;
		Brush color = mPBar.Color;
		double num = ((NinjaScriptBase)this).Close[0];
		double num2 = floor;
		for (int i = 0; i < numberOfPlots; i++)
		{
			double num3 = num2;
			string cachedPriceKey = GetCachedPriceKey(num3);
			if (!helper.TPOLetters.TryGetValue(cachedPriceKey, out var value))
			{
				value = new List<TPOLetter>(50);
				helper.TPOLetters.Add(cachedPriceKey, value);
				value.Add(new TPOLetter(color, letter));
				helper.TotalTPOCount++;
				helper.TagsPriceHistogram.Add("PH" + helper.CurrentIndexString + cachedPriceKey);
				refreshTPO = true;
			}
			else if (value[value.Count - 1].Letter != letter)
			{
				value.Add(new TPOLetter(color, letter));
				helper.TotalTPOCount++;
				refreshTPO = true;
			}
			if (num3 <= num)
			{
				int count = value.Count;
				helper.CloseBarEndBarsAgo = count * 4 + 4 - 1;
			}
			num2 += tickSize_x_TicksPerPlot;
		}
	}

	private void UpdateVolumeHistogram(MPHelper helper)
	{
		if (!DrawVolumeHistogramBool)
		{
			return;
		}
		double num = floor;
		for (int i = 0; i < numberOfPlots; i++)
		{
			string cachedPriceKey = GetCachedPriceKey(num);
			if (!helper.VolumeHistogram.TryGetValue(cachedPriceKey, out var value))
			{
				helper.VolumeHistogram.Add(cachedPriceKey, 0);
				helper.TagsVolumeHistogram.Add("VH" + helper.CurrentIndexString + cachedPriceKey);
				value = 0;
			}
			value += newVolumePerPlotRange;
			helper.VolumeHistogram[cachedPriceKey] = value;
			helper.MaxVolume = Math.Max(helper.MaxVolume, value);
			num += tickSize_x_TicksPerPlot;
		}
	}
}
