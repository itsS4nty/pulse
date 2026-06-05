using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
public class PulseHeikin : Indicator
{
	private Brush barColorDown = (Brush)(object)Brushes.White;

	private Brush barColorUp = (Brush)(object)Brushes.BlueViolet;

	private Brush shadowColor = (Brush)(object)Brushes.DimGray;

	private int shadowWidth = 1;

	private bool useStableCalculation = true;

	private bool useBodyColorForWicks = true;

	private bool updateOnTicks;

	private SolidColorBrush dxBarUpBrush;

	private SolidColorBrush dxBarDownBrush;

	private SolidColorBrush dxShadowBrush;

	[XmlIgnore]
	[Display(Name = "Bar Color Down", Description = "Color de velas bajistas", Order = 1, GroupName = "Visual")]
	public Brush BarColorDown
	{
		get
		{
			return barColorDown;
		}
		set
		{
			barColorDown = value;
		}
	}

	[Browsable(false)]
	public string BarColorDownSerializable
	{
		get
		{
			return Serialize.BrushToString(barColorDown);
		}
		set
		{
			barColorDown = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Bar Color Up", Description = "Color de velas alcistas", Order = 2, GroupName = "Visual")]
	public Brush BarColorUp
	{
		get
		{
			return barColorUp;
		}
		set
		{
			barColorUp = value;
		}
	}

	[Browsable(false)]
	public string BarColorUpSerializable
	{
		get
		{
			return Serialize.BrushToString(barColorUp);
		}
		set
		{
			barColorUp = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Shadow Color", Description = "Color de mechas/sombras", Order = 3, GroupName = "Visual")]
	public Brush ShadowColor
	{
		get
		{
			return shadowColor;
		}
		set
		{
			shadowColor = value;
		}
	}

	[Browsable(false)]
	public string ShadowColorSerializable
	{
		get
		{
			return Serialize.BrushToString(shadowColor);
		}
		set
		{
			shadowColor = Serialize.StringToBrush(value);
		}
	}

	[Range(1, int.MaxValue)]
	[Display(Name = "Shadow Width", Description = "Ancho de mechas (1-5 recomendado)", Order = 4, GroupName = "Visual")]
	public int ShadowWidth
	{
		get
		{
			return shadowWidth;
		}
		set
		{
			shadowWidth = value;
		}
	}

	[Display(Name = "Use Stable Calculation", Description = "Incluir HAClose en cálculos de HAHigh/HALow para mayor estabilidad", Order = 5, GroupName = "Calculation")]
	public bool UseStableCalculation
	{
		get
		{
			return useStableCalculation;
		}
		set
		{
			useStableCalculation = value;
		}
	}

	[Display(Name = "Body Color For Wicks", Description = "Usar color del cuerpo para mechas en lugar de color de sombra separado", Order = 6, GroupName = "Visual")]
	public bool UseBodyColorForWicks
	{
		get
		{
			return useBodyColorForWicks;
		}
		set
		{
			useBodyColorForWicks = value;
		}
	}

	[Display(Name = "Update On Ticks", Description = "Actualizar en tiempo real con cada tick (true) o solo al cierre de barra (false)", Order = 7, GroupName = "Calculation")]
	public bool UpdateOnTicks
	{
		get
		{
			return updateOnTicks;
		}
		set
		{
			updateOnTicks = value;
			Calculate = (Calculate)(value ? 1 : 0);
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> HAOpen => Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> HAHigh => Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> HALow => Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> HAClose => Values[3];

	protected override void OnStateChange()
	{
		if (State == State.SetDefaults)
		{
			Description = "Pulse Heiken Ashi - Velas suavizadas para análisis de tendencia. Filtra el ruido del mercado y facilita la identificación de tendencias. Parte de Pulse Suite v2.0.";
			Name = "PulseHeikin";
			Calculate = Calculate.OnBarClose;
			IsOverlay = true;
			DisplayInDataBox = true;
			DrawOnPricePanel = true;
			DrawHorizontalGridLines = true;
			DrawVerticalGridLines = true;
			PaintPriceMarkers = false;
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = false;
			BarsRequiredToPlot = 1;
			AddPlot((Brush)(object)Brushes.Transparent, "HAOpen");
			AddPlot((Brush)(object)Brushes.Transparent, "HAHigh");
			AddPlot((Brush)(object)Brushes.Transparent, "HALow");
			AddPlot((Brush)(object)Brushes.Transparent, "HAClose");
		}
		else
		{
			_ = State;
			_ = 2;
		}
	}

	protected override void OnBarUpdate()
	{
		BarBrushes[0] = (Brush)(object)Brushes.Transparent;
		CandleOutlineBrushes[0] = (Brush)(object)Brushes.Transparent;
		if (CurrentBar == 0)
		{
			HAOpen[0] = Open[0];
			HAHigh[0] = High[0];
			HALow[0] = Low[0];
			HAClose[0] = Close[0];
			return;
		}
		HAClose[0] = (Open[0] + High[0] + Low[0] + Close[0]) * 0.25;
		HAOpen[0] = (HAOpen[1] + HAClose[1]) * 0.5;
		if (UseStableCalculation)
		{
			HAHigh[0] = Math.Max(High[0], Math.Max(HAOpen[0], HAClose[0]));
			HALow[0] = Math.Min(Low[0], Math.Min(HAOpen[0], HAClose[0]));
		}
		else
		{
			HAHigh[0] = Math.Max(High[0], HAOpen[0]);
			HALow[0] = Math.Min(Low[0], HAOpen[0]);
		}
	}

	public override void OnCalculateMinMax()
	{
		OnCalculateMinMax();
		if (Bars == null || ChartControl == null)
		{
			return;
		}
		for (int i = ChartBars.FromIndex; i <= ChartBars.ToIndex; i++)
		{
			double valueAt = HAHigh.GetValueAt(i);
			double valueAt2 = HALow.GetValueAt(i);
			if (valueAt != 0.0 && valueAt > MaxValue)
			{
				MaxValue = valueAt;
			}
			if (valueAt2 != 0.0 && valueAt2 < MinValue)
			{
				MinValue = valueAt2;
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		OnRender(chartControl, chartScale);
		if (Bars == null || ChartControl == null || chartScale == null || RenderTarget == null || ChartBars == null)
		{
			return;
		}
		EnsureDxResources();
		int num = Math.Max(ChartBars.FromIndex, BarsRequiredToPlot);
		int num2 = Math.Min(ChartBars.ToIndex, CurrentBar);
		if (num2 < num)
		{
			return;
		}
		float num3 = Math.Max(1f, (float)Math.Round(ChartControl.BarWidth));
		float num4 = num3 * 0.5f;
		float num5 = Math.Max(1f, shadowWidth);
		AntialiasMode antialiasMode = RenderTarget.AntialiasMode;
		RenderTarget.AntialiasMode = (AntialiasMode)1;
		try
		{
			for (int i = num; i <= num2; i++)
			{
				int num6 = i - Displacement;
				if (num6 < BarsRequiredToPlot || num6 < 0 || num6 >= BarsArray[0].Count)
				{
					continue;
				}
				double valueAt = HAHigh.GetValueAt(num6);
				double valueAt2 = HALow.GetValueAt(num6);
				double valueAt3 = HAClose.GetValueAt(num6);
				double valueAt4 = HAOpen.GetValueAt(num6);
				if (!double.IsNaN(valueAt) && !double.IsNaN(valueAt2) && !double.IsNaN(valueAt3) && !double.IsNaN(valueAt4))
				{
					float num7 = SnapXToPixelCenter(chartControl.GetXByBarIndex(ChartBars, i), num5);
					float val = chartScale.GetYByValue(valueAt4);
					float num8 = chartScale.GetYByValue(valueAt);
					float num9 = chartScale.GetYByValue(valueAt2);
					float val2 = chartScale.GetYByValue(valueAt3);
					SolidColorBrush val3 = ((valueAt3 >= valueAt4) ? dxBarUpBrush : dxBarDownBrush);
					SolidColorBrush val4 = (UseBodyColorForWicks ? val3 : dxShadowBrush);
					float num10 = Math.Min(val, val2);
					float num11 = Math.Max(val, val2);
					float num12 = (float)Math.Round(num7 - num4);
					float num13 = num12 + num3;
					if (num13 <= num12)
					{
						num13 = num12 + 1f;
					}
					float num14 = num13 - num12;
					if (num11 - num10 <= 1f)
					{
						RenderTarget.DrawLine(new Vector2(num12, num10), new Vector2(num13, num10), (Brush)(object)val3, num5);
					}
					else
					{
						RenderTarget.FillRectangle(new RectangleF(num12, num10, num14, num11 - num10), (Brush)(object)val3);
					}
					if (num8 < num10)
					{
						RenderTarget.DrawLine(new Vector2(num7, num8), new Vector2(num7, num10), (Brush)(object)val4, num5);
					}
					if (num9 > num11)
					{
						RenderTarget.DrawLine(new Vector2(num7, num11), new Vector2(num7, num9), (Brush)(object)val4, num5);
					}
				}
			}
		}
		finally
		{
			RenderTarget.AntialiasMode = antialiasMode;
		}
	}

	private void EnsureDxResources()
	{
		if (RenderTarget == null)
		{
			return;
		}
		Color4 val = ToDxColor(GetBrushColor(barColorUp, Colors.BlueViolet));
		Color4 val2 = ToDxColor(GetBrushColor(barColorDown, Colors.White));
		Color4 val3 = ToDxColor(GetBrushColor(shadowColor, Colors.DimGray));
		if (dxBarUpBrush == null || !ColorEquals(dxBarUpBrush.Color, val))
		{
			SolidColorBrush obj = dxBarUpBrush;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			dxBarUpBrush = new SolidColorBrush(RenderTarget, val);
		}
		if (dxBarDownBrush == null || !ColorEquals(dxBarDownBrush.Color, val2))
		{
			SolidColorBrush obj2 = dxBarDownBrush;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			dxBarDownBrush = new SolidColorBrush(RenderTarget, val2);
		}
		if (dxShadowBrush == null || !ColorEquals(dxShadowBrush.Color, val3))
		{
			SolidColorBrush obj3 = dxShadowBrush;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			dxShadowBrush = new SolidColorBrush(RenderTarget, val3);
		}
	}

	private void DisposeDxResources()
	{
		SolidColorBrush obj = dxBarUpBrush;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		dxBarUpBrush = null;
		SolidColorBrush obj2 = dxBarDownBrush;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		dxBarDownBrush = null;
		SolidColorBrush obj3 = dxShadowBrush;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
		dxShadowBrush = null;
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		OnRenderTargetChanged();
	}

	private static Color GetBrushColor(Brush brush, Color fallback)
	{
		SolidColorBrush val = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
		if (val == null)
		{
			return fallback;
		}
		return val.Color;
	}

	private static Color4 ToDxColor(Color color)
	{
		return new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, (float)(int)color.A / 255f);
	}

	private static bool ColorEquals(Color4 a, Color4 b)
	{
		if (Math.Abs(a.Red - b.Red) < 0.0001f && Math.Abs(a.Green - b.Green) < 0.0001f && Math.Abs(a.Blue - b.Blue) < 0.0001f)
		{
			return Math.Abs(a.Alpha - b.Alpha) < 0.0001f;
		}
		return false;
	}

	private static float SnapXToPixelCenter(float x, float strokeWidth)
	{
		if ((Math.Max(1, (int)Math.Round(strokeWidth)) & 1) == 1)
		{
			return (float)Math.Floor(x) + 0.5f;
		}
		return (float)Math.Round(x);
	}
}
}
