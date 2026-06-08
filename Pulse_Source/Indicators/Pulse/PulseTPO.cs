#region Using declarations
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
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.ChartStyles;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
	[CategoryOrder("Session", 1)]
	[CategoryOrder("POC", 2)]
	[CategoryOrder("Virgin POC", 3)]
	[CategoryOrder("TPO Display", 4)]
	[CategoryOrder("Delta Profile", 5)]
	public class PulseTPO : Indicator
	{
		public class MPBar
		{
			public System.Windows.Media.Brush Color;

			public TimeSpan StartTime;

			public TimeSpan EndTime;

			public double Close;

			public double High;

			public double Low;

			public double Open;

			public string Letter;

			public string Tag;

			public MPBar(System.Windows.Media.Brush _color, TimeSpan _starttime, TimeSpan _endtime, double _close, double _high, double _low, double _open, string _letter, string _tag)
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

			public int POCStartBar;

			public int POCEndBar;

			public int StartBar;

			public int TotalTPOCount;

			public int VirginPOCStartBar;

			public List<MPBar> MPBars = new List<MPBar>(89);

			public Dictionary<string, List<TPOLetter>> TPOLetters = new Dictionary<string, List<TPOLetter>>(StringComparer.Ordinal);

			public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();

			public string CurrentIndexString;

			public string ID;

			public TimeSpan BeginTime;

			public TimeSpan EndTime;

			public TimeSpan IBEndTime;

			public TimeSpan OpenRangeEndTime;

			public PulseTPOEnums.TradingHours TradingHours;

			public MPHelper(int _CurrentIndex, string _CurrentIndexString, PulseTPOEnums.TradingHours _TradingHours, DateTime _BeginDate, int _FirstBar, int _StarttBar, double _Open, TimeSpan _BeginTime, TimeSpan _EndTime)
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
				ID = $"{TradingHours} {BeginDate.Month}/{BeginDate.Day.ToString()}-{BeginDate.DayOfWeek.ToString().Substring(0, 3)}";
			}
		}

		public class TPOLetter
		{
			public string Letter;

			public System.Windows.Media.Brush LetterColor;

			public TPOLetter(System.Windows.Media.Brush _LetterColor, string _Letter)
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

		private int numberOfPlots;

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

		private bool invalidPeriodType;

		private bool refreshTPO;

		private System.Windows.Media.Brush downColor;

		private System.Windows.Media.Brush upColor;

		private System.Windows.Media.Brush strokeColor;

		private System.Windows.Media.Brush stroke2Color;

		private bool chartStyleHidden;

		private MPBar currentTPOBar;

		private MPHelper currentTPOSession;

		private MPHelper currentTPOSessionDay;

		private MPHelper currentTPOSessionWeek;

		private MPHelper currentTPOSessionMonth;

		private DateTimeFormatInfo dateTimeFormatInfo;

		private bool isPrimaryOneMinuteChart;

		private bool isPrimaryVolumeChart;

		private bool hasSecondaryDataSeries;

		private PulseTPOEnums.TradingHours tradingHours;

		private System.Windows.Media.Brush[] TPOLetterColors;

		private TimeSpan ts1000 = new TimeSpan(1, 0, 0, 0);

		private const string tPOLetters = "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

		private SharpDX.Direct2D1.SolidColorBrush tpoTextBrush;

		private TextFormat tpoTextFormat;

		private SharpDX.DirectWrite.Factory dwFactory;

		private SharpDX.Direct2D1.SolidColorBrush deltaPosBrush;

		private SharpDX.Direct2D1.SolidColorBrush deltaNegBrush;

		private int tickSeriesIndex = -1;

		private double deltaGroupSize;

		public override string DisplayName
		{
			get
			{
				if (State != State.DataLoaded && State != State.Historical && State != State.Realtime)
				{
					return "Pulse TPO v1.0";
				}
				return $"Pulse TPO v1.0 ({Instrument.FullName} ({BarsPeriod.Value} {BarsPeriod.BarsPeriodType}))";
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Draw POC", Order = 1, GroupName = "POC")]
		public bool DrawPOCBool { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Draw Virgin POC", Order = 1, GroupName = "Virgin POC")]
		public bool DrawVirginPOCBool { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Draw TPO Letters", Order = 1, GroupName = "TPO Display")]
		public bool DrawTPOLetters { get; set; }

		[Browsable(false)]
		public DateTime ETHBeginTime { get; set; }

		[Browsable(false)]
		public DateTime ETHEndTime { get; set; }

		[Browsable(false)]
		public bool PlotETHBool { get; set; }

		[Browsable(false)]
		public PulseTPOEnums.SessionType MPSessionType { get; set; }

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

		[Display(Name = "TPO Color Mode", Order = 2, GroupName = "TPO Display")]
		public PulseTPOEnums.ColorMode TPOColorMode { get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "TPO Text Color", Order = 3, GroupName = "TPO Display")]
		public System.Windows.Media.Brush TPOTextColor { get; set; }

		[Browsable(false)]
		public string TPOTextColorSerializable
		{
			get
			{
				return Serialize.BrushToString(TPOTextColor);
			}
			set
			{
				TPOTextColor = Serialize.StringToBrush(value);
			}
		}

		[NinjaScriptProperty]
		[Range(6, 24)]
		[Display(Name = "TPO Font Size", Order = 4, GroupName = "TPO Display")]
		public int TPOFontSize { get; set; }

		[NinjaScriptProperty]
		[Range(4, 30)]
		[Display(Name = "TPO Letter Spacing", Order = 5, GroupName = "TPO Display")]
		public int TPOLetterSpacing { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Draw Delta Profile", Order = 1, GroupName = "Delta Profile")]
		public bool DrawDeltaProfile { get; set; }

		[NinjaScriptProperty]
		[Range(50, 500)]
		[Display(Name = "Delta Profile Width (px)", Order = 2, GroupName = "Delta Profile")]
		public int DeltaProfileWidth { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Delta Tick Compression", Order = 3, GroupName = "Delta Profile", Description = "Number of ticks to group for delta bars. Auto-set per instrument if left at 1.")]
		public int DeltaTickCompression { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name = "Delta Profile Opacity %", Order = 4, GroupName = "Delta Profile")]
		public int DeltaProfileOpacity { get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Delta Positive Color", Order = 5, GroupName = "Delta Profile")]
		public System.Windows.Media.Brush DeltaPositiveColor { get; set; }

		[Browsable(false)]
		public string DeltaPositiveColorSerializable
		{
			get
			{
				return Serialize.BrushToString(DeltaPositiveColor);
			}
			set
			{
				DeltaPositiveColor = Serialize.StringToBrush(value);
			}
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Delta Negative Color", Order = 6, GroupName = "Delta Profile")]
		public System.Windows.Media.Brush DeltaNegativeColor { get; set; }

		[Browsable(false)]
		public string DeltaNegativeColorSerializable
		{
			get
			{
				return Serialize.BrushToString(DeltaNegativeColor);
			}
			set
			{
				DeltaNegativeColor = Serialize.StringToBrush(value);
			}
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse TPO - Professional Market Profile with TPO Letters - Pulse Suite";
				Name = "Pulse TPO";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				DrawPOCBool = true;
				DrawVirginPOCBool = true;
				DrawTPOLetters = true;
				ETHBeginTime = DateTime.Parse("16:16");
				ETHEndTime = DateTime.Parse("9:29");
				MPSessionType = PulseTPOEnums.SessionType.RTHAndETH;
				PlotETHBool = false;
				POCStroke = new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204)), DashStyleHelper.Solid, 5f);
				POCVAHStroke = new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), DashStyleHelper.Solid, 2f);
				POCVALStroke = new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), DashStyleHelper.Solid, 2f);
				RTHBeginTime = DateTime.Parse("9:30");
				RTHEndTime = DateTime.Parse("16:15");
				ShowBars = false;
				TPOSize = 30;
				ValueAreaSize = 68;
				VirginPOCStroke = new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204)), DashStyleHelper.Dash, 5f);
				VirginPOCVAHStroke = new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), DashStyleHelper.Dash, 2f);
				VirginPOCVALStroke = new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), DashStyleHelper.Dash, 2f);
				TPOColorMode = PulseTPOEnums.ColorMode.SingleColor;
				TPOTextColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
				TPOFontSize = 10;
				TPOLetterSpacing = 12;
				DrawDeltaProfile = true;
				DeltaProfileWidth = 80;
				DeltaPositiveColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				DeltaNegativeColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				DeltaTickCompression = 4;
				DeltaProfileOpacity = 80;
				Print("Pulse TPO: Professional Market Profile initialized - Pulse Suite");
			}
			else if (State == State.Configure)
			{
				bool flag = Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && Bars.BarsPeriod.Value == 1;
				bool flag2 = Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Volume;
				if (!flag && !flag2)
				{
					Print($"Pulse TPO: Adding 1-minute series for {Bars.BarsPeriod.BarsPeriodType} {Bars.BarsPeriod.Value} chart");
					AddDataSeries(BarsPeriodType.Minute, 1);
				}
				else if (flag)
				{
					Print("Pulse TPO: Already on 1-minute chart, no additional series needed");
				}
				else if (flag2)
				{
					Print($"Pulse TPO: Volume chart detected ({Bars.BarsPeriod.Value} volume), using primary series directly");
				}
				if (DrawDeltaProfile)
				{
					AddDataSeries(BarsPeriodType.Tick, 1);
					Print("Pulse TPO: Added 1-tick series for delta profile");
				}
			}
			else if (State == State.DataLoaded)
			{
				SetZOrder(500);
				dateTimeFormatInfo = new CultureInfo("en-US", useUserOverride: false).DateTimeFormat;
				isPrimaryOneMinuteChart = Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && Bars.BarsPeriod.Value == 1;
				isPrimaryVolumeChart = Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Volume;
				hasSecondaryDataSeries = BarsArray.Length > 1;
				alMPHelper = new List<MPHelper>(3660);
				alMPHelperDay = new List<MPHelper>(3660);
				alMPHelperMonth = new List<MPHelper>(120);
				alMPHelperWeek = new List<MPHelper>(530);
				if (Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute)
				{
					barsPerDay = 1440 / BarsPeriod.Value;
				}
				else
				{
					barsPerDay = 1440;
				}
				dateTimeFormatInfo = DateTimeFormatInfo.CurrentInfo;
				invalidPeriodType = false;
				letterIndex = 25;
				mPHelperCurrentIndex = 0;
				tradingHours = PulseTPOEnums.TradingHours.None;
				TPOLetterColors = new System.Windows.Media.Brush[13]
				{
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
					new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45))
				};
				dwFactory = new SharpDX.DirectWrite.Factory();
				if (DrawDeltaProfile)
				{
					tickSeriesIndex = BarsArray.Length - 1;
				}
			}
			else if (State == State.Historical)
			{
				if (ChartControl != null)
				{
					if (!ShowBars)
					{
						ApplyHideBarsStyle();
					}
					else
					{
						RestoreBarsStyle();
					}
					CheckInstrumentAndBarType();
					if (DrawDeltaProfile)
					{
						deltaGroupSize = tickSize_x_TicksPerPlot;
					}
				}
			}
			else if (State == State.Terminated)
			{
				RestoreBarsStyle();
				if (tpoTextBrush != null)
				{
					tpoTextBrush.Dispose();
				}
				if (tpoTextFormat != null)
				{
					tpoTextFormat.Dispose();
				}
				if (dwFactory != null)
				{
					dwFactory.Dispose();
				}
				if (deltaPosBrush != null)
				{
					deltaPosBrush.Dispose();
				}
				if (deltaNegBrush != null)
				{
					deltaNegBrush.Dispose();
				}
			}
		}

		private void CheckInstrumentAndBarType()
		{
			if (ChartBars.Properties.ChartStyleType != ChartStyleType.CandleStick && ChartBars.Properties.ChartStyleType != ChartStyleType.OHLC)
			{
				Draw.TextFixed(this, "errormsg3", "This Indicator works best with CandleStick and OHLC charts.", TextPosition.BottomLeft, ChartControl.Properties.ChartText, new SimpleFont("Tahoma", 12), ChartControl.Properties.AxisPen.Brush, Brushes.Transparent, 100);
			}
			switch (Instrument.MasterInstrument.InstrumentType)
			{
			case InstrumentType.Forex:
				ticksPerPlotRange = 2;
				break;
			case InstrumentType.Future:
				ticksPerPlotRange = 1;
				break;
			case InstrumentType.Index:
				ticksPerPlotRange = 10;
				break;
			case InstrumentType.Option:
				ticksPerPlotRange = 1;
				break;
			case InstrumentType.Stock:
				if (High[0] < 89.0)
				{
					ticksPerPlotRange = 1;
				}
				else if (High[0] < 377.0)
				{
					ticksPerPlotRange = 2;
				}
				else if (High[0] < 610.0)
				{
					ticksPerPlotRange = 3;
				}
				else
				{
					ticksPerPlotRange = 5;
				}
				break;
			case InstrumentType.Unknown:
				ticksPerPlotRange = 1;
				break;
			default:
				ticksPerPlotRange = 1;
				break;
			}
			tickSize_x_TicksPerPlot = TickSize * (double)ticksPerPlotRange;
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
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter2 in helper.TPOLetters)
			{
				if (num == tPOLetter2.Value.Count)
				{
					list.Add(tPOLetter2.Key);
				}
			}
			if (list.Count <= 0)
			{
				return;
			}
			double num2 = (helper.HighestHigh + helper.LowestLow) / 2.0;
			double num3 = double.MaxValue;
			foreach (string item in list)
			{
				if (TryParsePriceKey(item, out var value) && Math.Abs(num2 - value) < num3)
				{
					num3 = Math.Abs(num2 - value);
					helper.POCPrice = value;
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
			string key = PriceToKey(helper.POCPrice);
			if (!helper.TPOLetters.ContainsKey(key))
			{
				return;
			}
			tPOCount = helper.TPOLetters[key].Count;
			keyHigh = helper.POCPrice + tickSize_x_TicksPerPlot;
			keyLow = helper.POCPrice - tickSize_x_TicksPerPlot;
			string key2 = PriceToKey(keyHigh);
			string key3 = PriceToKey(keyLow);
			while (helper.TPOLetters.ContainsKey(key2) || helper.TPOLetters.ContainsKey(key3))
			{
				if (helper.TPOLetters.ContainsKey(key2))
				{
					tPOsAbove = helper.TPOLetters[key2].Count;
				}
				else
				{
					tPOsAbove = 0;
				}
				if (helper.TPOLetters.ContainsKey(key3))
				{
					tPOsBelow = helper.TPOLetters[key3].Count;
				}
				else
				{
					tPOsBelow = 0;
				}
				if (tPOsAbove > tPOsBelow)
				{
					tPOCount += tPOsAbove;
					helper.POCVAHPrice = keyHigh + tickSize_x_TicksPerPlot;
					keyHigh += tickSize_x_TicksPerPlot;
					key2 = PriceToKey(keyHigh);
				}
				else if (tPOsAbove < tPOsBelow)
				{
					tPOCount += tPOsBelow;
					helper.POCVALPrice = keyLow;
					keyLow -= tickSize_x_TicksPerPlot;
					key3 = PriceToKey(keyLow);
				}
				else
				{
					tPOCount += tPOsAbove + tPOsBelow;
					helper.POCVAHPrice = keyHigh + tickSize_x_TicksPerPlot;
					helper.POCVALPrice = keyLow;
					keyHigh += tickSize_x_TicksPerPlot;
					key2 = PriceToKey(keyHigh);
					keyLow -= tickSize_x_TicksPerPlot;
					key3 = PriceToKey(keyLow);
				}
				if (tPOCount >= vA)
				{
					break;
				}
			}
		}

		private MPBar CreateTPOBar(TimeSpan begintime, TimeSpan endtime)
		{
			if (letterIndex >= "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Length - 1)
			{
				letterIndex = -1;
			}
			letterIndex++;
			endtime = ((endtime > ts1000) ? endtime.Subtract(ts1000) : endtime);
			return new MPBar(Brushes.Transparent, begintime, endtime, Close[0], High[0], Low[0], Open[0], "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Substring(letterIndex, 1), "");
		}

		private MPHelper CreateTPOSession(List<MPHelper> alMPHelper, PulseTPOEnums.TradingHours tradingHours)
		{
			mPHelperCurrentIndex++;
			DateTime dateTime = Time[0];
			int currentBar = CurrentBar;
			MPHelper mPHelper = new MPHelper(mPHelperCurrentIndex, tradingHours.ToString().Substring(0, 2) + mPHelperCurrentIndex, tradingHours, dateTime.Date, currentBar, currentBar, Open[0], (tradingHours == PulseTPOEnums.TradingHours.RTH) ? RTHBeginTime.TimeOfDay : ETHBeginTime.TimeOfDay, (tradingHours == PulseTPOEnums.TradingHours.RTH) ? RTHEndTime.TimeOfDay : ETHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0)));
			switch (tradingHours)
			{
			case PulseTPOEnums.TradingHours.Day:
				mPHelper.BeginTime = RTHBeginTime.TimeOfDay;
				mPHelper.EndTime = ETHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0));
				break;
			case PulseTPOEnums.TradingHours.Week:
				mPHelper.BeginTime = RTHBeginTime.TimeOfDay;
				mPHelper.EndTime = ETHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0));
				break;
			case PulseTPOEnums.TradingHours.Month:
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
			lowDecimal = Low[0] % 1.0;
			floor = (double)decimal.Truncate((decimal)Low[0]) + lowDecimal - lowDecimal % tickSize_x_TicksPerPlot;
			highDecimal = High[0] % 1.0;
			ceiling = (double)decimal.Truncate((decimal)High[0]) + highDecimal - highDecimal % tickSize_x_TicksPerPlot;
			numberOfPlots = (int)((ceiling - floor) / tickSize_x_TicksPerPlot + 1.0);
		}

		private void DrawPOC(MPHelper helper)
		{
			if (DrawPOCBool && helper.POCStartBar >= 0)
			{
				Draw.Line(this, "POC" + helper.CurrentIndexString, IsAutoScale, helper.POCStartBar, helper.POCPrice, (helper.POCStartBar >= helper.POCEndBar) ? helper.POCEndBar : 0, helper.POCPrice, POCStroke.Brush, POCStroke.DashStyleHelper, (int)POCStroke.Width);
				Draw.Line(this, "POCVAH" + helper.CurrentIndexString, IsAutoScale, helper.POCStartBar, helper.POCVAHPrice, (helper.POCStartBar >= helper.POCEndBar) ? helper.POCEndBar : 0, helper.POCVAHPrice, POCVAHStroke.Brush, POCVAHStroke.DashStyleHelper, (int)POCVAHStroke.Width);
				Draw.Line(this, "POCVAL" + helper.CurrentIndexString, IsAutoScale, helper.POCStartBar, helper.POCVALPrice, (helper.POCStartBar >= helper.POCEndBar) ? helper.POCEndBar : 0, helper.POCVALPrice, POCVALStroke.Brush, POCVALStroke.DashStyleHelper, (int)POCVALStroke.Width);
				helper.VirginPOCStartBar = CurrentBar;
			}
		}

		private void DrawVirginPOC(List<MPHelper> alMPHelper)
		{
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
				int num = CurrentBar - item.VirginPOCStartBar;
				if (num >= 0 && num <= CurrentBar && !double.IsNaN(item.POCPrice) && !double.IsNaN(item.POCVAHPrice) && !double.IsNaN(item.POCVALPrice) && (!item.VirginVAHCompleted || !item.VirginVALCompleted || !item.VirginPOCCompleted))
				{
					if (!item.VirginVAHCompleted)
					{
						Draw.Line(this, "VPOCVAH" + item.CurrentIndexString, IsAutoScale, num, item.POCVAHPrice, 0, item.POCVAHPrice, VirginPOCVAHStroke.Brush, VirginPOCVAHStroke.DashStyleHelper, (int)VirginPOCVAHStroke.Width);
						item.VirginVAHCompleted = item.POCVAHPrice >= Low[0] && item.POCVAHPrice <= High[0];
					}
					if (!item.VirginPOCCompleted)
					{
						Draw.Line(this, "VPOC" + item.CurrentIndexString, IsAutoScale, num, item.POCPrice, 0, item.POCPrice, VirginPOCStroke.Brush, VirginPOCStroke.DashStyleHelper, (int)VirginPOCStroke.Width);
						item.VirginPOCCompleted = item.POCPrice >= Low[0] && item.POCPrice <= High[0];
					}
					if (!item.VirginVALCompleted)
					{
						Draw.Line(this, "VPOCVAL" + item.CurrentIndexString, IsAutoScale, num, item.POCVALPrice, 0, item.POCVALPrice, VirginPOCVALStroke.Brush, VirginPOCVALStroke.DashStyleHelper, (int)VirginPOCVALStroke.Width);
						item.VirginVALCompleted = item.POCVALPrice >= Low[0] && item.POCVALPrice <= High[0];
					}
				}
			}
		}

		private void ProcessTickDelta()
		{
			if (tickSeriesIndex < 0 || tickSeriesIndex >= BarsArray.Length || CurrentBars[tickSeriesIndex] < 1 || deltaGroupSize <= 0.0)
			{
				return;
			}
			double close = BarsArray[tickSeriesIndex].GetClose(CurrentBars[tickSeriesIndex]);
			double bid = BarsArray[tickSeriesIndex].GetBid(CurrentBars[tickSeriesIndex]);
			double ask = BarsArray[tickSeriesIndex].GetAsk(CurrentBars[tickSeriesIndex]);
			double num = BarsArray[tickSeriesIndex].GetVolume(CurrentBars[tickSeriesIndex]);
			if (num <= 0.0 || double.IsNaN(close) || double.IsNaN(bid) || double.IsNaN(ask))
			{
				return;
			}
			long num2 = 0L;
			if (close >= ask)
			{
				num2 = (long)num;
			}
			else if (close <= bid)
			{
				num2 = -(long)num;
			}
			if (num2 == 0L)
			{
				return;
			}
			double key = Math.Round(Math.Floor(close / deltaGroupSize) * deltaGroupSize, 6);
			MPHelper mPHelper = currentTPOSession;
			if (mPHelper != null)
			{
				if (mPHelper.DeltaByPrice.TryGetValue(key, out var value))
				{
					mPHelper.DeltaByPrice[key] = value + num2;
				}
				else
				{
					mPHelper.DeltaByPrice[key] = num2;
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (invalidPeriodType)
			{
				return;
			}
			if (DrawDeltaProfile && tickSeriesIndex > 0 && BarsInProgress == tickSeriesIndex)
			{
				ProcessTickDelta();
			}
			else
			{
				if (BarsInProgress != 0 || CurrentBar < 100)
				{
					return;
				}
				try
				{
					if (!ShowBars)
					{
						ApplyHideBarsStyle();
					}
					else
					{
						RestoreBarsStyle();
					}
					DateTime dateTime = Time[0];
					if (TimeBetweenExclusive(dateTime.TimeOfDay, RTHBeginTime.TimeOfDay, RTHEndTime.TimeOfDay.Add(new TimeSpan(0, (dateTime.DayOfWeek == DayOfWeek.Friday) ? 120 : 0, 0))))
					{
						if (tradingHours != PulseTPOEnums.TradingHours.RTH)
						{
							if (mPHelperCurrentIndex > 0 && currentTPOSession != null)
							{
								currentTPOSession.POCCompleted = true;
							}
							tradingHours = PulseTPOEnums.TradingHours.RTH;
							letterIndex = 99;
							currentTPOSession = CreateTPOSession(alMPHelper, PulseTPOEnums.TradingHours.RTH);
							currentTPOBar = CreateTPOBar(RTHBeginTime.TimeOfDay, RTHBeginTime.TimeOfDay.Add(new TimeSpan(0, TPOSize, 0)));
							currentTPOSession.MPBars.Add(currentTPOBar);
							alMPHelper.Add(currentTPOSession);
							if (mPHelperCurrentIndex > 0 && currentTPOSessionDay != null)
							{
								currentTPOSessionDay.POCCompleted = true;
							}
							currentTPOSessionDay = CreateTPOSession(alMPHelperDay, PulseTPOEnums.TradingHours.Day);
							currentTPOSessionDay.MPBars.Add(currentTPOBar);
							alMPHelperDay.Add(currentTPOSessionDay);
							if (dateTimeFormatInfo.Calendar.GetWeekOfYear(dateTime.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday) != previousWeek)
							{
								if (mPHelperCurrentIndex > 0 && currentTPOSessionWeek != null)
								{
									currentTPOSessionWeek.POCCompleted = true;
								}
								currentTPOSessionWeek = CreateTPOSession(alMPHelperWeek, PulseTPOEnums.TradingHours.Week);
								currentTPOSessionWeek.MPBars.Add(currentTPOBar);
								alMPHelperWeek.Add(currentTPOSessionWeek);
								previousWeek = dateTimeFormatInfo.Calendar.GetWeekOfYear(dateTime.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
							}
							if (dateTime.Month != previousMonth)
							{
								if (mPHelperCurrentIndex > 0 && currentTPOSessionMonth != null)
								{
									currentTPOSessionMonth.POCCompleted = true;
								}
								currentTPOSessionMonth = CreateTPOSession(alMPHelperMonth, PulseTPOEnums.TradingHours.Month);
								currentTPOSessionMonth.MPBars.Add(currentTPOBar);
								alMPHelperMonth.Add(currentTPOSessionMonth);
								previousMonth = dateTime.Month;
							}
						}
					}
					else if (TimeBetweenExclusive(dateTime.TimeOfDay, ETHBeginTime.TimeOfDay, ETHEndTime.TimeOfDay) && tradingHours != PulseTPOEnums.TradingHours.ETH)
					{
						if (mPHelperCurrentIndex > 0 && currentTPOSession != null)
						{
							currentTPOSession.POCCompleted = true;
						}
						tradingHours = PulseTPOEnums.TradingHours.ETH;
						letterIndex = 99;
						currentTPOSession = CreateTPOSession(alMPHelper, PulseTPOEnums.TradingHours.ETH);
						currentTPOBar = CreateTPOBar(ETHBeginTime.TimeOfDay, ETHBeginTime.TimeOfDay.Add(new TimeSpan(0, TPOSize, 0)));
						currentTPOSession.MPBars.Add(currentTPOBar);
						alMPHelper.Add(currentTPOSession);
						if (dateTimeFormatInfo.Calendar.GetWeekOfYear(dateTime.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday) != previousWeek)
						{
							if (mPHelperCurrentIndex > 0 && currentTPOSessionWeek != null)
							{
								currentTPOSessionWeek.POCCompleted = true;
							}
							currentTPOSessionWeek = CreateTPOSession(alMPHelperWeek, PulseTPOEnums.TradingHours.Week);
							currentTPOSessionWeek.MPBars.Add(currentTPOBar);
							alMPHelperWeek.Add(currentTPOSessionWeek);
							previousWeek = dateTimeFormatInfo.Calendar.GetWeekOfYear(dateTime.Date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
						}
						if (dateTime.Month != previousMonth)
						{
							if (mPHelperCurrentIndex > 0 && currentTPOSessionMonth != null)
							{
								currentTPOSessionMonth.POCCompleted = true;
							}
							currentTPOSessionMonth = CreateTPOSession(alMPHelperMonth, PulseTPOEnums.TradingHours.Month);
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
					CalculatePOC(currentTPOSession);
					CalculateVAHAndVAL(currentTPOSession);
					if (MPSessionType != PulseTPOEnums.SessionType.RTHAndETH || currentTPOSession.TradingHours != PulseTPOEnums.TradingHours.ETH || PlotETHBool)
					{
						if (MPSessionType == PulseTPOEnums.SessionType.RTHAndETH && alMPHelper.Count > 1)
						{
							UpdatePOCStartBar(currentTPOSession);
							DrawPOC(currentTPOSession);
							DrawVirginPOC(alMPHelper);
						}
						else if (MPSessionType == PulseTPOEnums.SessionType.Day && alMPHelperDay.Count > 1)
						{
							UpdatePOCStartBar(currentTPOSessionDay);
							DrawPOC(currentTPOSessionDay);
							DrawVirginPOC(alMPHelperDay);
						}
						else if (MPSessionType == PulseTPOEnums.SessionType.Week && alMPHelperWeek.Count > 1)
						{
							UpdatePOCStartBar(currentTPOSessionWeek);
							DrawPOC(currentTPOSessionWeek);
							DrawVirginPOC(alMPHelperWeek);
						}
						refreshTPO = false;
					}
				}
				catch (Exception ex)
				{
					Print("Pulse TPO ERROR: " + ex.Message);
				}
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (!ShowBars)
			{
				ApplyHideBarsStyle();
			}
			else
			{
				RestoreBarsStyle();
			}
			if ((!DrawTPOLetters && !DrawDeltaProfile) || ChartControl == null || RenderTarget == null || alMPHelper == null || alMPHelper.Count == 0)
			{
				return;
			}
			System.Windows.Media.Color brushColor = GetBrushColor(TPOTextColor, System.Windows.Media.Color.FromRgb(45, 45, 45));
			Color4 color = new Color4((float)(int)brushColor.R / 255f, (float)(int)brushColor.G / 255f, (float)(int)brushColor.B / 255f, 1f);
			if (tpoTextBrush == null || !Color4Equals(tpoTextBrush.Color, color))
			{
				if (tpoTextBrush != null)
				{
					tpoTextBrush.Dispose();
				}
				tpoTextBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color);
			}
			if (tpoTextFormat == null || Math.Abs(tpoTextFormat.FontSize - (float)TPOFontSize) > 0.01f)
			{
				if (tpoTextFormat != null)
				{
					tpoTextFormat.Dispose();
				}
				tpoTextFormat = new TextFormat(dwFactory, "Consolas", TPOFontSize)
				{
					TextAlignment = TextAlignment.Center,
					ParagraphAlignment = ParagraphAlignment.Center
				};
			}
			float alpha = (float)DeltaProfileOpacity / 100f;
			if (DrawDeltaProfile)
			{
				System.Windows.Media.Color brushColor2 = GetBrushColor(DeltaPositiveColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
				System.Windows.Media.Color brushColor3 = GetBrushColor(DeltaNegativeColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
				Color4 color2 = new Color4((float)(int)brushColor2.R / 255f, (float)(int)brushColor2.G / 255f, (float)(int)brushColor2.B / 255f, alpha);
				Color4 color3 = new Color4((float)(int)brushColor3.R / 255f, (float)(int)brushColor3.G / 255f, (float)(int)brushColor3.B / 255f, alpha);
				if (deltaPosBrush == null || !Color4Equals(deltaPosBrush.Color, color2))
				{
					if (deltaPosBrush != null)
					{
						deltaPosBrush.Dispose();
					}
					deltaPosBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color2);
				}
				if (deltaNegBrush == null || !Color4Equals(deltaNegBrush.Color, color3))
				{
					if (deltaNegBrush != null)
					{
						deltaNegBrush.Dispose();
					}
					deltaNegBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color3);
				}
			}
			List<MPHelper> list = null;
			if (MPSessionType == PulseTPOEnums.SessionType.RTHAndETH)
			{
				list = alMPHelper;
			}
			else if (MPSessionType == PulseTPOEnums.SessionType.Day)
			{
				list = alMPHelperDay;
			}
			else if (MPSessionType == PulseTPOEnums.SessionType.Week)
			{
				list = alMPHelperWeek;
			}
			else if (MPSessionType == PulseTPOEnums.SessionType.Month)
			{
				list = alMPHelperMonth;
			}
			if (list == null || list.Count == 0)
			{
				return;
			}
			int fromIndex = ChartBars.FromIndex;
			int toIndex = ChartBars.ToIndex;
			float num = TPOLetterSpacing;
			float num2 = TPOFontSize + 2;
			float num3 = chartControl.GetXByBarIndex(ChartBars, fromIndex);
			float num4 = chartControl.GetXByBarIndex(ChartBars, toIndex);
			float val = chartScale.GetYByValue(chartScale.MinValue);
			float val2 = chartScale.GetYByValue(chartScale.MaxValue);
			float num5 = Math.Min(val, val2) - 50f;
			float num6 = Math.Max(val, val2) + 50f;
			for (int i = 0; i < list.Count; i++)
			{
				MPHelper mPHelper = list[i];
				if (mPHelper.TPOLetters.Count == 0 || (!PlotETHBool && mPHelper.TradingHours == PulseTPOEnums.TradingHours.ETH) || mPHelper.StartBar > toIndex)
				{
					continue;
				}
				int num7 = Math.Max(mPHelper.StartBar, fromIndex);
				if (num7 > toIndex)
				{
					continue;
				}
				float num8 = chartControl.GetXByBarIndex(ChartBars, num7);
				float num9;
				if (i + 1 >= list.Count || list[i + 1].StartBar > toIndex)
				{
					num9 = ((!mPHelper.POCCompleted || mPHelper.LastBar <= 0 || mPHelper.LastBar > toIndex) ? num4 : ((float)chartControl.GetXByBarIndex(ChartBars, Math.Min(mPHelper.LastBar, toIndex))));
				}
				else
				{
					int barIndex = Math.Max(list[i + 1].StartBar, fromIndex);
					num9 = chartControl.GetXByBarIndex(ChartBars, barIndex) - 2;
				}
				int num10 = 0;
				foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter2 in mPHelper.TPOLetters)
				{
					num10 = Math.Max(num10, tPOLetter2.Value.Count);
				}
				float num11 = num8 + (float)num10 * num;
				if (num11 > num9)
				{
					num11 = num9;
				}
				foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter3 in mPHelper.TPOLetters)
				{
					try
					{
						if (!TryParsePriceKey(tPOLetter3.Key, out var value))
						{
							continue;
						}
						float num12 = chartScale.GetYByValue(value);
						if (num12 < num5 || num12 > num6)
						{
							continue;
						}
						float num13 = num8;
						for (int j = 0; j < tPOLetter3.Value.Count; j++)
						{
							if (num13 > num9)
							{
								break;
							}
							if (num13 < num3 - num)
							{
								num13 += num;
								continue;
							}
							TPOLetter tPOLetter = tPOLetter3.Value[j];
							RectangleF layoutRect = new RectangleF(num13 - num / 2f, num12 - num2 / 2f, num, num2);
							RenderTarget.DrawText(tPOLetter.Letter, tpoTextFormat, layoutRect, tpoTextBrush);
							num13 += num;
						}
					}
					catch
					{
					}
				}
				if (!DrawDeltaProfile || mPHelper.DeltaByPrice.Count <= 0)
				{
					continue;
				}
				long num14 = 1L;
				foreach (KeyValuePair<double, long> item in mPHelper.DeltaByPrice)
				{
					num14 = Math.Max(num14, Math.Abs(item.Value));
				}
				float num15 = num9;
				float num16 = num9 - num11 - 4f;
				if (num16 < 10f)
				{
					continue;
				}
				float num17 = Math.Min(DeltaProfileWidth, num16);
				float num18 = chartScale.GetYByValue(deltaGroupSize);
				float num19 = chartScale.GetYByValue(0.0);
				float num20 = Math.Max(1f, Math.Abs(num19 - num18) - 1f);
				foreach (KeyValuePair<double, long> item2 in mPHelper.DeltaByPrice)
				{
					double key = item2.Key;
					long value2 = item2.Value;
					float num21 = chartScale.GetYByValue(key);
					if (!(num21 < num5) && !(num21 > num6))
					{
						float num22 = (float)((double)Math.Abs(value2) / (double)num14) * num17;
						if (num22 < 1f)
						{
							num22 = 1f;
						}
						SharpDX.Direct2D1.SolidColorBrush brush = ((value2 >= 0) ? deltaPosBrush : deltaNegBrush);
						RectangleF rect = new RectangleF(num15 - num22, num21 - num20 / 2f, num22, num20);
						RenderTarget.FillRectangle(rect, brush);
					}
				}
			}
		}

		private void ApplyHideBarsStyle()
		{
			if (ChartBars != null && ChartBars.Properties != null && ChartBars.Properties.ChartStyle != null)
			{
				ChartStyle chartStyle = ChartBars.Properties.ChartStyle;
				if (!chartStyleHidden)
				{
					downColor = chartStyle.DownBrush;
					upColor = chartStyle.UpBrush;
					strokeColor = ((chartStyle.Stroke != null) ? chartStyle.Stroke.Brush : null);
					stroke2Color = ((chartStyle.Stroke2 != null) ? chartStyle.Stroke2.Brush : null);
					chartStyleHidden = true;
				}
				chartStyle.DownBrush = Brushes.Transparent;
				chartStyle.UpBrush = Brushes.Transparent;
				if (chartStyle.Stroke != null)
				{
					chartStyle.Stroke.Brush = Brushes.Transparent;
				}
				if (chartStyle.Stroke2 != null)
				{
					chartStyle.Stroke2.Brush = Brushes.Transparent;
				}
			}
		}

		private void RestoreBarsStyle()
		{
			if (chartStyleHidden && ChartBars != null && ChartBars.Properties != null && ChartBars.Properties.ChartStyle != null)
			{
				ChartStyle chartStyle = ChartBars.Properties.ChartStyle;
				if (downColor != null)
				{
					chartStyle.DownBrush = downColor;
				}
				if (upColor != null)
				{
					chartStyle.UpBrush = upColor;
				}
				if (chartStyle.Stroke != null && strokeColor != null)
				{
					chartStyle.Stroke.Brush = strokeColor;
				}
				if (chartStyle.Stroke2 != null && stroke2Color != null)
				{
					chartStyle.Stroke2.Brush = stroke2Color;
				}
				chartStyleHidden = false;
			}
		}

		private static string PriceToKey(double price)
		{
			return price.ToString("0.##########", CultureInfo.InvariantCulture);
		}

		private static bool TryParsePriceKey(string key, out double value)
		{
			if (double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				return true;
			}
			return double.TryParse(key, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
		}

		private static System.Windows.Media.Color GetBrushColor(System.Windows.Media.Brush brush, System.Windows.Media.Color fallback)
		{
			if (!(brush is System.Windows.Media.SolidColorBrush solidColorBrush))
			{
				return fallback;
			}
			return solidColorBrush.Color;
		}

		private static bool Color4Equals(Color4 a, Color4 b)
		{
			if (Math.Abs(a.Red - b.Red) < 0.0001f && Math.Abs(a.Green - b.Green) < 0.0001f && Math.Abs(a.Blue - b.Blue) < 0.0001f)
			{
				return Math.Abs(a.Alpha - b.Alpha) < 0.0001f;
			}
			return false;
		}

		public override void OnRenderTargetChanged()
		{
			if (tpoTextBrush != null)
			{
				tpoTextBrush.Dispose();
				tpoTextBrush = null;
			}
			if (deltaPosBrush != null)
			{
				deltaPosBrush.Dispose();
				deltaPosBrush = null;
			}
			if (deltaNegBrush != null)
			{
				deltaNegBrush.Dispose();
				deltaNegBrush = null;
			}
			base.OnRenderTargetChanged();
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
			DateTime dateTime = Time[0];
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

		private void UpdateCurrentTPOBar(MPBar bar)
		{
			bar.Close = Close[0];
			bar.High = Math.Max(High[0], currentTPOBar.High);
			bar.Low = Math.Min(Low[0], currentTPOBar.Low);
		}

		private void UpdateCurrentTPOSession(MPHelper helper)
		{
			helper.Close = Close[0];
			helper.HighestHigh = Math.Max(High[0], helper.HighestHigh);
			helper.LowestLow = Math.Min(Low[0], helper.LowestLow);
			helper.LastBar = CurrentBar;
		}

		private void UpdateTPOLetters(MPHelper helper)
		{
			string letter = helper.MPBars[helper.MPBars.Count - 1].Letter;
			for (int i = 0; i < numberOfPlots; i++)
			{
				string key = PriceToKey(floor + (double)i * tickSize_x_TicksPerPlot);
				if (!helper.TPOLetters.TryGetValue(key, out var value))
				{
					value = new List<TPOLetter>(50);
					helper.TPOLetters.Add(key, value);
					value.Add(new TPOLetter(helper.MPBars[helper.MPBars.Count - 1].Color, letter));
					helper.TotalTPOCount++;
					refreshTPO = true;
				}
				else if (value[value.Count - 1].Letter != letter)
				{
					value.Add(new TPOLetter(helper.MPBars[helper.MPBars.Count - 1].Color, letter));
					helper.TotalTPOCount++;
					refreshTPO = true;
				}
			}
		}

		private void UpdatePOCStartBar(MPHelper helper)
		{
			int num = ((helper.TradingHours == PulseTPOEnums.TradingHours.RTH) ? (CurrentBar - helper.StartBar) : (CurrentBar - helper.FirstBar));
			if (num >= CurrentBar)
			{
				num = CurrentBar - 1;
			}
			if (num < 0)
			{
				num = 0;
			}
			if (num < 0 || num >= CurrentBar)
			{
				return;
			}
			foreach (KeyValuePair<string, List<TPOLetter>> tPOLetter in helper.TPOLetters)
			{
				if (TryParsePriceKey(tPOLetter.Key, out var value) && value == helper.POCPrice)
				{
					int val = num - tPOLetter.Value.Count * 4 - 1;
					helper.POCStartBar = Math.Max(0, Math.Min(val, CurrentBar - 1));
					break;
				}
			}
		}
	}
}
