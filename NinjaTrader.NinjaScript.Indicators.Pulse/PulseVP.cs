using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
[CategoryOrder("Volume Profile", 1)]
[CategoryOrder("Value Area", 2)]
[CategoryOrder("Lines", 3)]
[CategoryOrder("Delta", 4)]
[CategoryOrder("Composite", 5)]
[CategoryOrder("LVN", 6)]
[CategoryOrder("RTH Session", 7)]
public class PulseVP : Indicator
{
	public class TradingHoursStringConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new StandardValuesCollection(new string[2] { "RTH", "ETH" });
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = ((value == null) ? string.Empty : value.ToString());
			if (string.IsNullOrWhiteSpace(text))
			{
				return "RTH";
			}
			text = text.Trim().ToUpperInvariant();
			if (!(text == "1") && !(text == "ETH"))
			{
				return "RTH";
			}
			return "ETH";
		}
	}

	public class ProfilePeriodStringConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new StandardValuesCollection(new string[3] { "Daily", "Weekly", "Monthly" });
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = ((value == null) ? string.Empty : value.ToString());
			if (string.IsNullOrWhiteSpace(text))
			{
				return "Daily";
			}
			switch (text.Trim().ToUpperInvariant())
			{
			case "1":
			case "WEEKLY":
				return "Weekly";
			case "2":
			case "MONTHLY":
				return "Monthly";
			default:
				return "Daily";
			}
		}
	}

	private class VPSession
	{
		public DateTime SessionDate;

		public int StartBarIndex;

		public int EndBarIndex;

		public bool IsComplete;

		public Dictionary<double, long> VolumeByPrice = new Dictionary<double, long>();

		public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();

		public List<double> LVNPrices = new List<double>();

		public object VolumeLock = new object();

		public double LastTickPrice = double.NaN;

		public int LastTickDirection;

		public long VolumeVersion;

		public long LastCalculatedVolumeVersion = -1L;

		public long LastLvnCalculatedVolumeVersion = -1L;

		public long RenderSnapshotVersion = -1L;

		public Dictionary<double, long> RenderVolumeSnapshot;

		public Dictionary<double, long> RenderDeltaSnapshot;

		public bool ProfileCalculated;

		public bool HasLvnDrawnObjects;

		public int LastLvnDrawStartBar = int.MinValue;

		public int LastLvnDrawEndBar = int.MinValue;

		public int LastLvnDrawCount = -1;

		public int LastLvnDrawOpacity = -1;

		public int LastLvnDrawHeightTicks = -1;

		public long LastLvnDrawVersion = -1L;

		public double POCPrice;

		public long POCVolume;

		public double VAHPrice;

		public double VALPrice;

		public bool POCTouched;

		public bool VAHTouched;

		public bool VALTouched;

		public int POCTouchBar;

		public int VAHTouchBar;

		public int VALTouchBar;
	}

	private List<VPSession> sessions;

	private VPSession currentSession;

	private DateTime lastRTHSessionDate = DateTime.MinValue;

	private readonly object sessionsLock = new object();

	private int tickSeriesIndex = -1;

	private bool isPrimaryOneMinuteChart;

	private bool hasSecondaryDataSeries;

	private bool isDailyOrHigherTF;

	private bool candlesHidden;

	private Brush savedUpBrush;

	private Brush savedDownBrush;

	private Brush savedStrokeBrush;

	private Brush savedStroke2Brush;

	private double tickSize;

	private double volumeGroupSize;

	private const int MaxLvnTagsPerSession = 64;

	private bool lvnMarkersWereVisible;

	private Brush cachedPOCBrush = (Brush)(object)Brushes.Orange;

	private Brush cachedVAHBrush = (Brush)(object)Brushes.DodgerBlue;

	private Brush cachedVALBrush = (Brush)(object)Brushes.DodgerBlue;

	private Brush cachedLVNFillBrush = (Brush)(object)Brushes.Goldenrod;

	private Brush cachedLVNOutlineBrush = (Brush)(object)Brushes.Goldenrod;

	private DashStyleHelper cachedPOCDashStyle;

	private DashStyleHelper cachedVAHDashStyle = (DashStyleHelper)1;

	private DashStyleHelper cachedVALDashStyle = (DashStyleHelper)1;

	private int cachedPOCWidth = 2;

	private int cachedVAHWidth = 1;

	private int cachedVALWidth = 1;

	private bool drawingCacheInitialized;

	private Brush cacheSourcePOCBrush;

	private Brush cacheSourceVAHBrush;

	private Brush cacheSourceVALBrush;

	private Brush cacheSourceLVNFillBrush;

	private Brush cacheSourceLVNOutlineBrush;

	private DashStyleHelper cacheSourcePOCDashStyle;

	private DashStyleHelper cacheSourceVAHDashStyle = (DashStyleHelper)1;

	private DashStyleHelper cacheSourceVALDashStyle = (DashStyleHelper)1;

	private double cacheSourcePOCWidth = double.NaN;

	private double cacheSourceVAHWidth = double.NaN;

	private double cacheSourceVALWidth = double.NaN;

	private VPSession compositeSessionCache;

	private long compositeSessionCacheKey = long.MinValue;

	private SolidColorBrush dxProfileBrush;

	private SolidColorBrush dxValueAreaBrush;

	private SolidColorBrush dxPOCBrush;

	private SolidColorBrush dxDeltaPositiveBrush;

	private SolidColorBrush dxDeltaNegativeBrush;

	private TextFormat dxTextFormat;

	private TimeSpan rthStartTime = new TimeSpan(9, 30, 0);

	private TimeSpan rthEndTime = new TimeSpan(16, 15, 0);

	[NinjaScriptProperty]
	[Range(50, 500)]
	[Display(Name = "Profile Width (px)", Description = "Width in pixels for volume profile bars", Order = 1, GroupName = "Volume Profile")]
	public int VolumeProfileWidth { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Tick Compression", Description = "Group price levels by N ticks", Order = 2, GroupName = "Volume Profile")]
	public int VolumeTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(0, 100000)]
	[Display(Name = "Volume Threshold", Description = "Minimum volume to display a bar", Order = 3, GroupName = "Volume Profile")]
	public int VolumeThreshold { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Profile Bars", Description = "Render volume profile histogram bars", Order = 4, GroupName = "Volume Profile")]
	public bool ShowProfileBars { get; set; }

	[NinjaScriptProperty]
	[Range(1, 60)]
	[Display(Name = "Days to Show", Description = "Number of past sessions to display", Order = 5, GroupName = "Volume Profile")]
	public int DaysToShow { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Hide Candles", Description = "Hide chart candles when indicator is loaded", Order = 6, GroupName = "Volume Profile")]
	public bool HideCandles { get; set; }

	[NinjaScriptProperty]
	[Range(50, 95)]
	[Display(Name = "Value Area %", Description = "Percentage of volume for Value Area", Order = 1, GroupName = "Value Area")]
	public int ValueAreaPercentage { get; set; }

	[XmlIgnore]
	[Display(Name = "Profile Color", Description = "Color for bars outside Value Area", Order = 2, GroupName = "Value Area")]
	public Brush ProfileColor { get; set; }

	[Browsable(false)]
	public string ProfileColorSerializable
	{
		get
		{
			return Serialize.BrushToString(ProfileColor);
		}
		set
		{
			ProfileColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Profile Opacity", Description = "Opacity for bars outside Value Area", Order = 3, GroupName = "Value Area")]
	public int ProfileOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Value Area Color", Description = "Color for bars inside Value Area", Order = 4, GroupName = "Value Area")]
	public Brush ValueAreaColor { get; set; }

	[Browsable(false)]
	public string ValueAreaColorSerializable
	{
		get
		{
			return Serialize.BrushToString(ValueAreaColor);
		}
		set
		{
			ValueAreaColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Value Area Opacity", Description = "Opacity for Value Area bars", Order = 5, GroupName = "Value Area")]
	public int ValueAreaOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show POC Line", Description = "Draw POC line for each session", Order = 1, GroupName = "Lines")]
	public bool ShowPOCLine { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show VA Lines", Description = "Draw VAH/VAL lines for each session", Order = 2, GroupName = "Lines")]
	public bool ShowVALines { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Extend Until Touched", Description = "Extend POC/VAH/VAL lines forward until price touches them", Order = 3, GroupName = "Lines")]
	public bool ExtendLines { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "POC Stroke", Order = 4, GroupName = "Lines")]
	public Stroke POCStroke { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VAH Stroke", Order = 5, GroupName = "Lines")]
	public Stroke VAHStroke { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VAL Stroke", Order = 6, GroupName = "Lines")]
	public Stroke VALStroke { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta Bars", Description = "Render session horizontal delta bars to the left of the profile", Order = 1, GroupName = "Delta")]
	public bool ShowDeltaBars { get; set; }

	[NinjaScriptProperty]
	[Range(20, 500)]
	[Display(Name = "Delta Width (px)", Description = "Maximum width in pixels for horizontal delta bars", Order = 2, GroupName = "Delta")]
	public int DeltaProfileWidth { get; set; }

	[NinjaScriptProperty]
	[Range(0, 100000)]
	[Display(Name = "Delta Threshold", Description = "Minimum absolute delta to render a bar", Order = 3, GroupName = "Delta")]
	public int DeltaThreshold { get; set; }

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Delta Opacity", Description = "Opacity for delta bars", Order = 4, GroupName = "Delta")]
	public int DeltaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Delta Positive Color", Description = "Color for positive delta bars", Order = 5, GroupName = "Delta")]
	public Brush DeltaPositiveColor { get; set; }

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

	[XmlIgnore]
	[Display(Name = "Delta Negative Color", Description = "Color for negative delta bars", Order = 6, GroupName = "Delta")]
	public Brush DeltaNegativeColor { get; set; }

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

	[NinjaScriptProperty]
	[Display(Name = "Show Composite Profile", Description = "Merge and render multiple sessions as one composite profile", Order = 1, GroupName = "Composite")]
	public bool ShowCompositeProfile { get; set; }

	[NinjaScriptProperty]
	[Range(2, 20)]
	[Display(Name = "Composite Sessions", Description = "Number of sessions to include in the composite", Order = 2, GroupName = "Composite")]
	public int CompositeSessionsCount { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Include Current Session", Description = "Include the active session in the composite", Order = 3, GroupName = "Composite")]
	public bool CompositeIncludeCurrentSession { get; set; }

	[NinjaScriptProperty]
	[Range(80, 800)]
	[Display(Name = "Composite Width (px)", Description = "Maximum width in pixels for the composite profile", Order = 4, GroupName = "Composite")]
	public int CompositeProfileWidth { get; set; }

	[NinjaScriptProperty]
	[Range(10, 100)]
	[Display(Name = "Composite Opacity", Description = "Opacity scale applied to composite bars", Order = 5, GroupName = "Composite")]
	public int CompositeOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Composite Delta", Description = "Render delta bars for the composite profile", Order = 6, GroupName = "Composite")]
	public bool CompositeShowDelta { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show LVN Markers", Description = "Automatically mark low-volume nodes (LVN) with rectangles", Order = 1, GroupName = "LVN")]
	public bool ShowLVNMarkers { get; set; }

	[NinjaScriptProperty]
	[Range(5, 100)]
	[Display(Name = "LVN Max % of POC", Description = "Maximum LVN volume as % of POC volume", Order = 2, GroupName = "LVN")]
	public int LVNMaxPercentOfPOC { get; set; }

	[NinjaScriptProperty]
	[Range(0, 80)]
	[Display(Name = "LVN Min Prominence %", Description = "How much lower than neighboring levels an LVN must be", Order = 3, GroupName = "LVN")]
	public int LVNMinProminencePercent { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "LVN Ignore Inside VA", Description = "Ignore LVN candidates located inside the Value Area", Order = 4, GroupName = "LVN")]
	public bool LVNIgnoreInsideValueArea { get; set; }

	[NinjaScriptProperty]
	[Range(1, 40)]
	[Display(Name = "LVN Min Separation (ticks)", Description = "Minimum separation between LVN levels", Order = 5, GroupName = "LVN")]
	public int LVNMinSeparationTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, 20)]
	[Display(Name = "LVN Max Markers/Session", Description = "Maximum number of LVN markers per session", Order = 6, GroupName = "LVN")]
	public int LVNMaxPerSession { get; set; }

	[NinjaScriptProperty]
	[Range(1, 20)]
	[Display(Name = "LVN Height (ticks)", Description = "Rectangle height in ticks for each LVN marker", Order = 7, GroupName = "LVN")]
	public int LVNHeightTicks { get; set; }

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "LVN Opacity", Description = "Opacity for LVN rectangles", Order = 8, GroupName = "LVN")]
	public int LVNOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "LVN Fill Color", Description = "Fill color for LVN rectangles", Order = 9, GroupName = "LVN")]
	public Brush LVNColor { get; set; }

	[Browsable(false)]
	public string LVNColorSerializable
	{
		get
		{
			return Serialize.BrushToString(LVNColor);
		}
		set
		{
			LVNColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "LVN Border Color", Description = "Border color for LVN rectangles", Order = 10, GroupName = "LVN")]
	public Brush LVNOutlineColor { get; set; }

	[Browsable(false)]
	public string LVNOutlineColorSerializable
	{
		get
		{
			return Serialize.BrushToString(LVNOutlineColor);
		}
		set
		{
			LVNOutlineColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Browsable(false)]
	[Range(0, 1)]
	public int TradingHours { get; set; }

	[NinjaScriptProperty]
	[Browsable(false)]
	[Range(0, 2)]
	public int ProfilePeriod { get; set; }

	[TypeConverter(typeof(TradingHoursStringConverter))]
	[Display(Name = "Trading Hours", Description = "Selecciona RTH o ETH", Order = 1, GroupName = "RTH Session")]
	public string TradingHoursSelection
	{
		get
		{
			return GetTradingHoursLabel();
		}
		set
		{
			SetTradingHoursFromLabel(value);
		}
	}

	[TypeConverter(typeof(ProfilePeriodStringConverter))]
	[Display(Name = "Profile Period", Description = "Selecciona Daily, Weekly o Monthly", Order = 2, GroupName = "RTH Session")]
	public string ProfilePeriodSelection
	{
		get
		{
			return GetProfilePeriodLabel();
		}
		set
		{
			SetProfilePeriodFromLabel(value);
		}
	}

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
	[Display(Name = "RTH Start Time", Description = "Start time of RTH session (exchange time)", Order = 3, GroupName = "RTH Session")]
	public DateTime RTHStartTime
	{
		get
		{
			return DateTime.Today.Add(rthStartTime);
		}
		set
		{
			rthStartTime = value.TimeOfDay;
		}
	}

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
	[Display(Name = "RTH End Time", Description = "End time of RTH session (exchange time)", Order = 4, GroupName = "RTH Session")]
	public DateTime RTHEndTime
	{
		get
		{
			return DateTime.Today.Add(rthEndTime);
		}
		set
		{
			rthEndTime = value.TimeOfDay;
		}
	}

	public PulseVP()
	{
	}

	protected override void OnStateChange()
	{
		if (State == State.SetDefaults)
		{
			Description = "Pulse VP - Daily Volume Profile with extending VAH/VAL/POC lines until touched";
			Name = "PulseVP";
			Calculate = Calculate.OnBarClose;
			IsOverlay = true;
			DisplayInDataBox = false;
			DrawOnPricePanel = true;
			PaintPriceMarkers = false;
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = true;
			VolumeProfileWidth = 150;
			VolumeTickCompression = 1;
			ValueAreaPercentage = 70;
			VolumeThreshold = 0;
			ProfileColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)30, (byte)144, byte.MaxValue));
			ValueAreaColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)70, (byte)130, (byte)180));
			ProfileOpacity = 60;
			ValueAreaOpacity = 40;
			POCStroke = new Stroke((Brush)(object)Brushes.Orange, (DashStyleHelper)0, 2f);
			VAHStroke = new Stroke((Brush)(object)Brushes.DodgerBlue, (DashStyleHelper)1, 1f);
			VALStroke = new Stroke((Brush)(object)Brushes.DodgerBlue, (DashStyleHelper)1, 1f);
			ExtendLines = true;
			ShowProfileBars = true;
			ShowPOCLine = true;
			ShowVALines = true;
			DaysToShow = 10;
			HideCandles = true;
			ShowDeltaBars = true;
			DeltaProfileWidth = 120;
			DeltaThreshold = 0;
			DeltaOpacity = 55;
			DeltaPositiveColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)34, (byte)197, (byte)94));
			DeltaNegativeColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)239, (byte)68, (byte)68));
			ShowCompositeProfile = false;
			CompositeSessionsCount = 3;
			CompositeIncludeCurrentSession = true;
			CompositeProfileWidth = 220;
			CompositeOpacity = 55;
			CompositeShowDelta = true;
			TradingHours = 0;
			ProfilePeriod = 0;
			ShowLVNMarkers = true;
			LVNMaxPercentOfPOC = 35;
			LVNMinProminencePercent = 12;
			LVNIgnoreInsideValueArea = false;
			LVNMinSeparationTicks = 4;
			LVNMaxPerSession = 6;
			LVNHeightTicks = 1;
			LVNOpacity = 35;
			LVNColor = (Brush)new SolidColorBrush(Color.FromRgb(byte.MaxValue, (byte)191, (byte)0));
			LVNOutlineColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)161, (byte)98, (byte)7));
		}
		else if (State == State.Configure)
		{
			bool num = (int)Bars.BarsPeriod.BarsPeriodType == 4 && Bars.BarsPeriod.Value == 1;
			bool flag = (int)Bars.BarsPeriod.BarsPeriodType == 1;
			AddDataSeries((BarsPeriodType)0, 1);
			tickSeriesIndex = 1;
			if (!num && !flag)
			{
				AddDataSeries((BarsPeriodType)4, 1);
			}
		}
		else if (State == State.DataLoaded)
		{
			TradingHours = ((TradingHours == 1) ? 1 : 0);
			ProfilePeriod = Math.Max(0, Math.Min(2, ProfilePeriod));
			tickSize = Instrument.MasterInstrument.TickSize;
			string text = Instrument.MasterInstrument.Name.ToUpper();
			if (VolumeTickCompression == 1)
			{
				if (text.Contains("NQ") || text.Contains("MNQ"))
				{
					VolumeTickCompression = 3;
				}
				else if (text.Contains("CL") || text.Contains("MCL"))
				{
					VolumeTickCompression = 2;
				}
			}
			volumeGroupSize = tickSize * (double)VolumeTickCompression;
			isPrimaryOneMinuteChart = (int)Bars.BarsPeriod.BarsPeriodType == 4 && Bars.BarsPeriod.Value == 1;
			hasSecondaryDataSeries = BarsArray.Length > 2;
			isDailyOrHigherTF = (int)Bars.BarsPeriod.BarsPeriodType == 5 || (int)Bars.BarsPeriod.BarsPeriodType == 6 || (int)Bars.BarsPeriod.BarsPeriodType == 7 || ((int)Bars.BarsPeriod.BarsPeriodType == 4 && Bars.BarsPeriod.Value >= 1440);
			sessions = new List<VPSession>(256);
			lvnMarkersWereVisible = ShowLVNMarkers;
			compositeSessionCache = null;
			compositeSessionCacheKey = long.MinValue;
			if (!ShowProfileBars && !ShowCompositeProfile)
			{
				ShowProfileBars = true;
				if (HideCandles)
				{
					HideCandles = false;
				}
			}
		}
		else if (State == State.Terminated)
		{
			RestoreCandles();
			DisposeDx();
		}
	}

	protected override void OnBarUpdate()
	{
		if (BarsInProgress == tickSeriesIndex)
		{
			ProcessTickVolume();
		}
		else
		{
			if (BarsInProgress != 0 || CurrentBar < 1)
			{
				return;
			}
			DateTime dateTime = Time[0];
			DateTime sessionKey = GetSessionKey(dateTime);
			DateTime displayCutoffDate = GetDisplayCutoffDate(dateTime);
			bool flag = IsInConfiguredTimeWindow(dateTime);
			bool flag2 = isDailyOrHigherTF || flag;
			lock (sessionsLock)
			{
				for (int i = 0; i < sessions.Count; i++)
				{
					VPSession vPSession = sessions[i];
					if (vPSession.StartBarIndex < 0 && vPSession.SessionDate == sessionKey && flag2)
					{
						vPSession.StartBarIndex = CurrentBar;
						vPSession.EndBarIndex = CurrentBar;
					}
					if (vPSession.SessionDate == sessionKey && flag2)
					{
						vPSession.EndBarIndex = CurrentBar;
					}
					if (vPSession.StartBarIndex >= 0 && !vPSession.IsComplete)
					{
						bool num = vPSession.SessionDate < sessionKey;
						bool flag3 = ShouldCloseSessionOnWindowEnd() && vPSession.SessionDate == sessionKey && !flag;
						if (num || flag3)
						{
							vPSession.IsComplete = true;
							CalculatePOCAndValueArea(vPSession);
						}
					}
				}
				VPSession vPSession2 = null;
				for (int num2 = sessions.Count - 1; num2 >= 0; num2--)
				{
					if (sessions[num2].SessionDate == sessionKey && sessions[num2].StartBarIndex >= 0)
					{
						vPSession2 = sessions[num2];
						break;
					}
				}
				if (vPSession2 != null && !vPSession2.IsComplete)
				{
					CalculatePOCAndValueArea(vPSession2);
				}
				if (vPSession2 != null && vPSession2.SessionDate >= displayCutoffDate && ShowPOCLine)
				{
					DrawSessionPOC(vPSession2);
				}
				if (vPSession2 != null && vPSession2.SessionDate >= displayCutoffDate && ShowVALines)
				{
					DrawSessionVA(vPSession2);
				}
				if (vPSession2 != null && vPSession2.SessionDate >= displayCutoffDate && ShowLVNMarkers)
				{
					DrawSessionLVN(vPSession2);
				}
				for (int j = 0; j < sessions.Count; j++)
				{
					VPSession vPSession3 = sessions[j];
					if (vPSession3.IsComplete && vPSession3.StartBarIndex >= 0 && vPSession3 != vPSession2 && !(vPSession3.SessionDate < displayCutoffDate))
					{
						if (ShowPOCLine)
						{
							DrawSessionPOC(vPSession3);
						}
						if (ShowVALines)
						{
							DrawSessionVA(vPSession3);
						}
						if (ShowLVNMarkers)
						{
							DrawSessionLVN(vPSession3);
						}
					}
				}
				if (!ShowLVNMarkers)
				{
					if (lvnMarkersWereVisible)
					{
						for (int k = 0; k < sessions.Count; k++)
						{
							if (sessions[k].HasLvnDrawnObjects)
							{
								RemoveSessionLvnObjects(sessions[k].SessionDate);
								sessions[k].HasLvnDrawnObjects = false;
							}
						}
						lvnMarkersWereVisible = false;
					}
				}
				else
				{
					lvnMarkersWereVisible = true;
				}
				if (ExtendLines)
				{
					CheckAndExtendPreviousLines(displayCutoffDate);
				}
				DateTime dateTime2 = AddProfilePeriods(sessionKey, -(Math.Max(1, DaysToShow) + 30));
				while (sessions.Count > 0 && sessions[0].SessionDate < dateTime2)
				{
					string text = sessions[0].SessionDate.ToString("yyyyMMdd");
					RemoveDrawObject("POC_" + text);
					RemoveDrawObject("VAH_" + text);
					RemoveDrawObject("VAL_" + text);
					RemoveDrawObject("xPOC_" + text);
					RemoveDrawObject("xVAH_" + text);
					RemoveDrawObject("xVAL_" + text);
					RemoveSessionLvnObjects(sessions[0].SessionDate);
					sessions.RemoveAt(0);
				}
			}
		}
	}

	private void ProcessTickVolume()
	{
		if (tickSeriesIndex < 0 || CurrentBars == null || CurrentBars.Length <= tickSeriesIndex)
		{
			return;
		}
		int num = CurrentBars[tickSeriesIndex];
		if (num < 0)
		{
			return;
		}
		DateTime time = BarsArray[tickSeriesIndex].GetTime(num);
		if (!IsInConfiguredTimeWindow(time))
		{
			return;
		}
		DateTime sessionKey = GetSessionKey(time);
		VPSession vPSession = null;
		lock (sessionsLock)
		{
			for (int num2 = sessions.Count - 1; num2 >= 0; num2--)
			{
				if (sessions[num2].SessionDate == sessionKey)
				{
					vPSession = sessions[num2];
					break;
				}
			}
			if (vPSession == null)
			{
				vPSession = new VPSession();
				vPSession.SessionDate = sessionKey;
				vPSession.StartBarIndex = -1;
				vPSession.EndBarIndex = -1;
				sessions.Add(vPSession);
			}
		}
		double close = BarsArray[tickSeriesIndex].GetClose(num);
		long volume = BarsArray[tickSeriesIndex].GetVolume(num);
		if (volume <= 0 || double.IsNaN(close))
		{
			return;
		}
		double roundedPrice = GetRoundedPrice(close, volumeGroupSize);
		lock (vPSession.VolumeLock)
		{
			int num3 = 0;
			if (!double.IsNaN(vPSession.LastTickPrice))
			{
				num3 = ((close > vPSession.LastTickPrice) ? 1 : ((!(close < vPSession.LastTickPrice)) ? vPSession.LastTickDirection : (-1)));
			}
			vPSession.LastTickPrice = close;
			if (num3 != 0)
			{
				vPSession.LastTickDirection = num3;
			}
			if (vPSession.VolumeByPrice.ContainsKey(roundedPrice))
			{
				vPSession.VolumeByPrice[roundedPrice] += volume;
			}
			else
			{
				vPSession.VolumeByPrice[roundedPrice] = volume;
			}
			if (num3 != 0)
			{
				long num4 = ((num3 > 0) ? volume : (-volume));
				if (vPSession.DeltaByPrice.ContainsKey(roundedPrice))
				{
					vPSession.DeltaByPrice[roundedPrice] += num4;
				}
				else
				{
					vPSession.DeltaByPrice[roundedPrice] = num4;
				}
			}
			vPSession.VolumeVersion++;
		}
	}

	private void CalculatePOCAndValueArea(VPSession session, bool forceRecalculation = false)
	{
		lock (session.VolumeLock)
		{
			if (!forceRecalculation && session.ProfileCalculated && session.LastCalculatedVolumeVersion == session.VolumeVersion)
			{
				return;
			}
			if (session.VolumeByPrice.Count == 0)
			{
				session.ProfileCalculated = false;
				session.POCPrice = 0.0;
				session.POCVolume = 0L;
				session.VAHPrice = 0.0;
				session.VALPrice = 0.0;
				session.LastCalculatedVolumeVersion = session.VolumeVersion;
				return;
			}
			List<double> list = new List<double>(session.VolumeByPrice.Keys);
			list.Sort();
			if (list.Count == 0)
			{
				return;
			}
			double num = 0.0;
			long num2 = 0L;
			foreach (KeyValuePair<double, long> item in session.VolumeByPrice)
			{
				if (item.Value > num2)
				{
					num2 = item.Value;
					num = item.Key;
				}
			}
			session.POCPrice = num;
			session.POCVolume = num2;
			int num3 = list.IndexOf(num);
			if (num3 < 0)
			{
				return;
			}
			long num4 = 0L;
			foreach (KeyValuePair<double, long> item2 in session.VolumeByPrice)
			{
				num4 += item2.Value;
			}
			long num5 = (long)((double)num4 * ((double)ValueAreaPercentage / 100.0));
			long num6 = num2;
			int num7 = num3;
			int num8 = num3;
			while (num6 < num5)
			{
				bool flag = num7 < list.Count - 1;
				bool flag2 = num8 > 0;
				if (!flag && !flag2)
				{
					break;
				}
				long num9 = 0L;
				for (int i = num7 + 1; i <= Math.Min(num7 + 2, list.Count - 1); i++)
				{
					num9 += session.VolumeByPrice[list[i]];
				}
				long num10 = 0L;
				for (int num11 = num8 - 1; num11 >= Math.Max(num8 - 2, 0); num11--)
				{
					num10 += session.VolumeByPrice[list[num11]];
				}
				if (!flag2)
				{
					for (int j = num7 + 1; j <= Math.Min(num7 + 2, list.Count - 1); j++)
					{
						num6 += session.VolumeByPrice[list[j]];
					}
					num7 = Math.Min(num7 + 2, list.Count - 1);
					continue;
				}
				if (!flag)
				{
					for (int num12 = num8 - 1; num12 >= Math.Max(num8 - 2, 0); num12--)
					{
						num6 += session.VolumeByPrice[list[num12]];
					}
					num8 = Math.Max(num8 - 2, 0);
					continue;
				}
				if (num9 > num10)
				{
					for (int k = num7 + 1; k <= Math.Min(num7 + 2, list.Count - 1); k++)
					{
						num6 += session.VolumeByPrice[list[k]];
					}
					num7 = Math.Min(num7 + 2, list.Count - 1);
					continue;
				}
				if (num10 > num9)
				{
					for (int num13 = num8 - 1; num13 >= Math.Max(num8 - 2, 0); num13--)
					{
						num6 += session.VolumeByPrice[list[num13]];
					}
					num8 = Math.Max(num8 - 2, 0);
					continue;
				}
				for (int l = num7 + 1; l <= Math.Min(num7 + 2, list.Count - 1); l++)
				{
					num6 += session.VolumeByPrice[list[l]];
				}
				num7 = Math.Min(num7 + 2, list.Count - 1);
				if (num6 < num5)
				{
					for (int num14 = num8 - 1; num14 >= Math.Max(num8 - 2, 0); num14--)
					{
						num6 += session.VolumeByPrice[list[num14]];
					}
					num8 = Math.Max(num8 - 2, 0);
				}
			}
			session.VAHPrice = list[num7];
			session.VALPrice = list[num8];
			session.ProfileCalculated = true;
			session.LastCalculatedVolumeVersion = session.VolumeVersion;
			if (ShowLVNMarkers)
			{
				UpdateSessionLVNLevels(session, list, num2);
				session.LastLvnCalculatedVolumeVersion = session.VolumeVersion;
			}
		}
	}

	private void UpdateSessionLVNLevels(VPSession session, List<double> sortedPrices, long pocVolume)
	{
		session.LVNPrices.Clear();
		if (sortedPrices == null || sortedPrices.Count < 3 || pocVolume <= 0)
		{
			return;
		}
		double num = (double)pocVolume * ((double)LVNMaxPercentOfPOC / 100.0);
		double num2 = (double)Math.Max(1, LVNMinSeparationTicks) * tickSize;
		int num3 = Math.Max(1, Math.Min(LVNMaxPerSession, 64));
		List<KeyValuePair<double, long>> list = new List<KeyValuePair<double, long>>(sortedPrices.Count / 2);
		for (int i = 1; i < sortedPrices.Count - 1; i++)
		{
			double num4 = sortedPrices[i];
			long num5 = session.VolumeByPrice[num4];
			if (num5 <= 0 || (double)num5 > num || (LVNIgnoreInsideValueArea && num4 >= session.VALPrice && num4 <= session.VAHPrice))
			{
				continue;
			}
			long num6 = session.VolumeByPrice[sortedPrices[i - 1]];
			long num7 = session.VolumeByPrice[sortedPrices[i + 1]];
			if (LVNMinProminencePercent > 0)
			{
				double num8 = (double)(num6 + num7) * 0.5;
				if (num8 > 0.0)
				{
					double num9 = num8 * (1.0 - (double)LVNMinProminencePercent / 100.0);
					if ((double)num5 > num9)
					{
						continue;
					}
				}
			}
			if (num5 <= num6 && num5 <= num7 && (num5 != num6 || num5 != num7))
			{
				list.Add(new KeyValuePair<double, long>(num4, num5));
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		list.Sort(delegate(KeyValuePair<double, long> a, KeyValuePair<double, long> b)
		{
			int num12 = a.Value.CompareTo(b.Value);
			return (num12 == 0) ? a.Key.CompareTo(b.Key) : num12;
		});
		for (int num10 = 0; num10 < list.Count; num10++)
		{
			double key = list[num10].Key;
			bool flag = false;
			for (int num11 = 0; num11 < session.LVNPrices.Count; num11++)
			{
				if (Math.Abs(session.LVNPrices[num11] - key) < num2)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				session.LVNPrices.Add(key);
				if (session.LVNPrices.Count >= num3)
				{
					break;
				}
			}
		}
		session.LVNPrices.Sort();
	}

	private void EnsureSessionLVNUpToDate(VPSession session)
	{
		if (session == null)
		{
			return;
		}
		lock (session.VolumeLock)
		{
			if (session.VolumeByPrice.Count == 0)
			{
				session.LVNPrices.Clear();
				session.LastLvnCalculatedVolumeVersion = session.VolumeVersion;
			}
			else
			{
				if (session.LastLvnCalculatedVolumeVersion == session.VolumeVersion)
				{
					return;
				}
				List<double> list = new List<double>(session.VolumeByPrice.Keys);
				list.Sort();
				if (list.Count == 0)
				{
					session.LVNPrices.Clear();
					session.LastLvnCalculatedVolumeVersion = session.VolumeVersion;
					return;
				}
				long num = session.POCVolume;
				if (num <= 0)
				{
					foreach (KeyValuePair<double, long> item in session.VolumeByPrice)
					{
						if (item.Value > num)
						{
							num = item.Value;
						}
					}
				}
				UpdateSessionLVNLevels(session, list, num);
				session.LastLvnCalculatedVolumeVersion = session.VolumeVersion;
			}
		}
	}

	private void DrawSessionPOC(VPSession session)
	{
		if (session.POCPrice != 0.0 && session.EndBarIndex >= 0 && session.StartBarIndex >= 0)
		{
			string text = session.SessionDate.ToString("yyyyMMdd");
			int startBarIndex = session.StartBarIndex;
			int num = GetReferenceLineEndBar(session);
			if (num < startBarIndex)
			{
				num = startBarIndex;
			}
			int num2 = Math.Max(0, CurrentBar - startBarIndex);
			int endBarsAgo = Math.Max(0, CurrentBar - num);
			if (num2 >= 0 && num2 <= CurrentBar)
			{
				Brush brush = (Brush)(((object)cachedPOCBrush) ?? ((object)Brushes.Orange));
				Draw.Line(this, "POC_" + text, IsAutoScale, num2, session.POCPrice, endBarsAgo, session.POCPrice, brush, cachedPOCDashStyle, cachedPOCWidth);
			}
		}
	}

	private void DrawSessionVA(VPSession session)
	{
		if (session.VAHPrice != 0.0 && session.VALPrice != 0.0 && session.EndBarIndex >= 0 && session.StartBarIndex >= 0)
		{
			string text = session.SessionDate.ToString("yyyyMMdd");
			int startBarIndex = session.StartBarIndex;
			int num = GetReferenceLineEndBar(session);
			if (num < startBarIndex)
			{
				num = startBarIndex;
			}
			int num2 = Math.Max(0, CurrentBar - startBarIndex);
			int endBarsAgo = Math.Max(0, CurrentBar - num);
			if (num2 >= 0 && num2 <= CurrentBar)
			{
				Brush brush = (Brush)(((object)cachedVAHBrush) ?? ((object)Brushes.DodgerBlue));
				Brush brush2 = (Brush)(((object)cachedVALBrush) ?? ((object)Brushes.DodgerBlue));
				Draw.Line(this, "VAH_" + text, IsAutoScale, num2, session.VAHPrice, endBarsAgo, session.VAHPrice, brush, cachedVAHDashStyle, cachedVAHWidth);
				Draw.Line(this, "VAL_" + text, IsAutoScale, num2, session.VALPrice, endBarsAgo, session.VALPrice, brush2, cachedVALDashStyle, cachedVALWidth);
			}
		}
	}

	private void DrawSessionLVN(VPSession session)
	{
		if (session == null || session.StartBarIndex < 0 || session.EndBarIndex < 0)
		{
			return;
		}
		EnsureSessionLVNUpToDate(session);
		long lastLvnCalculatedVolumeVersion;
		List<double> list;
		lock (session.VolumeLock)
		{
			lastLvnCalculatedVolumeVersion = session.LastLvnCalculatedVolumeVersion;
			if (session.LVNPrices == null || session.LVNPrices.Count == 0)
			{
				if (session.HasLvnDrawnObjects)
				{
					RemoveSessionLvnObjects(session.SessionDate);
					session.HasLvnDrawnObjects = false;
				}
				session.LastLvnDrawCount = 0;
				session.LastLvnDrawVersion = lastLvnCalculatedVolumeVersion;
				return;
			}
			list = new List<double>(session.LVNPrices);
		}
		string text = session.SessionDate.ToString("yyyyMMdd");
		int startBarIndex = session.StartBarIndex;
		int num = GetReferenceLineEndBar(session);
		if (num < startBarIndex)
		{
			num = startBarIndex;
		}
		int num2 = Math.Max(0, CurrentBar - startBarIndex);
		int endBarsAgo = Math.Max(0, CurrentBar - num);
		if (num2 < 0 || num2 > CurrentBar)
		{
			return;
		}
		double num3 = Math.Max(tickSize * (double)Math.Max(1, LVNHeightTicks) * 0.5, tickSize * 0.5);
		int num4 = Math.Min(list.Count, Math.Max(1, Math.Min(LVNMaxPerSession, 64)));
		if (num4 <= 0)
		{
			if (session.HasLvnDrawnObjects)
			{
				RemoveSessionLvnObjects(session.SessionDate);
				session.HasLvnDrawnObjects = false;
			}
			session.LastLvnDrawCount = 0;
			session.LastLvnDrawVersion = lastLvnCalculatedVolumeVersion;
		}
		else
		{
			if (session.LastLvnDrawStartBar == startBarIndex && session.LastLvnDrawEndBar == num && session.LastLvnDrawCount == num4 && session.LastLvnDrawOpacity == LVNOpacity && session.LastLvnDrawHeightTicks == LVNHeightTicks && session.LastLvnDrawVersion == lastLvnCalculatedVolumeVersion)
			{
				return;
			}
			Brush val = (Brush)(((object)cachedLVNFillBrush) ?? ((object)Brushes.Goldenrod));
			Brush brush = cachedLVNOutlineBrush ?? val;
			for (int i = 0; i < num4; i++)
			{
				double num5 = list[i];
				string tag = "LVN_" + text + "_" + i.ToString(CultureInfo.InvariantCulture);
				Draw.Rectangle(this, tag, IsAutoScale, num2, num5 + num3, endBarsAgo, num5 - num3, brush, val, LVNOpacity);
			}
			if (session.HasLvnDrawnObjects)
			{
				for (int j = num4; j < 64; j++)
				{
					RemoveDrawObject("LVN_" + text + "_" + j.ToString(CultureInfo.InvariantCulture));
				}
			}
			session.HasLvnDrawnObjects = true;
			session.LastLvnDrawStartBar = startBarIndex;
			session.LastLvnDrawEndBar = num;
			session.LastLvnDrawCount = num4;
			session.LastLvnDrawOpacity = LVNOpacity;
			session.LastLvnDrawHeightTicks = LVNHeightTicks;
			session.LastLvnDrawVersion = lastLvnCalculatedVolumeVersion;
		}
	}

	private void RemoveSessionLvnObjects(DateTime sessionDate)
	{
		string text = sessionDate.ToString("yyyyMMdd");
		for (int i = 0; i < 64; i++)
		{
			RemoveDrawObject("LVN_" + text + "_" + i.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void CheckAndExtendPreviousLines(DateTime displayCutoffDate)
	{
		Brush brush = (Brush)(((object)cachedPOCBrush) ?? ((object)Brushes.Orange));
		Brush brush2 = (Brush)(((object)cachedVAHBrush) ?? ((object)Brushes.DodgerBlue));
		Brush brush3 = (Brush)(((object)cachedVALBrush) ?? ((object)Brushes.DodgerBlue));
		for (int i = 0; i < sessions.Count; i++)
		{
			VPSession vPSession = sessions[i];
			if (!vPSession.IsComplete || vPSession.SessionDate < displayCutoffDate || (vPSession.POCTouched && vPSession.VAHTouched && vPSession.VALTouched))
			{
				continue;
			}
			string text = vPSession.SessionDate.ToString("yyyyMMdd");
			if (vPSession.EndBarIndex < 0)
			{
				continue;
			}
			int referenceLineEndBar = GetReferenceLineEndBar(vPSession);
			int num = Math.Max(0, CurrentBar - referenceLineEndBar);
			if (num < 0 || num > CurrentBar)
			{
				continue;
			}
			if (!vPSession.POCTouched && vPSession.POCPrice > 0.0)
			{
				if (vPSession.POCPrice >= Low[0] && vPSession.POCPrice <= High[0])
				{
					vPSession.POCTouched = true;
					vPSession.POCTouchBar = CurrentBar;
					RemoveDrawObject("xPOC_" + text);
				}
				else
				{
					Draw.Line(this, "xPOC_" + text, IsAutoScale, num, vPSession.POCPrice, 0, vPSession.POCPrice, brush, (DashStyleHelper)4, cachedPOCWidth);
				}
			}
			if (!vPSession.VAHTouched && vPSession.VAHPrice > 0.0)
			{
				if (vPSession.VAHPrice >= Low[0] && vPSession.VAHPrice <= High[0])
				{
					vPSession.VAHTouched = true;
					vPSession.VAHTouchBar = CurrentBar;
					RemoveDrawObject("xVAH_" + text);
				}
				else
				{
					Draw.Line(this, "xVAH_" + text, IsAutoScale, num, vPSession.VAHPrice, 0, vPSession.VAHPrice, brush2, (DashStyleHelper)4, cachedVAHWidth);
				}
			}
			if (!vPSession.VALTouched && vPSession.VALPrice > 0.0)
			{
				if (vPSession.VALPrice >= Low[0] && vPSession.VALPrice <= High[0])
				{
					vPSession.VALTouched = true;
					vPSession.VALTouchBar = CurrentBar;
					RemoveDrawObject("xVAL_" + text);
				}
				else
				{
					Draw.Line(this, "xVAL_" + text, IsAutoScale, num, vPSession.VALPrice, 0, vPSession.VALPrice, brush3, (DashStyleHelper)4, cachedVALWidth);
				}
			}
		}
	}

	private int GetReferenceLineEndBar(VPSession session)
	{
		if (session == null)
		{
			return CurrentBar;
		}
		if (!session.IsComplete)
		{
			return CurrentBar;
		}
		int num = session.EndBarIndex;
		VPSession nextSession = GetNextSession(session);
		if (nextSession != null && nextSession.StartBarIndex >= 0)
		{
			num = ((!nextSession.IsComplete || nextSession.EndBarIndex < 0) ? Math.Max(num, CurrentBar) : Math.Max(num, nextSession.EndBarIndex));
		}
		return num;
	}

	private VPSession GetNextSession(VPSession session)
	{
		VPSession vPSession = null;
		for (int i = 0; i < sessions.Count; i++)
		{
			VPSession vPSession2 = sessions[i];
			if (!(vPSession2.SessionDate <= session.SessionDate) && (vPSession == null || vPSession2.SessionDate < vPSession.SessionDate))
			{
				vPSession = vPSession2;
			}
		}
		return vPSession;
	}

	private void EnsureCandlesVisibilityState()
	{
		if (ChartBars == null || ChartControl == null)
		{
			return;
		}
		bool shouldHide = HideCandles;
		if (((DispatcherObject)ChartControl).Dispatcher != null && !((DispatcherObject)ChartControl).Dispatcher.CheckAccess())
		{
			((DispatcherObject)ChartControl).Dispatcher.BeginInvoke((Delegate)(Action)delegate
			{
				ApplyCandlesVisibilityCore(shouldHide);
			}, Array.Empty<object>());
		}
		else
		{
			ApplyCandlesVisibilityCore(shouldHide);
		}
	}

	private void ApplyCandlesVisibilityCore(bool shouldHide)
	{
		if (ChartBars == null)
		{
			return;
		}
		try
		{
			if (shouldHide)
			{
				if (!candlesHidden)
				{
					savedUpBrush = ChartBars.Properties.ChartStyle.UpBrush;
					savedDownBrush = ChartBars.Properties.ChartStyle.DownBrush;
					savedStrokeBrush = ChartBars.Properties.ChartStyle.Stroke.Brush;
					savedStroke2Brush = ChartBars.Properties.ChartStyle.Stroke2.Brush;
					ChartBars.Properties.ChartStyle.UpBrush = (Brush)(object)Brushes.Transparent;
					ChartBars.Properties.ChartStyle.DownBrush = (Brush)(object)Brushes.Transparent;
					ChartBars.Properties.ChartStyle.Stroke.Brush = (Brush)(object)Brushes.Transparent;
					ChartBars.Properties.ChartStyle.Stroke2.Brush = (Brush)(object)Brushes.Transparent;
					candlesHidden = true;
				}
			}
			else if (candlesHidden)
			{
				ChartBars.Properties.ChartStyle.UpBrush = savedUpBrush;
				ChartBars.Properties.ChartStyle.DownBrush = savedDownBrush;
				ChartBars.Properties.ChartStyle.Stroke.Brush = savedStrokeBrush;
				ChartBars.Properties.ChartStyle.Stroke2.Brush = savedStroke2Brush;
				candlesHidden = false;
			}
		}
		catch
		{
		}
	}

	private void RestoreCandles()
	{
		if (!candlesHidden || ChartBars == null)
		{
			return;
		}
		if (ChartControl != null && ((DispatcherObject)ChartControl).Dispatcher != null && !((DispatcherObject)ChartControl).Dispatcher.CheckAccess())
		{
			((DispatcherObject)ChartControl).Dispatcher.BeginInvoke((Delegate)(Action)delegate
			{
				ApplyCandlesVisibilityCore(shouldHide: false);
			}, Array.Empty<object>());
		}
		else
		{
			ApplyCandlesVisibilityCore(shouldHide: false);
		}
	}

	private bool IsInRTHSession(DateTime barTime)
	{
		TimeSpan timeOfDay = barTime.TimeOfDay;
		if (timeOfDay >= rthStartTime)
		{
			return timeOfDay < rthEndTime;
		}
		return false;
	}

	private bool IsInConfiguredTimeWindow(DateTime barTime)
	{
		if (TradingHours != 1)
		{
			return IsInRTHSession(barTime);
		}
		return true;
	}

	private bool ShouldCloseSessionOnWindowEnd()
	{
		if (TradingHours == 0)
		{
			return ProfilePeriod == 0;
		}
		return false;
	}

	private DateTime GetTradingDate(DateTime dateTime)
	{
		DateTime result = dateTime.Date;
		if (TradingHours == 1 && dateTime.TimeOfDay >= rthEndTime)
		{
			result = result.AddDays(1.0);
		}
		return result;
	}

	private DateTime GetSessionKey(DateTime dateTime)
	{
		DateTime tradingDate = GetTradingDate(dateTime);
		if (ProfilePeriod == 1)
		{
			int num = (int)(tradingDate.DayOfWeek - 1 + 7) % 7;
			return tradingDate.AddDays(-num);
		}
		if (ProfilePeriod == 2)
		{
			return new DateTime(tradingDate.Year, tradingDate.Month, 1);
		}
		return tradingDate;
	}

	private DateTime AddProfilePeriods(DateTime sessionKey, int periods)
	{
		if (ProfilePeriod == 1)
		{
			return sessionKey.AddDays(7 * periods);
		}
		if (ProfilePeriod == 2)
		{
			return sessionKey.AddMonths(periods);
		}
		return sessionKey.AddDays(periods);
	}

	private string GetTradingHoursLabel()
	{
		if (TradingHours != 1)
		{
			return "RTH";
		}
		return "ETH";
	}

	private void SetTradingHoursFromLabel(string value)
	{
		string text = (value ?? string.Empty).Trim().ToUpperInvariant();
		TradingHours = ((text == "1" || text == "ETH") ? 1 : 0);
	}

	private string GetProfilePeriodLabel()
	{
		if (ProfilePeriod == 1)
		{
			return "Weekly";
		}
		if (ProfilePeriod == 2)
		{
			return "Monthly";
		}
		return "Daily";
	}

	private void SetProfilePeriodFromLabel(string value)
	{
		switch ((value ?? string.Empty).Trim().ToUpperInvariant())
		{
		case "1":
		case "WEEKLY":
			ProfilePeriod = 1;
			break;
		case "2":
		case "MONTHLY":
			ProfilePeriod = 2;
			break;
		default:
			ProfilePeriod = 0;
			break;
		}
	}

	private DateTime GetDisplayCutoffDate(DateTime referenceDateTime)
	{
		int num = Math.Max(1, DaysToShow);
		DateTime sessionKey = GetSessionKey(referenceDateTime);
		return AddProfilePeriods(sessionKey, -(num - 1));
	}

	private Color GetSafeBrushColor(Brush brush, Color fallbackColor)
	{
		SolidColorBrush val = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
		if (val == null)
		{
			return fallbackColor;
		}
		return val.Color;
	}

	private double GetRoundedPrice(double price, double groupSize)
	{
		return Math.Round(price / groupSize) * groupSize;
	}

	private Brush CloneAndFreezeBrushForCache(Brush source, Brush fallback)
	{
		Brush val = (Brush)(source ?? ((object)fallback) ?? ((object)Brushes.Transparent));
		if (((Freezable)val).IsFrozen)
		{
			return val;
		}
		if (((Freezable)val).CanFreeze)
		{
			Brush val2 = val.Clone();
			if (((Freezable)val2).CanFreeze)
			{
				((Freezable)val2).Freeze();
			}
			return val2;
		}
		return (Brush)(((object)fallback) ?? ((object)Brushes.Transparent));
	}

	private void RefreshThreadSafeDrawingCache()
	{
		Brush val = ((POCStroke != null) ? POCStroke.Brush : null);
		Brush val2 = ((VAHStroke != null) ? VAHStroke.Brush : null);
		Brush val3 = ((VALStroke != null) ? VALStroke.Brush : null);
		Brush lVNColor = LVNColor;
		Brush lVNOutlineColor = LVNOutlineColor;
		DashStyleHelper val4 = (DashStyleHelper)((POCStroke != null) ? ((int)POCStroke.DashStyleHelper) : 0);
		DashStyleHelper val5 = (DashStyleHelper)((VAHStroke == null) ? 1 : ((int)VAHStroke.DashStyleHelper));
		DashStyleHelper val6 = (DashStyleHelper)((VALStroke == null) ? 1 : ((int)VALStroke.DashStyleHelper));
		double num = ((POCStroke != null) ? POCStroke.Width : 2f);
		double num2 = ((VAHStroke != null) ? VAHStroke.Width : 1f);
		double num3 = ((VALStroke != null) ? VALStroke.Width : 1f);
		if (!drawingCacheInitialized || cacheSourcePOCBrush != val || cacheSourceVAHBrush != val2 || cacheSourceVALBrush != val3 || cacheSourceLVNFillBrush != lVNColor || cacheSourceLVNOutlineBrush != lVNOutlineColor || cacheSourcePOCDashStyle != val4 || cacheSourceVAHDashStyle != val5 || cacheSourceVALDashStyle != val6 || !(Math.Abs(cacheSourcePOCWidth - num) < 1E-06) || !(Math.Abs(cacheSourceVAHWidth - num2) < 1E-06) || !(Math.Abs(cacheSourceVALWidth - num3) < 1E-06))
		{
			cacheSourcePOCBrush = val;
			cacheSourceVAHBrush = val2;
			cacheSourceVALBrush = val3;
			cacheSourceLVNFillBrush = lVNColor;
			cacheSourceLVNOutlineBrush = lVNOutlineColor;
			cacheSourcePOCDashStyle = val4;
			cacheSourceVAHDashStyle = val5;
			cacheSourceVALDashStyle = val6;
			cacheSourcePOCWidth = num;
			cacheSourceVAHWidth = num2;
			cacheSourceVALWidth = num3;
			drawingCacheInitialized = true;
			cachedPOCBrush = CloneAndFreezeBrushForCache(val, (Brush)(object)Brushes.Orange);
			cachedVAHBrush = CloneAndFreezeBrushForCache(val2, (Brush)(object)Brushes.DodgerBlue);
			cachedVALBrush = CloneAndFreezeBrushForCache(val3, (Brush)(object)Brushes.DodgerBlue);
			cachedLVNFillBrush = CloneAndFreezeBrushForCache(lVNColor, (Brush)(object)Brushes.Goldenrod);
			cachedLVNOutlineBrush = CloneAndFreezeBrushForCache(lVNOutlineColor, cachedLVNFillBrush);
			cachedPOCDashStyle = val4;
			cachedVAHDashStyle = val5;
			cachedVALDashStyle = val6;
			cachedPOCWidth = Math.Max(1, (int)Math.Round(num));
			cachedVAHWidth = Math.Max(1, (int)Math.Round(num2));
			cachedVALWidth = Math.Max(1, (int)Math.Round(num3));
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		OnRender(chartControl, chartScale);
		EnsureCandlesVisibilityState();
		RefreshThreadSafeDrawingCache();
		if ((!ShowProfileBars && !ShowCompositeProfile) || Bars == null || chartControl == null || ChartBars == null || RenderTarget == null)
		{
			return;
		}
		EnsureDirectXResources();
		int fromIndex = ChartBars.FromIndex;
		int toIndex = ChartBars.ToIndex;
		DateTime dateTime = ((CurrentBar >= 0) ? GetDisplayCutoffDate(Time[0]) : DateTime.MinValue);
		List<VPSession> list;
		lock (sessionsLock)
		{
			if (sessions == null || sessions.Count == 0)
			{
				return;
			}
			list = new List<VPSession>(sessions);
		}
		if (ShowProfileBars)
		{
			for (int i = 0; i < list.Count; i++)
			{
				VPSession vPSession = list[i];
				if (!(vPSession.SessionDate < dateTime) && vPSession.StartBarIndex >= 0 && vPSession.EndBarIndex >= 0 && vPSession.EndBarIndex >= fromIndex && vPSession.StartBarIndex <= toIndex)
				{
					RenderSessionProfile(chartControl, chartScale, vPSession, i, fromIndex, toIndex);
				}
			}
		}
		if (ShowCompositeProfile)
		{
			VPSession vPSession2 = BuildCompositeSession(list, dateTime);
			if (vPSession2 != null)
			{
				RenderCompositeProfile(chartControl, chartScale, vPSession2, fromIndex, toIndex);
			}
		}
	}

	private void GetSessionRenderSnapshot(VPSession session, out Dictionary<double, long> volumeSnapshot, out Dictionary<double, long> deltaSnapshot, out double pocPrice, out double vahPrice, out double valPrice)
	{
		volumeSnapshot = null;
		deltaSnapshot = null;
		pocPrice = 0.0;
		vahPrice = 0.0;
		valPrice = 0.0;
		if (session == null)
		{
			return;
		}
		lock (session.VolumeLock)
		{
			if (session.VolumeByPrice.Count == 0)
			{
				session.RenderVolumeSnapshot = null;
				session.RenderDeltaSnapshot = null;
				session.RenderSnapshotVersion = session.VolumeVersion;
				return;
			}
			if (session.RenderVolumeSnapshot == null || session.RenderDeltaSnapshot == null || session.RenderSnapshotVersion != session.VolumeVersion)
			{
				session.RenderVolumeSnapshot = new Dictionary<double, long>(session.VolumeByPrice);
				session.RenderDeltaSnapshot = new Dictionary<double, long>(session.DeltaByPrice);
				session.RenderSnapshotVersion = session.VolumeVersion;
			}
			volumeSnapshot = session.RenderVolumeSnapshot;
			deltaSnapshot = session.RenderDeltaSnapshot;
			pocPrice = session.POCPrice;
			vahPrice = session.VAHPrice;
			valPrice = session.VALPrice;
		}
	}

	private void RenderSessionProfile(ChartControl chartControl, ChartScale chartScale, VPSession session, int sessionIndex, int firstBarOnChart, int lastBarOnChart)
	{
		GetSessionRenderSnapshot(session, out var volumeSnapshot, out var deltaSnapshot, out var pocPrice, out var vahPrice, out var valPrice);
		if (volumeSnapshot == null || volumeSnapshot.Count == 0)
		{
			return;
		}
		int num = Math.Max(session.StartBarIndex, firstBarOnChart);
		int num2 = Math.Min(session.EndBarIndex, lastBarOnChart);
		float num3 = chartControl.GetXByBarIndex(ChartBars, num);
		float num4 = (float)chartControl.GetXByBarIndex(ChartBars, num2) - num3;
		float num5;
		float num6;
		if (isDailyOrHigherTF || num4 < 20f)
		{
			float barDistance = chartControl.Properties.BarDistance;
			num5 = Math.Min(VolumeProfileWidth, barDistance * 0.9f);
			num6 = num3 - num5 / 2f;
		}
		else
		{
			num5 = Math.Min(VolumeProfileWidth, num4 * 0.8f);
			num6 = num3;
		}
		if (num5 < 5f)
		{
			return;
		}
		float num7 = Math.Max(5f, Math.Min(DeltaProfileWidth, num5));
		long num8 = 0L;
		foreach (long value3 in volumeSnapshot.Values)
		{
			if (value3 > num8)
			{
				num8 = value3;
			}
		}
		if (num8 == 0L)
		{
			return;
		}
		long num9 = 0L;
		if (ShowDeltaBars && deltaSnapshot.Count > 0)
		{
			foreach (KeyValuePair<double, long> item in deltaSnapshot)
			{
				long num10 = Math.Abs(item.Value);
				if (num10 > num9)
				{
					num9 = num10;
				}
			}
		}
		Color safeBrushColor = GetSafeBrushColor((POCStroke != null) ? POCStroke.Brush : null, Colors.Orange);
		Color safeBrushColor2 = GetSafeBrushColor(ValueAreaColor, Colors.SteelBlue);
		Color safeBrushColor3 = GetSafeBrushColor(ProfileColor, Colors.DodgerBlue);
		Color safeBrushColor4 = GetSafeBrushColor(DeltaPositiveColor, Colors.LimeGreen);
		Color safeBrushColor5 = GetSafeBrushColor(DeltaNegativeColor, Colors.IndianRed);
		Color4 color = default(Color4);
		color = new Color4((float)(int)safeBrushColor.R / 255f, (float)(int)safeBrushColor.G / 255f, (float)(int)safeBrushColor.B / 255f, 0.9f);
		Color4 color2 = default(Color4);
		color2 = new Color4((float)(int)safeBrushColor2.R / 255f, (float)(int)safeBrushColor2.G / 255f, (float)(int)safeBrushColor2.B / 255f, (float)ValueAreaOpacity / 100f);
		Color4 color3 = default(Color4);
		color3 = new Color4((float)(int)safeBrushColor3.R / 255f, (float)(int)safeBrushColor3.G / 255f, (float)(int)safeBrushColor3.B / 255f, (float)ProfileOpacity / 100f);
		Color4 color4 = default(Color4);
		color4 = new Color4((float)(int)safeBrushColor4.R / 255f, (float)(int)safeBrushColor4.G / 255f, (float)(int)safeBrushColor4.B / 255f, (float)DeltaOpacity / 100f);
		Color4 color5 = default(Color4);
		color5 = new Color4((float)(int)safeBrushColor5.R / 255f, (float)(int)safeBrushColor5.G / 255f, (float)(int)safeBrushColor5.B / 255f, (float)DeltaOpacity / 100f);
		if (dxPOCBrush == null || dxValueAreaBrush == null || dxProfileBrush == null || dxDeltaPositiveBrush == null || dxDeltaNegativeBrush == null)
		{
			return;
		}
		dxPOCBrush.Color = color;
		dxValueAreaBrush.Color = color2;
		dxProfileBrush.Color = color3;
		dxDeltaPositiveBrush.Color = color4;
		dxDeltaNegativeBrush.Color = color5;
		RectangleF val2 = default(RectangleF);
		foreach (KeyValuePair<double, long> item2 in volumeSnapshot)
		{
			double key = item2.Key;
			long value = item2.Value;
			if (value >= VolumeThreshold)
			{
				float num11 = Math.Max(2f, (float)((double)value / (double)num8 * (double)num5));
				float num12 = chartScale.GetYByValue(key);
				float num13 = chartScale.GetYByValue(key - volumeGroupSize);
				float num14 = Math.Max(1f, Math.Abs(num13 - num12));
				bool flag = key >= valPrice && key <= vahPrice;
				SolidColorBrush val = dxProfileBrush;
				if (MathExtentions.ApproxCompare(key, pocPrice) == 0)
				{
					val = dxPOCBrush;
				}
				else if (flag)
				{
					val = dxValueAreaBrush;
				}
				val2 = new RectangleF(num6, num12, num11, num14);
				RenderTarget.FillRectangle(val2, (Brush)(object)val);
			}
		}
		if (!ShowDeltaBars || num9 <= 0)
		{
			return;
		}
		RectangleF val3 = default(RectangleF);
		foreach (KeyValuePair<double, long> item3 in deltaSnapshot)
		{
			double key2 = item3.Key;
			long value2 = item3.Value;
			long num15 = Math.Abs(value2);
			if (num15 >= DeltaThreshold && num15 != 0L)
			{
				float num16 = Math.Max(1f, (float)((double)num15 / (double)num9 * (double)num7));
				float num17 = chartScale.GetYByValue(key2);
				float num18 = chartScale.GetYByValue(key2 - volumeGroupSize);
				float num19 = Math.Max(1f, Math.Abs(num18 - num17));
				float num20 = num6 - num16;
				val3 = new RectangleF(num20, num17, num16, num19);
				SolidColorBrush val4 = ((value2 > 0) ? dxDeltaPositiveBrush : dxDeltaNegativeBrush);
				RenderTarget.FillRectangle(val3, (Brush)(object)val4);
			}
		}
	}

	private static long CombineHash(long seed, long value)
	{
		return (seed ^ value) * 1099511628211L;
	}

	private long BuildCompositeCacheKey(List<VPSession> eligibleSessions, int firstIndex, int takeCount, DateTime displayCutoffDate)
	{
		long seed = 1469598103934665603L;
		seed = CombineHash(seed, CompositeSessionsCount);
		seed = CombineHash(seed, CompositeIncludeCurrentSession ? 1 : 0);
		seed = CombineHash(seed, displayCutoffDate.Ticks);
		seed = CombineHash(seed, ValueAreaPercentage);
		seed = CombineHash(seed, VolumeThreshold);
		seed = CombineHash(seed, DeltaThreshold);
		int num = firstIndex + takeCount;
		for (int i = firstIndex; i < num; i++)
		{
			VPSession vPSession = eligibleSessions[i];
			seed = CombineHash(seed, vPSession.SessionDate.Ticks);
			seed = CombineHash(seed, vPSession.VolumeVersion);
			seed = CombineHash(seed, vPSession.StartBarIndex);
			seed = CombineHash(seed, vPSession.EndBarIndex);
			seed = CombineHash(seed, vPSession.IsComplete ? 1 : 0);
		}
		return seed;
	}

	private VPSession BuildCompositeSession(List<VPSession> sessionsSnapshot, DateTime displayCutoffDate)
	{
		if (sessionsSnapshot == null || sessionsSnapshot.Count == 0)
		{
			return null;
		}
		int val = Math.Max(2, CompositeSessionsCount);
		List<VPSession> list = new List<VPSession>(sessionsSnapshot.Count);
		for (int i = 0; i < sessionsSnapshot.Count; i++)
		{
			VPSession vPSession = sessionsSnapshot[i];
			if (vPSession != null && vPSession.StartBarIndex >= 0 && vPSession.EndBarIndex >= 0 && !(vPSession.SessionDate < displayCutoffDate) && (CompositeIncludeCurrentSession || vPSession.IsComplete))
			{
				list.Add(vPSession);
			}
		}
		if (list.Count < 2)
		{
			compositeSessionCache = null;
			compositeSessionCacheKey = long.MinValue;
			return null;
		}
		int num = Math.Min(val, list.Count);
		int num2 = list.Count - num;
		long num3 = BuildCompositeCacheKey(list, num2, num, displayCutoffDate);
		if (compositeSessionCache != null && compositeSessionCacheKey == num3)
		{
			return compositeSessionCache;
		}
		VPSession vPSession2 = new VPSession
		{
			SessionDate = list[num2].SessionDate,
			StartBarIndex = int.MaxValue,
			EndBarIndex = -1,
			IsComplete = true
		};
		for (int j = num2; j < list.Count; j++)
		{
			VPSession vPSession3 = list[j];
			vPSession2.StartBarIndex = Math.Min(vPSession2.StartBarIndex, vPSession3.StartBarIndex);
			vPSession2.EndBarIndex = Math.Max(vPSession2.EndBarIndex, vPSession3.EndBarIndex);
			lock (vPSession3.VolumeLock)
			{
				foreach (KeyValuePair<double, long> item in vPSession3.VolumeByPrice)
				{
					if (vPSession2.VolumeByPrice.ContainsKey(item.Key))
					{
						vPSession2.VolumeByPrice[item.Key] += item.Value;
					}
					else
					{
						vPSession2.VolumeByPrice[item.Key] = item.Value;
					}
				}
				foreach (KeyValuePair<double, long> item2 in vPSession3.DeltaByPrice)
				{
					if (vPSession2.DeltaByPrice.ContainsKey(item2.Key))
					{
						vPSession2.DeltaByPrice[item2.Key] += item2.Value;
					}
					else
					{
						vPSession2.DeltaByPrice[item2.Key] = item2.Value;
					}
				}
			}
		}
		if (vPSession2.StartBarIndex == int.MaxValue || vPSession2.EndBarIndex < 0 || vPSession2.VolumeByPrice.Count == 0)
		{
			compositeSessionCache = null;
			compositeSessionCacheKey = long.MinValue;
			return null;
		}
		CalculatePOCAndValueArea(vPSession2, forceRecalculation: true);
		compositeSessionCache = vPSession2;
		compositeSessionCacheKey = num3;
		return vPSession2;
	}

	private void RenderCompositeProfile(ChartControl chartControl, ChartScale chartScale, VPSession compositeSession, int firstBarOnChart, int lastBarOnChart)
	{
		if (compositeSession == null || compositeSession.StartBarIndex < 0 || compositeSession.EndBarIndex < 0)
		{
			return;
		}
		Dictionary<double, long> volumeByPrice;
		Dictionary<double, long> deltaByPrice;
		double pOCPrice;
		double vAHPrice;
		double vALPrice;
		lock (compositeSession.VolumeLock)
		{
			if (compositeSession.VolumeByPrice.Count == 0)
			{
				return;
			}
			volumeByPrice = compositeSession.VolumeByPrice;
			deltaByPrice = compositeSession.DeltaByPrice;
			pOCPrice = compositeSession.POCPrice;
			vAHPrice = compositeSession.VAHPrice;
			vALPrice = compositeSession.VALPrice;
		}
		int num = Math.Max(compositeSession.StartBarIndex, firstBarOnChart);
		int num2 = Math.Min(compositeSession.EndBarIndex, lastBarOnChart);
		if (num2 < firstBarOnChart || num > lastBarOnChart || num2 < num)
		{
			return;
		}
		float num3 = chartControl.GetXByBarIndex(ChartBars, num);
		float num4 = chartControl.GetXByBarIndex(ChartBars, num2);
		float val = Math.Max(1f, num4 - num3);
		float num5 = Math.Max(20f, Math.Min(CompositeProfileWidth, val));
		float num6 = num4 - num5;
		if (num6 < num3)
		{
			num6 = num3;
		}
		long num7 = 0L;
		foreach (long value3 in volumeByPrice.Values)
		{
			if (value3 > num7)
			{
				num7 = value3;
			}
		}
		if (num7 == 0L)
		{
			return;
		}
		long num8 = 0L;
		bool flag = ShowDeltaBars && CompositeShowDelta;
		if (flag && deltaByPrice.Count > 0)
		{
			foreach (KeyValuePair<double, long> item in deltaByPrice)
			{
				long num9 = Math.Abs(item.Value);
				if (num9 > num8)
				{
					num8 = num9;
				}
			}
		}
		float num10 = Math.Max(0.1f, Math.Min(1f, (float)CompositeOpacity / 100f));
		float num11 = Math.Max(5f, Math.Min(DeltaProfileWidth, num5));
		Color safeBrushColor = GetSafeBrushColor((POCStroke != null) ? POCStroke.Brush : null, Colors.Orange);
		Color safeBrushColor2 = GetSafeBrushColor(ValueAreaColor, Colors.SteelBlue);
		Color safeBrushColor3 = GetSafeBrushColor(ProfileColor, Colors.DodgerBlue);
		Color safeBrushColor4 = GetSafeBrushColor(DeltaPositiveColor, Colors.LimeGreen);
		Color safeBrushColor5 = GetSafeBrushColor(DeltaNegativeColor, Colors.IndianRed);
		Color4 color = default(Color4);
		color = new Color4((float)(int)safeBrushColor.R / 255f, (float)(int)safeBrushColor.G / 255f, (float)(int)safeBrushColor.B / 255f, Math.Min(1f, 0.9f * num10));
		Color4 color2 = default(Color4);
		color2 = new Color4((float)(int)safeBrushColor2.R / 255f, (float)(int)safeBrushColor2.G / 255f, (float)(int)safeBrushColor2.B / 255f, Math.Min(1f, (float)ValueAreaOpacity / 100f * num10));
		Color4 color3 = default(Color4);
		color3 = new Color4((float)(int)safeBrushColor3.R / 255f, (float)(int)safeBrushColor3.G / 255f, (float)(int)safeBrushColor3.B / 255f, Math.Min(1f, (float)ProfileOpacity / 100f * num10));
		Color4 color4 = default(Color4);
		color4 = new Color4((float)(int)safeBrushColor4.R / 255f, (float)(int)safeBrushColor4.G / 255f, (float)(int)safeBrushColor4.B / 255f, Math.Min(1f, (float)DeltaOpacity / 100f * num10));
		Color4 color5 = default(Color4);
		color5 = new Color4((float)(int)safeBrushColor5.R / 255f, (float)(int)safeBrushColor5.G / 255f, (float)(int)safeBrushColor5.B / 255f, Math.Min(1f, (float)DeltaOpacity / 100f * num10));
		if (dxPOCBrush == null || dxValueAreaBrush == null || dxProfileBrush == null || dxDeltaPositiveBrush == null || dxDeltaNegativeBrush == null)
		{
			return;
		}
		dxPOCBrush.Color = color;
		dxValueAreaBrush.Color = color2;
		dxProfileBrush.Color = color3;
		dxDeltaPositiveBrush.Color = color4;
		dxDeltaNegativeBrush.Color = color5;
		RectangleF val3 = default(RectangleF);
		foreach (KeyValuePair<double, long> item2 in volumeByPrice)
		{
			double key = item2.Key;
			long value = item2.Value;
			if (value >= VolumeThreshold)
			{
				float num12 = Math.Max(2f, (float)((double)value / (double)num7 * (double)num5));
				float num13 = chartScale.GetYByValue(key);
				float num14 = chartScale.GetYByValue(key - volumeGroupSize);
				float num15 = Math.Max(1f, Math.Abs(num14 - num13));
				bool flag2 = key >= vALPrice && key <= vAHPrice;
				SolidColorBrush val2 = dxProfileBrush;
				if (MathExtentions.ApproxCompare(key, pOCPrice) == 0)
				{
					val2 = dxPOCBrush;
				}
				else if (flag2)
				{
					val2 = dxValueAreaBrush;
				}
				val3 = new RectangleF(num6, num13, num12, num15);
				RenderTarget.FillRectangle(val3, (Brush)(object)val2);
			}
		}
		if (!flag || num8 <= 0)
		{
			return;
		}
		RectangleF val4 = default(RectangleF);
		foreach (KeyValuePair<double, long> item3 in deltaByPrice)
		{
			double key2 = item3.Key;
			long value2 = item3.Value;
			long num16 = Math.Abs(value2);
			if (num16 >= DeltaThreshold && num16 != 0L)
			{
				float num17 = Math.Max(1f, (float)((double)num16 / (double)num8 * (double)num11));
				float num18 = chartScale.GetYByValue(key2);
				float num19 = chartScale.GetYByValue(key2 - volumeGroupSize);
				float num20 = Math.Max(1f, Math.Abs(num19 - num18));
				float num21 = num6 - num17;
				val4 = new RectangleF(num21, num18, num17, num20);
				SolidColorBrush val5 = ((value2 > 0) ? dxDeltaPositiveBrush : dxDeltaNegativeBrush);
				RenderTarget.FillRectangle(val4, (Brush)(object)val5);
			}
		}
	}

	private void EnsureDirectXResources()
	{
		if (RenderTarget != null)
		{
			if (dxProfileBrush == null)
			{
				dxProfileBrush = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 1f));
			}
			if (dxValueAreaBrush == null)
			{
				dxValueAreaBrush = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 1f));
			}
			if (dxPOCBrush == null)
			{
				dxPOCBrush = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 1f));
			}
			if (dxDeltaPositiveBrush == null)
			{
				dxDeltaPositiveBrush = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 1f));
			}
			if (dxDeltaNegativeBrush == null)
			{
				dxDeltaNegativeBrush = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 1f));
			}
			if (dxTextFormat == null)
			{
				dxTextFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", 9f)
				{
					TextAlignment = (TextAlignment)0,
					ParagraphAlignment = (ParagraphAlignment)2
				};
			}
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		OnRenderTargetChanged();
	}

	private void DisposeDx()
	{
		SolidColorBrush obj = dxProfileBrush;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		dxProfileBrush = null;
		SolidColorBrush obj2 = dxValueAreaBrush;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		dxValueAreaBrush = null;
		SolidColorBrush obj3 = dxPOCBrush;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
		dxPOCBrush = null;
		SolidColorBrush obj4 = dxDeltaPositiveBrush;
		if (obj4 != null)
		{
			((DisposeBase)obj4).Dispose();
		}
		dxDeltaPositiveBrush = null;
		SolidColorBrush obj5 = dxDeltaNegativeBrush;
		if (obj5 != null)
		{
			((DisposeBase)obj5).Dispose();
		}
		dxDeltaNegativeBrush = null;
		TextFormat obj6 = dxTextFormat;
		if (obj6 != null)
		{
			((DisposeBase)obj6).Dispose();
		}
		dxTextFormat = null;
		drawingCacheInitialized = false;
	}
}
}
