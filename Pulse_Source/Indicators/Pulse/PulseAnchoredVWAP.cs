#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.Pulse
{
	public class PulseAnchoredVWAP : Indicator
	{
		private const int AnchorSyncThrottleMs = 60;

		private const bool EnableDebugLogging = false;

		private readonly List<double> barVolume = new List<double>(4096);

		private readonly List<double> barVolumePrice = new List<double>(4096);

		private readonly List<double> barVolumeSquared = new List<double>(4096);

		private readonly List<double> cumulativeVolume = new List<double>(4096);

		private readonly List<double> cumulativeVolumePrice = new List<double>(4096);

		private readonly List<double> cumulativeVolumeSquared = new List<double>(4096);

		private int lastProcessedBar = -1;

		private int anchorBarIndex = -1;

		private DateTime anchorTime = Globals.MinDate;

		private string anchorTag = string.Empty;

		private DateTime nextAnchorSyncAtUtc = DateTime.MinValue;

		private bool fullRebuildPending = true;

		private bool rebuildQueued;

		private bool showStandardDeviations = true;

		private double sd1Multiplier = 1.0;

		private double sd2Multiplier = 2.0;

		private PulseAnchoredVWAPPriceSource priceSource;

		private bool preferSelectedDrawing = true;

		private bool useLatestIfNoneSelected = true;

		private string anchorTagFilter = string.Empty;

		private bool showLevelLabels = true;

		private int labelTextSize = 12;

		private double labelOffsetX = -10.0;

		private System.Windows.Media.Brush vwapLabelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));

		private System.Windows.Media.Brush sd1LabelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));

		private System.Windows.Media.Brush sd2LabelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));

		private TextFormat labelTextFormat;

		private SharpDX.Direct2D1.SolidColorBrush vwapLabelBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush sd1LabelBrushDx;

		private SharpDX.Direct2D1.SolidColorBrush sd2LabelBrushDx;

		[NinjaScriptProperty]
		[Display(Name = "Show Standard Deviations", Description = "Mostrar bandas de desviacion estandar", Order = 1, GroupName = "VWAP")]
		public bool ShowStandardDeviations
		{
			get
			{
				return showStandardDeviations;
			}
			set
			{
				if (showStandardDeviations != value)
				{
					showStandardDeviations = value;
					MarkRebuildRequired();
				}
			}
		}

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "SD1 Multiplier", Description = "Multiplicador para la primera desviacion", Order = 2, GroupName = "VWAP")]
		public double SD1Multiplier
		{
			get
			{
				return sd1Multiplier;
			}
			set
			{
				double @double = Math.Max(0.1, Math.Min(10.0, value));
				if (sd1Multiplier.ApproxCompare(@double) != 0)
				{
					sd1Multiplier = @double;
					MarkRebuildRequired();
				}
			}
		}

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "SD2 Multiplier", Description = "Multiplicador para la segunda desviacion", Order = 3, GroupName = "VWAP")]
		public double SD2Multiplier
		{
			get
			{
				return sd2Multiplier;
			}
			set
			{
				double @double = Math.Max(0.1, Math.Min(10.0, value));
				if (sd2Multiplier.ApproxCompare(@double) != 0)
				{
					sd2Multiplier = @double;
					MarkRebuildRequired();
				}
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Price Source", Description = "Precio usado para el calculo del VWAP", Order = 4, GroupName = "VWAP")]
		public PulseAnchoredVWAPPriceSource PriceSource
		{
			get
			{
				return priceSource;
			}
			set
			{
				if (priceSource != value)
				{
					priceSource = value;
					MarkRebuildRequired();
				}
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Prefer Selected Drawing", Description = "Prioriza el dibujo seleccionado como ancla", Order = 1, GroupName = "Anchor")]
		public bool PreferSelectedDrawing
		{
			get
			{
				return preferSelectedDrawing;
			}
			set
			{
				if (preferSelectedDrawing != value)
				{
					preferSelectedDrawing = value;
					MarkRebuildRequired();
				}
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Use Latest If None Selected", Description = "Si no hay dibujo seleccionado, usa el dibujo mas reciente", Order = 2, GroupName = "Anchor")]
		public bool UseLatestIfNoneSelected
		{
			get
			{
				return useLatestIfNoneSelected;
			}
			set
			{
				if (useLatestIfNoneSelected != value)
				{
					useLatestIfNoneSelected = value;
					MarkRebuildRequired();
				}
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Anchor Tag Filter", Description = "Si se define, solo usa dibujos cuyo Tag contenga este texto", Order = 3, GroupName = "Anchor")]
		public string AnchorTagFilter
		{
			get
			{
				return anchorTagFilter;
			}
			set
			{
				string b = ((value == null) ? string.Empty : value.Trim());
				if (!string.Equals(anchorTagFilter, b, StringComparison.Ordinal))
				{
					anchorTagFilter = b;
					MarkRebuildRequired();
				}
			}
		}

		[Display(Name = "Show Labels", Description = "Mostrar etiquetas VWAP/SD1/SD2", Order = 1, GroupName = "Labels")]
		public bool ShowLabels
		{
			get
			{
				return showLevelLabels;
			}
			set
			{
				showLevelLabels = value;
			}
		}

		[Range(8, 40)]
		[Display(Name = "Label Text Size", Description = "Tamano de letra de etiquetas", Order = 2, GroupName = "Labels")]
		public int LabelTextSize
		{
			get
			{
				return labelTextSize;
			}
			set
			{
				int num = Math.Max(8, Math.Min(40, value));
				if (labelTextSize != num)
				{
					labelTextSize = num;
					DisposeLabelResources();
				}
			}
		}

		[Range(-300, 300)]
		[Display(Name = "Label Offset X", Description = "Desplazamiento horizontal de etiquetas (negativo = izquierda)", Order = 3, GroupName = "Labels")]
		public double LabelOffsetX
		{
			get
			{
				return labelOffsetX;
			}
			set
			{
				labelOffsetX = Math.Max(-300.0, Math.Min(300.0, value));
			}
		}

		[XmlIgnore]
		[Display(Name = "VWAP Label Color", Description = "Color etiqueta VWAP", Order = 1, GroupName = "Label Colors")]
		public System.Windows.Media.Brush VwapLabelBrush
		{
			get
			{
				return vwapLabelBrush;
			}
			set
			{
				vwapLabelBrush = value;
				if (vwapLabelBrushDx != null)
				{
					vwapLabelBrushDx.Dispose();
					vwapLabelBrushDx = null;
				}
			}
		}

		[Browsable(false)]
		public string VwapLabelBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(vwapLabelBrush);
			}
			set
			{
				VwapLabelBrush = Serialize.StringToBrush(value);
			}
		}

		[XmlIgnore]
		[Display(Name = "SD1 Label Color", Description = "Color etiquetas SD1", Order = 2, GroupName = "Label Colors")]
		public System.Windows.Media.Brush SD1LabelBrush
		{
			get
			{
				return sd1LabelBrush;
			}
			set
			{
				sd1LabelBrush = value;
				if (sd1LabelBrushDx != null)
				{
					sd1LabelBrushDx.Dispose();
					sd1LabelBrushDx = null;
				}
			}
		}

		[Browsable(false)]
		public string SD1LabelBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(sd1LabelBrush);
			}
			set
			{
				SD1LabelBrush = Serialize.StringToBrush(value);
			}
		}

		[XmlIgnore]
		[Display(Name = "SD2 Label Color", Description = "Color etiquetas SD2", Order = 3, GroupName = "Label Colors")]
		public System.Windows.Media.Brush SD2LabelBrush
		{
			get
			{
				return sd2LabelBrush;
			}
			set
			{
				sd2LabelBrush = value;
				if (sd2LabelBrushDx != null)
				{
					sd2LabelBrushDx.Dispose();
					sd2LabelBrushDx = null;
				}
			}
		}

		[Browsable(false)]
		public string SD2LabelBrushSerializable
		{
			get
			{
				return Serialize.BrushToString(sd2LabelBrush);
			}
			set
			{
				SD2LabelBrush = Serialize.StringToBrush(value);
			}
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> AVWAP => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> UpperBand1 => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowerBand1 => Values[2];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> UpperBand2 => Values[3];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowerBand2 => Values[4];

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse Anchored VWAP - VWAP + desviaciones desde un dibujo/anchor";
				Name = "PulseAnchoredVWAP";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				showStandardDeviations = true;
				sd1Multiplier = 1.0;
				sd2Multiplier = 2.0;
				priceSource = PulseAnchoredVWAPPriceSource.TypicalPrice;
				preferSelectedDrawing = true;
				useLatestIfNoneSelected = true;
				anchorTagFilter = string.Empty;
				showLevelLabels = true;
				labelTextSize = 12;
				labelOffsetX = -10.0;
				vwapLabelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				sd1LabelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				sd2LabelBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204)), 2f), PlotStyle.Line, "AVWAP");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), 1f), PlotStyle.Line, "UpperBand1");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), 1f), PlotStyle.Line, "LowerBand1");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), 1f), PlotStyle.Line, "UpperBand2");
				AddPlot(new Stroke(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)), 1f), PlotStyle.Line, "LowerBand2");
			}
			else if (State == State.DataLoaded)
			{
				ClearRuntimeState();
			}
			else if (State == State.Terminated)
			{
				barVolume.Clear();
				barVolumePrice.Clear();
				barVolumeSquared.Clear();
				cumulativeVolume.Clear();
				cumulativeVolumePrice.Clear();
				cumulativeVolumeSquared.Clear();
				DisposeLabelResources();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 0 && CurrentBar >= 0)
			{
				UpdateBarAccumulators();
				if (SyncAnchorFromDrawObjects(force: false, scheduleRebuild: false) || fullRebuildPending)
				{
					RebuildAllPlotsFromAnchor();
					fullRebuildPending = false;
				}
				else
				{
					UpdateCurrentBarPlots();
				}
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			SyncAnchorFromDrawObjects(force: true, scheduleRebuild: true);
			RenderLevelLabels(chartControl, chartScale);
		}

		private void ClearRuntimeState()
		{
			barVolume.Clear();
			barVolumePrice.Clear();
			barVolumeSquared.Clear();
			cumulativeVolume.Clear();
			cumulativeVolumePrice.Clear();
			cumulativeVolumeSquared.Clear();
			lastProcessedBar = -1;
			anchorBarIndex = -1;
			anchorTime = Globals.MinDate;
			anchorTag = string.Empty;
			nextAnchorSyncAtUtc = DateTime.MinValue;
			fullRebuildPending = true;
			rebuildQueued = false;
		}

		private void UpdateBarAccumulators()
		{
			int currentBar = CurrentBar;
			EnsureCapacity(currentBar);
			double num = Math.Max(0.0, Volume[0]);
			double priceSourceValue = GetPriceSourceValue();
			double num2 = priceSourceValue * num;
			double num3 = priceSourceValue * priceSourceValue * num;
			barVolume[currentBar] = num;
			barVolumePrice[currentBar] = num2;
			barVolumeSquared[currentBar] = num3;
			double num4 = ((currentBar > 0) ? cumulativeVolume[currentBar - 1] : 0.0);
			double num5 = ((currentBar > 0) ? cumulativeVolumePrice[currentBar - 1] : 0.0);
			double num6 = ((currentBar > 0) ? cumulativeVolumeSquared[currentBar - 1] : 0.0);
			cumulativeVolume[currentBar] = num4 + num;
			cumulativeVolumePrice[currentBar] = num5 + num2;
			cumulativeVolumeSquared[currentBar] = num6 + num3;
			lastProcessedBar = currentBar;
		}

		private void EnsureCapacity(int barIndex)
		{
			int num = barIndex + 1;
			while (barVolume.Count < num)
			{
				barVolume.Add(0.0);
				barVolumePrice.Add(0.0);
				barVolumeSquared.Add(0.0);
				cumulativeVolume.Add(0.0);
				cumulativeVolumePrice.Add(0.0);
				cumulativeVolumeSquared.Add(0.0);
			}
		}

		private double GetPriceSourceValue()
		{
			if (priceSource == PulseAnchoredVWAPPriceSource.Close)
			{
				return Close[0];
			}
			if (priceSource == PulseAnchoredVWAPPriceSource.HL2)
			{
				return (High[0] + Low[0]) * 0.5;
			}
			if (priceSource == PulseAnchoredVWAPPriceSource.OHLC4)
			{
				return (Open[0] + High[0] + Low[0] + Close[0]) * 0.25;
			}
			return (High[0] + Low[0] + Close[0]) / 3.0;
		}

		private bool SyncAnchorFromDrawObjects(bool force, bool scheduleRebuild)
		{
			if (DrawObjects == null)
			{
				return false;
			}
			DateTime utcNow = DateTime.UtcNow;
			if (!force && utcNow < nextAnchorSyncAtUtc)
			{
				return false;
			}
			nextAnchorSyncAtUtc = utcNow.AddMilliseconds(60.0);
			bool flag = false;
			bool flag2 = false;
			int num = -1;
			DateTime dateTime = Globals.MinDate;
			string text = string.Empty;
			foreach (IDrawingTool drawObject in DrawObjects)
			{
				if (!TryExtractAnchor(drawObject, out var anchor, out var tag, out var isSelected))
				{
					continue;
				}
				int num2 = ResolveAnchorBarIndex(anchor);
				if (num2 < 0 || num2 > CurrentBar)
				{
					continue;
				}
				if (!flag)
				{
					flag = true;
					flag2 = isSelected;
					num = num2;
					dateTime = anchor.Time;
					text = tag;
					continue;
				}
				if (preferSelectedDrawing)
				{
					if (isSelected && !flag2)
					{
						flag2 = true;
						num = num2;
						dateTime = anchor.Time;
						text = tag;
						continue;
					}
					if (!isSelected && flag2)
					{
						continue;
					}
				}
				if (num2 > num || (num2 == num && anchor.Time > dateTime))
				{
					flag2 = isSelected;
					num = num2;
					dateTime = anchor.Time;
					text = tag;
				}
			}
			if (preferSelectedDrawing && !flag2 && !useLatestIfNoneSelected)
			{
				flag = false;
			}
			int num3 = (flag ? num : (-1));
			DateTime dateTime2 = (flag ? dateTime : Globals.MinDate);
			string a = (flag ? text : string.Empty);
			if (num3 == anchorBarIndex && !(dateTime2 != anchorTime) && string.Equals(a, anchorTag, StringComparison.Ordinal))
			{
				return false;
			}
			anchorBarIndex = num3;
			anchorTime = dateTime2;
			anchorTag = a;
			fullRebuildPending = true;
			if (scheduleRebuild)
			{
				ScheduleRebuild();
			}
			return true;
		}

		private bool TryExtractAnchor(object drawObj, out ChartAnchor anchor, out string tag, out bool isSelected)
		{
			anchor = null;
			tag = string.Empty;
			isSelected = false;
			if (drawObj == null)
			{
				return false;
			}
			if (drawObj is DrawingTool drawingTool)
			{
				tag = drawingTool.Tag ?? string.Empty;
				isSelected = drawingTool.IsSelected;
			}
			if (!PassTagFilter(tag))
			{
				return false;
			}
			if (!IsTriangleUpMarker(drawObj))
			{
				return false;
			}
			ChartAnchor chartAnchor = ReadAnchorProperty(drawObj, "StartAnchor");
			if (IsValidAnchor(chartAnchor))
			{
				anchor = chartAnchor;
				return true;
			}
			ChartAnchor chartAnchor2 = ReadAnchorProperty(drawObj, "Anchor");
			if (IsValidAnchor(chartAnchor2))
			{
				anchor = chartAnchor2;
				return true;
			}
			ChartAnchor chartAnchor3 = ReadAnchorProperty(drawObj, "EndAnchor");
			if (IsValidAnchor(chartAnchor3))
			{
				anchor = chartAnchor3;
				return true;
			}
			return false;
		}

		private bool IsTriangleUpMarker(object drawObj)
		{
			if (drawObj == null)
			{
				return false;
			}
			Type type = drawObj.GetType();
			if (((type != null && type.Name != null) ? type.Name : string.Empty).IndexOf("TriangleUp", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string text = ReadStringProperty(drawObj, "MarkerType");
			if (text.IndexOf("TriangleUp", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Triangle Up", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string text2 = ReadStringProperty(drawObj, "Shape");
			if (text2.IndexOf("TriangleUp", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("Triangle Up", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string text3 = ReadStringProperty(drawObj, "DisplayName");
			if (text3.IndexOf("TriangleUp", StringComparison.OrdinalIgnoreCase) >= 0 || text3.IndexOf("Triangle Up", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			return false;
		}

		private bool PassTagFilter(string tag)
		{
			if (string.IsNullOrWhiteSpace(anchorTagFilter))
			{
				return true;
			}
			if (string.IsNullOrEmpty(tag))
			{
				return false;
			}
			return tag.IndexOf(anchorTagFilter, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private ChartAnchor ReadAnchorProperty(object source, string propertyName)
		{
			try
			{
				PropertyInfo property = source.GetType().GetProperty(propertyName);
				if (property == null)
				{
					return null;
				}
				return property.GetValue(source, null) as ChartAnchor;
			}
			catch
			{
				return null;
			}
		}

		private string ReadStringProperty(object source, string propertyName)
		{
			try
			{
				PropertyInfo property = source.GetType().GetProperty(propertyName);
				if (property == null)
				{
					return string.Empty;
				}
				object value = property.GetValue(source, null);
				return (value != null) ? value.ToString() : string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private static bool IsValidAnchor(ChartAnchor anchor)
		{
			if (anchor != null)
			{
				return anchor.Time != Globals.MinDate;
			}
			return false;
		}

		private int ResolveAnchorBarIndex(ChartAnchor anchor)
		{
			Bars primaryBarsSeries = GetPrimaryBarsSeries();
			if (!IsValidAnchor(anchor) || primaryBarsSeries == null)
			{
				return -1;
			}
			int num = -1;
			if (ChartControl != null && ChartBars != null)
			{
				float num2 = ChartControl.GetXByTime(anchor.Time);
				if (!float.IsNaN(num2) && !float.IsInfinity(num2))
				{
					int barIdxByX = ChartBars.GetBarIdxByX(ChartControl, (int)num2);
					if (barIdxByX >= 0)
					{
						num = ClampBarIndex(barIdxByX, primaryBarsSeries.Count);
					}
				}
			}
			int bar = primaryBarsSeries.GetBar(anchor.Time);
			if (bar >= 0 && bar < primaryBarsSeries.Count)
			{
				if (num >= 0 && Math.Abs((long)bar - (long)num) > 5000)
				{
					return num;
				}
				return bar;
			}
			if (num >= 0)
			{
				return num;
			}
			try
			{
				PropertyInfo property = anchor.GetType().GetProperty("SlotIndex");
				if (property != null && property.GetValue(anchor, null) is int num3 && num3 >= 0)
				{
					if (num3 < primaryBarsSeries.Count)
					{
						return num3;
					}
					if (ChartBars != null)
					{
						int num4 = Math.Max(0, ChartBars.ToIndex - ChartBars.FromIndex);
						if (num3 <= num4)
						{
							int num5 = ChartBars.FromIndex + num3;
							if (num5 >= 0 && num5 < primaryBarsSeries.Count)
							{
								return num5;
							}
						}
					}
				}
			}
			catch
			{
			}
			return -1;
		}

		private Bars GetPrimaryBarsSeries()
		{
			if (BarsArray != null && BarsArray.Length != 0 && BarsArray[0] != null)
			{
				return BarsArray[0];
			}
			return Bars;
		}

		private static int ClampBarIndex(int barIndex, int primaryBarsCount)
		{
			if (primaryBarsCount <= 0)
			{
				return 0;
			}
			return Math.Max(0, Math.Min(barIndex, primaryBarsCount - 1));
		}

		private void ScheduleRebuild()
		{
			if (rebuildQueued || State == State.SetDefaults || State == State.Terminated)
			{
				return;
			}
			rebuildQueued = true;
			TriggerCustomEvent(delegate
			{
				rebuildQueued = false;
				if (CurrentBar >= 0)
				{
					RebuildAllPlotsFromAnchor();
					fullRebuildPending = false;
					if (ChartControl != null)
					{
						ChartControl.InvalidateVisual();
					}
				}
			}, null);
		}

		private void RebuildAllPlotsFromAnchor()
		{
			if (CurrentBar < 0)
			{
				return;
			}
			for (int i = 0; i <= CurrentBar; i++)
			{
				int barsAgo = CurrentBar - i;
				if (!TryComputeFromAnchorToBar(i, out var vwap, out var standardDeviation))
				{
					ResetAllPlotsAt(barsAgo);
				}
				else
				{
					ApplyPlotsAt(barsAgo, vwap, standardDeviation);
				}
			}
		}

		private void UpdateCurrentBarPlots()
		{
			if (!TryComputeFromAnchorToBar(CurrentBar, out var vwap, out var standardDeviation))
			{
				ResetAllPlotsAt(0);
			}
			else
			{
				ApplyPlotsAt(0, vwap, standardDeviation);
			}
		}

		private bool TryComputeFromAnchorToBar(int barIndex, out double vwap, out double standardDeviation)
		{
			vwap = double.NaN;
			standardDeviation = 0.0;
			if (anchorBarIndex < 0 || barIndex < anchorBarIndex)
			{
				return false;
			}
			if (barIndex >= cumulativeVolume.Count)
			{
				return false;
			}
			double rangeValue = GetRangeValue(cumulativeVolume, anchorBarIndex, barIndex);
			if (rangeValue <= 0.0)
			{
				return false;
			}
			double rangeValue2 = GetRangeValue(cumulativeVolumePrice, anchorBarIndex, barIndex);
			double rangeValue3 = GetRangeValue(cumulativeVolumeSquared, anchorBarIndex, barIndex);
			vwap = rangeValue2 / rangeValue;
			double num = rangeValue3 / rangeValue - vwap * vwap;
			if (num < 0.0)
			{
				num = 0.0;
			}
			standardDeviation = Math.Sqrt(num);
			return true;
		}

		private static double GetRangeValue(List<double> cumulative, int startBarIndex, int endBarIndex)
		{
			if (endBarIndex < startBarIndex || endBarIndex < 0 || endBarIndex >= cumulative.Count)
			{
				return 0.0;
			}
			double num = cumulative[endBarIndex];
			double num2 = ((startBarIndex > 0) ? cumulative[startBarIndex - 1] : 0.0);
			return num - num2;
		}

		private void ApplyPlotsAt(int barsAgo, double vwap, double standardDeviation)
		{
			Values[0][barsAgo] = vwap;
			if (showStandardDeviations && standardDeviation > 0.0)
			{
				Values[1][barsAgo] = vwap + standardDeviation * sd1Multiplier;
				Values[2][barsAgo] = vwap - standardDeviation * sd1Multiplier;
				Values[3][barsAgo] = vwap + standardDeviation * sd2Multiplier;
				Values[4][barsAgo] = vwap - standardDeviation * sd2Multiplier;
			}
			else
			{
				Values[1].Reset(barsAgo);
				Values[2].Reset(barsAgo);
				Values[3].Reset(barsAgo);
				Values[4].Reset(barsAgo);
			}
		}

		private void ResetAllPlotsAt(int barsAgo)
		{
			Values[0].Reset(barsAgo);
			Values[1].Reset(barsAgo);
			Values[2].Reset(barsAgo);
			Values[3].Reset(barsAgo);
			Values[4].Reset(barsAgo);
		}

		private void MarkRebuildRequired()
		{
			fullRebuildPending = true;
			ScheduleRebuild();
		}

		private void RenderLevelLabels(ChartControl chartControl, ChartScale chartScale)
		{
			if (!showLevelLabels || RenderTarget == null || chartControl == null || chartScale == null || ChartPanel == null || CurrentBar < 0 || anchorBarIndex < 0)
			{
				return;
			}
			double num = Values[0][0];
			if (double.IsNaN(num))
			{
				return;
			}
			EnsureLabelResources();
			if (labelTextFormat != null && vwapLabelBrushDx != null && sd1LabelBrushDx != null && sd2LabelBrushDx != null)
			{
				float rightX = ((ChartBars != null) ? ((float)chartControl.GetXByBarIndex(ChartBars, CurrentBar)) : ((float)((double)(ChartPanel.X + ChartPanel.W) - 8.0))) + (float)labelOffsetX;
				DrawLabel("VWAP", num, rightX, chartScale, vwapLabelBrushDx);
				double num2 = Values[1][0];
				double num3 = Values[2][0];
				double num4 = Values[3][0];
				double num5 = Values[4][0];
				if (!double.IsNaN(num2))
				{
					DrawLabel("SD1+", num2, rightX, chartScale, sd1LabelBrushDx);
				}
				if (!double.IsNaN(num3))
				{
					DrawLabel("SD1-", num3, rightX, chartScale, sd1LabelBrushDx);
				}
				if (!double.IsNaN(num4))
				{
					DrawLabel("SD2+", num4, rightX, chartScale, sd2LabelBrushDx);
				}
				if (!double.IsNaN(num5))
				{
					DrawLabel("SD2-", num5, rightX, chartScale, sd2LabelBrushDx);
				}
			}
		}

		private void DrawLabel(string text, double price, float rightX, ChartScale chartScale, SharpDX.Direct2D1.SolidColorBrush brush)
		{
			if (string.IsNullOrEmpty(text) || labelTextFormat == null || brush == null || double.IsNaN(price) || double.IsInfinity(price))
			{
				return;
			}
			float num = chartScale.GetYByValue(price);
			if (!float.IsNaN(num) && !float.IsInfinity(num))
			{
				float num2 = 100f;
				float num3 = 20f;
				float val = rightX - 64f;
				float num4 = (float)ChartPanel.X + 2f;
				float num5 = (float)(ChartPanel.X + ChartPanel.W) - num2 - 2f;
				if (num5 < num4)
				{
					num5 = num4;
				}
				val = Math.Max(num4, Math.Min(num5, val));
				float val2 = num - num3 * 0.5f;
				float num6 = (float)ChartPanel.Y + 1f;
				float num7 = (float)(ChartPanel.Y + ChartPanel.H) - num3 - 1f;
				if (num7 < num6)
				{
					num7 = num6;
				}
				val2 = Math.Max(num6, Math.Min(num7, val2));
				RectangleF layoutRect = new RectangleF(val, val2, num2, num3);
				RenderTarget.DrawText(text, labelTextFormat, layoutRect, brush);
			}
		}

		private void EnsureLabelResources()
		{
			if (RenderTarget != null)
			{
				if (labelTextFormat == null)
				{
					labelTextFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", Math.Max(8f, labelTextSize))
					{
						TextAlignment = TextAlignment.Leading,
						ParagraphAlignment = ParagraphAlignment.Center
					};
				}
				if (vwapLabelBrushDx == null)
				{
					vwapLabelBrushDx = CreateDxBrush(vwapLabelBrush, System.Windows.Media.Color.FromRgb(107, 111, 204));
				}
				if (sd1LabelBrushDx == null)
				{
					sd1LabelBrushDx = CreateDxBrush(sd1LabelBrush, System.Windows.Media.Color.FromRgb(74, 74, 74));
				}
				if (sd2LabelBrushDx == null)
				{
					sd2LabelBrushDx = CreateDxBrush(sd2LabelBrush, System.Windows.Media.Color.FromRgb(74, 74, 74));
				}
			}
		}

		private SharpDX.Direct2D1.SolidColorBrush CreateDxBrush(System.Windows.Media.Brush sourceBrush, System.Windows.Media.Color fallbackColor)
		{
			System.Windows.Media.Color color = ((sourceBrush is System.Windows.Media.SolidColorBrush solidColorBrush) ? solidColorBrush.Color : fallbackColor);
			return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, 1f));
		}

		public override void OnRenderTargetChanged()
		{
			DisposeLabelResources();
			base.OnRenderTargetChanged();
		}

		private void DisposeLabelResources()
		{
			if (vwapLabelBrushDx != null)
			{
				vwapLabelBrushDx.Dispose();
				vwapLabelBrushDx = null;
			}
			if (sd1LabelBrushDx != null)
			{
				sd1LabelBrushDx.Dispose();
				sd1LabelBrushDx = null;
			}
			if (sd2LabelBrushDx != null)
			{
				sd2LabelBrushDx.Dispose();
				sd2LabelBrushDx = null;
			}
			if (labelTextFormat != null)
			{
				labelTextFormat.Dispose();
				labelTextFormat = null;
			}
		}
	}
}
