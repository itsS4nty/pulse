using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
public class PulseWeeklyLevels : Indicator
{
	private DateTime currentWeekStart = DateTime.MinValue;

	private DateTime ibEndTime = DateTime.MinValue;

	private double wibHigh = double.MinValue;

	private double wibLow = double.MaxValue;

	private double wibMid;

	private bool ibCalculated;

	private double weekOpen;

	private double weekHigh = double.MinValue;

	private double weekLow = double.MaxValue;

	private double weekClose;

	private double priorWeekHigh;

	private double priorWeekLow;

	private double priorWeekClose;

	private double priorWeekOpen;

	private double priorWeekMid;

	private double priorWeekVAH;

	private double priorWeekVAL;

	private double priorWeekPOC;

	private Dictionary<double, long> weekVolumeByPrice = new Dictionary<double, long>();

	private readonly Dictionary<DateTime, Dictionary<double, long>> weeklyVolumeProfiles = new Dictionary<DateTime, Dictionary<double, long>>();

	private bool currentWeekVaDirty = true;

	private int lastCalculatedValueAreaPercent = -1;

	private int levelTextSize = 14;

	private int rightMarginPx = 120;

	private double tickSize = 0.25;

	private double vwapSum;

	private long vwapVolume;

	private TextFormat textFormat;

	private Dictionary<string, SolidColorBrush> dxBrushes = new Dictionary<string, SolidColorBrush>();

	[NinjaScriptProperty]
	[Range(1, 168)]
	[Display(Name = "Weekly IB Duration (Hours)", Description = "Duration of Weekly Initial Balance in hours", Order = 1, GroupName = "1. Parameters")]
	public int WeeklyIBDurationHours { get; set; }

	[NinjaScriptProperty]
	[Range(50, 90)]
	[Display(Name = "Value Area (%)", Description = "Percentage for Value Area calculation", Order = 2, GroupName = "1. Parameters")]
	public int ValueAreaPercent { get; set; }

	[NinjaScriptProperty]
	[Range(8, 40)]
	[Display(Name = "Label Text Size", Description = "Tamaño del texto de labels (PW-VAH, WK-POC, etc.)", Order = 3, GroupName = "1. Parameters")]
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
	[Display(Name = "Weekly IB", Description = "Show Weekly Initial Balance levels", Order = 1, GroupName = "2. Plots to Show")]
	public bool ShowWeeklyIB { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "WIB 50% Extensions", Description = "Show 50% extensions", Order = 2, GroupName = "2. Plots to Show")]
	public bool ShowWIB50 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "WIB 100% Extensions", Description = "Show 100% extensions", Order = 3, GroupName = "2. Plots to Show")]
	public bool ShowWIB100 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "WIB 150% Extensions", Description = "Show 150% extensions", Order = 4, GroupName = "2. Plots to Show")]
	public bool ShowWIB150 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "WIB 200% Extensions", Description = "Show 200% extensions", Order = 5, GroupName = "2. Plots to Show")]
	public bool ShowWIB200 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Current Week Open", Description = "Show current week open", Order = 6, GroupName = "2. Plots to Show")]
	public bool ShowWeekOpen { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Current Week Mid", Description = "Show current week midpoint", Order = 7, GroupName = "2. Plots to Show")]
	public bool ShowWeekMid { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Current Week VAH / VAL / VPOC", Description = "Show current week value area", Order = 8, GroupName = "2. Plots to Show")]
	public bool ShowWeekVA { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Current Week VWAP", Description = "Show current week VWAP", Order = 9, GroupName = "2. Plots to Show")]
	public bool ShowWeekVWAP { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Prior Week HLC", Description = "Show prior week high/low/close", Order = 10, GroupName = "2. Plots to Show")]
	public bool ShowPriorWeekHLC { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Prior Week Open", Description = "Show prior week open", Order = 11, GroupName = "2. Plots to Show")]
	public bool ShowPriorWeekOpen { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Prior Week Mid", Description = "Show prior week midpoint", Order = 12, GroupName = "2. Plots to Show")]
	public bool ShowPriorWeekMid { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Prior Week VAH / VAL / VPOC", Description = "Show prior week value area", Order = 13, GroupName = "2. Plots to Show")]
	public bool ShowPriorWeekVA { get; set; }

	public PulseWeeklyLevels()
	{
	}

	protected override void OnStateChange()
	{
		if (State == State.SetDefaults)
		{
			Description = "Pulse Weekly Levels - Weekly IB with extensions and key levels";
			Name = "PulseWeeklyLevels";
			Calculate = Calculate.OnBarClose;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			PaintPriceMarkers = true;
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = true;
			BarsRequiredToPlot = 0;
			WeeklyIBDurationHours = 30;
			ValueAreaPercent = 70;
			levelTextSize = 14;
			ShowWeeklyIB = true;
			ShowWIB50 = false;
			ShowWIB100 = false;
			ShowWIB150 = false;
			ShowWIB200 = false;
			ShowWeekOpen = true;
			ShowWeekMid = true;
			ShowWeekVA = true;
			ShowWeekVWAP = true;
			ShowPriorWeekHLC = true;
			ShowPriorWeekOpen = false;
			ShowPriorWeekMid = false;
			ShowPriorWeekVA = true;
			AddPlot(new Stroke((Brush)(object)Brushes.White, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB-Hi");
			AddPlot(new Stroke((Brush)(object)Brushes.White, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB-Mid");
			AddPlot(new Stroke((Brush)(object)Brushes.White, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB-Lo");
			AddPlot(new Stroke((Brush)(object)Brushes.DodgerBlue, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB 50%");
			AddPlot(new Stroke((Brush)(object)Brushes.OrangeRed, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB -50%");
			AddPlot(new Stroke((Brush)(object)Brushes.DodgerBlue, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB 100%");
			AddPlot(new Stroke((Brush)(object)Brushes.OrangeRed, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB -100%");
			AddPlot(new Stroke((Brush)(object)Brushes.DodgerBlue, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB 150%");
			AddPlot(new Stroke((Brush)(object)Brushes.OrangeRed, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB -150%");
			AddPlot(new Stroke((Brush)(object)Brushes.DodgerBlue, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB 200%");
			AddPlot(new Stroke((Brush)(object)Brushes.OrangeRed, (DashStyleHelper)2, 1f), (PlotStyle)6, "WIB -200%");
			AddPlot(new Stroke((Brush)(object)Brushes.LightPink, 1f), (PlotStyle)6, "PW-Hi");
			AddPlot(new Stroke((Brush)(object)Brushes.LightPink, 1f), (PlotStyle)6, "PW-Lo");
			AddPlot(new Stroke((Brush)(object)Brushes.LightPink, 1f), (PlotStyle)6, "PW-Cl");
			AddPlot(new Stroke((Brush)(object)Brushes.LimeGreen, 1f), (PlotStyle)6, "WK-Op");
			AddPlot(new Stroke((Brush)(object)Brushes.Gray, (DashStyleHelper)2, 1f), (PlotStyle)6, "WK-VAH");
			AddPlot(new Stroke((Brush)(object)Brushes.Gray, (DashStyleHelper)2, 1f), (PlotStyle)6, "WK-POC");
			AddPlot(new Stroke((Brush)(object)Brushes.Gray, (DashStyleHelper)2, 1f), (PlotStyle)6, "WK-VAL");
			AddPlot(new Stroke((Brush)(object)Brushes.CornflowerBlue, 1f), (PlotStyle)6, "PW-VAH");
			AddPlot(new Stroke((Brush)(object)Brushes.CornflowerBlue, 1f), (PlotStyle)6, "PW-POC");
			AddPlot(new Stroke((Brush)(object)Brushes.CornflowerBlue, 1f), (PlotStyle)6, "PW-VAL");
			AddPlot(new Stroke((Brush)(object)Brushes.Yellow, (DashStyleHelper)0, 3f), (PlotStyle)6, "WK-VWAP");
			AddPlot(new Stroke((Brush)(object)Brushes.Gray, (DashStyleHelper)4, 1f), (PlotStyle)6, "PW-Op");
			AddPlot(new Stroke((Brush)(object)Brushes.White, (DashStyleHelper)1, 2f), (PlotStyle)6, "WK-Mid");
			AddPlot(new Stroke((Brush)(object)Brushes.Gray, (DashStyleHelper)1, 1f), (PlotStyle)6, "PW-Mid");
		}
		else if (State == State.Configure)
		{
			AddDataSeries((BarsPeriodType)0, 1);
		}
		else if (State == State.DataLoaded)
		{
			tickSize = Instrument.MasterInstrument.TickSize;
			Print((object)"================================================================================");
			Print((object)"PulseWeeklyLevels: IMPORTANTE - Carga mínimo 15 días de datos históricos");
			Print((object)"PulseWeeklyLevels: IMPORTANT - Load minimum 15 days of historical data");
			Print((object)"PulseWeeklyLevels: Para cálculos precisos de Weekly IB y Prior Week levels");
			Print((object)"PulseWeeklyLevels: For accurate Weekly IB and Prior Week level calculations");
			Print((object)"================================================================================");
		}
		else if (State == State.Historical)
		{
			int loadedTradingDays = GetLoadedTradingDays();
			if (loadedTradingDays < 15)
			{
				Draw.TextFixed(this, "DataWarning", "PulseWeeklyLevels: Load minimum 15 days of data\nPulseWeeklyLevels: Carga minimo 15 dias de datos historicos\n" + $"Loaded: {loadedTradingDays}/15", TextPosition.TopLeft, (Brush)(object)Brushes.Yellow, new SimpleFont("Arial", 12)
				{
					Bold = true
				}, (Brush)(object)Brushes.Transparent, (Brush)(object)Brushes.Black, 100);
			}
			else
			{
				RemoveDrawObject("DataWarning");
			}
		}
		else if (State == State.Terminated)
		{
			DisposeDxResources();
		}
	}

	protected override void OnBarUpdate()
	{
		if (BarsInProgress == 1)
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
			DateTime weekStart = GetWeekStart(dateTime);
			if (currentWeekStart == DateTime.MinValue)
			{
				currentWeekStart = weekStart;
				ibEndTime = currentWeekStart.AddHours(WeeklyIBDurationHours);
				weekOpen = Open[0];
				weekHigh = High[0];
				weekLow = Low[0];
				weekVolumeByPrice = GetOrCreateWeekProfile(currentWeekStart);
				currentWeekVaDirty = true;
			}
			else if (weekStart > currentWeekStart && dateTime > weekStart)
			{
				Dictionary<double, long> value = null;
				weeklyVolumeProfiles.TryGetValue(currentWeekStart, out value);
				SavePriorWeekData(value);
				currentWeekStart = weekStart;
				ibEndTime = currentWeekStart.AddHours(WeeklyIBDurationHours);
				weekOpen = Open[0];
				weekHigh = High[0];
				weekLow = Low[0];
				weekClose = Close[0];
				wibHigh = double.MinValue;
				wibLow = double.MaxValue;
				ibCalculated = false;
				weekVolumeByPrice = GetOrCreateWeekProfile(currentWeekStart);
				vwapSum = 0.0;
				vwapVolume = 0L;
				currentWeekVaDirty = true;
				TrimOldWeekProfiles(currentWeekStart);
			}
			else
			{
				weekVolumeByPrice = GetOrCreateWeekProfile(currentWeekStart);
			}
			if (High[0] > weekHigh)
			{
				weekHigh = High[0];
			}
			if (Low[0] < weekLow)
			{
				weekLow = Low[0];
			}
			weekClose = Close[0];
			if (!ibCalculated && dateTime < ibEndTime)
			{
				if (High[0] > wibHigh)
				{
					wibHigh = High[0];
				}
				if (Low[0] < wibLow)
				{
					wibLow = Low[0];
				}
			}
			else if (!ibCalculated && dateTime >= ibEndTime)
			{
				wibMid = (wibHigh + wibLow) / 2.0;
				ibCalculated = true;
			}
			long num = (long)Volume[0];
			if (num > 0)
			{
				double num2 = (High[0] + Low[0] + Close[0]) / 3.0;
				vwapSum += num2 * (double)num;
				vwapVolume += num;
			}
			CalculateCurrentWeekValueArea();
			PlotLevels();
		}
	}

	private DateTime GetWeekStart(DateTime time)
	{
		DateTime dateTime = time.Date;
		while (dateTime.DayOfWeek != DayOfWeek.Sunday)
		{
			dateTime = dateTime.AddDays(-1.0);
		}
		dateTime = dateTime.AddHours(18.0);
		if (time < dateTime)
		{
			dateTime = dateTime.AddDays(-7.0);
		}
		return dateTime;
	}

	private int GetLoadedTradingDays()
	{
		if (Bars == null || Bars.Count <= 0)
		{
			return 0;
		}
		DateTime dateTime = DateTime.MaxValue;
		DateTime dateTime2 = DateTime.MinValue;
		for (int i = 0; i < Bars.Count; i++)
		{
			DateTime date = Bars.GetTime(i).Date;
			if (date < dateTime)
			{
				dateTime = date;
			}
			if (date > dateTime2)
			{
				dateTime2 = date;
			}
		}
		if (dateTime == DateTime.MaxValue || dateTime2 == DateTime.MinValue || dateTime2 < dateTime)
		{
			return 0;
		}
		return (dateTime2 - dateTime).Days + 1;
	}

	private void SavePriorWeekData(Dictionary<double, long> completedWeekProfile)
	{
		priorWeekHigh = weekHigh;
		priorWeekLow = weekLow;
		priorWeekClose = weekClose;
		priorWeekOpen = weekOpen;
		priorWeekMid = (weekHigh + weekLow) / 2.0;
		priorWeekVAH = 0.0;
		priorWeekVAL = 0.0;
		priorWeekPOC = 0.0;
		if (TryCalculateValueArea(completedWeekProfile, out var poc, out var vah, out var val))
		{
			priorWeekPOC = poc;
			priorWeekVAH = vah;
			priorWeekVAL = val;
		}
	}

	private Dictionary<double, long> GetOrCreateWeekProfile(DateTime weekStart)
	{
		if (!weeklyVolumeProfiles.TryGetValue(weekStart, out var value))
		{
			value = new Dictionary<double, long>(4096);
			weeklyVolumeProfiles[weekStart] = value;
		}
		return value;
	}

	private void TrimOldWeekProfiles(DateTime activeWeekStart)
	{
		if (weeklyVolumeProfiles.Count <= 4)
		{
			return;
		}
		DateTime dateTime = activeWeekStart.AddDays(-21.0);
		List<DateTime> list = new List<DateTime>();
		foreach (DateTime key in weeklyVolumeProfiles.Keys)
		{
			if (key < dateTime)
			{
				list.Add(key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			weeklyVolumeProfiles.Remove(list[i]);
		}
	}

	private void ProcessTickVolume()
	{
		if (BarsArray.Length < 2 || CurrentBars[1] < 0)
		{
			return;
		}
		double num = Closes[1][0];
		long num2 = (long)Volumes[1][0];
		if (!double.IsNaN(num) && num2 > 0)
		{
			DateTime weekStart = GetWeekStart(Times[1][0]);
			Dictionary<double, long> orCreateWeekProfile = GetOrCreateWeekProfile(weekStart);
			double key = RoundToTick(num);
			if (orCreateWeekProfile.TryGetValue(key, out var value))
			{
				orCreateWeekProfile[key] = value + num2;
			}
			else
			{
				orCreateWeekProfile[key] = num2;
			}
			if (weekStart == currentWeekStart)
			{
				currentWeekVaDirty = true;
			}
		}
	}

	private bool TryCalculateValueArea(Dictionary<double, long> volumeByPrice, out double poc, out double vah, out double val)
	{
		poc = 0.0;
		vah = 0.0;
		val = 0.0;
		if (volumeByPrice == null || volumeByPrice.Count == 0)
		{
			return false;
		}
		long num = 0L;
		long num2 = 0L;
		double num3 = 0.0;
		foreach (KeyValuePair<double, long> item in volumeByPrice)
		{
			long value = item.Value;
			if (value > 0)
			{
				num += value;
				if (value > num2)
				{
					num2 = value;
					num3 = item.Key;
				}
			}
		}
		if (num <= 0 || num2 <= 0)
		{
			return false;
		}
		List<double> list = new List<double>(volumeByPrice.Keys);
		list.Sort();
		if (list.Count == 0)
		{
			return false;
		}
		int num4 = list.BinarySearch(num3);
		if (num4 < 0)
		{
			num4 = ~num4;
		}
		if (num4 >= list.Count)
		{
			num4 = list.Count - 1;
		}
		long num5 = (long)Math.Ceiling((double)num * ((double)ValueAreaPercent / 100.0));
		long num6 = num2;
		int num7 = num4;
		int num8 = num4;
		while (num6 < num5 && (num7 < list.Count - 1 || num8 > 0))
		{
			long num9 = long.MinValue;
			long num10 = long.MinValue;
			if (num7 < list.Count - 1)
			{
				num9 = volumeByPrice[list[num7 + 1]];
			}
			if (num8 > 0)
			{
				num10 = volumeByPrice[list[num8 - 1]];
			}
			if (num9 > num10)
			{
				num7++;
				num6 += Math.Max(0L, num9);
				continue;
			}
			if (num10 > num9)
			{
				num8--;
				num6 += Math.Max(0L, num10);
				continue;
			}
			if (num7 < list.Count - 1)
			{
				num7++;
				num6 += Math.Max(0L, volumeByPrice[list[num7]]);
			}
			if (num6 >= num5)
			{
				break;
			}
			if (num8 > 0)
			{
				num8--;
				num6 += Math.Max(0L, volumeByPrice[list[num8]]);
			}
		}
		poc = num3;
		vah = list[num7];
		val = list[num8];
		return true;
	}

	private double RoundToTick(double price)
	{
		if (tickSize <= 0.0)
		{
			return price;
		}
		return Math.Round(Math.Round(price / tickSize) * tickSize, 10);
	}

	private void CalculateCurrentWeekValueArea()
	{
		if (lastCalculatedValueAreaPercent != ValueAreaPercent)
		{
			currentWeekVaDirty = true;
			lastCalculatedValueAreaPercent = ValueAreaPercent;
		}
		if (currentWeekVaDirty)
		{
			if (TryCalculateValueArea(weekVolumeByPrice, out var poc, out var vah, out var val))
			{
				Values[15][0] = vah;
				Values[16][0] = poc;
				Values[17][0] = val;
			}
			else
			{
				Values[15][0] = double.NaN;
				Values[16][0] = double.NaN;
				Values[17][0] = double.NaN;
			}
			currentWeekVaDirty = false;
		}
	}

	private void PlotLevels()
	{
		if (ShowWeeklyIB && ibCalculated)
		{
			Values[0][0] = wibHigh;
			Values[1][0] = wibMid;
			Values[2][0] = wibLow;
		}
		else
		{
			Values[0][0] = double.NaN;
			Values[1][0] = double.NaN;
			Values[2][0] = double.NaN;
		}
		if (ibCalculated)
		{
			double num = wibHigh - wibLow;
			Values[3][0] = (ShowWIB50 ? (wibHigh + num * 0.5) : double.NaN);
			Values[4][0] = (ShowWIB50 ? (wibLow - num * 0.5) : double.NaN);
			Values[5][0] = (ShowWIB100 ? (wibHigh + num) : double.NaN);
			Values[6][0] = (ShowWIB100 ? (wibLow - num) : double.NaN);
			Values[7][0] = (ShowWIB150 ? (wibHigh + num * 1.5) : double.NaN);
			Values[8][0] = (ShowWIB150 ? (wibLow - num * 1.5) : double.NaN);
			Values[9][0] = (ShowWIB200 ? (wibHigh + num * 2.0) : double.NaN);
			Values[10][0] = (ShowWIB200 ? (wibLow - num * 2.0) : double.NaN);
		}
		Values[11][0] = (ShowPriorWeekHLC ? priorWeekHigh : double.NaN);
		Values[12][0] = (ShowPriorWeekHLC ? priorWeekLow : double.NaN);
		Values[13][0] = (ShowPriorWeekHLC ? priorWeekClose : double.NaN);
		Values[14][0] = (ShowWeekOpen ? weekOpen : double.NaN);
		if (!ShowWeekVA)
		{
			Values[15][0] = double.NaN;
			Values[16][0] = double.NaN;
			Values[17][0] = double.NaN;
		}
		Values[18][0] = (ShowPriorWeekVA ? priorWeekVAH : double.NaN);
		Values[19][0] = (ShowPriorWeekVA ? priorWeekPOC : double.NaN);
		Values[20][0] = (ShowPriorWeekVA ? priorWeekVAL : double.NaN);
		if (ShowWeekVWAP && vwapVolume > 0)
		{
			Values[21][0] = vwapSum / (double)vwapVolume;
		}
		else
		{
			Values[21][0] = double.NaN;
		}
		Values[22][0] = (ShowPriorWeekOpen ? priorWeekOpen : double.NaN);
		if (ShowWeekMid && weekHigh > 0.0 && weekLow > 0.0)
		{
			Values[23][0] = (weekHigh + weekLow) / 2.0;
		}
		else
		{
			Values[23][0] = double.NaN;
		}
		Values[24][0] = (ShowPriorWeekMid ? priorWeekMid : double.NaN);
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		try
		{
			if (chartControl != null && chartScale != null && RenderTarget != null)
			{
				EnsureDxResources();
				if (ShowWeeklyIB && ibCalculated)
				{
					DrawLevel(chartControl, chartScale, wibHigh, "WIB-Hi", GetDxBrush((Brush)(object)Brushes.White));
					DrawLevel(chartControl, chartScale, wibMid, "WIB-Mid", GetDxBrush((Brush)(object)Brushes.White));
					DrawLevel(chartControl, chartScale, wibLow, "WIB-Lo", GetDxBrush((Brush)(object)Brushes.White));
				}
				if (ShowPriorWeekHLC && priorWeekHigh > 0.0)
				{
					DrawLevel(chartControl, chartScale, priorWeekHigh, "PW-Hi", GetDxBrush((Brush)(object)Brushes.LightPink));
					DrawLevel(chartControl, chartScale, priorWeekLow, "PW-Lo", GetDxBrush((Brush)(object)Brushes.LightPink));
					DrawLevel(chartControl, chartScale, priorWeekClose, "PDL", GetDxBrush((Brush)(object)Brushes.LightPink));
				}
				if (ShowWeekOpen && weekOpen > 0.0)
				{
					DrawLevel(chartControl, chartScale, weekOpen, "WK-Op", GetDxBrush((Brush)(object)Brushes.LimeGreen));
				}
				if (ShowWeekVA && Values[16][0] > 0.0)
				{
					DrawLevel(chartControl, chartScale, Values[15][0], "WK-VAH", GetDxBrush((Brush)(object)Brushes.Gray));
					DrawLevel(chartControl, chartScale, Values[16][0], "WK-POC", GetDxBrush((Brush)(object)Brushes.Gray));
					DrawLevel(chartControl, chartScale, Values[17][0], "WK-VAL", GetDxBrush((Brush)(object)Brushes.Gray));
				}
				if (ShowPriorWeekVA && priorWeekPOC > 0.0)
				{
					DrawLevel(chartControl, chartScale, priorWeekVAH, "PW-VAH", GetDxBrush((Brush)(object)Brushes.CornflowerBlue));
					DrawLevel(chartControl, chartScale, priorWeekPOC, "PW-POC", GetDxBrush((Brush)(object)Brushes.CornflowerBlue));
					DrawLevel(chartControl, chartScale, priorWeekVAL, "PW-VAL", GetDxBrush((Brush)(object)Brushes.CornflowerBlue));
				}
				if (ShowWeekVWAP && vwapVolume > 0)
				{
					double price = vwapSum / (double)vwapVolume;
					DrawLevel(chartControl, chartScale, price, "WK-VWAP", GetDxBrush((Brush)(object)Brushes.Yellow));
				}
				if (ShowWeekMid && weekHigh > 0.0 && weekLow > 0.0)
				{
					double price2 = (weekHigh + weekLow) / 2.0;
					DrawLevel(chartControl, chartScale, price2, "WK-Mid", GetDxBrush((Brush)(object)Brushes.White));
				}
			}
		}
		catch (Exception ex)
		{
			Print((object)("PulseWeeklyLevels: OnRender error - " + ex.Message));
		}
	}

	private void DrawLevel(ChartControl chartControl, ChartScale chartScale, double price, string label, SolidColorBrush brush)
	{
		if (brush == null || double.IsNaN(price) || ChartBars == null)
		{
			return;
		}
		try
		{
			float num = chartScale.GetYByValue(price);
			int fromIndex = ChartBars.FromIndex;
			float num2 = chartControl.GetXByBarIndex(ChartBars, fromIndex);
			float num3 = (label.Contains("+") ? 120f : 80f);
			float num4 = ChartPanel.X + ChartPanel.W;
			float val = num4 - num3 - 2f;
			float val2 = Math.Min(num4 - (float)rightMarginPx, val);
			val2 = Math.Max(val2, num2 + 6f);
			float num5 = Math.Max(num2 + 1f, val2 - 5f);
			if (!float.IsNaN(num) && !float.IsInfinity(num))
			{
				Vector2 val3 = default(Vector2);
				val3 = new Vector2(num2, num);
				Vector2 val4 = default(Vector2);
				val4 = new Vector2(num5, num);
				RenderTarget.DrawLine(val3, val4, (Brush)(object)brush, 1.5f);
				if (textFormat != null)
				{
					RectangleF val5 = default(RectangleF);
					val5 = new RectangleF(val2, num - 10f, num3, 20f);
					RenderTarget.DrawText(label, textFormat, val5, (Brush)(object)brush);
				}
			}
		}
		catch (Exception ex)
		{
			Print((object)$"PulseWeeklyLevels: Error drawing level {label} at {price:F2} - {ex.Message}");
		}
	}

	private void DrawRightLabel(string text, double price, ChartControl chartControl, ChartScale chartScale)
	{
		Print((object)string.Format("DrawRightLabel called: text={0}, price={1:F2}, textFormat={2}", text, price, (textFormat == null) ? "NULL" : "OK"));
		if (double.IsNaN(price) || price <= 0.0 || textFormat == null)
		{
			return;
		}
		try
		{
			Color color = ((SolidColorBrush)chartControl.Properties.ChartText).Color;
			SolidColorBrush val = new SolidColorBrush(RenderTarget, new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, 0.8f));
			try
			{
				int currentBar = CurrentBar;
				float num = (float)chartControl.GetXByBarIndex(ChartBars, currentBar) + 50f;
				float num2 = chartScale.GetYByValue(price);
				if (!float.IsNaN(num2) && !float.IsInfinity(num2))
				{
					RectangleF val2 = default(RectangleF);
					val2 = new RectangleF(num - 45f, num2 - 8f, 80f, 16f);
					RenderTarget.DrawText(text, textFormat, val2, (Brush)(object)val);
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Print((object)("PulseWeeklyLevels: Error drawing label " + text + " - " + ex.Message));
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
					TextAlignment = (TextAlignment)0,
					ParagraphAlignment = (ParagraphAlignment)2
				};
			}
		}
		catch (Exception ex)
		{
			Print((object)("PulseWeeklyLevels: Error creating DX resources - " + ex.Message));
		}
	}

	private SolidColorBrush GetDxBrush(Brush wpfBrush)
	{
		if (RenderTarget == null || wpfBrush == null)
		{
			return null;
		}
		SolidColorBrush val = (SolidColorBrush)(object)((wpfBrush is SolidColorBrush) ? wpfBrush : null);
		if (val != null)
		{
			Color color = val.Color;
			string key = color.A + "_" + color.R + "_" + color.G + "_" + color.B;
			if (!dxBrushes.TryGetValue(key, out var value) || value == null)
			{
				float num = (float)(int)color.A / 255f * 0.8f;
				value = new SolidColorBrush(RenderTarget, new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, num));
				dxBrushes[key] = value;
			}
			return value;
		}
		return null;
	}

	private void DisposeDxResources()
	{
		TextFormat obj = textFormat;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		textFormat = null;
		foreach (SolidColorBrush value in dxBrushes.Values)
		{
			if (value != null)
			{
				((DisposeBase)value).Dispose();
			}
		}
		dxBrushes.Clear();
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		OnRenderTargetChanged();
	}
}
}
