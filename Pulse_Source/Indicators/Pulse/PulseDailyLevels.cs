#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Xml.Serialization;
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
	public class PulseDailyLevels : Indicator
	{
		private class LevelInfo
		{
			public double Price { get; set; }

			public string Label { get; set; }

			public SharpDX.Direct2D1.SolidColorBrush Brush { get; set; }

			public string Category { get; set; }

			public int StartBarIndex { get; set; }
		}

		private double tickSize;

		private bool isPrimaryOneMinuteChart;

		private int minuteSeriesIndex = -1;

		private DateTime sessionDate = DateTime.MinValue;

		private bool sessionStarted;

		private bool levelsInitialized;

		private double sessionOpenPrice = double.NaN;

		private double onHigh = double.MinValue;

		private double onLow = double.MaxValue;

		private double pdHigh = double.MinValue;

		private double pdLow = double.MaxValue;

		private double pdClose = double.NaN;

		private DateTime pdCalculatedForDate = DateTime.MinValue;

		private double orHigh = double.MinValue;

		private double orLow = double.MaxValue;

		private DateTime orEndTime;

		private double ibHigh = double.MinValue;

		private double ibLow = double.MaxValue;

		private DateTime ibEndTime;

		private int rthStartBarIndex = -1;

		private int orEndBarIndex = -1;

		private int ibEndBarIndex = -1;

		private int ethStartBarIndex = -1;

		private System.Windows.Media.Brush overnightBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));

		private System.Windows.Media.Brush sessionOpenBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));

		private System.Windows.Media.Brush orBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));

		private System.Windows.Media.Brush ibBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));

		private System.Windows.Media.Brush previousDayBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));

		private SharpDX.Direct2D1.SolidColorBrush overnightBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush sessionOpenBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush orBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush ibBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush previousDayBrushDx;

		private int openingRangeMinutes = 5;

		private int initialBalanceMinutes = 60;

		private bool showOvernight = true;

		private bool showSessionOpen = true;

		private bool showOpeningRange = true;

		private bool showInitialBalance = true;

		private bool showPreviousDay = true;

		private int rightMarginPx = 280;

		private int levelTextSize = 14;

		private TextFormat textFormat;

		private List<LevelInfo> uniqueLevels = new List<LevelInfo>();

		[NinjaScriptProperty]
		[Range(1, 30)]
		[Display(Name = "Opening Range Minutes", Description = "DuraciÃ³n del Opening Range en minutos", Order = 1, GroupName = "ConfiguraciÃ³n")]
		public int OpeningRangeMinutes
		{
			get
			{
				return openingRangeMinutes;
			}
			set
			{
				openingRangeMinutes = Math.Max(1, Math.Min(30, value));
			}
		}

		[NinjaScriptProperty]
		[Range(30, 120)]
		[Display(Name = "Initial Balance Minutes", Description = "DuraciÃ³n del Initial Balance en minutos", Order = 2, GroupName = "ConfiguraciÃ³n")]
		public int InitialBalanceMinutes
		{
			get
			{
				return initialBalanceMinutes;
			}
			set
			{
				initialBalanceMinutes = Math.Max(30, Math.Min(120, value));
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Show Overnight", Description = "Mostrar niveles Overnight", Order = 3, GroupName = "Visibilidad")]
		public bool ShowOvernight
		{
			get
			{
				return showOvernight;
			}
			set
			{
				showOvernight = value;
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Show Session Open", Description = "Mostrar precio de apertura de sesiÃ³n", Order = 4, GroupName = "Visibilidad")]
		public bool ShowSessionOpen
		{
			get
			{
				return showSessionOpen;
			}
			set
			{
				showSessionOpen = value;
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Show Opening Range", Description = "Mostrar niveles Opening Range", Order = 5, GroupName = "Visibilidad")]
		public bool ShowOpeningRange
		{
			get
			{
				return showOpeningRange;
			}
			set
			{
				showOpeningRange = value;
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Show Initial Balance", Description = "Mostrar niveles Initial Balance", Order = 6, GroupName = "Visibilidad")]
		public bool ShowInitialBalance
		{
			get
			{
				return showInitialBalance;
			}
			set
			{
				showInitialBalance = value;
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Show Previous Day", Description = "Mostrar niveles del dÃ\u00ada anterior", Order = 7, GroupName = "Visibilidad")]
		public bool ShowPreviousDay
		{
			get
			{
				return showPreviousDay;
			}
			set
			{
				showPreviousDay = value;
			}
		}

		[NinjaScriptProperty]
		[Range(8, 40)]
		[Display(Name = "Level Text Size", Description = "TamaÃ±o del texto de niveles", Order = 8, GroupName = "Visual")]
		public int LevelTextSize
		{
			get
			{
				return levelTextSize;
			}
			set
			{
				levelTextSize = Math.Max(8, Math.Min(40, value));
				DisposeDxResources();
			}
		}

		[NinjaScriptProperty]
		[Range(20, 100)]
		[Display(Name = "Right Margin (px)", Description = "Margen derecho en pÃ\u00adxeles para evitar que las lÃ\u00adneas pisen los labels", Order = 9, GroupName = "Visual")]
		public int RightMarginPx
		{
			get
			{
				return rightMarginPx;
			}
			set
			{
				rightMarginPx = Math.Max(20, Math.Min(100, value));
			}
		}

		[XmlIgnore]
		[Display(Name = "Overnight Color", Description = "Color para niveles Overnight", Order = 1, GroupName = "Colores")]
		public System.Windows.Media.Brush OvernightBrush
		{
			get
			{
				return overnightBrush;
			}
			set
			{
				overnightBrush = value;
				DisposeDxResources();
			}
		}

		[Browsable(false)]
		public string OvernightBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(overnightBrush);
			}
			set
			{
				overnightBrush = Serialize.StringToBrush(value);
				DisposeDxResources();
			}
		}

		[XmlIgnore]
		[Display(Name = "Session Open Color", Description = "Color para Session Open", Order = 2, GroupName = "Colores")]
		public System.Windows.Media.Brush SessionOpenBrush
		{
			get
			{
				return sessionOpenBrush;
			}
			set
			{
				sessionOpenBrush = value;
				DisposeDxResources();
			}
		}

		[Browsable(false)]
		public string SessionOpenBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(sessionOpenBrush);
			}
			set
			{
				sessionOpenBrush = Serialize.StringToBrush(value);
				DisposeDxResources();
			}
		}

		[XmlIgnore]
		[Display(Name = "Opening Range Color", Description = "Color para Opening Range", Order = 3, GroupName = "Colores")]
		public System.Windows.Media.Brush OrBrush
		{
			get
			{
				return orBrush;
			}
			set
			{
				orBrush = value;
				DisposeDxResources();
			}
		}

		[Browsable(false)]
		public string OrBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(orBrush);
			}
			set
			{
				orBrush = Serialize.StringToBrush(value);
				DisposeDxResources();
			}
		}

		[XmlIgnore]
		[Display(Name = "Initial Balance Color", Description = "Color para Initial Balance", Order = 4, GroupName = "Colores")]
		public System.Windows.Media.Brush IbBrush
		{
			get
			{
				return ibBrush;
			}
			set
			{
				ibBrush = value;
				DisposeDxResources();
			}
		}

		[Browsable(false)]
		public string IbBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(ibBrush);
			}
			set
			{
				ibBrush = Serialize.StringToBrush(value);
				DisposeDxResources();
			}
		}

		[XmlIgnore]
		[Display(Name = "Previous Day Color", Description = "Color para Previous Day", Order = 5, GroupName = "Colores")]
		public System.Windows.Media.Brush PreviousDayBrush
		{
			get
			{
				return previousDayBrush;
			}
			set
			{
				previousDayBrush = value;
				DisposeDxResources();
			}
		}

		[Browsable(false)]
		public string PreviousDayBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(previousDayBrush);
			}
			set
			{
				previousDayBrush = Serialize.StringToBrush(value);
				DisposeDxResources();
			}
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
				Description = "Pulse Daily Levels - Professional trading levels: Overnight, Opening Range, Initial Balance, Session Open, Previous Day H/L/C";
				Name = "Pulse Daily Levels";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				openingRangeMinutes = 5;
				initialBalanceMinutes = 60;
				showOvernight = true;
				showSessionOpen = true;
				showOpeningRange = true;
				showInitialBalance = true;
				showPreviousDay = true;
				rightMarginPx = 120;
				levelTextSize = 14;
				Print("Pulse Daily Levels: Professional trading levels initialized - Pulse Suite");
			}
			else if (State == State.Configure)
			{
				isPrimaryOneMinuteChart = Bars.BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && Bars.BarsPeriod.Value == 1;
				if (!isPrimaryOneMinuteChart)
				{
					AddDataSeries(BarsPeriodType.Minute, 1);
					minuteSeriesIndex = 1;
				}
				else
				{
					minuteSeriesIndex = 0;
				}
			}
			else if (State == State.DataLoaded)
			{
				if (minuteSeriesIndex < 0)
				{
					minuteSeriesIndex = 0;
				}
				tickSize = Instrument.MasterInstrument.TickSize;
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0)
			{
				return;
			}
			DateTime dateTime = Times[0][0];
			if (sessionDate != DateTime.MinValue && dateTime.Date != sessionDate)
			{
				sessionStarted = false;
				levelsInitialized = false;
				sessionDate = dateTime.Date;
				TimeSpan timeSpan = new TimeSpan(9, 30, 0);
				if (dateTime.TimeOfDay > timeSpan && dateTime.Hour < 18 && CurrentBar >= 100)
				{
					sessionStarted = true;
					levelsInitialized = true;
					sessionOpenPrice = GetSessionOpenFromPrecisionSeries(dateTime.Date, Open[0]);
					ComputeOvernightRange();
					ComputePreviousDayRange();
					orEndTime = dateTime;
					ibEndTime = dateTime;
					orHigh = double.MinValue;
					orLow = double.MaxValue;
					ibHigh = double.MinValue;
					ibLow = double.MaxValue;
				}
			}
			if (Bars.IsFirstBarOfSession)
			{
				if (dateTime.Hour == 18 && CurrentBar >= 100)
				{
					levelsInitialized = true;
					ComputeOvernightRange();
					ComputePreviousDayRange();
				}
				sessionDate = Times[0][0].Date;
				sessionStarted = false;
				if (dateTime.Hour < 18)
				{
					levelsInitialized = false;
				}
				sessionOpenPrice = double.NaN;
				orHigh = double.MinValue;
				orLow = double.MaxValue;
				ibHigh = double.MinValue;
				ibLow = double.MaxValue;
				orEndTime = DateTime.MinValue;
				ibEndTime = DateTime.MinValue;
				rthStartBarIndex = -1;
				orEndBarIndex = -1;
				ibEndBarIndex = -1;
				ethStartBarIndex = -1;
			}
			TimeSpan timeSpan2 = new TimeSpan(9, 30, 0);
			TimeSpan timeOfDay = dateTime.TimeOfDay;
			bool flag = timeOfDay > timeSpan2 && dateTime.Hour < 18;
			int num;
			int num2;
			if (CurrentBar >= 100)
			{
				num = ((pdCalculatedForDate != dateTime.Date) ? 1 : 0);
				if (num != 0)
				{
					num2 = ((!flag) ? 1 : 0);
					goto IL_025b;
				}
			}
			else
			{
				num = 0;
			}
			num2 = 0;
			goto IL_025b;
			IL_025b:
			bool flag2 = (byte)num2 != 0;
			if (num != 0)
			{
				if (flag2)
				{
					sessionDate = dateTime.Date;
					ComputePreviousDayRange();
				}
				else
				{
					sessionStarted = true;
					levelsInitialized = true;
					sessionDate = dateTime.Date;
					sessionOpenPrice = GetSessionOpenFromPrecisionSeries(sessionDate, Open[0]);
					ComputeOvernightRange();
					ComputePreviousDayRange();
					orEndTime = dateTime;
					ibEndTime = dateTime;
				}
			}
			bool num3 = !sessionStarted && CurrentBar >= 100 && flag;
			if (CurrentBar >= 100 && dateTime.Hour >= 18 && !levelsInitialized)
			{
				sessionStarted = true;
				levelsInitialized = true;
				ComputeOvernightRange();
				ComputePreviousDayRange();
			}
			if (num3)
			{
				sessionStarted = true;
				levelsInitialized = true;
				sessionDate = Times[0][0].Date;
				sessionOpenPrice = GetSessionOpenFromPrecisionSeries(sessionDate, Open[0]);
				rthStartBarIndex = CurrentBar;
				ComputeOvernightRange();
				ComputePreviousDayRange();
				ComputeOpeningRange();
				ComputeInitialBalance();
			}
			if (sessionStarted)
			{
				DateTime dateTime2 = dateTime.Date.Add(new TimeSpan(9, 30, 0)).AddMinutes(openingRangeMinutes);
				DateTime dateTime3 = dateTime.Date.Add(new TimeSpan(9, 30, 0)).AddMinutes(initialBalanceMinutes);
				if (dateTime <= dateTime2)
				{
					ComputeOpeningRange();
				}
				if (dateTime <= dateTime3)
				{
					ComputeInitialBalance();
				}
				if (orEndBarIndex < 0 && dateTime >= dateTime2)
				{
					orEndBarIndex = CurrentBar;
				}
				if (ibEndBarIndex < 0 && dateTime >= dateTime3)
				{
					ibEndBarIndex = CurrentBar;
				}
			}
			if (timeOfDay > timeSpan2 && dateTime.Hour < 18 && rthStartBarIndex < 0)
			{
				rthStartBarIndex = CurrentBar;
			}
			if (dateTime.Hour >= 18 && ethStartBarIndex < 0)
			{
				ethStartBarIndex = CurrentBar;
			}
		}

		private int GetPrecisionSeriesIndex()
		{
			if (minuteSeriesIndex >= 0 && CurrentBars != null && CurrentBars.Length > minuteSeriesIndex && CurrentBars[minuteSeriesIndex] >= 0)
			{
				return minuteSeriesIndex;
			}
			return 0;
		}

		private int GetPrecisionBarsCount()
		{
			int precisionSeriesIndex = GetPrecisionSeriesIndex();
			if (CurrentBars == null || CurrentBars.Length <= precisionSeriesIndex)
			{
				return -1;
			}
			return CurrentBars[precisionSeriesIndex];
		}

		private double GetSessionOpenFromPrecisionSeries(DateTime sessionDay, double fallbackOpen)
		{
			int precisionSeriesIndex = GetPrecisionSeriesIndex();
			int precisionBarsCount = GetPrecisionBarsCount();
			if (precisionBarsCount < 0)
			{
				return fallbackOpen;
			}
			DateTime dateTime = sessionDay.Date.Add(new TimeSpan(9, 30, 0));
			DateTime dateTime2 = dateTime.AddMinutes(1.0);
			for (int i = 0; i <= precisionBarsCount && i < 3000; i++)
			{
				DateTime dateTime3 = Times[precisionSeriesIndex][i];
				if (dateTime3 < dateTime.AddHours(-12.0))
				{
					break;
				}
				if (dateTime3 > dateTime && dateTime3 <= dateTime2)
				{
					return Opens[precisionSeriesIndex][i];
				}
			}
			return fallbackOpen;
		}

		private void ComputeOvernightRange()
		{
			onHigh = double.MinValue;
			onLow = double.MaxValue;
			DateTime dateTime = Times[0][0];
			int num = 0;
			int precisionSeriesIndex = GetPrecisionSeriesIndex();
			int precisionBarsCount = GetPrecisionBarsCount();
			if (precisionBarsCount < 0)
			{
				onHigh = double.NaN;
				onLow = double.NaN;
				return;
			}
			DateTime dateTime2 = GetPreviousTradingDay(dateTime.Date).Add(new TimeSpan(18, 0, 0));
			DateTime dateTime3 = dateTime.Date.Add(new TimeSpan(9, 30, 0));
			for (int i = 0; i <= precisionBarsCount; i++)
			{
				if (num >= 6000)
				{
					break;
				}
				DateTime dateTime4 = Times[precisionSeriesIndex][i];
				if (dateTime4 < dateTime2.AddDays(-1.0))
				{
					break;
				}
				if (dateTime4 >= dateTime2 && dateTime4 <= dateTime3)
				{
					if (Highs[precisionSeriesIndex][i] > onHigh)
					{
						onHigh = Highs[precisionSeriesIndex][i];
					}
					if (Lows[precisionSeriesIndex][i] < onLow)
					{
						onLow = Lows[precisionSeriesIndex][i];
					}
					num++;
				}
			}
			if (onHigh == double.MinValue)
			{
				onHigh = double.NaN;
			}
			if (onLow == double.MaxValue)
			{
				onLow = double.NaN;
			}
		}

		private void ComputeOpeningRange()
		{
			orHigh = double.MinValue;
			orLow = double.MaxValue;
			DateTime dateTime = Times[0][0];
			int num = 0;
			int precisionSeriesIndex = GetPrecisionSeriesIndex();
			int precisionBarsCount = GetPrecisionBarsCount();
			if (precisionBarsCount < 0)
			{
				orHigh = double.NaN;
				orLow = double.NaN;
				return;
			}
			DateTime dateTime2 = dateTime.Date.Add(new TimeSpan(9, 30, 0));
			DateTime dateTime3 = dateTime2.AddMinutes(openingRangeMinutes);
			for (int i = 0; i <= precisionBarsCount; i++)
			{
				if (num >= 6000)
				{
					break;
				}
				DateTime dateTime4 = Times[precisionSeriesIndex][i];
				if (dateTime4 < dateTime2.AddHours(-12.0))
				{
					break;
				}
				if (dateTime4 > dateTime2 && dateTime4 <= dateTime3)
				{
					if (Highs[precisionSeriesIndex][i] > orHigh)
					{
						orHigh = Highs[precisionSeriesIndex][i];
					}
					if (Lows[precisionSeriesIndex][i] < orLow)
					{
						orLow = Lows[precisionSeriesIndex][i];
					}
					num++;
				}
			}
			if (orHigh == double.MinValue)
			{
				orHigh = double.NaN;
			}
			if (orLow == double.MaxValue)
			{
				orLow = double.NaN;
			}
		}

		private void ComputeInitialBalance()
		{
			ibHigh = double.MinValue;
			ibLow = double.MaxValue;
			DateTime dateTime = Times[0][0];
			int num = 0;
			int precisionSeriesIndex = GetPrecisionSeriesIndex();
			int precisionBarsCount = GetPrecisionBarsCount();
			if (precisionBarsCount < 0)
			{
				ibHigh = double.NaN;
				ibLow = double.NaN;
				return;
			}
			DateTime dateTime2 = dateTime.Date.Add(new TimeSpan(9, 30, 0));
			DateTime dateTime3 = dateTime2.AddMinutes(initialBalanceMinutes);
			for (int i = 0; i <= precisionBarsCount; i++)
			{
				if (num >= 6000)
				{
					break;
				}
				DateTime dateTime4 = Times[precisionSeriesIndex][i];
				if (dateTime4 < dateTime2.AddHours(-12.0))
				{
					break;
				}
				if (dateTime4 > dateTime2 && dateTime4 <= dateTime3)
				{
					if (Highs[precisionSeriesIndex][i] > ibHigh)
					{
						ibHigh = Highs[precisionSeriesIndex][i];
					}
					if (Lows[precisionSeriesIndex][i] < ibLow)
					{
						ibLow = Lows[precisionSeriesIndex][i];
					}
					num++;
				}
			}
			if (ibHigh == double.MinValue)
			{
				ibHigh = double.NaN;
			}
			if (ibLow == double.MaxValue)
			{
				ibLow = double.NaN;
			}
		}

		private DateTime GetPreviousTradingDay(DateTime currentDate)
		{
			DateTime result = currentDate.AddDays(-1.0);
			if (currentDate.DayOfWeek == DayOfWeek.Monday)
			{
				result = currentDate.AddDays(-3.0);
			}
			else if (currentDate.DayOfWeek == DayOfWeek.Sunday)
			{
				result = currentDate.AddDays(-2.0);
			}
			else if (currentDate.DayOfWeek == DayOfWeek.Saturday)
			{
				result = currentDate.AddDays(-1.0);
			}
			return result;
		}

		private void ComputePreviousDayRange()
		{
			pdHigh = double.MinValue;
			pdLow = double.MaxValue;
			pdClose = double.NaN;
			DateTime dateTime = Times[0][0];
			int precisionSeriesIndex = GetPrecisionSeriesIndex();
			int precisionBarsCount = GetPrecisionBarsCount();
			if (precisionBarsCount < 0)
			{
				pdHigh = double.NaN;
				pdLow = double.NaN;
				pdClose = double.NaN;
				pdCalculatedForDate = dateTime.Date;
				return;
			}
			DateTime previousTradingDay = GetPreviousTradingDay(dateTime.Date);
			DateTime dateTime2 = new DateTime(previousTradingDay.Year, previousTradingDay.Month, previousTradingDay.Day, 9, 30, 0);
			DateTime dateTime3 = new DateTime(previousTradingDay.Year, previousTradingDay.Month, previousTradingDay.Day, 16, 0, 0);
			int i = 0;
			int num = 0;
			double d = double.NaN;
			for (; i <= precisionBarsCount; i++)
			{
				if (num >= 6000)
				{
					break;
				}
				DateTime dateTime4 = Times[precisionSeriesIndex][i];
				if (dateTime4.Date < previousTradingDay)
				{
					break;
				}
				if (dateTime4 > dateTime2 && dateTime4 <= dateTime3)
				{
					if (Highs[precisionSeriesIndex][i] > pdHigh)
					{
						pdHigh = Highs[precisionSeriesIndex][i];
					}
					if (Lows[precisionSeriesIndex][i] < pdLow)
					{
						pdLow = Lows[precisionSeriesIndex][i];
					}
					if (dateTime4.Hour == 16 && dateTime4.Minute == 0)
					{
						d = Closes[precisionSeriesIndex][i];
					}
					else if (dateTime4.Hour == 15 && dateTime4.Minute >= 55 && double.IsNaN(d))
					{
						d = Closes[precisionSeriesIndex][i];
					}
					num++;
				}
			}
			pdClose = d;
			pdCalculatedForDate = dateTime.Date;
			if (pdHigh == double.MinValue)
			{
				pdHigh = double.NaN;
			}
			if (pdLow == double.MaxValue)
			{
				pdLow = double.NaN;
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				if (chartControl != null && chartScale != null && RenderTarget != null)
				{
					EnsureDxResources();
					RenderPulseLevels(chartControl, chartScale);
				}
			}
			catch (Exception ex)
			{
				Print("[ERROR] PulseDailyLevels: OnRender failed - " + ex.Message);
			}
		}

		private void RenderPulseLevels(ChartControl chartControl, ChartScale chartScale)
		{
			uniqueLevels.Clear();
			DateTime dateTime = Times[0][0];
			int primaryCurrentBarIndex = GetPrimaryCurrentBarIndex();
			bool flag = dateTime.Hour >= 18 || dateTime.Hour < 17;
			int startBarIndex = ((rthStartBarIndex >= 0) ? rthStartBarIndex : primaryCurrentBarIndex);
			int startBarIndex2 = ((orEndBarIndex >= 0) ? orEndBarIndex : primaryCurrentBarIndex);
			int startBarIndex3 = ((ibEndBarIndex >= 0) ? ibEndBarIndex : primaryCurrentBarIndex);
			int startBarIndex4 = ((ethStartBarIndex >= 0) ? ethStartBarIndex : primaryCurrentBarIndex);
			if (showOvernight && overnightBrushDx != null && flag)
			{
				if (!double.IsNaN(onHigh))
				{
					AddUniqueLevel(onHigh, "ONH", overnightBrushDx, "Overnight", startBarIndex);
				}
				if (!double.IsNaN(onLow))
				{
					AddUniqueLevel(onLow, "ONL", overnightBrushDx, "Overnight", startBarIndex);
				}
			}
			if (showSessionOpen && sessionOpenBrushDx != null && sessionStarted && !double.IsNaN(sessionOpenPrice))
			{
				AddUniqueLevel(sessionOpenPrice, "OPEN", sessionOpenBrushDx, "SessionOpen", startBarIndex);
			}
			if (showOpeningRange && orBrushDx != null && sessionStarted && orHigh > 0.0 && orHigh != double.MinValue && orLow > 0.0 && orLow != double.MaxValue)
			{
				AddUniqueLevel(orHigh, "ORH", orBrushDx, "OR", startBarIndex2);
				AddUniqueLevel(orLow, "ORL", orBrushDx, "OR", startBarIndex2);
			}
			if (showInitialBalance && ibBrushDx != null && sessionStarted && ibHigh > 0.0 && ibHigh != double.MinValue && ibLow > 0.0 && ibLow != double.MaxValue)
			{
				AddUniqueLevel(ibHigh, "IBH", ibBrushDx, "IB", startBarIndex3);
				AddUniqueLevel(ibLow, "IBL", ibBrushDx, "IB", startBarIndex3);
			}
			if (showPreviousDay && previousDayBrushDx != null && flag)
			{
				if (!double.IsNaN(pdHigh))
				{
					AddUniqueLevel(pdHigh, "PDH", previousDayBrushDx, "PreviousDay", startBarIndex4);
				}
				if (!double.IsNaN(pdLow))
				{
					AddUniqueLevel(pdLow, "PDL", previousDayBrushDx, "PreviousDay", startBarIndex4);
				}
				if (!double.IsNaN(pdClose))
				{
					AddUniqueLevel(pdClose, "PDC", previousDayBrushDx, "PreviousDay", startBarIndex4);
				}
			}
			foreach (LevelInfo uniqueLevel in uniqueLevels)
			{
				DrawLevel(chartControl, chartScale, uniqueLevel.Price, uniqueLevel.Label, uniqueLevel.Brush, uniqueLevel.StartBarIndex, primaryCurrentBarIndex);
			}
			DrawPulseWatermark(chartControl);
		}

		private void AddUniqueLevel(double price, string label, SharpDX.Direct2D1.SolidColorBrush brush, string category, int startBarIndex)
		{
			if (!double.IsNaN(price) && !double.IsInfinity(price) && !(price <= 0.0) && price != double.MinValue && price != double.MaxValue && uniqueLevels.FirstOrDefault((LevelInfo l) => Math.Abs(l.Price - price) < tickSize * 0.1) == null)
			{
				uniqueLevels.Add(new LevelInfo
				{
					Price = price,
					Label = label,
					Brush = brush,
					Category = category,
					StartBarIndex = startBarIndex
				});
			}
		}

		private int GetPrimaryCurrentBarIndex()
		{
			if (CurrentBars != null && CurrentBars.Length != 0 && CurrentBars[0] >= 0)
			{
				return CurrentBars[0];
			}
			if (ChartBars != null)
			{
				return Math.Max(0, ChartBars.ToIndex);
			}
			return Math.Max(0, CurrentBar);
		}

		private void DrawLevel(ChartControl chartControl, ChartScale chartScale, double price, string label, SharpDX.Direct2D1.SolidColorBrush brush, int startBarIndex, int primaryCurrentBar)
		{
			if (brush == null || double.IsNaN(price))
			{
				return;
			}
			try
			{
				float num = chartScale.GetYByValue(price);
				startBarIndex = Math.Max(0, Math.Min(startBarIndex, primaryCurrentBar));
				float val = chartControl.GetXByBarIndex(ChartBars, startBarIndex);
				float val2 = chartControl.GetXByBarIndex(barIndex: ChartBars.FromIndex, chartBars: ChartBars);
				float num2 = Math.Max(val, val2);
				float num3 = (float)chartControl.GetXByBarIndex(ChartBars, primaryCurrentBar) + 70f - 60f;
				float x = Math.Max(num2 + 1f, num3 - 5f);
				if (!float.IsNaN(num) && !float.IsInfinity(num))
				{
					Vector2 point = new Vector2(num2, num);
					Vector2 point2 = new Vector2(x, num);
					RenderTarget.DrawLine(point, point2, brush, 1.5f);
					if (textFormat != null)
					{
						float width = (label.Contains("+") ? 120f : 80f);
						RectangleF layoutRect = new RectangleF(num3, num - 10f, width, 20f);
						RenderTarget.DrawText(label, textFormat, layoutRect, brush);
					}
				}
			}
			catch (Exception ex)
			{
				Print($"PulseDailyLevels: Error drawing level {label} at {price:F2} - {ex.Message}");
			}
		}

		private void DrawPulseWatermark(ChartControl chartControl)
		{
			try
			{
				if (textFormat != null)
				{
					SharpDX.Direct2D1.SolidColorBrush solidColorBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.1765f, 0.1765f, 0.1765f, 0.3f));
					float x = ChartPanel.X + ChartPanel.W - 120;
					float y = ChartPanel.Y + ChartPanel.H - 25;
					RectangleF layoutRect = new RectangleF(x, y, 115f, 20f);
					RenderTarget.DrawText("Pulse Suite", textFormat, layoutRect, solidColorBrush);
					solidColorBrush?.Dispose();
				}
			}
			catch (Exception ex)
			{
				Print("PulseDailyLevels: Error drawing watermark - " + ex.Message);
			}
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null)
			{
				return;
			}
			try
			{
				if (textFormat == null)
				{
					textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", Math.Max(8f, levelTextSize))
					{
						TextAlignment = TextAlignment.Leading,
						ParagraphAlignment = ParagraphAlignment.Center
					};
				}
				CreateDxBrush(ref overnightBrushDx, overnightBrush);
				CreateDxBrush(ref sessionOpenBrushDx, sessionOpenBrush);
				CreateDxBrush(ref orBrushDx, orBrush);
				CreateDxBrush(ref ibBrushDx, ibBrush);
				CreateDxBrush(ref previousDayBrushDx, previousDayBrush);
			}
			catch (Exception ex)
			{
				Print("PulseDailyLevels: Error creating DX resources - " + ex.Message);
			}
		}

		private void CreateDxBrush(ref SharpDX.Direct2D1.SolidColorBrush dxBrush, System.Windows.Media.Brush mediaBrush)
		{
			if (dxBrush == null && mediaBrush != null && mediaBrush is System.Windows.Media.SolidColorBrush { Color: var color })
			{
				dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, 0.8f));
			}
		}

		private void DisposeDxResources()
		{
			try
			{
				textFormat?.Dispose();
				textFormat = null;
				overnightBrushDx?.Dispose();
				overnightBrushDx = null;
				sessionOpenBrushDx?.Dispose();
				sessionOpenBrushDx = null;
				orBrushDx?.Dispose();
				orBrushDx = null;
				ibBrushDx?.Dispose();
				ibBrushDx = null;
				previousDayBrushDx?.Dispose();
				previousDayBrushDx = null;
			}
			catch (Exception ex)
			{
				Print("PulseDailyLevels: Error disposing DX resources - " + ex.Message);
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}
	}
}
