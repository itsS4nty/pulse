using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

namespace NinjaTrader.NinjaScript.Indicators.Pulse;

public class PulseDeltaProfile : Indicator
{
	private Dictionary<double, long> volumeByPrice;

	private object volumeLock = new object();

	private Dictionary<double, long> volumeSnapshot;

	private double pocPrice;

	private long pocVolume;

	private double vaHigh;

	private double vaLow;

	private Dictionary<double, long> deltaByPriceCurrentLeg;

	private Dictionary<double, long> deltaPendingBuffer;

	private object deltaLock = new object();

	private Dictionary<double, long> deltaSnapshot;

	private long buys;

	private long sells;

	private double currentZigZagHigh;

	private double currentZigZagLow;

	private int lastSwingIdx = -1;

	private double lastSwingPrice;

	private int trendDir;

	private bool useHighLow = true;

	private double lastZigZagHigh;

	private double lastZigZagLow;

	private int lastHighBar;

	private int lastLowBar;

	private int currentLegDirection;

	private double lastPivotForReset;

	private bool pivotCrossed;

	private SolidColorBrush vpProfileBrush;

	private SolidColorBrush vpValueAreaBrush;

	private SolidColorBrush vpPOCBrush;

	private SolidColorBrush deltaPositiveBrush;

	private SolidColorBrush deltaNegativeBrush;

	private SolidColorBrush tempBrushDx;

	private TextFormat textFormat;

	private TextFormat deltaTextFormat;

	private float lastBarWidth;

	private int deltaTextSize = 13;

	private Brush deltaTextColor;

	private double tickSize;

	private double volumeGroupSize;

	private double deltaGroupSize;

	private int volumeProfileWidth = 200;

	private int deltaLegWidth = 100;

	private int volumeTickCompression = 1;

	private int deltaTickCompression = 1;

	private int rotationSize = 10;

	private int valueAreaPercentage = 70;

	private bool showMaximumVolume = true;

	private bool showValues = true;

	private int volumeThreshold;

	private Brush profileColor;

	private Brush valueAreaColor;

	private Brush pocColor;

	private Brush positiveColor;

	private Brush negativeColor;

	private int profileOpacity = 70;

	private int valueAreaOpacity = 50;

	private int maxOpacity = 100;

	private int minOpacity = 20;

	private int gradientSteps = 10;

	private TimeSpan rthStartTime = new TimeSpan(9, 30, 0);

	private TimeSpan rthEndTime = new TimeSpan(16, 0, 0);

	private DateTime lastRTHSessionDate = DateTime.MinValue;

	private bool historicalVPLoaded;

	private DateTime historicalLoadDate = DateTime.MinValue;

	private bool isLoadingHistorical;

	private DateTime historicalLoadStartTime = DateTime.MinValue;

	private const bool DEBUG_MODE = false;

	[NinjaScriptProperty]
	[Range(50, 500)]
	[Display(Name = " Volume Profile Width", Description = "Width in pixels for volume profile", Order = 1, GroupName = "Volume Profile")]
	public int VolumeProfileWidth
	{
		get
		{
			return volumeProfileWidth;
		}
		set
		{
			volumeProfileWidth = Math.Max(50, Math.Min(500, value));
		}
	}

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = " Volume Tick Compression", Description = "Group volume price levels by ticks", Order = 2, GroupName = "Volume Profile")]
	public int VolumeTickCompression
	{
		get
		{
			return volumeTickCompression;
		}
		set
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Invalid comparison between Unknown and I4
			volumeTickCompression = Math.Max(1, Math.Min(100, value));
			if ((int)((NinjaScript)this).State == 4)
			{
				volumeGroupSize = tickSize * (double)volumeTickCompression;
			}
		}
	}

	[NinjaScriptProperty]
	[Display(Name = " Show Maximum Volume (POC)", Description = "Highlight the Point of Control", Order = 3, GroupName = "Volume Profile")]
	public bool ShowMaximumVolume
	{
		get
		{
			return showMaximumVolume;
		}
		set
		{
			showMaximumVolume = value;
		}
	}

	[NinjaScriptProperty]
	[Display(Name = " Show Values", Description = "Display volume values on bars", Order = 4, GroupName = "Volume Profile")]
	public bool ShowValues
	{
		get
		{
			return showValues;
		}
		set
		{
			showValues = value;
		}
	}

	[NinjaScriptProperty]
	[Range(0, 10000)]
	[Display(Name = " Volume Threshold", Description = "Minimum volume to display", Order = 5, GroupName = "Volume Profile")]
	public int VolumeThreshold
	{
		get
		{
			return volumeThreshold;
		}
		set
		{
			volumeThreshold = Math.Max(0, value);
		}
	}

	[NinjaScriptProperty]
	[Range(50, 95)]
	[Display(Name = " Value Area %", Description = "Percentage of volume for Value Area", Order = 1, GroupName = "Value Area Settings")]
	public int ValueAreaPercentage
	{
		get
		{
			return valueAreaPercentage;
		}
		set
		{
			valueAreaPercentage = Math.Max(50, Math.Min(95, value));
		}
	}

	[XmlIgnore]
	[Display(Name = " Profile Color", Description = "Color for volume profile bars", Order = 2, GroupName = "Value Area Settings")]
	public Brush ProfileColor
	{
		get
		{
			return profileColor;
		}
		set
		{
			profileColor = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string ProfileColorSerializable
	{
		get
		{
			return Serialize.BrushToString(profileColor);
		}
		set
		{
			profileColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = " Profile Opacity", Description = "Opacity for profile bars", Order = 3, GroupName = "Value Area Settings")]
	public int ProfileOpacity
	{
		get
		{
			return profileOpacity;
		}
		set
		{
			profileOpacity = Math.Max(0, Math.Min(100, value));
			DisposeDx();
		}
	}

	[XmlIgnore]
	[Display(Name = " Value Area Color", Description = "Color for Value Area bars", Order = 4, GroupName = "Value Area Settings")]
	public Brush ValueAreaColor
	{
		get
		{
			return valueAreaColor;
		}
		set
		{
			valueAreaColor = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string ValueAreaColorSerializable
	{
		get
		{
			return Serialize.BrushToString(valueAreaColor);
		}
		set
		{
			valueAreaColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = " Value Area Opacity", Description = "Opacity for Value Area bars", Order = 5, GroupName = "Value Area Settings")]
	public int ValueAreaOpacity
	{
		get
		{
			return valueAreaOpacity;
		}
		set
		{
			valueAreaOpacity = Math.Max(0, Math.Min(100, value));
			DisposeDx();
		}
	}

	[XmlIgnore]
	[Display(Name = " POC Color", Description = "Color for Point of Control highlight", Order = 6, GroupName = "Value Area Settings")]
	public Brush POCColor
	{
		get
		{
			return pocColor;
		}
		set
		{
			pocColor = value;
			DisposeDx();
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

	[NinjaScriptProperty]
	[Range(50, 300)]
	[Display(Name = " Delta Leg Width", Description = "Width in pixels for delta leg-to-leg", Order = 1, GroupName = "Delta Leg-to-Leg")]
	public int DeltaLegWidth
	{
		get
		{
			return deltaLegWidth;
		}
		set
		{
			deltaLegWidth = Math.Max(50, Math.Min(300, value));
		}
	}

	[NinjaScriptProperty]
	[Range(5, 50)]
	[Display(Name = " Rotation Size", Description = "ZigZag sensitivity in ticks", Order = 2, GroupName = "Delta Leg-to-Leg")]
	public int RotationSize
	{
		get
		{
			return rotationSize;
		}
		set
		{
			rotationSize = Math.Max(5, Math.Min(50, value));
		}
	}

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = " Delta Tick Compression", Description = "Group delta price levels by ticks", Order = 3, GroupName = "Delta Leg-to-Leg")]
	public int DeltaTickCompression
	{
		get
		{
			return deltaTickCompression;
		}
		set
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Invalid comparison between Unknown and I4
			deltaTickCompression = Math.Max(1, Math.Min(100, value));
			if ((int)((NinjaScript)this).State == 4)
			{
				deltaGroupSize = tickSize * (double)deltaTickCompression;
			}
		}
	}

	[NinjaScriptProperty]
	[Range(8, 24)]
	[Display(Name = " Delta Text Size", Description = "Font size for delta values", Order = 4, GroupName = "Delta Leg-to-Leg")]
	public int DeltaTextSize
	{
		get
		{
			return deltaTextSize;
		}
		set
		{
			deltaTextSize = Math.Max(8, Math.Min(24, value));
			DisposeDx();
		}
	}

	[XmlIgnore]
	[Display(Name = " Delta Text Color", Description = "Color for delta text values", Order = 5, GroupName = "Delta Leg-to-Leg")]
	public Brush DeltaTextColor
	{
		get
		{
			return deltaTextColor;
		}
		set
		{
			deltaTextColor = value;
		}
	}

	[Browsable(false)]
	public string DeltaTextColorSerializable
	{
		get
		{
			return Serialize.BrushToString(deltaTextColor);
		}
		set
		{
			deltaTextColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = " Positive Color", Description = "Color for positive delta", Order = 1, GroupName = "Color Gradient Settings")]
	public Brush PositiveColor
	{
		get
		{
			return positiveColor;
		}
		set
		{
			positiveColor = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string PositiveColorSerializable
	{
		get
		{
			return Serialize.BrushToString(positiveColor);
		}
		set
		{
			positiveColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = " Negative Color", Description = "Color for negative delta", Order = 2, GroupName = "Color Gradient Settings")]
	public Brush NegativeColor
	{
		get
		{
			return negativeColor;
		}
		set
		{
			negativeColor = value;
			DisposeDx();
		}
	}

	[Browsable(false)]
	public string NegativeColorSerializable
	{
		get
		{
			return Serialize.BrushToString(negativeColor);
		}
		set
		{
			negativeColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(20, 100)]
	[Display(Name = " Max Opacity", Description = "Maximum opacity for gradient", Order = 3, GroupName = "Color Gradient Settings")]
	public int MaxOpacity
	{
		get
		{
			return maxOpacity;
		}
		set
		{
			maxOpacity = Math.Max(20, Math.Min(100, value));
			DisposeDx();
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = " Min Opacity", Description = "Minimum opacity for gradient", Order = 4, GroupName = "Color Gradient Settings")]
	public int MinOpacity
	{
		get
		{
			return minOpacity;
		}
		set
		{
			minOpacity = Math.Max(0, Math.Min(100, value));
			DisposeDx();
		}
	}

	[NinjaScriptProperty]
	[Range(2, 10)]
	[Display(Name = " Gradient Steps", Description = "Number of gradient steps for smoothness", Order = 5, GroupName = "Color Gradient Settings")]
	public int GradientSteps
	{
		get
		{
			return gradientSteps;
		}
		set
		{
			gradientSteps = Math.Max(2, Math.Min(10, value));
			DisposeDx();
		}
	}

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
	[Display(Name = " RTH Start Time", Description = "Start time of RTH session (NY time)", Order = 1, GroupName = "RTH Session")]
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
	[Display(Name = " RTH End Time", Description = "End time of RTH session (NY time)", Order = 2, GroupName = "RTH Session")]
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

	public PulseDeltaProfile()
	{
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Invalid comparison between Unknown and I4
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Expected O, but got Unknown
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Expected O, but got Unknown
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected O, but got Unknown
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Invalid comparison between Unknown and I4
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			((NinjaScript)this).Description = "Pulse Delta Profile - Volume Profile + Delta Leg-to-Leg with ZigZag detection";
			((NinjaScriptBase)this).Name = "PulseDeltaProfile";
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).PaintPriceMarkers = false;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			volumeProfileWidth = 200;
			deltaLegWidth = 100;
			volumeTickCompression = 1;
			deltaTickCompression = 1;
			rotationSize = 10;
			valueAreaPercentage = 70;
			showMaximumVolume = true;
			showValues = true;
			volumeThreshold = 50;
			profileColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)30, (byte)144, byte.MaxValue));
			valueAreaColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)112, (byte)128, (byte)144));
			pocColor = (Brush)new SolidColorBrush(Color.FromRgb(byte.MaxValue, (byte)165, (byte)0));
			positiveColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)138, (byte)43, (byte)226));
			negativeColor = (Brush)new SolidColorBrush(Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue));
			deltaTextSize = 13;
			deltaTextColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)30, (byte)144, byte.MaxValue));
			profileOpacity = 70;
			valueAreaOpacity = 50;
			maxOpacity = 100;
			minOpacity = 20;
			gradientSteps = 10;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			tickSize = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
			string text = ((NinjaScriptBase)this).Instrument.MasterInstrument.Name.ToUpper();
			if (text.Contains("NQ") || text.Contains("MNQ"))
			{
				if (deltaTickCompression == 1)
				{
					deltaTickCompression = 40;
				}
				if (volumeTickCompression == 1)
				{
					volumeTickCompression = 3;
				}
			}
			else if (text.Contains("ES"))
			{
				if (deltaTickCompression == 1)
				{
					deltaTickCompression = 4;
				}
				if (volumeTickCompression == 1)
				{
					volumeTickCompression = 1;
				}
			}
			volumeGroupSize = tickSize * (double)volumeTickCompression;
			deltaGroupSize = tickSize * (double)deltaTickCompression;
			volumeByPrice = new Dictionary<double, long>();
			volumeSnapshot = new Dictionary<double, long>();
			deltaByPriceCurrentLeg = new Dictionary<double, long>();
			deltaPendingBuffer = new Dictionary<double, long>();
			deltaSnapshot = new Dictionary<double, long>();
			currentZigZagHigh = 0.0;
			currentZigZagLow = 0.0;
			lastSwingIdx = -1;
			lastSwingPrice = 0.0;
			trendDir = 0;
			useHighLow = true;
			lastZigZagHigh = 0.0;
			lastZigZagLow = 0.0;
			lastHighBar = -1;
			lastLowBar = -1;
			currentLegDirection = 0;
			lastPivotForReset = 0.0;
			pivotCrossed = false;
			LoadHistoricalVolumeProfile();
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeDx();
		}
	}

	protected override void OnBarUpdate()
	{
		if (isLoadingHistorical)
		{
			if (!(DateTime.Now - historicalLoadStartTime > TimeSpan.FromSeconds(10.0)))
			{
				return;
			}
			isLoadingHistorical = false;
		}
		if (((NinjaScriptBase)this).CurrentBars[0] < 5 || (((NinjaScriptBase)this).BarsArray.Length > 1 && ((NinjaScriptBase)this).CurrentBars[1] < 5))
		{
			return;
		}
		if (((NinjaScriptBase)this).BarsInProgress == 0)
		{
			bool num = IsInRTHSession(((NinjaScriptBase)this).Time[0]);
			DateTime date = ((NinjaScriptBase)this).Time[0].Date;
			if (num && date != lastRTHSessionDate)
			{
				ResetVolumeProfile();
				lastRTHSessionDate = date;
				historicalVPLoaded = false;
				if (date == DateTime.Today)
				{
					LoadHistoricalVolumeProfile();
				}
			}
			DetectZigZagPivot();
			CalculatePOCAndValueArea();
		}
		else if (((NinjaScriptBase)this).BarsInProgress == 1 && ((NinjaScriptBase)this).BarsArray.Length > 1)
		{
			ProcessTickDelta();
			if (IsInRTHSession(((NinjaScriptBase)this).Times[1][0]))
			{
				AccumulateTickVolume();
			}
		}
	}

	private void ResetSession()
	{
		lock (volumeLock)
		{
			volumeByPrice.Clear();
			pocPrice = 0.0;
			pocVolume = 0L;
			vaHigh = 0.0;
			vaLow = 0.0;
		}
		lock (deltaLock)
		{
			deltaByPriceCurrentLeg.Clear();
			deltaPendingBuffer.Clear();
			buys = 0L;
			sells = 0L;
		}
		currentZigZagHigh = 0.0;
		currentZigZagLow = 0.0;
		lastSwingIdx = -1;
		lastSwingPrice = 0.0;
		trendDir = 0;
		lastZigZagHigh = 0.0;
		lastZigZagLow = 0.0;
		lastHighBar = -1;
		lastLowBar = -1;
		currentLegDirection = 0;
		lastPivotForReset = 0.0;
		pivotCrossed = false;
	}

	private void ResetVolumeProfile()
	{
		lock (volumeLock)
		{
			volumeByPrice.Clear();
			pocPrice = 0.0;
			pocVolume = 0L;
			vaHigh = 0.0;
			vaLow = 0.0;
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

	private void LoadHistoricalVolumeProfile()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		DateTime today = DateTime.Today;
		DateTime dateTime = today.Add(rthStartTime);
		DateTime now = DateTime.Now;
		if (now < dateTime)
		{
			return;
		}
		isLoadingHistorical = true;
		historicalLoadStartTime = DateTime.Now;
		try
		{
			new BarsRequest(((NinjaScriptBase)this).Instrument, dateTime, now)
			{
				BarsPeriod = new BarsPeriod
				{
					BarsPeriodType = (BarsPeriodType)0,
					Value = 1
				},
				TradingHours = TradingHours.Get("Default 24 x 7")
			}.Request((Action<BarsRequest, ErrorCode, string>)delegate(BarsRequest bars, ErrorCode errorCode, string errorMessage)
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				if ((int)errorCode != 0)
				{
					isLoadingHistorical = false;
				}
				else if (bars == null || bars.Bars == null || bars.Bars.Count == 0)
				{
					isLoadingHistorical = false;
				}
				else
				{
					int num = 0;
					lock (volumeLock)
					{
						for (int i = 0; i < bars.Bars.Count; i++)
						{
							DateTime time = bars.Bars.GetTime(i);
							if (IsInRTHSession(time))
							{
								double close = bars.Bars.GetClose(i);
								long volume = bars.Bars.GetVolume(i);
								double roundedPrice = GetRoundedPrice(close, volumeGroupSize);
								if (volumeByPrice.TryGetValue(roundedPrice, out var value))
								{
									volumeByPrice[roundedPrice] = value + volume;
								}
								else
								{
									volumeByPrice[roundedPrice] = volume;
								}
								num++;
							}
						}
					}
					historicalVPLoaded = true;
					historicalLoadDate = today;
					isLoadingHistorical = false;
					CalculatePOCAndValueArea();
				}
			});
		}
		catch (Exception)
		{
			isLoadingHistorical = false;
		}
	}

	private void AccumulateTickVolume()
	{
		double price = ((NinjaScriptBase)this).Closes[1][0];
		long num = (long)((NinjaScriptBase)this).Volumes[1][0];
		double roundedPrice = GetRoundedPrice(price, volumeGroupSize);
		lock (volumeLock)
		{
			if (volumeByPrice.TryGetValue(roundedPrice, out var value))
			{
				volumeByPrice[roundedPrice] = value + num;
			}
			else
			{
				volumeByPrice[roundedPrice] = num;
			}
		}
	}

	private void DetectZigZagPivot()
	{
		if (((NinjaScriptBase)this).CurrentBar < 2)
		{
			return;
		}
		if (lastSwingPrice == 0.0)
		{
			lastSwingPrice = ((NinjaScriptBase)this).Close[1];
		}
		ISeries<double> val = ((NinjaScriptBase)this).High;
		ISeries<double> val2 = ((NinjaScriptBase)this).Low;
		if (!useHighLow)
		{
			val = ((NinjaScriptBase)this).Close;
			val2 = ((NinjaScriptBase)this).Close;
		}
		_ = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
		bool flag = val[1] >= val[0] - double.Epsilon && val[1] >= val[2] - double.Epsilon;
		bool flag2 = val2[1] <= val2[0] + double.Epsilon && val2[1] <= val2[2] + double.Epsilon;
		bool flag3 = IsPriceGreater(val[1], lastSwingPrice + (double)rotationSize);
		bool flag4 = IsPriceGreater(lastSwingPrice - (double)rotationSize, val2[1]);
		double num = 0.0;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		if (!flag && !flag2)
		{
			return;
		}
		if (trendDir <= 0 && flag && flag3)
		{
			num = val[1];
			flag5 = true;
			trendDir = 1;
		}
		else if (trendDir >= 0 && flag2 && flag4)
		{
			num = val2[1];
			flag6 = true;
			trendDir = -1;
		}
		else if (trendDir == 1 && flag && IsPriceGreater(val[1], lastSwingPrice))
		{
			num = val[1];
			flag7 = true;
		}
		else if (trendDir == -1 && flag2 && IsPriceGreater(lastSwingPrice, val2[1]))
		{
			num = val2[1];
			flag8 = true;
		}
		if (!(flag5 || flag6 || flag7 || flag8))
		{
			return;
		}
		if (flag5 || flag7)
		{
			currentZigZagHigh = num;
			lastZigZagHigh = num;
			lastHighBar = ((NinjaScriptBase)this).CurrentBar - 1;
		}
		else if (flag6 || flag8)
		{
			currentZigZagLow = num;
			lastZigZagLow = num;
			lastLowBar = ((NinjaScriptBase)this).CurrentBar - 1;
		}
		lastSwingIdx = ((NinjaScriptBase)this).CurrentBar - 1;
		lastSwingPrice = num;
		int num2 = currentLegDirection;
		double num3 = 0.0;
		if (flag5)
		{
			num2 = -1;
			num3 = num;
		}
		else if (flag6)
		{
			num2 = 1;
			num3 = num;
		}
		else if (flag7)
		{
			num2 = -1;
			num3 = num;
		}
		else if (flag8)
		{
			num2 = 1;
			num3 = num;
		}
		if (num2 != 0 && num2 != currentLegDirection && num3 > 0.0)
		{
			lock (deltaLock)
			{
				deltaByPriceCurrentLeg.Clear();
				foreach (KeyValuePair<double, long> item in deltaPendingBuffer)
				{
					deltaByPriceCurrentLeg[item.Key] = item.Value;
				}
				deltaPendingBuffer.Clear();
				buys = 0L;
				sells = 0L;
			}
			currentLegDirection = num2;
			lastPivotForReset = num3;
			pivotCrossed = true;
		}
		else if (num2 != 0 && num3 != lastPivotForReset && num3 > 0.0)
		{
			lastPivotForReset = num3;
		}
		DebugZigZagState();
	}

	private void ProcessTickDelta()
	{
		int num = ((NinjaScriptBase)this).CurrentBars[1];
		double close = ((NinjaScriptBase)this).BarsArray[1].GetClose(num);
		double bid = ((NinjaScriptBase)this).BarsArray[1].GetBid(num);
		double ask = ((NinjaScriptBase)this).BarsArray[1].GetAsk(num);
		double num2 = ((NinjaScriptBase)this).BarsArray[1].GetVolume(num);
		if (num2 <= 0.0 || double.IsNaN(close) || double.IsNaN(bid) || double.IsNaN(ask))
		{
			return;
		}
		long num3 = 0L;
		if (close >= ask)
		{
			buys += (long)num2;
			num3 = (long)num2;
		}
		else if (close <= bid)
		{
			sells += (long)num2;
			num3 = -(long)num2;
		}
		if (num3 == 0L)
		{
			return;
		}
		double roundedPrice = GetRoundedPrice(close, deltaGroupSize);
		lock (deltaLock)
		{
			if (deltaByPriceCurrentLeg.TryGetValue(roundedPrice, out var value))
			{
				deltaByPriceCurrentLeg[roundedPrice] = value + num3;
			}
			else
			{
				deltaByPriceCurrentLeg[roundedPrice] = num3;
			}
			if (deltaPendingBuffer.TryGetValue(roundedPrice, out var value2))
			{
				deltaPendingBuffer[roundedPrice] = value2 + num3;
			}
			else
			{
				deltaPendingBuffer[roundedPrice] = num3;
			}
		}
	}

	private void CalculatePOCAndValueArea()
	{
		lock (volumeLock)
		{
			if (volumeByPrice.Count == 0)
			{
				return;
			}
			long num = 0L;
			long num2 = 0L;
			double num3 = 0.0;
			foreach (KeyValuePair<double, long> item in volumeByPrice)
			{
				long value = item.Value;
				num += value;
				if (value > num2)
				{
					num2 = value;
					num3 = item.Key;
				}
			}
			pocPrice = num3;
			pocVolume = num2;
			if (num <= 0)
			{
				vaHigh = pocPrice;
				vaLow = pocPrice;
				return;
			}
			long num4 = (long)((double)num * ((double)valueAreaPercentage / 100.0));
			long num5 = pocVolume;
			List<double> list = new List<double>(volumeByPrice.Keys);
			list.Sort();
			int num6 = list.BinarySearch(pocPrice);
			if (num6 < 0)
			{
				num6 = ~num6;
			}
			int num7 = num6 + 1;
			int num8 = num6 - 1;
			double num9 = pocPrice;
			double num10 = pocPrice;
			while (num5 < num4 && (num7 < list.Count || num8 >= 0))
			{
				long num11 = -1L;
				long num12 = -1L;
				if (num7 < list.Count)
				{
					num11 = volumeByPrice[list[num7]];
				}
				if (num8 >= 0)
				{
					num12 = volumeByPrice[list[num8]];
				}
				if (num11 >= num12)
				{
					if (num7 >= list.Count)
					{
						break;
					}
					num9 = list[num7];
					num5 += num11;
					num7++;
				}
				else
				{
					if (num8 < 0)
					{
						break;
					}
					num10 = list[num8];
					num5 += num12;
					num8--;
				}
			}
			vaHigh = num9;
			vaLow = num10;
		}
	}

	private double GetRoundedPrice(double price, double groupSize)
	{
		return Math.Round(price / groupSize) * groupSize;
	}

	private bool IsPriceGreater(double price1, double price2)
	{
		return price1 > price2 + double.Epsilon;
	}

	private static Color GetBrushColor(Brush brush, Color fallback)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		SolidColorBrush val = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
		if (val == null)
		{
			return fallback;
		}
		return val.Color;
	}

	private void DebugZigZagState()
	{
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (((NinjaScriptBase)this).Bars == null || ((IndicatorRenderBase)this).ChartControl == null || ((IndicatorRenderBase)this).RenderTarget == null || volumeSnapshot == null || deltaSnapshot == null)
		{
			return;
		}
		lock (volumeLock)
		{
			volumeSnapshot.Clear();
			foreach (KeyValuePair<double, long> item in volumeByPrice)
			{
				volumeSnapshot[item.Key] = item.Value;
			}
		}
		lock (deltaLock)
		{
			deltaSnapshot.Clear();
			foreach (KeyValuePair<double, long> item2 in deltaByPriceCurrentLeg)
			{
				deltaSnapshot[item2.Key] = item2.Value;
			}
		}
		EnsureDirectXResources();
		RenderVolumeProfile(chartControl, chartScale);
		RenderDeltaLegToLeg(chartControl, chartScale);
	}

	private void RenderVolumeProfile(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		if (((IndicatorRenderBase)this).RenderTarget == null || volumeSnapshot.Count == 0 || tempBrushDx == null)
		{
			return;
		}
		int w = ((IndicatorRenderBase)this).ChartPanel.W;
		long num = 0L;
		foreach (KeyValuePair<double, long> item in volumeSnapshot)
		{
			num = Math.Max(num, item.Value);
		}
		if (num == 0L)
		{
			return;
		}
		int num2 = 0;
		Color4 color = default(Color4);
		RectangleF val2 = default(RectangleF);
		foreach (KeyValuePair<double, long> item2 in volumeSnapshot)
		{
			double key = item2.Key;
			long value = item2.Value;
			if (value >= volumeThreshold)
			{
				num2++;
				float num3 = Math.Max(3f, (float)((double)value / (double)num * (double)volumeProfileWidth));
				float num4 = (float)w - num3;
				float num5 = chartScale.GetYByValue(key);
				float val = Math.Abs((float)chartScale.GetYByValue(key - volumeGroupSize) - num5);
				float num6 = Math.Max(0.5f, val);
				float num7 = ((key >= vaLow && key <= vaHigh) ? 0.85f : 0.45f);
				((Color4)(ref color))._002Ector(0.4f, 0.7f, 0.9f, num7);
				tempBrushDx.Color = color;
				((RectangleF)(ref val2))._002Ector(num4, num5, num3, num6);
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(val2, (Brush)(object)tempBrushDx);
			}
		}
		if (showMaximumVolume && pocPrice > 0.0)
		{
			float num8 = chartScale.GetYByValue(pocPrice);
			if (!volumeSnapshot.TryGetValue(pocPrice, out var value2))
			{
				value2 = pocVolume;
			}
			float num9 = (float)((double)value2 / (double)num * (double)volumeProfileWidth);
			float num10 = (float)w - num9;
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num10, num8), new Vector2((float)w, num8), (Brush)(object)vpPOCBrush, 2f);
		}
	}

	private void RenderDeltaLegToLeg(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		if (((IndicatorRenderBase)this).RenderTarget == null || deltaSnapshot.Count == 0 || tempBrushDx == null)
		{
			return;
		}
		int num = ((IndicatorRenderBase)this).ChartPanel.W - volumeProfileWidth;
		int num2 = num - deltaLegWidth;
		int num3 = num;
		long num4 = 0L;
		foreach (KeyValuePair<double, long> item in deltaSnapshot)
		{
			num4 = Math.Max(num4, Math.Abs(item.Value));
		}
		if (num4 == 0L)
		{
			return;
		}
		Color brushColor = GetBrushColor(positiveColor, Colors.BlueViolet);
		Color brushColor2 = GetBrushColor(negativeColor, Colors.White);
		Color brushColor3 = GetBrushColor(deltaTextColor, Colors.DodgerBlue);
		RectangleF val2 = default(RectangleF);
		RectangleF val3 = default(RectangleF);
		foreach (KeyValuePair<double, long> item2 in deltaSnapshot)
		{
			double key = item2.Key;
			long value = item2.Value;
			if (value != 0L)
			{
				float num5 = (float)((double)Math.Abs(value) / (double)num4 * (double)deltaLegWidth);
				float num6 = (float)num3 - num5;
				float num7 = chartScale.GetYByValue(key);
				float num8 = Math.Max(3f, Math.Abs((float)chartScale.GetYByValue(key - deltaGroupSize) - num7));
				float num9 = (float)((double)Math.Abs(value) / (double)num4);
				float num10 = ((float)minOpacity + (float)(maxOpacity - minOpacity) * num9) / 100f;
				Color val = ((value > 0) ? brushColor : brushColor2);
				tempBrushDx.Color = new Color4((float)(int)((Color)(ref val)).R / 255f, (float)(int)((Color)(ref val)).G / 255f, (float)(int)((Color)(ref val)).B / 255f, num10);
				((RectangleF)(ref val2))._002Ector(num6, num7, num5, num8);
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(val2, (Brush)(object)tempBrushDx);
				if (showValues && deltaTextFormat != null)
				{
					string text = value.ToString("+#;-#;0");
					float num11 = Math.Max(0.7f, num10);
					tempBrushDx.Color = new Color4((float)(int)((Color)(ref brushColor3)).R / 255f, (float)(int)((Color)(ref brushColor3)).G / 255f, (float)(int)((Color)(ref brushColor3)).B / 255f, num11);
					((RectangleF)(ref val3))._002Ector((float)num2 + 2f, num7, Math.Max(1f, (float)deltaLegWidth - 8f), num8);
					((IndicatorRenderBase)this).RenderTarget.DrawText(text, deltaTextFormat, val3, (Brush)(object)tempBrushDx);
				}
			}
		}
	}

	private void RenderCurrentLegIndicator(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Expected O, but got Unknown
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Expected O, but got Unknown
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Expected O, but got Unknown
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		if (((IndicatorRenderBase)this).RenderTarget == null || ((NinjaScriptBase)this).CurrentBar < 1)
		{
			return;
		}
		double close = ((NinjaScriptBase)this).Bars.GetClose(((NinjaScriptBase)this).CurrentBar);
		double num = 0.0;
		string arg = "INIT";
		double num2 = ((lastZigZagHigh > 0.0) ? Math.Abs(close - lastZigZagHigh) : double.MaxValue);
		double num3 = ((lastZigZagLow > 0.0) ? Math.Abs(close - lastZigZagLow) : double.MaxValue);
		if (num3 > num2 && lastZigZagLow > 0.0)
		{
			num = lastZigZagLow;
			arg = "UP";
		}
		else if (num2 > num3 && lastZigZagHigh > 0.0)
		{
			num = lastZigZagHigh;
			arg = "DOWN";
		}
		if (num == 0.0)
		{
			return;
		}
		double num4 = Math.Max(num, close);
		double num5 = Math.Min(num, close);
		float num6 = chartScale.GetYByValue(num4);
		float num7 = chartScale.GetYByValue(num5);
		int num8 = ((IndicatorRenderBase)this).ChartPanel.W - volumeProfileWidth - deltaLegWidth;
		float num9 = num8 - 5;
		float num10 = 3f;
		SolidColorBrush val = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(1f, 0.8f, 0f, 0.6f));
		try
		{
			RectangleF val2 = default(RectangleF);
			((RectangleF)(ref val2))._002Ector(num9, num6, num10, num7 - num6);
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val2, (Brush)(object)val);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		float num11 = chartScale.GetYByValue(num);
		SolidColorBrush val3 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(1f, 0f, 0f, 1f));
		try
		{
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2((float)(num8 - 10), num11), new Vector2((float)num8, num11), (Brush)(object)val3, 2f);
			Ellipse val4 = default(Ellipse);
			((Ellipse)(ref val4))._002Ector(new Vector2((float)(num8 - 5), num11), 4f, 4f);
			((IndicatorRenderBase)this).RenderTarget.FillEllipse(val4, (Brush)(object)val3);
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
		if (textFormat == null)
		{
			return;
		}
		string text = $"{arg} Leg\nPivot: {num:F2}\nCurrent: {close:F2}";
		SolidColorBrush val5 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(1f, 1f, 1f, 1f));
		try
		{
			RectangleF val6 = default(RectangleF);
			((RectangleF)(ref val6))._002Ector((float)(num8 - 80), num6, 70f, 60f);
			((IndicatorRenderBase)this).RenderTarget.DrawText(text, textFormat, val6, (Brush)(object)val5);
		}
		finally
		{
			((IDisposable)val5)?.Dispose();
		}
	}

	private void EnsureDirectXResources()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Expected O, but got Unknown
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Expected O, but got Unknown
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Expected O, but got Unknown
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Expected O, but got Unknown
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Expected O, but got Unknown
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget == null)
		{
			return;
		}
		if (vpProfileBrush == null && profileColor != null)
		{
			Color brushColor = GetBrushColor(profileColor, Colors.DodgerBlue);
			vpProfileBrush = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref brushColor)).R / 255f, (float)(int)((Color)(ref brushColor)).G / 255f, (float)(int)((Color)(ref brushColor)).B / 255f, (float)profileOpacity / 100f));
		}
		if (vpValueAreaBrush == null && valueAreaColor != null)
		{
			Color brushColor2 = GetBrushColor(valueAreaColor, Colors.SlateGray);
			vpValueAreaBrush = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref brushColor2)).R / 255f, (float)(int)((Color)(ref brushColor2)).G / 255f, (float)(int)((Color)(ref brushColor2)).B / 255f, (float)valueAreaOpacity / 100f));
		}
		if (vpPOCBrush == null && pocColor != null)
		{
			Color brushColor3 = GetBrushColor(pocColor, Colors.Orange);
			vpPOCBrush = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref brushColor3)).R / 255f, (float)(int)((Color)(ref brushColor3)).G / 255f, (float)(int)((Color)(ref brushColor3)).B / 255f, 1f));
		}
		if (deltaPositiveBrush == null && positiveColor != null)
		{
			Color brushColor4 = GetBrushColor(positiveColor, Colors.BlueViolet);
			deltaPositiveBrush = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref brushColor4)).R / 255f, (float)(int)((Color)(ref brushColor4)).G / 255f, (float)(int)((Color)(ref brushColor4)).B / 255f, 1f));
		}
		if (deltaNegativeBrush == null && negativeColor != null)
		{
			Color brushColor5 = GetBrushColor(negativeColor, Colors.White);
			deltaNegativeBrush = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref brushColor5)).R / 255f, (float)(int)((Color)(ref brushColor5)).G / 255f, (float)(int)((Color)(ref brushColor5)).B / 255f, 1f));
		}
		if (tempBrushDx == null)
		{
			tempBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(1f, 1f, 1f, 1f));
		}
		if (textFormat == null)
		{
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", 9f)
			{
				TextAlignment = (TextAlignment)0,
				ParagraphAlignment = (ParagraphAlignment)2
			};
		}
		float num = ((((IndicatorRenderBase)this).ChartControl != null) ? ((float)((IndicatorRenderBase)this).ChartControl.BarWidth) : 5f);
		if (deltaTextFormat == null || Math.Abs(num - lastBarWidth) > 0.5f)
		{
			TextFormat obj = deltaTextFormat;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			lastBarWidth = num;
			float num2 = deltaTextSize;
			float num3 = Math.Max(0.9f, Math.Min(2.2f, num / 4f));
			float num4 = num2 * num3;
			deltaTextFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", (FontWeight)600, (FontStyle)0, num4)
			{
				TextAlignment = (TextAlignment)1,
				ParagraphAlignment = (ParagraphAlignment)2
			};
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}

	private void DisposeDx()
	{
		SolidColorBrush obj = vpProfileBrush;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		vpProfileBrush = null;
		SolidColorBrush obj2 = vpValueAreaBrush;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		vpValueAreaBrush = null;
		SolidColorBrush obj3 = vpPOCBrush;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
		vpPOCBrush = null;
		SolidColorBrush obj4 = deltaPositiveBrush;
		if (obj4 != null)
		{
			((DisposeBase)obj4).Dispose();
		}
		deltaPositiveBrush = null;
		SolidColorBrush obj5 = deltaNegativeBrush;
		if (obj5 != null)
		{
			((DisposeBase)obj5).Dispose();
		}
		deltaNegativeBrush = null;
		SolidColorBrush obj6 = tempBrushDx;
		if (obj6 != null)
		{
			((DisposeBase)obj6).Dispose();
		}
		tempBrushDx = null;
		TextFormat obj7 = textFormat;
		if (obj7 != null)
		{
			((DisposeBase)obj7).Dispose();
		}
		textFormat = null;
		TextFormat obj8 = deltaTextFormat;
		if (obj8 != null)
		{
			((DisposeBase)obj8).Dispose();
		}
		deltaTextFormat = null;
		lastBarWidth = 0f;
	}
}
