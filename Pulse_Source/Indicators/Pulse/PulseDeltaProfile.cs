#region Using declarations
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
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
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

		private SharpDX.Direct2D1.SolidColorBrush vpProfileBrush;

		private SharpDX.Direct2D1.SolidColorBrush vpValueAreaBrush;

		private SharpDX.Direct2D1.SolidColorBrush vpPOCBrush;

		private SharpDX.Direct2D1.SolidColorBrush deltaPositiveBrush;

		private SharpDX.Direct2D1.SolidColorBrush deltaNegativeBrush;

		private SharpDX.Direct2D1.SolidColorBrush tempBrushDx;

		private TextFormat textFormat;

		private TextFormat deltaTextFormat;

		private float lastBarWidth;

		private int deltaTextSize = 13;

		private System.Windows.Media.Brush deltaTextColor;

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

		private System.Windows.Media.Brush profileColor;

		private System.Windows.Media.Brush valueAreaColor;

		private System.Windows.Media.Brush pocColor;

		private System.Windows.Media.Brush positiveColor;

		private System.Windows.Media.Brush negativeColor;

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
				volumeTickCompression = Math.Max(1, Math.Min(100, value));
				if (State == State.DataLoaded)
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
		public System.Windows.Media.Brush ProfileColor
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
		public System.Windows.Media.Brush ValueAreaColor
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
		public System.Windows.Media.Brush POCColor
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
				deltaTickCompression = Math.Max(1, Math.Min(100, value));
				if (State == State.DataLoaded)
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
		public System.Windows.Media.Brush DeltaTextColor
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
		public System.Windows.Media.Brush PositiveColor
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
		public System.Windows.Media.Brush NegativeColor
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

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
				Description = "Pulse Delta Profile - Volume Profile + Delta Leg-to-Leg with ZigZag detection";
				Name = "PulseDeltaProfile";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				volumeProfileWidth = 200;
				deltaLegWidth = 100;
				volumeTickCompression = 1;
				deltaTickCompression = 1;
				rotationSize = 10;
				valueAreaPercentage = 70;
				showMaximumVolume = true;
				showValues = true;
				volumeThreshold = 50;
				profileColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				valueAreaColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				pocColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				positiveColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				negativeColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				deltaTextSize = 13;
				deltaTextColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
				profileOpacity = 70;
				valueAreaOpacity = 50;
				maxOpacity = 100;
				minOpacity = 20;
				gradientSteps = 10;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				tickSize = Instrument.MasterInstrument.TickSize;
				string text = Instrument.MasterInstrument.Name.ToUpper();
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
			else if (State == State.Terminated)
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
			if (CurrentBars[0] < 5 || (BarsArray.Length > 1 && CurrentBars[1] < 5))
			{
				return;
			}
			if (BarsInProgress == 0)
			{
				bool num = IsInRTHSession(Time[0]);
				DateTime date = Time[0].Date;
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
			else if (BarsInProgress == 1 && BarsArray.Length > 1)
			{
				ProcessTickDelta();
				if (IsInRTHSession(Times[1][0]))
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
				BarsRequest barsRequest = new BarsRequest(Instrument, dateTime, now);
				barsRequest.BarsPeriod = new BarsPeriod
				{
					BarsPeriodType = BarsPeriodType.Tick,
					Value = 1
				};
				barsRequest.TradingHours = TradingHours.Get("Default 24 x 7");
				barsRequest.Request(delegate(BarsRequest bars, ErrorCode errorCode, string errorMessage)
				{
					if (errorCode != ErrorCode.NoError)
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
			double price = Closes[1][0];
			long num = (long)Volumes[1][0];
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
			if (CurrentBar < 2)
			{
				return;
			}
			if (lastSwingPrice == 0.0)
			{
				lastSwingPrice = Close[1];
			}
			ISeries<double> series = High;
			ISeries<double> series2 = Low;
			if (!useHighLow)
			{
				series = Close;
				series2 = Close;
			}
			_ = Instrument.MasterInstrument.TickSize;
			bool flag = series[1] >= series[0] - double.Epsilon && series[1] >= series[2] - double.Epsilon;
			bool flag2 = series2[1] <= series2[0] + double.Epsilon && series2[1] <= series2[2] + double.Epsilon;
			bool flag3 = IsPriceGreater(series[1], lastSwingPrice + (double)rotationSize);
			bool flag4 = IsPriceGreater(lastSwingPrice - (double)rotationSize, series2[1]);
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
				num = series[1];
				flag5 = true;
				trendDir = 1;
			}
			else if (trendDir >= 0 && flag2 && flag4)
			{
				num = series2[1];
				flag6 = true;
				trendDir = -1;
			}
			else if (trendDir == 1 && flag && IsPriceGreater(series[1], lastSwingPrice))
			{
				num = series[1];
				flag7 = true;
			}
			else if (trendDir == -1 && flag2 && IsPriceGreater(lastSwingPrice, series2[1]))
			{
				num = series2[1];
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
				lastHighBar = CurrentBar - 1;
			}
			else if (flag6 || flag8)
			{
				currentZigZagLow = num;
				lastZigZagLow = num;
				lastLowBar = CurrentBar - 1;
			}
			lastSwingIdx = CurrentBar - 1;
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
			int index = CurrentBars[1];
			double close = BarsArray[1].GetClose(index);
			double bid = BarsArray[1].GetBid(index);
			double ask = BarsArray[1].GetAsk(index);
			double num = BarsArray[1].GetVolume(index);
			if (num <= 0.0 || double.IsNaN(close) || double.IsNaN(bid) || double.IsNaN(ask))
			{
				return;
			}
			long num2 = 0L;
			if (close >= ask)
			{
				buys += (long)num;
				num2 = (long)num;
			}
			else if (close <= bid)
			{
				sells += (long)num;
				num2 = -(long)num;
			}
			if (num2 == 0L)
			{
				return;
			}
			double roundedPrice = GetRoundedPrice(close, deltaGroupSize);
			lock (deltaLock)
			{
				if (deltaByPriceCurrentLeg.TryGetValue(roundedPrice, out var value))
				{
					deltaByPriceCurrentLeg[roundedPrice] = value + num2;
				}
				else
				{
					deltaByPriceCurrentLeg[roundedPrice] = num2;
				}
				if (deltaPendingBuffer.TryGetValue(roundedPrice, out var value2))
				{
					deltaPendingBuffer[roundedPrice] = value2 + num2;
				}
				else
				{
					deltaPendingBuffer[roundedPrice] = num2;
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

		private static System.Windows.Media.Color GetBrushColor(System.Windows.Media.Brush brush, System.Windows.Media.Color fallback)
		{
			if (!(brush is System.Windows.Media.SolidColorBrush solidColorBrush))
			{
				return fallback;
			}
			return solidColorBrush.Color;
		}

		private void DebugZigZagState()
		{
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (Bars == null || ChartControl == null || RenderTarget == null || volumeSnapshot == null || deltaSnapshot == null)
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
			if (RenderTarget == null || volumeSnapshot.Count == 0 || tempBrushDx == null)
			{
				return;
			}
			int w = ChartPanel.W;
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
			foreach (KeyValuePair<double, long> item2 in volumeSnapshot)
			{
				double key = item2.Key;
				long value = item2.Value;
				if (value >= volumeThreshold)
				{
					num2++;
					float num3 = Math.Max(3f, (float)((double)value / (double)num * (double)volumeProfileWidth));
					float x = (float)w - num3;
					float num4 = chartScale.GetYByValue(key);
					float val = Math.Abs((float)chartScale.GetYByValue(key - volumeGroupSize) - num4);
					float height = Math.Max(0.5f, val);
					float alpha = ((key >= vaLow && key <= vaHigh) ? 0.85f : 0.45f);
					Color4 color = new Color4(0.4196f, 0.4353f, 0.8000f, alpha);
					tempBrushDx.Color = color;
					RectangleF rect = new RectangleF(x, num4, num3, height);
					RenderTarget.FillRectangle(rect, tempBrushDx);
				}
			}
			if (showMaximumVolume && pocPrice > 0.0)
			{
				float y = chartScale.GetYByValue(pocPrice);
				if (!volumeSnapshot.TryGetValue(pocPrice, out var value2))
				{
					value2 = pocVolume;
				}
				float num5 = (float)((double)value2 / (double)num * (double)volumeProfileWidth);
				float x2 = (float)w - num5;
				RenderTarget.DrawLine(new Vector2(x2, y), new Vector2(w, y), vpPOCBrush, 2f);
			}
		}

		private void RenderDeltaLegToLeg(ChartControl chartControl, ChartScale chartScale)
		{
			if (RenderTarget == null || deltaSnapshot.Count == 0 || tempBrushDx == null)
			{
				return;
			}
			int num = ChartPanel.W - volumeProfileWidth;
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
			System.Windows.Media.Color brushColor = GetBrushColor(positiveColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
			System.Windows.Media.Color brushColor2 = GetBrushColor(negativeColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
			System.Windows.Media.Color brushColor3 = GetBrushColor(deltaTextColor, System.Windows.Media.Color.FromRgb(45, 45, 45));
			foreach (KeyValuePair<double, long> item2 in deltaSnapshot)
			{
				double key = item2.Key;
				long value = item2.Value;
				if (value != 0L)
				{
					float num5 = (float)((double)Math.Abs(value) / (double)num4 * (double)deltaLegWidth);
					float x = (float)num3 - num5;
					float num6 = chartScale.GetYByValue(key);
					float height = Math.Max(3f, Math.Abs((float)chartScale.GetYByValue(key - deltaGroupSize) - num6));
					float num7 = (float)((double)Math.Abs(value) / (double)num4);
					float num8 = ((float)minOpacity + (float)(maxOpacity - minOpacity) * num7) / 100f;
					System.Windows.Media.Color color = ((value > 0) ? brushColor : brushColor2);
					tempBrushDx.Color = new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, num8);
					RectangleF rect = new RectangleF(x, num6, num5, height);
					RenderTarget.FillRectangle(rect, tempBrushDx);
					if (showValues && deltaTextFormat != null)
					{
						string text = value.ToString("+#;-#;0");
						float alpha = Math.Max(0.7f, num8);
						tempBrushDx.Color = new Color4((float)(int)brushColor3.R / 255f, (float)(int)brushColor3.G / 255f, (float)(int)brushColor3.B / 255f, alpha);
						RectangleF layoutRect = new RectangleF((float)num2 + 2f, num6, Math.Max(1f, (float)deltaLegWidth - 8f), height);
						RenderTarget.DrawText(text, deltaTextFormat, layoutRect, tempBrushDx);
					}
				}
			}
		}

		private void RenderCurrentLegIndicator(ChartControl chartControl, ChartScale chartScale)
		{
			if (RenderTarget == null || CurrentBar < 1)
			{
				return;
			}
			double close = Bars.GetClose(CurrentBar);
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
			double val = Math.Max(num, close);
			double val2 = Math.Min(num, close);
			float num4 = chartScale.GetYByValue(val);
			float num5 = chartScale.GetYByValue(val2);
			int num6 = ChartPanel.W - volumeProfileWidth - deltaLegWidth;
			float x = num6 - 5;
			float width = 3f;
			using (SharpDX.Direct2D1.SolidColorBrush brush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.7216f, 0.7373f, 0.7765f, 0.6f)))
			{
				RectangleF rect = new RectangleF(x, num4, width, num5 - num4);
				RenderTarget.FillRectangle(rect, brush);
			}
			float y = chartScale.GetYByValue(num);
			using (SharpDX.Direct2D1.SolidColorBrush brush2 = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.2902f, 0.2902f, 0.2902f, 1f)))
			{
				RenderTarget.DrawLine(new Vector2(num6 - 10, y), new Vector2(num6, y), brush2, 2f);
				Ellipse ellipse = new Ellipse(new Vector2(num6 - 5, y), 4f, 4f);
				RenderTarget.FillEllipse(ellipse, brush2);
			}
			if (textFormat == null)
			{
				return;
			}
			string text = $"{arg} Leg\nPivot: {num:F2}\nCurrent: {close:F2}";
			using SharpDX.Direct2D1.SolidColorBrush defaultForegroundBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.1765f, 0.1765f, 0.1765f, 1f));
			RectangleF layoutRect = new RectangleF(num6 - 80, num4, 70f, 60f);
			RenderTarget.DrawText(text, textFormat, layoutRect, defaultForegroundBrush);
		}

		private void EnsureDirectXResources()
		{
			if (RenderTarget != null)
			{
				if (vpProfileBrush == null && profileColor != null)
				{
					System.Windows.Media.Color brushColor = GetBrushColor(profileColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
					vpProfileBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)brushColor.R / 255f, (float)(int)brushColor.G / 255f, (float)(int)brushColor.B / 255f, (float)profileOpacity / 100f));
				}
				if (vpValueAreaBrush == null && valueAreaColor != null)
				{
					System.Windows.Media.Color brushColor2 = GetBrushColor(valueAreaColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					vpValueAreaBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)brushColor2.R / 255f, (float)(int)brushColor2.G / 255f, (float)(int)brushColor2.B / 255f, (float)valueAreaOpacity / 100f));
				}
				if (vpPOCBrush == null && pocColor != null)
				{
					System.Windows.Media.Color brushColor3 = GetBrushColor(pocColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
					vpPOCBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)brushColor3.R / 255f, (float)(int)brushColor3.G / 255f, (float)(int)brushColor3.B / 255f, 1f));
				}
				if (deltaPositiveBrush == null && positiveColor != null)
				{
					System.Windows.Media.Color brushColor4 = GetBrushColor(positiveColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
					deltaPositiveBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)brushColor4.R / 255f, (float)(int)brushColor4.G / 255f, (float)(int)brushColor4.B / 255f, 1f));
				}
				if (deltaNegativeBrush == null && negativeColor != null)
				{
					System.Windows.Media.Color brushColor5 = GetBrushColor(negativeColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					deltaNegativeBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)brushColor5.R / 255f, (float)(int)brushColor5.G / 255f, (float)(int)brushColor5.B / 255f, 1f));
				}
				if (tempBrushDx == null)
				{
					tempBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.1765f, 0.1765f, 0.1765f, 1f));
				}
				if (textFormat == null)
				{
					textFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", 9f)
					{
						TextAlignment = TextAlignment.Leading,
						ParagraphAlignment = ParagraphAlignment.Center
					};
				}
				float num = ((ChartControl != null) ? ((float)ChartControl.BarWidth) : 5f);
				if (deltaTextFormat == null || Math.Abs(num - lastBarWidth) > 0.5f)
				{
					deltaTextFormat?.Dispose();
					lastBarWidth = num;
					float num2 = deltaTextSize;
					float num3 = Math.Max(0.9f, Math.Min(2.2f, num / 4f));
					float fontSize = num2 * num3;
					deltaTextFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", FontWeight.DemiBold, FontStyle.Normal, fontSize)
					{
						TextAlignment = TextAlignment.Trailing,
						ParagraphAlignment = ParagraphAlignment.Center
					};
				}
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}

		private void DisposeDx()
		{
			vpProfileBrush?.Dispose();
			vpProfileBrush = null;
			vpValueAreaBrush?.Dispose();
			vpValueAreaBrush = null;
			vpPOCBrush?.Dispose();
			vpPOCBrush = null;
			deltaPositiveBrush?.Dispose();
			deltaPositiveBrush = null;
			deltaNegativeBrush?.Dispose();
			deltaNegativeBrush = null;
			tempBrushDx?.Dispose();
			tempBrushDx = null;
			textFormat?.Dispose();
			textFormat = null;
			deltaTextFormat?.Dispose();
			deltaTextFormat = null;
			lastBarWidth = 0f;
		}
	}
}
