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
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators.Pulse;

public class PulseDailyLevels : Indicator
{
	private class LevelInfo
	{
		public double Price { get; set; }

		public string Label { get; set; }

		public SolidColorBrush Brush { get; set; }

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

	private Brush overnightBrush = (Brush)(object)Brushes.IndianRed;

	private Brush sessionOpenBrush = (Brush)(object)Brushes.WhiteSmoke;

	private Brush orBrush = (Brush)(object)Brushes.DodgerBlue;

	private Brush ibBrush = (Brush)(object)Brushes.DodgerBlue;

	private Brush previousDayBrush = (Brush)(object)Brushes.IndianRed;

	private SolidColorBrush overnightBrushDx;

	private SolidColorBrush sessionOpenBrushDx;

	private SolidColorBrush orBrushDx;

	private SolidColorBrush ibBrushDx;

	private SolidColorBrush previousDayBrushDx;

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
	public Brush OvernightBrush
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
	public Brush SessionOpenBrush
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
	public Brush OrBrush
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
	public Brush IbBrush
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
	public Brush PreviousDayBrush
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

	public PulseDailyLevels()
	{
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Invalid comparison between Unknown and I4
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Invalid comparison between Unknown and I4
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			((NinjaScript)this).Description = "Pulse Daily Levels - Professional trading levels: Overnight, Opening Range, Initial Balance, Session Open, Previous Day H/L/C";
			((NinjaScriptBase)this).Name = "Pulse Daily Levels";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			openingRangeMinutes = 5;
			initialBalanceMinutes = 60;
			showOvernight = true;
			showSessionOpen = true;
			showOpeningRange = true;
			showInitialBalance = true;
			showPreviousDay = true;
			rightMarginPx = 120;
			levelTextSize = 14;
			((NinjaScript)this).Print((object)"Pulse Daily Levels: Professional trading levels initialized - Pulse Suite");
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			isPrimaryOneMinuteChart = (int)((NinjaScriptBase)this).Bars.BarsPeriod.BarsPeriodType == 4 && ((NinjaScriptBase)this).Bars.BarsPeriod.Value == 1;
			if (!isPrimaryOneMinuteChart)
			{
				((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)4, 1);
				minuteSeriesIndex = 1;
			}
			else
			{
				minuteSeriesIndex = 0;
			}
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			if (minuteSeriesIndex < 0)
			{
				minuteSeriesIndex = 0;
			}
			tickSize = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsInProgress != 0)
		{
			return;
		}
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
		if (sessionDate != DateTime.MinValue && dateTime.Date != sessionDate)
		{
			sessionStarted = false;
			levelsInitialized = false;
			sessionDate = dateTime.Date;
			TimeSpan timeSpan = new TimeSpan(9, 30, 0);
			if (dateTime.TimeOfDay > timeSpan && dateTime.Hour < 18 && ((NinjaScriptBase)this).CurrentBar >= 100)
			{
				sessionStarted = true;
				levelsInitialized = true;
				sessionOpenPrice = GetSessionOpenFromPrecisionSeries(dateTime.Date, ((NinjaScriptBase)this).Open[0]);
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
		if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
		{
			if (dateTime.Hour == 18 && ((NinjaScriptBase)this).CurrentBar >= 100)
			{
				levelsInitialized = true;
				ComputeOvernightRange();
				ComputePreviousDayRange();
			}
			sessionDate = ((NinjaScriptBase)this).Times[0][0].Date;
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
		if (((NinjaScriptBase)this).CurrentBar >= 100)
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
				sessionOpenPrice = GetSessionOpenFromPrecisionSeries(sessionDate, ((NinjaScriptBase)this).Open[0]);
				ComputeOvernightRange();
				ComputePreviousDayRange();
				orEndTime = dateTime;
				ibEndTime = dateTime;
			}
		}
		bool num3 = !sessionStarted && ((NinjaScriptBase)this).CurrentBar >= 100 && flag;
		if (((NinjaScriptBase)this).CurrentBar >= 100 && dateTime.Hour >= 18 && !levelsInitialized)
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
			sessionDate = ((NinjaScriptBase)this).Times[0][0].Date;
			sessionOpenPrice = GetSessionOpenFromPrecisionSeries(sessionDate, ((NinjaScriptBase)this).Open[0]);
			rthStartBarIndex = ((NinjaScriptBase)this).CurrentBar;
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
				orEndBarIndex = ((NinjaScriptBase)this).CurrentBar;
			}
			if (ibEndBarIndex < 0 && dateTime >= dateTime3)
			{
				ibEndBarIndex = ((NinjaScriptBase)this).CurrentBar;
			}
		}
		if (timeOfDay > timeSpan2 && dateTime.Hour < 18 && rthStartBarIndex < 0)
		{
			rthStartBarIndex = ((NinjaScriptBase)this).CurrentBar;
		}
		if (dateTime.Hour >= 18 && ethStartBarIndex < 0)
		{
			ethStartBarIndex = ((NinjaScriptBase)this).CurrentBar;
		}
	}

	private int GetPrecisionSeriesIndex()
	{
		if (minuteSeriesIndex >= 0 && ((NinjaScriptBase)this).CurrentBars != null && ((NinjaScriptBase)this).CurrentBars.Length > minuteSeriesIndex && ((NinjaScriptBase)this).CurrentBars[minuteSeriesIndex] >= 0)
		{
			return minuteSeriesIndex;
		}
		return 0;
	}

	private int GetPrecisionBarsCount()
	{
		int precisionSeriesIndex = GetPrecisionSeriesIndex();
		if (((NinjaScriptBase)this).CurrentBars == null || ((NinjaScriptBase)this).CurrentBars.Length <= precisionSeriesIndex)
		{
			return -1;
		}
		return ((NinjaScriptBase)this).CurrentBars[precisionSeriesIndex];
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
			DateTime dateTime3 = ((NinjaScriptBase)this).Times[precisionSeriesIndex][i];
			if (dateTime3 < dateTime.AddHours(-12.0))
			{
				break;
			}
			if (dateTime3 > dateTime && dateTime3 <= dateTime2)
			{
				return ((NinjaScriptBase)this).Opens[precisionSeriesIndex][i];
			}
		}
		return fallbackOpen;
	}

	private void ComputeOvernightRange()
	{
		onHigh = double.MinValue;
		onLow = double.MaxValue;
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
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
			DateTime dateTime4 = ((NinjaScriptBase)this).Times[precisionSeriesIndex][i];
			if (dateTime4 < dateTime2.AddDays(-1.0))
			{
				break;
			}
			if (dateTime4 >= dateTime2 && dateTime4 <= dateTime3)
			{
				if (((NinjaScriptBase)this).Highs[precisionSeriesIndex][i] > onHigh)
				{
					onHigh = ((NinjaScriptBase)this).Highs[precisionSeriesIndex][i];
				}
				if (((NinjaScriptBase)this).Lows[precisionSeriesIndex][i] < onLow)
				{
					onLow = ((NinjaScriptBase)this).Lows[precisionSeriesIndex][i];
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
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
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
			DateTime dateTime4 = ((NinjaScriptBase)this).Times[precisionSeriesIndex][i];
			if (dateTime4 < dateTime2.AddHours(-12.0))
			{
				break;
			}
			if (dateTime4 > dateTime2 && dateTime4 <= dateTime3)
			{
				if (((NinjaScriptBase)this).Highs[precisionSeriesIndex][i] > orHigh)
				{
					orHigh = ((NinjaScriptBase)this).Highs[precisionSeriesIndex][i];
				}
				if (((NinjaScriptBase)this).Lows[precisionSeriesIndex][i] < orLow)
				{
					orLow = ((NinjaScriptBase)this).Lows[precisionSeriesIndex][i];
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
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
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
			DateTime dateTime4 = ((NinjaScriptBase)this).Times[precisionSeriesIndex][i];
			if (dateTime4 < dateTime2.AddHours(-12.0))
			{
				break;
			}
			if (dateTime4 > dateTime2 && dateTime4 <= dateTime3)
			{
				if (((NinjaScriptBase)this).Highs[precisionSeriesIndex][i] > ibHigh)
				{
					ibHigh = ((NinjaScriptBase)this).Highs[precisionSeriesIndex][i];
				}
				if (((NinjaScriptBase)this).Lows[precisionSeriesIndex][i] < ibLow)
				{
					ibLow = ((NinjaScriptBase)this).Lows[precisionSeriesIndex][i];
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
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
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
			DateTime dateTime4 = ((NinjaScriptBase)this).Times[precisionSeriesIndex][i];
			if (dateTime4.Date < previousTradingDay)
			{
				break;
			}
			if (dateTime4 > dateTime2 && dateTime4 <= dateTime3)
			{
				if (((NinjaScriptBase)this).Highs[precisionSeriesIndex][i] > pdHigh)
				{
					pdHigh = ((NinjaScriptBase)this).Highs[precisionSeriesIndex][i];
				}
				if (((NinjaScriptBase)this).Lows[precisionSeriesIndex][i] < pdLow)
				{
					pdLow = ((NinjaScriptBase)this).Lows[precisionSeriesIndex][i];
				}
				if (dateTime4.Hour == 16 && dateTime4.Minute == 0)
				{
					d = ((NinjaScriptBase)this).Closes[precisionSeriesIndex][i];
				}
				else if (dateTime4.Hour == 15 && dateTime4.Minute >= 55 && double.IsNaN(d))
				{
					d = ((NinjaScriptBase)this).Closes[precisionSeriesIndex][i];
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
			if (chartControl != null && chartScale != null && ((IndicatorRenderBase)this).RenderTarget != null)
			{
				EnsureDxResources();
				RenderPulseLevels(chartControl, chartScale);
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("[ERROR] PulseDailyLevels: OnRender failed - " + ex.Message));
		}
	}

	private void RenderPulseLevels(ChartControl chartControl, ChartScale chartScale)
	{
		uniqueLevels.Clear();
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
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

	private void AddUniqueLevel(double price, string label, SolidColorBrush brush, string category, int startBarIndex)
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
		if (((NinjaScriptBase)this).CurrentBars != null && ((NinjaScriptBase)this).CurrentBars.Length != 0 && ((NinjaScriptBase)this).CurrentBars[0] >= 0)
		{
			return ((NinjaScriptBase)this).CurrentBars[0];
		}
		if (((IndicatorRenderBase)this).ChartBars != null)
		{
			return Math.Max(0, ((IndicatorRenderBase)this).ChartBars.ToIndex);
		}
		return Math.Max(0, ((NinjaScriptBase)this).CurrentBar);
	}

	private void DrawLevel(ChartControl chartControl, ChartScale chartScale, double price, string label, SolidColorBrush brush, int startBarIndex, int primaryCurrentBar)
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		if (brush == null || double.IsNaN(price))
		{
			return;
		}
		try
		{
			float num = chartScale.GetYByValue(price);
			startBarIndex = Math.Max(0, Math.Min(startBarIndex, primaryCurrentBar));
			float val = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, startBarIndex);
			int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
			float val2 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, fromIndex);
			float num2 = Math.Max(val, val2);
			float num3 = (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, primaryCurrentBar) + 70f - 60f;
			float num4 = Math.Max(num2 + 1f, num3 - 5f);
			if (!float.IsNaN(num) && !float.IsInfinity(num))
			{
				Vector2 val3 = default(Vector2);
				((Vector2)(ref val3))._002Ector(num2, num);
				Vector2 val4 = default(Vector2);
				((Vector2)(ref val4))._002Ector(num4, num);
				((IndicatorRenderBase)this).RenderTarget.DrawLine(val3, val4, (Brush)(object)brush, 1.5f);
				if (textFormat != null)
				{
					float num5 = (label.Contains("+") ? 120f : 80f);
					RectangleF val5 = default(RectangleF);
					((RectangleF)(ref val5))._002Ector(num3, num - 10f, num5, 20f);
					((IndicatorRenderBase)this).RenderTarget.DrawText(label, textFormat, val5, (Brush)(object)brush);
				}
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)$"PulseDailyLevels: Error drawing level {label} at {price:F2} - {ex.Message}");
		}
	}

	private void DrawPulseWatermark(ChartControl chartControl)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (textFormat != null)
			{
				SolidColorBrush val = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(0.5f, 0.5f, 0.5f, 0.3f));
				float num = ((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W - 120;
				float num2 = ((IndicatorRenderBase)this).ChartPanel.Y + ((IndicatorRenderBase)this).ChartPanel.H - 25;
				RectangleF val2 = default(RectangleF);
				((RectangleF)(ref val2))._002Ector(num, num2, 115f, 20f);
				((IndicatorRenderBase)this).RenderTarget.DrawText("Pulse Suite", textFormat, val2, (Brush)(object)val);
				if (val != null)
				{
					((DisposeBase)val).Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("PulseDailyLevels: Error drawing watermark - " + ex.Message));
		}
	}

	private void EnsureDxResources()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget == null)
		{
			return;
		}
		try
		{
			if (textFormat == null)
			{
				textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", Math.Max(8f, levelTextSize))
				{
					TextAlignment = (TextAlignment)0,
					ParagraphAlignment = (ParagraphAlignment)2
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
			((NinjaScript)this).Print((object)("PulseDailyLevels: Error creating DX resources - " + ex.Message));
		}
	}

	private void CreateDxBrush(ref SolidColorBrush dxBrush, Brush mediaBrush)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		if (dxBrush == null && mediaBrush != null)
		{
			SolidColorBrush val = (SolidColorBrush)(object)((mediaBrush is SolidColorBrush) ? mediaBrush : null);
			if (val != null)
			{
				Color color = val.Color;
				dxBrush = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)((Color)(ref color)).R / 255f, (float)(int)((Color)(ref color)).G / 255f, (float)(int)((Color)(ref color)).B / 255f, 0.8f));
			}
		}
	}

	private void DisposeDxResources()
	{
		try
		{
			TextFormat obj = textFormat;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			textFormat = null;
			SolidColorBrush obj2 = overnightBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			overnightBrushDx = null;
			SolidColorBrush obj3 = sessionOpenBrushDx;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			sessionOpenBrushDx = null;
			SolidColorBrush obj4 = orBrushDx;
			if (obj4 != null)
			{
				((DisposeBase)obj4).Dispose();
			}
			orBrushDx = null;
			SolidColorBrush obj5 = ibBrushDx;
			if (obj5 != null)
			{
				((DisposeBase)obj5).Dispose();
			}
			ibBrushDx = null;
			SolidColorBrush obj6 = previousDayBrushDx;
			if (obj6 != null)
			{
				((DisposeBase)obj6).Dispose();
			}
			previousDayBrushDx = null;
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("PulseDailyLevels: Error disposing DX resources - " + ex.Message));
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}
}
