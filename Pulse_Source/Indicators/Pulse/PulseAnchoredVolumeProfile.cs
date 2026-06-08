#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;
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
	[CategoryOrder("Volume Profile", 1)]
	[CategoryOrder("Value Area", 2)]
	[CategoryOrder("Delta", 3)]
	[CategoryOrder("Labels", 4)]
	[CategoryOrder("Rectangle", 5)]
	[CategoryOrder("Display", 6)]
	public class PulseAnchoredVolumeProfile : Indicator
	{
		private struct PriceLevel
		{
			public int PriceIndex;

			public double Price;

			public long Volume;

			public long Delta;

			public byte Zone;
		}

		private class AnchoredRegion
		{
			public int Id;

			public int StartBarIndex;

			public int EndBarIndex;

			public double HighPrice;

			public double LowPrice;

			public Dictionary<int, long> VolumeByPrice = new Dictionary<int, long>(256);

			public Dictionary<int, long> DeltaByPrice = new Dictionary<int, long>(256);

			public bool IsDirty = true;

			public PriceLevel[] RenderLevels;

			public int RenderLevelCount;

			public long TotalVolume;

			public long MaxVolume;

			public long MaxAbsDelta;

			public int POCIndex;

			public long POCVolume;

			public int VAHIndex;

			public int VALIndex;

			public double POCPrice;

			public double VAHPrice;

			public double VALPrice;

			public string POCLabelCached;

			public string VAHLabelCached;

			public string VALLabelCached;

			public DateTime LastRebuildUtc = DateTime.MinValue;

			public bool IsBeingDragged;
		}

		private enum DragMode
		{
			None,
			Creating,
			MovingBody,
			ResizingTopLeft,
			ResizingTopRight,
			ResizingBottomLeft,
			ResizingBottomRight,
			ResizingLeft,
			ResizingRight,
			ResizingTop,
			ResizingBottom
		}

		private static readonly IComparer<PriceLevel> PriceLevelComparer = Comparer<PriceLevel>.Create((PriceLevel a, PriceLevel b) => a.PriceIndex.CompareTo(b.PriceIndex));

		private List<AnchoredRegion> regions;

		private readonly Dictionary<string, AnchoredRegion> regionsByTag = new Dictionary<string, AnchoredRegion>(StringComparer.Ordinal);

		private readonly HashSet<string> syncSeenRectangleTags = new HashSet<string>(StringComparer.Ordinal);

		private int nextRegionId;

		private const int DrawObjectSyncThrottleMs = 40;

		private DateTime lastDrawObjectSyncUtc = DateTime.MinValue;

		private int tickSeriesIndex = -1;

		private double tickSize;

		private double volumeGroupSize;

		private double inverseVolumeGroupSize;

		private Dictionary<int, Dictionary<int, long>> volumeByBarPrice;

		private Dictionary<int, Dictionary<int, long>> deltaByBarPrice;

		private double lastTickPriceForDelta = double.NaN;

		private int lastTickDirectionForDelta;

		private DragMode currentDrag;

		private AnchoredRegion dragRegion;

		private AnchoredRegion selectedRegion;

		private int dragStartBar;

		private double dragStartPrice;

		private int dragOrigStartBar;

		private int dragOrigEndBar;

		private double dragOrigHighPrice;

		private double dragOrigLowPrice;

		private bool mouseEventsSubscribed;

		private const float AnchorHitRadius = 8f;

		private const float EdgeHitDistance = 6f;

		private const int DragRebuildThrottleMs = 60;

		private const int InvalidateThrottleMs = 16;

		private DateTime lastInvalidateUtc = DateTime.MinValue;

		private bool invalidateQueued;

		private const bool EnableInteractionDebug = false;

		private const int InteractionDebugMoveThrottleMs = 120;

		private DateTime lastInteractionDebugMoveUtc = DateTime.MinValue;

		private int lastSyncedRectangleCount = -1;

		private string lastDrawObjectSyncSummary;

		private readonly HashSet<int> noTickDataWarnedRegionIds = new HashSet<int>();

		private readonly HashSet<int> noRenderedBarsWarnedRegionIds = new HashSet<int>();

		private readonly HashSet<string> preparedSourceRectangleTags = new HashSet<string>(StringComparer.Ordinal);

		private readonly HashSet<int> offscreenRegionWarnedIds = new HashSet<int>();

		private bool dxInitFailureLogged;

		private SharpDX.Direct2D1.SolidColorBrush dxProfileBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxValueAreaBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxPOCBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxDeltaPositiveBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxDeltaNegativeBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxPOCLineBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxVAHLineBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxVALLineBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxPOCLabelBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxVAHLabelBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxVALLabelBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxRectBorderBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxRectFillBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxAnchorBrush;

		private SharpDX.Direct2D1.SolidColorBrush dxSelectedBorderBrush;

		private StrokeStyle dxSolidStroke;

		private StrokeStyle dxDashStroke;

		private TextFormat dxLabelTextFormat;

		private bool dxResourcesValid;

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Tick Compression", Description = "Group price levels by N ticks", Order = 1, GroupName = "Volume Profile")]
		public int VolumeTickCompression { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100000)]
		[Display(Name = "Volume Threshold", Description = "Minimum volume to display a bar", Order = 2, GroupName = "Volume Profile")]
		public int VolumeThreshold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Alignment", Description = "Alignment of profile within rectangle", Order = 3, GroupName = "Volume Profile")]
		public VPAlignment ProfileAlignment { get; set; }

		[NinjaScriptProperty]
		[Range(50, 95)]
		[Display(Name = "Value Area %", Description = "Percentage of volume for Value Area", Order = 1, GroupName = "Value Area")]
		public int ValueAreaPercentage { get; set; }

		[XmlIgnore]
		[Display(Name = "Profile Color", Description = "Color for bars outside Value Area", Order = 2, GroupName = "Value Area")]
		public System.Windows.Media.Brush ProfileColor { get; set; }

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
		public System.Windows.Media.Brush ValueAreaColor { get; set; }

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

		[XmlIgnore]
		[Display(Name = "POC Bar Color", Description = "Color for the POC level bar", Order = 6, GroupName = "Value Area")]
		public System.Windows.Media.Brush POCBarColor { get; set; }

		[Browsable(false)]
		public string POCBarColorSerializable
		{
			get
			{
				return Serialize.BrushToString(POCBarColor);
			}
			set
			{
				POCBarColor = Serialize.StringToBrush(value);
			}
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "POC Bar Opacity", Description = "Opacity for the POC bar", Order = 7, GroupName = "Value Area")]
		public int POCBarOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC Line", Description = "Draw POC line across rectangle", Order = 8, GroupName = "Value Area")]
		public bool ShowPOCLine { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VA Lines", Description = "Draw VAH/VAL lines across rectangle", Order = 9, GroupName = "Value Area")]
		public bool ShowVALines { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Line Color", Order = 10, GroupName = "Value Area")]
		public System.Windows.Media.Brush POCLineColor { get; set; }

		[Browsable(false)]
		public string POCLineColorSerializable
		{
			get
			{
				return Serialize.BrushToString(POCLineColor);
			}
			set
			{
				POCLineColor = Serialize.StringToBrush(value);
			}
		}

		[NinjaScriptProperty]
		[Range(0.5, 10.0)]
		[Display(Name = "POC Line Width", Order = 11, GroupName = "Value Area")]
		public float POCLineWidth { get; set; }

		[XmlIgnore]
		[Display(Name = "VAH Line Color", Order = 12, GroupName = "Value Area")]
		public System.Windows.Media.Brush VAHLineColor { get; set; }

		[Browsable(false)]
		public string VAHLineColorSerializable
		{
			get
			{
				return Serialize.BrushToString(VAHLineColor);
			}
			set
			{
				VAHLineColor = Serialize.StringToBrush(value);
			}
		}

		[NinjaScriptProperty]
		[Range(0.5, 10.0)]
		[Display(Name = "VAH Line Width", Order = 13, GroupName = "Value Area")]
		public float VAHLineWidth { get; set; }

		[XmlIgnore]
		[Display(Name = "VAL Line Color", Order = 14, GroupName = "Value Area")]
		public System.Windows.Media.Brush VALLineColor { get; set; }

		[Browsable(false)]
		public string VALLineColorSerializable
		{
			get
			{
				return Serialize.BrushToString(VALLineColor);
			}
			set
			{
				VALLineColor = Serialize.StringToBrush(value);
			}
		}

		[NinjaScriptProperty]
		[Range(0.5, 10.0)]
		[Display(Name = "VAL Line Width", Order = 15, GroupName = "Value Area")]
		public float VALLineWidth { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Bars", Description = "Render horizontal delta bars to the left of the profile", Order = 1, GroupName = "Delta")]
		public bool ShowDeltaBars { get; set; }

		[NinjaScriptProperty]
		[Range(20, 500)]
		[Display(Name = "Delta Width (px)", Description = "Maximum width in pixels for delta bars", Order = 2, GroupName = "Delta")]
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
		public System.Windows.Media.Brush DeltaPositiveColor { get; set; }

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
		public System.Windows.Media.Brush DeltaNegativeColor { get; set; }

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
		[Display(Name = "Show Labels", Description = "Show POC/VAH/VAL labels", Order = 1, GroupName = "Labels")]
		public bool ShowLabels { get; set; }

		[NinjaScriptProperty]
		[Range(6, 30)]
		[Display(Name = "Font Size", Description = "Font size for labels", Order = 2, GroupName = "Labels")]
		public int LabelFontSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Label Offset (px)", Description = "Horizontal offset from line end to label", Order = 3, GroupName = "Labels")]
		public int LabelOffset { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Label Color", Order = 4, GroupName = "Labels")]
		public System.Windows.Media.Brush POCLabelColor { get; set; }

		[Browsable(false)]
		public string POCLabelColorSerializable
		{
			get
			{
				return Serialize.BrushToString(POCLabelColor);
			}
			set
			{
				POCLabelColor = Serialize.StringToBrush(value);
			}
		}

		[XmlIgnore]
		[Display(Name = "VAH Label Color", Order = 5, GroupName = "Labels")]
		public System.Windows.Media.Brush VAHLabelColor { get; set; }

		[Browsable(false)]
		public string VAHLabelColorSerializable
		{
			get
			{
				return Serialize.BrushToString(VAHLabelColor);
			}
			set
			{
				VAHLabelColor = Serialize.StringToBrush(value);
			}
		}

		[XmlIgnore]
		[Display(Name = "VAL Label Color", Order = 6, GroupName = "Labels")]
		public System.Windows.Media.Brush VALLabelColor { get; set; }

		[Browsable(false)]
		public string VALLabelColorSerializable
		{
			get
			{
				return Serialize.BrushToString(VALLabelColor);
			}
			set
			{
				VALLabelColor = Serialize.StringToBrush(value);
			}
		}

		[XmlIgnore]
		[Display(Name = "Rectangle Border Color", Description = "Color for the rectangle outline", Order = 1, GroupName = "Rectangle")]
		public System.Windows.Media.Brush RectangleBorderColor { get; set; }

		[Browsable(false)]
		public string RectangleBorderColorSerializable
		{
			get
			{
				return Serialize.BrushToString(RectangleBorderColor);
			}
			set
			{
				RectangleBorderColor = Serialize.StringToBrush(value);
			}
		}

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Rectangle Fill Opacity", Description = "Opacity of the rectangle background fill (0 = transparent)", Order = 2, GroupName = "Rectangle")]
		public int RectangleFillOpacity { get; set; }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Pulse Anchored Volume Profile â€” Draw native chart rectangles with the Rectangle drawing tool, and the indicator will fill each one with a volume profile (POC, VAH, VAL).";
				Name = "PulseAnchoredVolumeProfile";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				VolumeTickCompression = 1;
				ValueAreaPercentage = 70;
				VolumeThreshold = 0;
				ProfileAlignment = VPAlignment.Left;
				ProfileColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				ValueAreaColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				POCBarColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				ProfileOpacity = 60;
				ValueAreaOpacity = 40;
				POCBarOpacity = 90;
				POCLineColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				VAHLineColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				VALLineColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				POCLineWidth = 2f;
				VAHLineWidth = 1f;
				VALLineWidth = 1f;
				ShowPOCLine = true;
				ShowVALines = true;
				ShowDeltaBars = true;
				DeltaProfileWidth = 120;
				DeltaThreshold = 0;
				DeltaOpacity = 55;
				DeltaPositiveColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 111, 204));
				DeltaNegativeColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				ShowLabels = true;
				LabelFontSize = 11;
				POCLabelColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
				VAHLabelColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
				VALLabelColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
				LabelOffset = 5;
				RectangleBorderColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
				RectangleFillOpacity = 8;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
				tickSeriesIndex = 1;
			}
			else if (State == State.DataLoaded)
			{
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
				inverseVolumeGroupSize = ((volumeGroupSize > 0.0) ? (1.0 / volumeGroupSize) : 0.0);
				regions = new List<AnchoredRegion>(16);
				volumeByBarPrice = new Dictionary<int, Dictionary<int, long>>(4096);
				deltaByBarPrice = new Dictionary<int, Dictionary<int, long>>(4096);
				nextRegionId = 0;
				regionsByTag.Clear();
				syncSeenRectangleTags.Clear();
				noTickDataWarnedRegionIds.Clear();
				noRenderedBarsWarnedRegionIds.Clear();
				preparedSourceRectangleTags.Clear();
				offscreenRegionWarnedIds.Clear();
				dxInitFailureLogged = false;
				lastTickPriceForDelta = double.NaN;
				lastTickDirectionForDelta = 0;
			}
			else if (State == State.Historical)
			{
				InteractionDebug("Using native Rectangle drawing tool as anchor source.");
			}
			else if (State == State.Terminated)
			{
				regionsByTag.Clear();
				syncSeenRectangleTags.Clear();
				noTickDataWarnedRegionIds.Clear();
				noRenderedBarsWarnedRegionIds.Clear();
				preparedSourceRectangleTags.Clear();
				offscreenRegionWarnedIds.Clear();
				dxInitFailureLogged = false;
				volumeByBarPrice?.Clear();
				deltaByBarPrice?.Clear();
				DisposeDx();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == tickSeriesIndex)
			{
				StoreTickData();
			}
			else
			{
				if (BarsInProgress != 0 || CurrentBar < 1)
				{
					return;
				}
				bool flag = false;
				for (int i = 0; i < regions.Count; i++)
				{
					AnchoredRegion anchoredRegion = regions[i];
					if (anchoredRegion.IsDirty && !anchoredRegion.IsBeingDragged)
					{
						BuildRegionVolume(anchoredRegion);
						flag = true;
					}
				}
				if (flag)
				{
					RequestInvalidateFromDataThread();
				}
			}
		}

		private void StoreTickData()
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
			double close = BarsArray[tickSeriesIndex].GetClose(num);
			long volume = BarsArray[tickSeriesIndex].GetVolume(num);
			if (volume <= 0 || double.IsNaN(close))
			{
				return;
			}
			int key = PriceToIndex(close);
			int num2 = 0;
			if (!double.IsNaN(lastTickPriceForDelta))
			{
				num2 = ((close > lastTickPriceForDelta) ? 1 : ((!(close < lastTickPriceForDelta)) ? lastTickDirectionForDelta : (-1)));
			}
			lastTickPriceForDelta = close;
			if (num2 != 0)
			{
				lastTickDirectionForDelta = num2;
			}
			int num3 = ((CurrentBars != null && CurrentBars.Length != 0) ? CurrentBars[0] : (-1));
			if (num3 < 0)
			{
				Bars primaryBarsSeries = GetPrimaryBarsSeries();
				if (primaryBarsSeries == null)
				{
					return;
				}
				DateTime time = BarsArray[tickSeriesIndex].GetTime(num);
				num3 = primaryBarsSeries.GetBar(time);
				if (num3 < 0)
				{
					return;
				}
			}
			if (!volumeByBarPrice.TryGetValue(num3, out var value))
			{
				value = new Dictionary<int, long>(64);
				volumeByBarPrice[num3] = value;
			}
			if (value.TryGetValue(key, out var value2))
			{
				value[key] = value2 + volume;
			}
			else
			{
				value[key] = volume;
			}
			if (num2 != 0)
			{
				if (deltaByBarPrice == null)
				{
					deltaByBarPrice = new Dictionary<int, Dictionary<int, long>>(4096);
				}
				if (!deltaByBarPrice.TryGetValue(num3, out var value3))
				{
					value3 = new Dictionary<int, long>(64);
					deltaByBarPrice[num3] = value3;
				}
				long num4 = ((num2 > 0) ? volume : (-volume));
				if (value3.TryGetValue(key, out var value4))
				{
					value3[key] = value4 + num4;
				}
				else
				{
					value3[key] = num4;
				}
			}
			for (int i = 0; i < regions.Count; i++)
			{
				AnchoredRegion anchoredRegion = regions[i];
				if (!anchoredRegion.IsBeingDragged && num3 >= anchoredRegion.StartBarIndex && num3 <= anchoredRegion.EndBarIndex)
				{
					anchoredRegion.IsDirty = true;
				}
			}
		}

		private void OnChartMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (ChartControl == null || ChartPanel == null || GetPrimaryBarsSeries() == null)
			{
				return;
			}
			System.Windows.Point position = e.GetPosition(ChartPanel);
			if (position.X < 0.0 || position.X > (double)ChartPanel.W || position.Y < 0.0 || position.Y > (double)ChartPanel.H)
			{
				return;
			}
			float num = (float)position.X;
			float num2 = (float)position.Y;
			ChartScale chartScale = ChartPanel.Scales[(ScaleJustification)0];
			if (chartScale == null)
			{
				return;
			}
			int barIndexFromX = GetBarIndexFromX(num);
			double priceFromY = GetPriceFromY(num2, chartScale);
			if (IsShiftCreateGesture())
			{
				AnchoredRegion item = new AnchoredRegion
				{
					Id = nextRegionId++,
					StartBarIndex = barIndexFromX,
					EndBarIndex = barIndexFromX,
					HighPrice = priceFromY,
					LowPrice = priceFromY,
					IsDirty = true,
					IsBeingDragged = true
				};
				regions.Add(item);
				dragRegion = item;
				selectedRegion = item;
				currentDrag = DragMode.Creating;
				dragStartBar = barIndexFromX;
				dragStartPrice = priceFromY;
				ChartPanel.Focus();
				Mouse.Capture(ChartPanel);
				e.Handled = true;
				return;
			}
			for (int num3 = regions.Count - 1; num3 >= 0; num3--)
			{
				AnchoredRegion anchoredRegion = regions[num3];
				DragMode dragMode = HitTestRegion(anchoredRegion, num, num2, chartScale);
				if (dragMode != DragMode.None)
				{
					currentDrag = dragMode;
					dragRegion = anchoredRegion;
					selectedRegion = anchoredRegion;
					anchoredRegion.IsBeingDragged = true;
					dragStartBar = barIndexFromX;
					dragStartPrice = priceFromY;
					ChartPanel.Focus();
					Mouse.Capture(ChartPanel);
					dragOrigStartBar = anchoredRegion.StartBarIndex;
					dragOrigEndBar = anchoredRegion.EndBarIndex;
					dragOrigHighPrice = anchoredRegion.HighPrice;
					dragOrigLowPrice = anchoredRegion.LowPrice;
					e.Handled = true;
					ChartControl.InvalidateVisual();
					return;
				}
			}
			InteractionDebug("MouseDown empty space -> deselect.");
			selectedRegion = null;
			ChartControl.InvalidateVisual();
		}

		private void OnChartMouseMove(object sender, MouseEventArgs e)
		{
			if (currentDrag == DragMode.None || dragRegion == null || ChartControl == null || ChartPanel == null)
			{
				return;
			}
			System.Windows.Point position = e.GetPosition(ChartPanel);
			float x = (float)position.X;
			float y = (float)position.Y;
			ChartScale chartScale = ChartPanel.Scales[(ScaleJustification)0];
			if (chartScale == null)
			{
				return;
			}
			int barIndexFromX = GetBarIndexFromX(x);
			double priceFromY = GetPriceFromY(y, chartScale);
			switch (currentDrag)
			{
			case DragMode.Creating:
				dragRegion.StartBarIndex = Math.Min(dragStartBar, barIndexFromX);
				dragRegion.EndBarIndex = Math.Max(dragStartBar, barIndexFromX);
				dragRegion.HighPrice = Math.Max(dragStartPrice, priceFromY);
				dragRegion.LowPrice = Math.Min(dragStartPrice, priceFromY);
				break;
			case DragMode.MovingBody:
			{
				int num = barIndexFromX - dragStartBar;
				double num2 = priceFromY - dragStartPrice;
				int num3 = dragOrigEndBar - dragOrigStartBar;
				int num4 = ClampBarIndex(dragOrigStartBar + num);
				int num5 = num4 + num3;
				int primaryBarsCount = GetPrimaryBarsCount();
				if (primaryBarsCount > 0 && num5 >= primaryBarsCount)
				{
					num5 = primaryBarsCount - 1;
					num4 = Math.Max(0, num5 - num3);
				}
				dragRegion.StartBarIndex = num4;
				dragRegion.EndBarIndex = num5;
				dragRegion.HighPrice = ClampAndRoundPrice(dragOrigHighPrice + num2);
				dragRegion.LowPrice = ClampAndRoundPrice(dragOrigLowPrice + num2);
				break;
			}
			case DragMode.ResizingLeft:
				dragRegion.StartBarIndex = Math.Min(barIndexFromX, dragRegion.EndBarIndex - 1);
				break;
			case DragMode.ResizingRight:
				dragRegion.EndBarIndex = Math.Max(barIndexFromX, dragRegion.StartBarIndex + 1);
				break;
			case DragMode.ResizingTop:
				dragRegion.HighPrice = Math.Max(priceFromY, dragRegion.LowPrice + tickSize);
				break;
			case DragMode.ResizingBottom:
				dragRegion.LowPrice = Math.Min(priceFromY, dragRegion.HighPrice - tickSize);
				break;
			case DragMode.ResizingTopLeft:
				dragRegion.StartBarIndex = Math.Min(barIndexFromX, dragRegion.EndBarIndex - 1);
				dragRegion.HighPrice = Math.Max(priceFromY, dragRegion.LowPrice + tickSize);
				break;
			case DragMode.ResizingTopRight:
				dragRegion.EndBarIndex = Math.Max(barIndexFromX, dragRegion.StartBarIndex + 1);
				dragRegion.HighPrice = Math.Max(priceFromY, dragRegion.LowPrice + tickSize);
				break;
			case DragMode.ResizingBottomLeft:
				dragRegion.StartBarIndex = Math.Min(barIndexFromX, dragRegion.EndBarIndex - 1);
				dragRegion.LowPrice = Math.Min(priceFromY, dragRegion.HighPrice - tickSize);
				break;
			case DragMode.ResizingBottomRight:
				dragRegion.EndBarIndex = Math.Max(barIndexFromX, dragRegion.StartBarIndex + 1);
				dragRegion.LowPrice = Math.Min(priceFromY, dragRegion.HighPrice - tickSize);
				break;
			}
			NormalizeRegion(dragRegion);
			dragRegion.IsDirty = true;
			dragRegion.IsBeingDragged = true;
			DateTime utcNow = DateTime.UtcNow;
			if ((utcNow - dragRegion.LastRebuildUtc).TotalMilliseconds >= 60.0)
			{
				BuildRegionVolume(dragRegion);
				dragRegion.LastRebuildUtc = utcNow;
			}
			ThrottledInvalidate();
			e.Handled = true;
		}

		private void OnChartMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (currentDrag == DragMode.None || dragRegion == null)
			{
				return;
			}
			bool flag = false;
			dragRegion.IsBeingDragged = false;
			if (currentDrag == DragMode.Creating)
			{
				NormalizeRegion(dragRegion);
				if (Math.Abs(dragRegion.EndBarIndex - dragRegion.StartBarIndex) < 2 || Math.Abs(dragRegion.HighPrice - dragRegion.LowPrice) < tickSize * 2.0)
				{
					regions.Remove(dragRegion);
					if (selectedRegion == dragRegion)
					{
						selectedRegion = null;
					}
					flag = true;
					InteractionDebug("MouseUp: region removed because it was too small.");
				}
			}
			if (!flag && dragRegion.IsDirty)
			{
				BuildRegionVolume(dragRegion);
			}
			currentDrag = DragMode.None;
			dragRegion = null;
			if (Mouse.Captured == ChartPanel)
			{
				Mouse.Capture(null);
				InteractionDebug("MouseUp end: drag finalized.");
			}
			ChartControl.InvalidateVisual();
			e.Handled = true;
		}

		private void OnChartKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete && selectedRegion != null)
			{
				if (dragRegion == selectedRegion)
				{
					dragRegion.IsBeingDragged = false;
					dragRegion = null;
					currentDrag = DragMode.None;
				}
				regions.Remove(selectedRegion);
				selectedRegion = null;
				if (ChartControl != null)
				{
					ChartControl.InvalidateVisual();
				}
				e.Handled = true;
			}
		}

		private DragMode HitTestRegion(AnchoredRegion region, float mouseX, float mouseY, ChartScale chartScale)
		{
			if (ChartBars == null)
			{
				return DragMode.None;
			}
			int fromIndex = ChartBars.FromIndex;
			int toIndex = ChartBars.ToIndex;
			int num = Math.Max(region.StartBarIndex, fromIndex);
			int num2 = Math.Min(region.EndBarIndex, toIndex);
			if (num > toIndex || num2 < fromIndex)
			{
				return DragMode.None;
			}
			float num3 = ChartControl.GetXByBarIndex(ChartBars, num);
			float num4 = ChartControl.GetXByBarIndex(ChartBars, num2);
			float num5 = chartScale.GetYByValue(region.HighPrice);
			float num6 = chartScale.GetYByValue(region.LowPrice);
			if (num3 > num4)
			{
				float num7 = num3;
				num3 = num4;
				num4 = num7;
			}
			if (num5 > num6)
			{
				float num8 = num5;
				num5 = num6;
				num6 = num8;
			}
			if (IsNear(mouseX, mouseY, num3, num5))
			{
				return DragMode.ResizingTopLeft;
			}
			if (IsNear(mouseX, mouseY, num4, num5))
			{
				return DragMode.ResizingTopRight;
			}
			if (IsNear(mouseX, mouseY, num3, num6))
			{
				return DragMode.ResizingBottomLeft;
			}
			if (IsNear(mouseX, mouseY, num4, num6))
			{
				return DragMode.ResizingBottomRight;
			}
			if (Math.Abs(mouseX - num3) < 6f && mouseY >= num5 && mouseY <= num6)
			{
				return DragMode.ResizingLeft;
			}
			if (Math.Abs(mouseX - num4) < 6f && mouseY >= num5 && mouseY <= num6)
			{
				return DragMode.ResizingRight;
			}
			if (Math.Abs(mouseY - num5) < 6f && mouseX >= num3 && mouseX <= num4)
			{
				return DragMode.ResizingTop;
			}
			if (Math.Abs(mouseY - num6) < 6f && mouseX >= num3 && mouseX <= num4)
			{
				return DragMode.ResizingBottom;
			}
			if (mouseX >= num3 && mouseX <= num4 && mouseY >= num5 && mouseY <= num6)
			{
				return DragMode.MovingBody;
			}
			return DragMode.None;
		}

		private static bool IsShiftCreateGesture()
		{
			if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
			{
				return true;
			}
			if (!Keyboard.IsKeyDown(Key.LeftShift))
			{
				return Keyboard.IsKeyDown(Key.RightShift);
			}
			return true;
		}

		private void InteractionDebug(string message)
		{
		}

		private void InteractionDebugMove(string message)
		{
		}

		private static string TryGetDrawingToolTypeName(dynamic drawObj)
		{
			if (drawObj == null)
			{
				return string.Empty;
			}
			try
			{
				Type type = drawObj.GetType();
				if (type != null && !string.IsNullOrEmpty(type.FullName))
				{
					return type.FullName;
				}
			}
			catch
			{
			}
			try
			{
				return drawObj.ToString();
			}
			catch
			{
			}
			return string.Empty;
		}

		private static string TryGetDrawingToolTag(dynamic drawObj)
		{
			if (drawObj == null)
			{
				return string.Empty;
			}
			try
			{
				object obj = drawObj.Tag;
				if (obj is string result)
				{
					return result;
				}
				return (obj != null) ? obj.ToString() : string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private static bool TryGetAnchorTime(dynamic anchor, out DateTime time)
		{
			time = Globals.MinDate;
			if (anchor == null)
			{
				return false;
			}
			try
			{
				if (anchor.Time is DateTime dateTime)
				{
					time = dateTime;
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private static bool TryGetAnchorPrice(dynamic anchor, out double price)
		{
			price = double.NaN;
			if (anchor == null)
			{
				return false;
			}
			try
			{
				if (anchor.Price is double num)
				{
					price = num;
					return !double.IsNaN(price) && !double.IsInfinity(price);
				}
			}
			catch
			{
			}
			return false;
		}

		private void TryPrepareSourceRectangleVisual(dynamic rect)
		{
			string text = TryGetDrawingToolTag(rect);
			dynamic val = rect == null;
			if ((val) || (val | string.IsNullOrEmpty(text)) || preparedSourceRectangleTags.Contains(text))
			{
				return;
			}
			preparedSourceRectangleTags.Add(text);
			try
			{
				dynamic property = rect.GetType().GetProperty("AreaOpacity");
				if (property != null && property.CanWrite)
				{
					property.SetValue(rect, 0, null);
				}
			}
			catch
			{
			}
			try
			{
				dynamic property2 = rect.GetType().GetProperty("AreaBrush");
				if (property2 != null && property2.CanWrite)
				{
					property2.SetValue(rect, Brushes.Transparent, null);
				}
			}
			catch
			{
			}
		}

		private int ResolveAnchorBarIndex(dynamic anchor)
		{
			Bars primaryBarsSeries = GetPrimaryBarsSeries();
			if (anchor == null || primaryBarsSeries == null)
			{
				return -1;
			}
			DateTime dateTime = default(DateTime);
			bool flag = TryGetAnchorTime(anchor, out dateTime);
			int num = -1;
			if (ChartControl != null && ChartBars != null && flag && dateTime != Globals.MinDate)
			{
				float num2 = ChartControl.GetXByTime(dateTime);
				if (!float.IsNaN(num2) && !float.IsInfinity(num2))
				{
					int barIdxByX = ChartBars.GetBarIdxByX(ChartControl, (int)num2);
					if (barIdxByX >= 0)
					{
						num = ClampBarIndex(barIdxByX);
					}
				}
			}
			if (flag && dateTime != Globals.MinDate)
			{
				int bar = primaryBarsSeries.GetBar(dateTime);
				if (bar >= 0 && bar < primaryBarsSeries.Count)
				{
					if (num >= 0 && Math.Abs((long)bar - (long)num) > 5000)
					{
						return num;
					}
					return bar;
				}
			}
			if (num >= 0)
			{
				return num;
			}
			try
			{
				dynamic property = anchor.GetType().GetProperty("SlotIndex");
				if (((object)property != null) && ((object)property.GetValue(anchor, null)) is int num3 && num3 >= 0)
				{
					int count = primaryBarsSeries.Count;
					if (num3 < count)
					{
						return num3;
					}
					if (ChartBars != null)
					{
						int num4 = Math.Max(0, ChartBars.ToIndex - ChartBars.FromIndex);
						if (num3 <= num4)
						{
							int num5 = ChartBars.FromIndex + num3;
							if (num5 >= 0 && num5 < count)
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

		private bool IsNear(float mx, float my, float px, float py)
		{
			float num = mx - px;
			float num2 = my - py;
			return num * num + num2 * num2 <= 64f;
		}

		private Bars GetPrimaryBarsSeries()
		{
			if (BarsArray != null && BarsArray.Length != 0 && BarsArray[0] != null)
			{
				return BarsArray[0];
			}
			return Bars;
		}

		private int GetPrimaryBarsCount()
		{
			return GetPrimaryBarsSeries()?.Count ?? 0;
		}

		private int GetBarIndexFromX(float x)
		{
			if (ChartControl == null || ChartBars == null)
			{
				return 0;
			}
			int barIdxByX = ChartBars.GetBarIdxByX(ChartControl, (int)x);
			int primaryBarsCount = GetPrimaryBarsCount();
			if (primaryBarsCount <= 0)
			{
				return 0;
			}
			return Math.Max(0, Math.Min(barIdxByX, primaryBarsCount - 1));
		}

		private double GetPriceFromY(float y, ChartScale chartScale)
		{
			if (chartScale == null)
			{
				return 0.0;
			}
			double valueByY = chartScale.GetValueByY(y);
			return Instrument.MasterInstrument.RoundToTickSize(valueByY);
		}

		private int ClampBarIndex(int barIndex)
		{
			int primaryBarsCount = GetPrimaryBarsCount();
			if (primaryBarsCount <= 0)
			{
				return 0;
			}
			return Math.Max(0, Math.Min(barIndex, primaryBarsCount - 1));
		}

		private double ClampAndRoundPrice(double price)
		{
			return Instrument.MasterInstrument.RoundToTickSize(price);
		}

		private void NormalizeRegion(AnchoredRegion r)
		{
			r.StartBarIndex = ClampBarIndex(r.StartBarIndex);
			r.EndBarIndex = ClampBarIndex(r.EndBarIndex);
			r.HighPrice = ClampAndRoundPrice(r.HighPrice);
			r.LowPrice = ClampAndRoundPrice(r.LowPrice);
			if (r.StartBarIndex > r.EndBarIndex)
			{
				int startBarIndex = r.StartBarIndex;
				r.StartBarIndex = r.EndBarIndex;
				r.EndBarIndex = startBarIndex;
			}
			if (r.LowPrice > r.HighPrice)
			{
				double lowPrice = r.LowPrice;
				r.LowPrice = r.HighPrice;
				r.HighPrice = lowPrice;
			}
		}

		private void ThrottledInvalidate()
		{
			if (ChartControl != null)
			{
				DateTime utcNow = DateTime.UtcNow;
				if ((utcNow - lastInvalidateUtc).TotalMilliseconds >= 16.0)
				{
					ChartControl.InvalidateVisual();
					lastInvalidateUtc = utcNow;
				}
			}
		}

		private void RequestInvalidateFromDataThread()
		{
			if (ChartControl == null || invalidateQueued)
			{
				return;
			}
			invalidateQueued = true;
			ChartControl.Dispatcher.InvokeAsync(delegate
			{
				try
				{
					ThrottledInvalidate();
				}
				finally
				{
					invalidateQueued = false;
				}
			});
		}

		private void SyncRegionsFromDrawObjects(bool rebuildDirtyNow)
		{
			Bars primaryBarsSeries = GetPrimaryBarsSeries();
			if (primaryBarsSeries == null || DrawObjects == null || regions == null)
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			if (!rebuildDirtyNow && (utcNow - lastDrawObjectSyncUtc).TotalMilliseconds < 40.0)
			{
				return;
			}
			lastDrawObjectSyncUtc = utcNow;
			syncSeenRectangleTags.Clear();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			List<IDrawingTool> list = DrawObjects.ToList();
			num8 = list.Count;
			foreach (dynamic item in list)
			{
				string text = TryGetDrawingToolTypeName(item);
				if (!string.Equals(text, "NinjaTrader.NinjaScript.DrawingTools.Rectangle", StringComparison.Ordinal) && !text.EndsWith(".Rectangle", StringComparison.Ordinal))
				{
					continue;
				}
				string text2 = TryGetDrawingToolTag(item);
				if (string.IsNullOrEmpty(text2))
				{
					num6++;
				}
				else
				{
					if (text2.StartsWith("StackZone_", StringComparison.Ordinal) || text2.StartsWith("StackImb_", StringComparison.Ordinal) || text2.StartsWith("LVN_", StringComparison.Ordinal))
					{
						continue;
					}
					TryPrepareSourceRectangleVisual(item);
					dynamic val = null;
					dynamic val2 = null;
					try
					{
						val = item.StartAnchor;
						val2 = item.EndAnchor;
					}
					catch
					{
						num4++;
						continue;
					}
					if (val == null || val2 == null)
					{
						num4++;
						continue;
					}
					DateTime minDate = Globals.MinDate;
					DateTime minDate2 = Globals.MinDate;
					bool flag = TryGetAnchorTime(val, out minDate);
					bool flag2 = TryGetAnchorTime(val2, out minDate2);
					if (!flag || !flag2 || minDate == Globals.MinDate || minDate2 == Globals.MinDate)
					{
						num5++;
						continue;
					}
					double val3 = double.NaN;
					double val4 = double.NaN;
					bool flag3 = TryGetAnchorPrice(val, out val3);
					bool flag4 = TryGetAnchorPrice(val2, out val4);
					if (!flag3 || !flag4)
					{
						num5++;
						continue;
					}
					num++;
					syncSeenRectangleTags.Add(text2);
					int num9 = ResolveAnchorBarIndex(val);
					int num10 = ResolveAnchorBarIndex(val2);
					if (num9 < 0 || num10 < 0)
					{
						num7++;
						continue;
					}
					int num11 = ClampBarIndex(Math.Min(num9, num10));
					int num12 = ClampBarIndex(Math.Max(num9, num10));
					if (primaryBarsSeries.Count > 1 && num12 <= num11)
					{
						num12 = Math.Min(primaryBarsSeries.Count - 1, num11 + 1);
					}
					double num13 = ClampAndRoundPrice(Math.Max(val3, val4));
					double num14 = ClampAndRoundPrice(Math.Min(val3, val4));
					if (num13 <= num14)
					{
						num13 = num14 + tickSize;
					}
					if (!regionsByTag.TryGetValue(text2, out var value))
					{
						value = new AnchoredRegion
						{
							Id = nextRegionId++,
							StartBarIndex = num11,
							EndBarIndex = num12,
							HighPrice = num13,
							LowPrice = num14,
							IsDirty = true
						};
						regionsByTag[text2] = value;
						regions.Add(value);
						num2++;
					}
					else if (value.StartBarIndex != num11 || value.EndBarIndex != num12 || value.HighPrice.ApproxCompare(num13) != 0 || value.LowPrice.ApproxCompare(num14) != 0)
					{
						value.StartBarIndex = num11;
						value.EndBarIndex = num12;
						value.HighPrice = num13;
						value.LowPrice = num14;
						value.IsDirty = true;
						num3++;
					}
				}
			}
			if (regionsByTag.Count > 0)
			{
				List<string> list2 = null;
				foreach (KeyValuePair<string, AnchoredRegion> item2 in regionsByTag)
				{
					if (!syncSeenRectangleTags.Contains(item2.Key))
					{
						if (list2 == null)
						{
							list2 = new List<string>();
						}
						list2.Add(item2.Key);
					}
				}
				if (list2 != null)
				{
					for (int i = 0; i < list2.Count; i++)
					{
						string key = list2[i];
						AnchoredRegion anchoredRegion = regionsByTag[key];
						regions.Remove(anchoredRegion);
						regionsByTag.Remove(key);
						if (selectedRegion == anchoredRegion)
						{
							selectedRegion = null;
						}
						if (dragRegion == anchoredRegion)
						{
							dragRegion = null;
							currentDrag = DragMode.None;
						}
					}
				}
			}
			if (!rebuildDirtyNow)
			{
				return;
			}
			for (int j = 0; j < regions.Count; j++)
			{
				AnchoredRegion anchoredRegion2 = regions[j];
				if (anchoredRegion2.IsDirty)
				{
					try
					{
						BuildRegionVolume(anchoredRegion2);
					}
					catch
					{
						anchoredRegion2.IsDirty = true;
					}
				}
			}
		}

		private void BuildRegionVolume(AnchoredRegion region)
		{
			Bars primaryBarsSeries = GetPrimaryBarsSeries();
			region.VolumeByPrice.Clear();
			region.DeltaByPrice.Clear();
			if (primaryBarsSeries == null || primaryBarsSeries.Count == 0)
			{
				RebuildRegionCache(region);
				return;
			}
			int num = (int)Math.Floor(region.LowPrice / volumeGroupSize);
			int num2 = (int)Math.Ceiling(region.HighPrice / volumeGroupSize);
			int num3 = Math.Max(0, region.StartBarIndex);
			int num4 = Math.Min(region.EndBarIndex, primaryBarsSeries.Count - 1);
			bool flag = false;
			for (int i = num3; i <= num4; i++)
			{
				Dictionary<int, long> value;
				bool flag2 = volumeByBarPrice.TryGetValue(i, out value);
				Dictionary<int, long> value2 = null;
				bool flag3 = false;
				if (deltaByBarPrice != null)
				{
					flag3 = deltaByBarPrice.TryGetValue(i, out value2);
				}
				if (!flag2 && !flag3)
				{
					continue;
				}
				flag = true;
				if (flag2)
				{
					foreach (KeyValuePair<int, long> item in value)
					{
						int key = item.Key;
						if (key >= num && key <= num2)
						{
							if (region.VolumeByPrice.TryGetValue(key, out var value3))
							{
								region.VolumeByPrice[key] = value3 + item.Value;
							}
							else
							{
								region.VolumeByPrice[key] = item.Value;
							}
						}
					}
				}
				if (!flag3 || value2 == null)
				{
					continue;
				}
				foreach (KeyValuePair<int, long> item2 in value2)
				{
					int key2 = item2.Key;
					if (key2 >= num && key2 <= num2)
					{
						if (region.DeltaByPrice.TryGetValue(key2, out var value4))
						{
							region.DeltaByPrice[key2] = value4 + item2.Value;
						}
						else
						{
							region.DeltaByPrice[key2] = item2.Value;
						}
					}
				}
			}
			if (region.VolumeByPrice.Count == 0)
			{
				BuildRegionVolumeFromPrimaryBars(region, num3, num4, num, num2, primaryBarsSeries);
				if (flag)
				{
				}
			}
			else
			{
				noTickDataWarnedRegionIds.Remove(region.Id);
			}
			RebuildRegionCache(region);
		}

		private void BuildRegionVolumeFromPrimaryBars(AnchoredRegion region, int startBar, int endBar, int lowPriceIdx, int highPriceIdx, Bars primaryBars)
		{
			if (primaryBars == null)
			{
				return;
			}
			for (int i = startBar; i <= endBar; i++)
			{
				long volume = primaryBars.GetVolume(i);
				if (volume <= 0)
				{
					continue;
				}
				double high = primaryBars.GetHigh(i);
				double low = primaryBars.GetLow(i);
				double open = primaryBars.GetOpen(i);
				double close = primaryBars.GetClose(i);
				if (double.IsNaN(high) || double.IsNaN(low))
				{
					continue;
				}
				int num = 0;
				if (!double.IsNaN(open) && !double.IsNaN(close))
				{
					if (close > open)
					{
						num = 1;
					}
					else if (close < open)
					{
						num = -1;
					}
				}
				int num2 = PriceToIndex(Math.Min(low, high));
				int num3 = PriceToIndex(Math.Max(low, high));
				if (num3 < lowPriceIdx || num2 > highPriceIdx)
				{
					continue;
				}
				int num4 = Math.Max(lowPriceIdx, num2);
				int num5 = Math.Min(highPriceIdx, num3);
				if (num5 < num4)
				{
					continue;
				}
				int num6 = num5 - num4 + 1;
				if (num6 > 256)
				{
					int num7 = PriceToIndex(close);
					if (num7 < lowPriceIdx || num7 > highPriceIdx)
					{
						continue;
					}
					if (region.VolumeByPrice.TryGetValue(num7, out var value))
					{
						region.VolumeByPrice[num7] = value + volume;
					}
					else
					{
						region.VolumeByPrice[num7] = volume;
					}
					if (num != 0)
					{
						long num8 = ((num > 0) ? volume : (-volume));
						if (region.DeltaByPrice.TryGetValue(num7, out var value2))
						{
							region.DeltaByPrice[num7] = value2 + num8;
						}
						else
						{
							region.DeltaByPrice[num7] = num8;
						}
					}
					continue;
				}
				long num9 = volume / num6;
				long num10 = volume % num6;
				for (int j = num4; j <= num5; j++)
				{
					long num11 = num9;
					if (num10 > 0)
					{
						num11++;
						num10--;
					}
					if (num11 <= 0)
					{
						continue;
					}
					if (region.VolumeByPrice.TryGetValue(j, out var value3))
					{
						region.VolumeByPrice[j] = value3 + num11;
					}
					else
					{
						region.VolumeByPrice[j] = num11;
					}
					if (num != 0)
					{
						long num12 = ((num > 0) ? num11 : (-num11));
						if (region.DeltaByPrice.TryGetValue(j, out var value4))
						{
							region.DeltaByPrice[j] = value4 + num12;
						}
						else
						{
							region.DeltaByPrice[j] = num12;
						}
					}
				}
			}
		}

		private int PriceToIndex(double price)
		{
			if (!(inverseVolumeGroupSize > 0.0))
			{
				return (int)Math.Round(price / volumeGroupSize);
			}
			return (int)Math.Round(price * inverseVolumeGroupSize);
		}

		private void RebuildRegionCache(AnchoredRegion region)
		{
			if (region.VolumeByPrice.Count == 0)
			{
				region.RenderLevelCount = 0;
				region.TotalVolume = 0L;
				region.MaxVolume = 0L;
				region.MaxAbsDelta = 0L;
				region.POCIndex = 0;
				region.POCVolume = 0L;
				region.VAHIndex = 0;
				region.VALIndex = 0;
				region.POCPrice = 0.0;
				region.VAHPrice = 0.0;
				region.VALPrice = 0.0;
				region.POCLabelCached = null;
				region.VAHLabelCached = null;
				region.VALLabelCached = null;
				region.IsDirty = false;
				return;
			}
			int count = region.VolumeByPrice.Count;
			if (region.RenderLevels == null || region.RenderLevels.Length < count)
			{
				region.RenderLevels = new PriceLevel[count];
			}
			region.RenderLevelCount = count;
			int num = 0;
			foreach (KeyValuePair<int, long> item in region.VolumeByPrice)
			{
				region.RenderLevels[num].PriceIndex = item.Key;
				region.RenderLevels[num].Volume = item.Value;
				region.RenderLevels[num].Delta = (region.DeltaByPrice.TryGetValue(item.Key, out var value) ? value : 0);
				num++;
			}
			Array.Sort(region.RenderLevels, 0, count, PriceLevelComparer);
			long num2 = 0L;
			long num3 = 0L;
			long num4 = 0L;
			int num5 = 0;
			for (int i = 0; i < count; i++)
			{
				long volume = region.RenderLevels[i].Volume;
				long num6 = Math.Abs(region.RenderLevels[i].Delta);
				num2 += volume;
				if (num6 > num4)
				{
					num4 = num6;
				}
				if (volume > num3)
				{
					num3 = volume;
					num5 = i;
				}
			}
			region.TotalVolume = num2;
			region.MaxVolume = num3;
			region.MaxAbsDelta = num4;
			int pOCIndex = region.POCIndex;
			int vAHIndex = region.VAHIndex;
			int vALIndex = region.VALIndex;
			int num7 = (region.POCIndex = region.RenderLevels[num5].PriceIndex);
			region.POCVolume = num3;
			region.POCPrice = (double)num7 * volumeGroupSize;
			long num8 = (long)((double)num2 * ((double)ValueAreaPercentage / 100.0));
			long num9 = num3;
			int num10 = num5;
			int num11 = num5;
			while (num9 < num8)
			{
				bool flag = num10 < count - 1;
				bool flag2 = num11 > 0;
				if (!flag && !flag2)
				{
					break;
				}
				long num12 = 0L;
				if (flag)
				{
					for (int j = num10 + 1; j <= Math.Min(num10 + 2, count - 1); j++)
					{
						num12 += region.RenderLevels[j].Volume;
					}
				}
				long num13 = 0L;
				if (flag2)
				{
					for (int num14 = num11 - 1; num14 >= Math.Max(num11 - 2, 0); num14--)
					{
						num13 += region.RenderLevels[num14].Volume;
					}
				}
				if (!flag2)
				{
					for (int k = num10 + 1; k <= Math.Min(num10 + 2, count - 1); k++)
					{
						num9 += region.RenderLevels[k].Volume;
					}
					num10 = Math.Min(num10 + 2, count - 1);
					continue;
				}
				if (!flag)
				{
					for (int num15 = num11 - 1; num15 >= Math.Max(num11 - 2, 0); num15--)
					{
						num9 += region.RenderLevels[num15].Volume;
					}
					num11 = Math.Max(num11 - 2, 0);
					continue;
				}
				if (num12 > num13)
				{
					for (int l = num10 + 1; l <= Math.Min(num10 + 2, count - 1); l++)
					{
						num9 += region.RenderLevels[l].Volume;
					}
					num10 = Math.Min(num10 + 2, count - 1);
					continue;
				}
				if (num13 > num12)
				{
					for (int num16 = num11 - 1; num16 >= Math.Max(num11 - 2, 0); num16--)
					{
						num9 += region.RenderLevels[num16].Volume;
					}
					num11 = Math.Max(num11 - 2, 0);
					continue;
				}
				for (int m = num10 + 1; m <= Math.Min(num10 + 2, count - 1); m++)
				{
					num9 += region.RenderLevels[m].Volume;
				}
				num10 = Math.Min(num10 + 2, count - 1);
				if (num9 < num8)
				{
					for (int num17 = num11 - 1; num17 >= Math.Max(num11 - 2, 0); num17--)
					{
						num9 += region.RenderLevels[num17].Volume;
					}
					num11 = Math.Max(num11 - 2, 0);
				}
			}
			int priceIndex = region.RenderLevels[num10].PriceIndex;
			int priceIndex2 = region.RenderLevels[num11].PriceIndex;
			region.VAHIndex = priceIndex;
			region.VALIndex = priceIndex2;
			region.VAHPrice = (double)priceIndex * volumeGroupSize;
			region.VALPrice = (double)priceIndex2 * volumeGroupSize;
			for (int n = 0; n < count; n++)
			{
				int priceIndex3 = region.RenderLevels[n].PriceIndex;
				region.RenderLevels[n].Price = (double)priceIndex3 * volumeGroupSize;
				if (priceIndex3 == num7)
				{
					region.RenderLevels[n].Zone = 2;
				}
				else if (priceIndex3 >= priceIndex2 && priceIndex3 <= priceIndex)
				{
					region.RenderLevels[n].Zone = 1;
				}
				else
				{
					region.RenderLevels[n].Zone = 0;
				}
			}
			if (region.POCLabelCached == null || pOCIndex != region.POCIndex)
			{
				region.POCLabelCached = "POC " + Instrument.MasterInstrument.FormatPrice(region.POCPrice);
			}
			if (region.VAHLabelCached == null || vAHIndex != region.VAHIndex)
			{
				region.VAHLabelCached = "VAH " + Instrument.MasterInstrument.FormatPrice(region.VAHPrice);
			}
			if (region.VALLabelCached == null || vALIndex != region.VALIndex)
			{
				region.VALLabelCached = "VAL " + Instrument.MasterInstrument.FormatPrice(region.VALPrice);
			}
			region.IsDirty = false;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (GetPrimaryBarsSeries() == null || ChartControl == null || ChartBars == null || RenderTarget == null)
			{
				return;
			}
			SyncRegionsFromDrawObjects(rebuildDirtyNow: true);
			if (regions == null || regions.Count == 0)
			{
				return;
			}
			EnsureDirectXResources();
			if (!dxResourcesValid)
			{
				return;
			}
			int fromIndex = ChartBars.FromIndex;
			int toIndex = ChartBars.ToIndex;
			for (int i = 0; i < regions.Count; i++)
			{
				AnchoredRegion anchoredRegion = regions[i];
				if (anchoredRegion.EndBarIndex >= fromIndex && anchoredRegion.StartBarIndex <= toIndex)
				{
					offscreenRegionWarnedIds.Remove(anchoredRegion.Id);
					RenderRectangle(chartControl, chartScale, anchoredRegion, fromIndex, toIndex);
					if (anchoredRegion.RenderLevelCount > 0)
					{
						RenderRegionProfile(chartControl, chartScale, anchoredRegion, fromIndex, toIndex);
						RenderRegionLines(chartControl, chartScale, anchoredRegion, fromIndex, toIndex);
					}
					if (anchoredRegion == selectedRegion)
					{
						RenderAnchors(chartControl, chartScale, anchoredRegion, fromIndex, toIndex);
					}
				}
			}
		}

		private void RenderRectangle(ChartControl chartControl, ChartScale chartScale, AnchoredRegion region, int firstBarOnChart, int lastBarOnChart)
		{
			int barIndex = Math.Max(region.StartBarIndex, firstBarOnChart);
			int barIndex2 = Math.Min(region.EndBarIndex, lastBarOnChart);
			float num = chartControl.GetXByBarIndex(ChartBars, barIndex);
			float num2 = chartControl.GetXByBarIndex(ChartBars, barIndex2);
			float num3 = chartScale.GetYByValue(region.HighPrice);
			float num4 = chartScale.GetYByValue(region.LowPrice);
			RectangleF rect = new RectangleF(Math.Min(num, num2), Math.Min(num3, num4), Math.Abs(num2 - num), Math.Abs(num4 - num3));
			RenderTarget.FillRectangle(rect, dxRectFillBrush);
			SharpDX.Direct2D1.SolidColorBrush brush = ((region == selectedRegion) ? dxSelectedBorderBrush : dxRectBorderBrush);
			float strokeWidth = ((region == selectedRegion) ? 2f : 1f);
			RenderTarget.DrawRectangle(rect, brush, strokeWidth, dxSolidStroke);
		}

		private void RenderAnchors(ChartControl chartControl, ChartScale chartScale, AnchoredRegion region, int firstBarOnChart, int lastBarOnChart)
		{
			int barIndex = Math.Max(region.StartBarIndex, firstBarOnChart);
			int barIndex2 = Math.Min(region.EndBarIndex, lastBarOnChart);
			float num = chartControl.GetXByBarIndex(ChartBars, barIndex);
			float num2 = chartControl.GetXByBarIndex(ChartBars, barIndex2);
			float num3 = chartScale.GetYByValue(region.HighPrice);
			float num4 = chartScale.GetYByValue(region.LowPrice);
			float radius = 4f;
			RenderAnchorDot(num, num3, radius);
			RenderAnchorDot(num2, num3, radius);
			RenderAnchorDot(num, num4, radius);
			RenderAnchorDot(num2, num4, radius);
			float x = (num + num2) / 2f;
			float y = (num3 + num4) / 2f;
			RenderAnchorDot(num, y, radius);
			RenderAnchorDot(num2, y, radius);
			RenderAnchorDot(x, num3, radius);
			RenderAnchorDot(x, num4, radius);
		}

		private void RenderAnchorDot(float x, float y, float radius)
		{
			SharpDX.Direct2D1.Ellipse ellipse = new SharpDX.Direct2D1.Ellipse(new Vector2(x, y), radius, radius);
			RenderTarget.FillEllipse(ellipse, dxAnchorBrush);
			RenderTarget.DrawEllipse(ellipse, dxRectBorderBrush, 1f);
		}

		private void RenderRegionProfile(ChartControl chartControl, ChartScale chartScale, AnchoredRegion region, int firstBarOnChart, int lastBarOnChart)
		{
			if (region.RenderLevelCount == 0)
			{
				return;
			}
			long maxVolume = region.MaxVolume;
			if (maxVolume == 0L)
			{
				return;
			}
			int barIndex = Math.Max(region.StartBarIndex, firstBarOnChart);
			int barIndex2 = Math.Min(region.EndBarIndex, lastBarOnChart);
			float num = chartControl.GetXByBarIndex(ChartBars, barIndex);
			float num2 = chartControl.GetXByBarIndex(ChartBars, barIndex2);
			float num3 = Math.Abs(num2 - num);
			if (num3 < 5f)
			{
				return;
			}
			float num4 = num3 * 0.85f;
			float num5 = Math.Min(num, num2);
			float num6 = Math.Max(num, num2);
			float num7 = ((ProfileAlignment == VPAlignment.Left) ? num5 : ((ProfileAlignment != VPAlignment.Right) ? (num5 + (num3 - num4) / 2f) : (num6 - num4)));
			double num8 = volumeGroupSize * 0.5;
			if (ShowDeltaBars && region.MaxAbsDelta > 0 && dxDeltaPositiveBrush != null && dxDeltaNegativeBrush != null)
			{
				float num9 = Math.Max(5f, Math.Min(DeltaProfileWidth, num4));
				for (int i = 0; i < region.RenderLevelCount; i++)
				{
					PriceLevel priceLevel = region.RenderLevels[i];
					long num10 = Math.Abs(priceLevel.Delta);
					if (num10 >= DeltaThreshold && num10 != 0L)
					{
						double num11 = priceLevel.Price - num8;
						double num12 = priceLevel.Price + num8;
						if (!(num12 < region.LowPrice) && !(num11 > region.HighPrice))
						{
							double val = Math.Min(num12, region.HighPrice);
							double val2 = Math.Max(num11, region.LowPrice);
							float num13 = chartScale.GetYByValue(val);
							float num14 = chartScale.GetYByValue(val2);
							float y = Math.Min(num13, num14);
							float height = Math.Max(1f, Math.Abs(num14 - num13));
							float num15 = Math.Max(1f, (float)((double)num10 / (double)region.MaxAbsDelta * (double)num9));
							float x = num7 - num15;
							RectangleF rect = new RectangleF(x, y, num15, height);
							SharpDX.Direct2D1.SolidColorBrush brush = ((priceLevel.Delta > 0) ? dxDeltaPositiveBrush : dxDeltaNegativeBrush);
							RenderTarget.FillRectangle(rect, brush);
						}
					}
				}
			}
			int num16 = 0;
			for (int j = 0; j < region.RenderLevelCount; j++)
			{
				PriceLevel priceLevel2 = region.RenderLevels[j];
				if (priceLevel2.Volume >= VolumeThreshold)
				{
					double num17 = priceLevel2.Price - num8;
					double num18 = priceLevel2.Price + num8;
					if (!(num18 < region.LowPrice) && !(num17 > region.HighPrice))
					{
						double val3 = Math.Min(num18, region.HighPrice);
						double val4 = Math.Max(num17, region.LowPrice);
						float num19 = chartScale.GetYByValue(val3);
						float num20 = chartScale.GetYByValue(val4);
						float y2 = Math.Min(num19, num20);
						float height2 = Math.Max(1f, Math.Abs(num20 - num19));
						float width = Math.Max(2f, (float)((double)priceLevel2.Volume / (double)maxVolume * (double)num4));
						SharpDX.Direct2D1.SolidColorBrush brush2 = priceLevel2.Zone switch
						{
							2 => dxPOCBrush, 
							1 => dxValueAreaBrush, 
							_ => dxProfileBrush, 
						};
						RectangleF rect2 = new RectangleF(num7, y2, width, height2);
						RenderTarget.FillRectangle(rect2, brush2);
						num16++;
					}
				}
			}
			if (num16 != 0)
			{
				noRenderedBarsWarnedRegionIds.Remove(region.Id);
			}
		}

		private void RenderRegionLines(ChartControl chartControl, ChartScale chartScale, AnchoredRegion region, int firstBarOnChart, int lastBarOnChart)
		{
			if (region.POCPrice == 0.0 && region.VAHPrice == 0.0)
			{
				return;
			}
			StrokeStyle strokeStyle = dxDashStroke ?? dxSolidStroke;
			int barIndex = Math.Max(region.StartBarIndex, firstBarOnChart);
			int barIndex2 = Math.Min(region.EndBarIndex, lastBarOnChart);
			float val = chartControl.GetXByBarIndex(ChartBars, barIndex);
			float val2 = chartControl.GetXByBarIndex(ChartBars, barIndex2);
			float x = Math.Min(val, val2);
			float num = Math.Max(val, val2);
			float num2 = ChartPanel.Y;
			float num3 = ChartPanel.Y + ChartPanel.H;
			float num4 = ChartPanel.X + ChartPanel.W;
			if (ShowPOCLine && region.POCPrice > 0.0 && region.POCPrice >= region.LowPrice && region.POCPrice <= region.HighPrice)
			{
				float num5 = chartScale.GetYByValue(region.POCPrice);
				RenderTarget.DrawLine(new Vector2(x, num5), new Vector2(num, num5), dxPOCLineBrush, POCLineWidth, dxSolidStroke);
				if (ShowLabels && num5 >= num2 && num5 <= num3 && num + (float)LabelOffset <= num4)
				{
					RenderCachedLabel(num, num5, region.POCLabelCached, dxPOCLabelBrush);
				}
			}
			if (!ShowVALines)
			{
				return;
			}
			if (region.VAHPrice > 0.0 && region.VAHPrice >= region.LowPrice && region.VAHPrice <= region.HighPrice)
			{
				float num6 = chartScale.GetYByValue(region.VAHPrice);
				RenderTarget.DrawLine(new Vector2(x, num6), new Vector2(num, num6), dxVAHLineBrush, VAHLineWidth, strokeStyle);
				if (ShowLabels && num6 >= num2 && num6 <= num3 && num + (float)LabelOffset <= num4)
				{
					RenderCachedLabel(num, num6, region.VAHLabelCached, dxVAHLabelBrush);
				}
			}
			if (region.VALPrice > 0.0 && region.VALPrice >= region.LowPrice && region.VALPrice <= region.HighPrice)
			{
				float num7 = chartScale.GetYByValue(region.VALPrice);
				RenderTarget.DrawLine(new Vector2(x, num7), new Vector2(num, num7), dxVALLineBrush, VALLineWidth, strokeStyle);
				if (ShowLabels && num7 >= num2 && num7 <= num3 && num + (float)LabelOffset <= num4)
				{
					RenderCachedLabel(num, num7, region.VALLabelCached, dxVALLabelBrush);
				}
			}
		}

		private void RenderCachedLabel(float x, float y, string cachedText, SharpDX.Direct2D1.SolidColorBrush brush)
		{
			if (dxLabelTextFormat != null && cachedText != null)
			{
				RectangleF layoutRect = new RectangleF(x + (float)LabelOffset, y - (float)LabelFontSize - 2f, 200f, LabelFontSize + 6);
				RenderTarget.DrawText(cachedText, dxLabelTextFormat, layoutRect, brush);
			}
		}

		private System.Windows.Media.Color GetSafeBrushColor(System.Windows.Media.Brush brush, System.Windows.Media.Color fallbackColor)
		{
			if (!(brush is System.Windows.Media.SolidColorBrush solidColorBrush))
			{
				return fallbackColor;
			}
			return solidColorBrush.Color;
		}

		private void EnsureDirectXResources()
		{
			if (RenderTarget == null)
			{
				dxResourcesValid = false;
			}
			else
			{
				if (dxResourcesValid)
				{
					return;
				}
				DisposeDx();
				try
				{
					System.Windows.Media.Color safeBrushColor = GetSafeBrushColor(ProfileColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					dxProfileBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor.R / 255f, (float)(int)safeBrushColor.G / 255f, (float)(int)safeBrushColor.B / 255f, (float)ProfileOpacity / 100f));
					System.Windows.Media.Color safeBrushColor2 = GetSafeBrushColor(ValueAreaColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					dxValueAreaBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor2.R / 255f, (float)(int)safeBrushColor2.G / 255f, (float)(int)safeBrushColor2.B / 255f, (float)ValueAreaOpacity / 100f));
					System.Windows.Media.Color safeBrushColor3 = GetSafeBrushColor(POCBarColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
					dxPOCBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor3.R / 255f, (float)(int)safeBrushColor3.G / 255f, (float)(int)safeBrushColor3.B / 255f, (float)POCBarOpacity / 100f));
					System.Windows.Media.Color safeBrushColor4 = GetSafeBrushColor(DeltaPositiveColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
					dxDeltaPositiveBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor4.R / 255f, (float)(int)safeBrushColor4.G / 255f, (float)(int)safeBrushColor4.B / 255f, (float)DeltaOpacity / 100f));
					System.Windows.Media.Color safeBrushColor5 = GetSafeBrushColor(DeltaNegativeColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					dxDeltaNegativeBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor5.R / 255f, (float)(int)safeBrushColor5.G / 255f, (float)(int)safeBrushColor5.B / 255f, (float)DeltaOpacity / 100f));
					System.Windows.Media.Color safeBrushColor6 = GetSafeBrushColor(POCLineColor, System.Windows.Media.Color.FromRgb(107, 111, 204));
					dxPOCLineBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor6.R / 255f, (float)(int)safeBrushColor6.G / 255f, (float)(int)safeBrushColor6.B / 255f, 1f));
					System.Windows.Media.Color safeBrushColor7 = GetSafeBrushColor(VAHLineColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					dxVAHLineBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor7.R / 255f, (float)(int)safeBrushColor7.G / 255f, (float)(int)safeBrushColor7.B / 255f, 1f));
					System.Windows.Media.Color safeBrushColor8 = GetSafeBrushColor(VALLineColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					dxVALLineBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor8.R / 255f, (float)(int)safeBrushColor8.G / 255f, (float)(int)safeBrushColor8.B / 255f, 1f));
					System.Windows.Media.Color safeBrushColor9 = GetSafeBrushColor(POCLabelColor, System.Windows.Media.Color.FromRgb(45, 45, 45));
					dxPOCLabelBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor9.R / 255f, (float)(int)safeBrushColor9.G / 255f, (float)(int)safeBrushColor9.B / 255f, 1f));
					System.Windows.Media.Color safeBrushColor10 = GetSafeBrushColor(VAHLabelColor, System.Windows.Media.Color.FromRgb(45, 45, 45));
					dxVAHLabelBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor10.R / 255f, (float)(int)safeBrushColor10.G / 255f, (float)(int)safeBrushColor10.B / 255f, 1f));
					System.Windows.Media.Color safeBrushColor11 = GetSafeBrushColor(VALLabelColor, System.Windows.Media.Color.FromRgb(45, 45, 45));
					dxVALLabelBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor11.R / 255f, (float)(int)safeBrushColor11.G / 255f, (float)(int)safeBrushColor11.B / 255f, 1f));
					System.Windows.Media.Color safeBrushColor12 = GetSafeBrushColor(RectangleBorderColor, System.Windows.Media.Color.FromRgb(74, 74, 74));
					dxRectBorderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor12.R / 255f, (float)(int)safeBrushColor12.G / 255f, (float)(int)safeBrushColor12.B / 255f, 0.7f));
					dxRectFillBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor12.R / 255f, (float)(int)safeBrushColor12.G / 255f, (float)(int)safeBrushColor12.B / 255f, (float)RectangleFillOpacity / 100f));
					dxSelectedBorderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.2902f, 0.2902f, 0.2902f, 1f));
					dxAnchorBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.7216f, 0.7373f, 0.7765f, 0.9f));
					dxSolidStroke = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties
					{
						DashStyle = SharpDX.Direct2D1.DashStyle.Solid
					});
					try
					{
						dxDashStroke = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties
						{
							DashStyle = SharpDX.Direct2D1.DashStyle.Dash
						});
					}
					catch
					{
						dxDashStroke = null;
					}
					try
					{
						dxLabelTextFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", LabelFontSize)
						{
							TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
							ParagraphAlignment = ParagraphAlignment.Center
						};
					}
					catch
					{
						dxLabelTextFormat = null;
					}
					dxResourcesValid = true;
					dxInitFailureLogged = false;
				}
				catch (Exception)
				{
					dxResourcesValid = false;
				}
			}
		}

		public override void OnRenderTargetChanged()
		{
			dxResourcesValid = false;
			dxInitFailureLogged = false;
			DisposeDx();
			base.OnRenderTargetChanged();
		}

		private void DisposeDx()
		{
			dxProfileBrush?.Dispose();
			dxProfileBrush = null;
			dxValueAreaBrush?.Dispose();
			dxValueAreaBrush = null;
			dxPOCBrush?.Dispose();
			dxPOCBrush = null;
			dxDeltaPositiveBrush?.Dispose();
			dxDeltaPositiveBrush = null;
			dxDeltaNegativeBrush?.Dispose();
			dxDeltaNegativeBrush = null;
			dxPOCLineBrush?.Dispose();
			dxPOCLineBrush = null;
			dxVAHLineBrush?.Dispose();
			dxVAHLineBrush = null;
			dxVALLineBrush?.Dispose();
			dxVALLineBrush = null;
			dxPOCLabelBrush?.Dispose();
			dxPOCLabelBrush = null;
			dxVAHLabelBrush?.Dispose();
			dxVAHLabelBrush = null;
			dxVALLabelBrush?.Dispose();
			dxVALLabelBrush = null;
			dxRectBorderBrush?.Dispose();
			dxRectBorderBrush = null;
			dxRectFillBrush?.Dispose();
			dxRectFillBrush = null;
			dxSelectedBorderBrush?.Dispose();
			dxSelectedBorderBrush = null;
			dxAnchorBrush?.Dispose();
			dxAnchorBrush = null;
			dxSolidStroke?.Dispose();
			dxSolidStroke = null;
			dxDashStroke?.Dispose();
			dxDashStroke = null;
			dxLabelTextFormat?.Dispose();
			dxLabelTextFormat = null;
			dxResourcesValid = false;
		}
	}
}
