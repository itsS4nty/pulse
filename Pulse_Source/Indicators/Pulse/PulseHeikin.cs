#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
	public class PulseHeikin : Indicator
	{
		private System.Windows.Media.Brush barColorDown = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));

		private System.Windows.Media.Brush barColorUp = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));

		private System.Windows.Media.Brush shadowColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 188, 198));

		private int shadowWidth = 1;

		private bool useStableCalculation = true;

		private bool useBodyColorForWicks = true;

		private bool updateOnTicks;

		private SharpDX.Direct2D1.SolidColorBrush dxBarUpBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxBarDownBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxShadowBrush;

		[XmlIgnore]
		[Display(Name = "Bar Color Down", Description = "Color de velas bajistas", Order = 1, GroupName = "Visual")]
		public System.Windows.Media.Brush BarColorDown
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
		public System.Windows.Media.Brush BarColorUp
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
		public System.Windows.Media.Brush ShadowColor
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
				Calculate = (value ? Calculate.OnEachTick : Calculate.OnBarClose);
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
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = false;
				BarsRequiredToPlot = 1;
				AddPlot(Brushes.Transparent, "HAOpen");
				AddPlot(Brushes.Transparent, "HAHigh");
				AddPlot(Brushes.Transparent, "HALow");
				AddPlot(Brushes.Transparent, "HAClose");
			}
			else
			{
				_ = State;
				_ = 2;
			}
		}

		protected override void OnBarUpdate()
		{
			BarBrushes[0] = Brushes.Transparent;
			CandleOutlineBrushes[0] = Brushes.Transparent;
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
			base.OnCalculateMinMax();
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
			base.OnRender(chartControl, chartScale);
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
			float strokeWidth = Math.Max(1f, shadowWidth);
			AntialiasMode antialiasMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;
			try
			{
				for (int i = num; i <= num2; i++)
				{
					int num5 = i - Displacement;
					if (num5 < BarsRequiredToPlot || num5 < 0 || num5 >= BarsArray[0].Count)
					{
						continue;
					}
					double valueAt = HAHigh.GetValueAt(num5);
					double valueAt2 = HALow.GetValueAt(num5);
					double valueAt3 = HAClose.GetValueAt(num5);
					double valueAt4 = HAOpen.GetValueAt(num5);
					if (!double.IsNaN(valueAt) && !double.IsNaN(valueAt2) && !double.IsNaN(valueAt3) && !double.IsNaN(valueAt4))
					{
						float num6 = SnapXToPixelCenter(chartControl.GetXByBarIndex(ChartBars, i), strokeWidth);
						float val = chartScale.GetYByValue(valueAt4);
						float num7 = chartScale.GetYByValue(valueAt);
						float num8 = chartScale.GetYByValue(valueAt2);
						float val2 = chartScale.GetYByValue(valueAt3);
						SharpDX.Direct2D1.SolidColorBrush solidColorBrush = ((valueAt3 >= valueAt4) ? dxBarUpBrush : dxBarDownBrush);
						SharpDX.Direct2D1.SolidColorBrush brush = (UseBodyColorForWicks ? solidColorBrush : dxShadowBrush);
						float num9 = Math.Min(val, val2);
						float num10 = Math.Max(val, val2);
						float num11 = (float)Math.Round(num6 - num4);
						float num12 = num11 + num3;
						if (num12 <= num11)
						{
							num12 = num11 + 1f;
						}
						float width = num12 - num11;
						if (num10 - num9 <= 1f)
						{
							RenderTarget.DrawLine(new Vector2(num11, num9), new Vector2(num12, num9), solidColorBrush, strokeWidth);
						}
						else
						{
							RenderTarget.FillRectangle(new RectangleF(num11, num9, width, num10 - num9), solidColorBrush);
						}
						if (num7 < num9)
						{
							RenderTarget.DrawLine(new Vector2(num6, num7), new Vector2(num6, num9), brush, strokeWidth);
						}
						if (num8 > num10)
						{
							RenderTarget.DrawLine(new Vector2(num6, num10), new Vector2(num6, num8), brush, strokeWidth);
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
			if (RenderTarget != null)
			{
				Color4 color = ToDxColor(GetBrushColor(barColorUp, System.Windows.Media.Color.FromRgb(255, 255, 255)));
				Color4 color2 = ToDxColor(GetBrushColor(barColorDown, System.Windows.Media.Color.FromRgb(26, 26, 26)));
				Color4 color3 = ToDxColor(GetBrushColor(shadowColor, System.Windows.Media.Color.FromRgb(184, 188, 198)));
				if (dxBarUpBrush == null || !ColorEquals(dxBarUpBrush.Color, color))
				{
					dxBarUpBrush?.Dispose();
					dxBarUpBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color);
				}
				if (dxBarDownBrush == null || !ColorEquals(dxBarDownBrush.Color, color2))
				{
					dxBarDownBrush?.Dispose();
					dxBarDownBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color2);
				}
				if (dxShadowBrush == null || !ColorEquals(dxShadowBrush.Color, color3))
				{
					dxShadowBrush?.Dispose();
					dxShadowBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color3);
				}
			}
		}

		private void DisposeDxResources()
		{
			dxBarUpBrush?.Dispose();
			dxBarUpBrush = null;
			dxBarDownBrush?.Dispose();
			dxBarDownBrush = null;
			dxShadowBrush?.Dispose();
			dxShadowBrush = null;
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}

		private static System.Windows.Media.Color GetBrushColor(System.Windows.Media.Brush brush, System.Windows.Media.Color fallback)
		{
			if (!(brush is System.Windows.Media.SolidColorBrush solidColorBrush))
			{
				return fallback;
			}
			return solidColorBrush.Color;
		}

		private static Color4 ToDxColor(System.Windows.Media.Color color)
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
