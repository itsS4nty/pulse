using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
public class PulseDivergences : Indicator
{
	private double tickSize;

	private int lookbackBars = 5;

	private double minDeltaThreshold = 40.0;

	private bool showBullishDivergence = true;

	private bool showBearishDivergence = true;

	private int symbolSize = 3;

	private Brush bullishDivergenceBrush = Brushes.BlueViolet;

	private Brush bearishDivergenceBrush = Brushes.White;

	private int bullishDivergenceCount;

	private int bearishDivergenceCount;

	private DateTime sessionDate = DateTime.MinValue;

	private double minCandleSize = 2.0;

	private bool filterSmallCandles = true;

	private bool resetCountersDaily = true;

	private int lastDivergenceBar = -1;

	private int minBarsBetweenSignals = 1;

	private readonly Dictionary<int, double> barDeltaByPrimaryBar = new Dictionary<int, double>(2048);

	private readonly List<int> staleDeltaKeys = new List<int>(256);

	private double lastTickPrice = double.NaN;

	private int lastTickDirection;

	private int lastDeltaPruneBar = -1;

	private const string DivergenceSymbol = "★";

	private SimpleFont symbolFont;

	private int cachedSymbolFontSize = -1;

	[NinjaScriptProperty]
	[Range(3, 20)]
	[Display(Name = "Lookback Bars (NO USADO)", Description = "Mantenido por compatibilidad", Order = 1, GroupName = "Configuracion")]
	public int LookbackBars
	{
		get
		{
			return lookbackBars;
		}
		set
		{
			lookbackBars = Math.Max(3, Math.Min(20, value));
		}
	}

	[NinjaScriptProperty]
	[Range(25, 5000)]
	[Display(Name = "Min Delta Threshold", Description = "Delta minimo absoluto para considerar divergencia", Order = 2, GroupName = "Configuracion")]
	public double MinDeltaThreshold
	{
		get
		{
			return minDeltaThreshold;
		}
		set
		{
			minDeltaThreshold = Math.Max(25.0, value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 10)]
	[Display(Name = "Min Bars Between Signals", Description = "Minimo de barras entre senales consecutivas", Order = 3, GroupName = "Configuracion")]
	public int MinBarsBetweenSignals
	{
		get
		{
			return minBarsBetweenSignals;
		}
		set
		{
			minBarsBetweenSignals = Math.Max(0, Math.Min(10, value));
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Bullish Divergence", Description = "Mostrar divergencias alcistas (vela verde + delta negativo)", Order = 4, GroupName = "Visibilidad")]
	public bool ShowBullishDivergence
	{
		get
		{
			return showBullishDivergence;
		}
		set
		{
			showBullishDivergence = value;
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Bearish Divergence", Description = "Mostrar divergencias bajistas (vela roja + delta positivo)", Order = 5, GroupName = "Visibilidad")]
	public bool ShowBearishDivergence
	{
		get
		{
			return showBearishDivergence;
		}
		set
		{
			showBearishDivergence = value;
		}
	}

	[NinjaScriptProperty]
	[Range(1, 5)]
	[Display(Name = "Symbol Size", Description = "Tamano del simbolo (1=pequeno, 5=grande)", Order = 5, GroupName = "Visual")]
	public int SymbolSize
	{
		get
		{
			return symbolSize;
		}
		set
		{
			int num = Math.Max(1, Math.Min(5, value));
			if (symbolSize != num)
			{
				symbolSize = num;
				cachedSymbolFontSize = -1;
				symbolFont = null;
			}
		}
	}

	[NinjaScriptProperty]
	[Range(0.5, 20.0)]
	[Display(Name = "Min Candle Size (ticks)", Description = "Tamano minimo de vela en ticks para considerar divergencia", Order = 6, GroupName = "Filtros")]
	public double MinCandleSize
	{
		get
		{
			return minCandleSize;
		}
		set
		{
			minCandleSize = Math.Max(0.5, value);
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Filter Small Candles", Description = "Filtrar velas pequenas", Order = 7, GroupName = "Filtros")]
	public bool FilterSmallCandles
	{
		get
		{
			return filterSmallCandles;
		}
		set
		{
			filterSmallCandles = value;
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Reset Counters Daily", Description = "Resetear contadores cada dia", Order = 8, GroupName = "Estadisticas")]
	public bool ResetCountersDaily
	{
		get
		{
			return resetCountersDaily;
		}
		set
		{
			resetCountersDaily = value;
		}
	}

	[XmlIgnore]
	[Display(Name = "Bullish Divergence Color", Description = "Color para divergencias alcistas", Order = 1, GroupName = "Colores")]
	public Brush BullishDivergenceBrush
	{
		get
		{
			return bullishDivergenceBrush;
		}
		set
		{
			bullishDivergenceBrush = value;
		}
	}

	[Browsable(false)]
	public string BullishDivergenceBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(bullishDivergenceBrush);
		}
		set
		{
			bullishDivergenceBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Bearish Divergence Color", Description = "Color para divergencias bajistas", Order = 2, GroupName = "Colores")]
	public Brush BearishDivergenceBrush
	{
		get
		{
			return bearishDivergenceBrush;
		}
		set
		{
			bearishDivergenceBrush = value;
		}
	}

	[Browsable(false)]
	public string BearishDivergenceBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(bearishDivergenceBrush);
		}
		set
		{
			bearishDivergenceBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(Name = "Bullish Divergences Today", Description = "Numero de divergencias alcistas detectadas hoy", Order = 1, GroupName = "Estadisticas")]
	public int BullishDivergenceCount => bullishDivergenceCount;

	[Display(Name = "Bearish Divergences Today", Description = "Numero de divergencias bajistas detectadas hoy", Order = 2, GroupName = "Estadisticas")]
	public int BearishDivergenceCount => bearishDivergenceCount;

	public PulseDivergences()
	{
	}

	protected override void OnStateChange()
	{
		if (State == State.SetDefaults)
		{
			Description = "Pulse Divergences - Detecta divergencias entre direccion del precio y flujo delta";
			Name = "Pulse Divergences";
			Calculate = Calculate.OnBarClose;
			IsOverlay = true;
			DisplayInDataBox = false;
			DrawOnPricePanel = true;
			DrawHorizontalGridLines = false;
			DrawVerticalGridLines = false;
			PaintPriceMarkers = false;
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = true;
			lookbackBars = 5;
			minDeltaThreshold = 40.0;
			showBullishDivergence = true;
			showBearishDivergence = true;
			symbolSize = 3;
			minCandleSize = 2.0;
			filterSmallCandles = true;
			resetCountersDaily = true;
			minBarsBetweenSignals = 1;
			bullishDivergenceBrush = Brushes.BlueViolet;
			bearishDivergenceBrush = Brushes.White;
		}
		else if (State == State.Configure)
		{
			AddDataSeries((BarsPeriodType)0, 1);
		}
		else if (State == State.DataLoaded)
		{
			tickSize = Instrument.MasterInstrument.TickSize;
			symbolFont = new SimpleFont("Arial", GetSymbolFontSize());
			cachedSymbolFontSize = GetSymbolFontSize();
		}
		else if (State == State.Terminated)
		{
			barDeltaByPrimaryBar.Clear();
			staleDeltaKeys.Clear();
			symbolFont = null;
		}
	}

	protected override void OnBarUpdate()
	{
		if (BarsInProgress == 1)
		{
			ProcessTickDelta();
		}
		else
		{
			if (BarsInProgress != 0 || CurrentBar < 1 || (BarsArray.Length > 1 && CurrentBars[1] < 0))
			{
				return;
			}
			if (resetCountersDaily && Bars.IsFirstBarOfSession)
			{
				DateTime date = Times[0][0].Date;
				if (sessionDate != date)
				{
					sessionDate = date;
					bullishDivergenceCount = 0;
					bearishDivergenceCount = 0;
				}
			}
			AnalyzeDivergence(GetCurrentBarDelta());
			PruneOldDeltaData();
		}
	}

	private void AnalyzeDivergence(double currentBarDelta)
	{
		if (lastDivergenceBar != -1 && CurrentBar - lastDivergenceBar < minBarsBetweenSignals)
		{
			return;
		}
		double num = Open[0];
		double num2 = Close[0];
		double num3 = ((tickSize > 0.0) ? (Math.Abs(num2 - num) / tickSize) : 0.0);
		if (!filterSmallCandles || !(num3 < minCandleSize))
		{
			bool flag = num2 > num;
			bool flag2 = num2 < num;
			if (showBullishDivergence && flag && currentBarDelta < 0.0 - minDeltaThreshold)
			{
				DrawBullishDivergence();
				bullishDivergenceCount++;
				lastDivergenceBar = CurrentBar;
			}
			else if (showBearishDivergence && flag2 && currentBarDelta > minDeltaThreshold)
			{
				DrawBearishDivergence();
				bearishDivergenceCount++;
				lastDivergenceBar = CurrentBar;
			}
		}
	}

	private double GetCurrentBarDelta()
	{
		if (barDeltaByPrimaryBar.TryGetValue(CurrentBar, out var value))
		{
			return value;
		}
		return 0.0;
	}

	private void ProcessTickDelta()
	{
		if (BarsArray.Length < 2 || CurrentBars[0] < 0 || CurrentBars[1] < 0)
		{
			return;
		}
		int key = CurrentBars[0];
		double num = Closes[1][0];
		double num2 = Volumes[1][0];
		if (double.IsNaN(num) || num2 <= 0.0)
		{
			return;
		}
		int num3 = 0;
		if (!double.IsNaN(lastTickPrice))
		{
			num3 = ((num > lastTickPrice) ? 1 : ((!(num < lastTickPrice)) ? lastTickDirection : (-1)));
		}
		if (num3 != 0)
		{
			double num4 = ((num3 > 0) ? num2 : (0.0 - num2));
			if (barDeltaByPrimaryBar.TryGetValue(key, out var value))
			{
				barDeltaByPrimaryBar[key] = value + num4;
			}
			else
			{
				barDeltaByPrimaryBar[key] = num4;
			}
			lastTickDirection = num3;
		}
		lastTickPrice = num;
	}

	private void PruneOldDeltaData()
	{
		if (CurrentBar - lastDeltaPruneBar < 50 || barDeltaByPrimaryBar.Count < 3000)
		{
			return;
		}
		lastDeltaPruneBar = CurrentBar;
		int num = Math.Max(0, CurrentBar - 5000);
		staleDeltaKeys.Clear();
		foreach (int key in barDeltaByPrimaryBar.Keys)
		{
			if (key < num)
			{
				staleDeltaKeys.Add(key);
			}
		}
		for (int i = 0; i < staleDeltaKeys.Count; i++)
		{
			barDeltaByPrimaryBar.Remove(staleDeltaKeys[i]);
		}
	}

	private void DrawBullishDivergence()
	{
		double y = Low[0] - tickSize * 3.0;
		string tag = string.Format(CultureInfo.InvariantCulture, "BullDiv_{0}_{1}", CurrentBar, Time[0].Ticks);
		Draw.Text(this, tag, isAutoScale: false, "★", 0, y, 0, bullishDivergenceBrush, GetSymbolFont(), (TextAlignment)2, Brushes.Transparent, Brushes.Transparent, 0);
	}

	private void DrawBearishDivergence()
	{
		double y = High[0] + tickSize * 3.0;
		string tag = string.Format(CultureInfo.InvariantCulture, "BearDiv_{0}_{1}", CurrentBar, Time[0].Ticks);
		Draw.Text(this, tag, isAutoScale: false, "★", 0, y, 0, bearishDivergenceBrush, GetSymbolFont(), (TextAlignment)2, Brushes.Transparent, Brushes.Transparent, 0);
	}

	private int GetSymbolFontSize()
	{
		return symbolSize == 1 ? 8 : (symbolSize == 2 ? 10 : (symbolSize == 3 ? 12 : (symbolSize == 4 ? 14 : (symbolSize == 5 ? 16 : 12))));
	}

	private SimpleFont GetSymbolFont()
	{
		int symbolFontSize = GetSymbolFontSize();
		if (symbolFont == null || cachedSymbolFontSize != symbolFontSize)
		{
			symbolFont = new SimpleFont("Arial", symbolFontSize);
			cachedSymbolFontSize = symbolFontSize;
		}
		return symbolFont;
	}
}
}
