using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

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

	private SolidColorBrush dxProfileBrush;

	private SolidColorBrush dxValueAreaBrush;

	private SolidColorBrush dxPOCBrush;

	private SolidColorBrush dxDeltaPositiveBrush;

	private SolidColorBrush dxDeltaNegativeBrush;

	private SolidColorBrush dxPOCLineBrush;

	private SolidColorBrush dxVAHLineBrush;

	private SolidColorBrush dxVALLineBrush;

	private SolidColorBrush dxPOCLabelBrush;

	private SolidColorBrush dxVAHLabelBrush;

	private SolidColorBrush dxVALLabelBrush;

	private SolidColorBrush dxRectBorderBrush;

	private SolidColorBrush dxRectFillBrush;

	private SolidColorBrush dxAnchorBrush;

	private SolidColorBrush dxSelectedBorderBrush;

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
	public Brush ProfileColor { get; set; }

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
	public Brush ValueAreaColor { get; set; }

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
	public Brush POCBarColor { get; set; }

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
	public Brush POCLineColor { get; set; }

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
	public Brush VAHLineColor { get; set; }

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
	public Brush VALLineColor { get; set; }

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
	public Brush DeltaPositiveColor { get; set; }

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
	public Brush DeltaNegativeColor { get; set; }

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
	public Brush POCLabelColor { get; set; }

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
	public Brush VAHLabelColor { get; set; }

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
	public Brush VALLabelColor { get; set; }

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
	public Brush RectangleBorderColor { get; set; }

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
			ScaleJustification = (ScaleJustification)1;
			IsSuspendedWhileInactive = true;
			VolumeTickCompression = 1;
			ValueAreaPercentage = 70;
			VolumeThreshold = 0;
			ProfileAlignment = VPAlignment.Left;
			ProfileColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)30, (byte)144, byte.MaxValue));
			ValueAreaColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)70, (byte)130, (byte)180));
			POCBarColor = (Brush)new SolidColorBrush(Color.FromRgb(byte.MaxValue, (byte)165, (byte)0));
			ProfileOpacity = 60;
			ValueAreaOpacity = 40;
			POCBarOpacity = 90;
			POCLineColor = (Brush)(object)Brushes.Orange;
			VAHLineColor = (Brush)(object)Brushes.DodgerBlue;
			VALLineColor = (Brush)(object)Brushes.DodgerBlue;
			POCLineWidth = 2f;
			VAHLineWidth = 1f;
			VALLineWidth = 1f;
			ShowPOCLine = true;
			ShowVALines = true;
			ShowDeltaBars = true;
			DeltaProfileWidth = 120;
			DeltaThreshold = 0;
			DeltaOpacity = 55;
			DeltaPositiveColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)34, (byte)197, (byte)94));
			DeltaNegativeColor = (Brush)new SolidColorBrush(Color.FromRgb((byte)239, (byte)68, (byte)68));
			ShowLabels = true;
			LabelFontSize = 11;
			POCLabelColor = (Brush)(object)Brushes.Orange;
			VAHLabelColor = (Brush)(object)Brushes.DodgerBlue;
			VALLabelColor = (Brush)(object)Brushes.DodgerBlue;
			LabelOffset = 5;
			RectangleBorderColor = (Brush)(object)Brushes.White;
			RectangleFillOpacity = 8;
		}
		else if (State == State.Configure)
		{
			AddDataSeries((BarsPeriodType)0, 1);
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
		Point position = ((MouseEventArgs)e).GetPosition((IInputElement)(object)ChartPanel);
		if (position.X < 0.0 || position.X > (double)ChartPanel.W || position.Y < 0.0 || position.Y > (double)ChartPanel.H)
		{
			return;
		}
		float num = (float)position.X;
		float num2 = (float)position.Y;
		ChartScale val = ChartPanel.Scales[(ScaleJustification)0];
		if (val == null)
		{
			return;
		}
		int barIndexFromX = GetBarIndexFromX(num);
		double priceFromY = GetPriceFromY(num2, val);
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
			((UIElement)ChartPanel).Focus();
			Mouse.Capture((IInputElement)(object)ChartPanel);
			((RoutedEventArgs)e).Handled = true;
			return;
		}
		for (int num3 = regions.Count - 1; num3 >= 0; num3--)
		{
			AnchoredRegion anchoredRegion = regions[num3];
			DragMode dragMode = HitTestRegion(anchoredRegion, num, num2, val);
			if (dragMode != DragMode.None)
			{
				currentDrag = dragMode;
				dragRegion = anchoredRegion;
				selectedRegion = anchoredRegion;
				anchoredRegion.IsBeingDragged = true;
				dragStartBar = barIndexFromX;
				dragStartPrice = priceFromY;
				((UIElement)ChartPanel).Focus();
				Mouse.Capture((IInputElement)(object)ChartPanel);
				dragOrigStartBar = anchoredRegion.StartBarIndex;
				dragOrigEndBar = anchoredRegion.EndBarIndex;
				dragOrigHighPrice = anchoredRegion.HighPrice;
				dragOrigLowPrice = anchoredRegion.LowPrice;
				((RoutedEventArgs)e).Handled = true;
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
		Point position = e.GetPosition((IInputElement)(object)ChartPanel);
		float x = (float)position.X;
		float y = (float)position.Y;
		ChartScale val = ChartPanel.Scales[(ScaleJustification)0];
		if (val == null)
		{
			return;
		}
		int barIndexFromX = GetBarIndexFromX(x);
		double priceFromY = GetPriceFromY(y, val);
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
		((RoutedEventArgs)e).Handled = true;
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
		if ((object)Mouse.Captured == ChartPanel)
		{
			Mouse.Capture((IInputElement)null);
			InteractionDebug("MouseUp end: drag finalized.");
		}
		ChartControl.InvalidateVisual();
		((RoutedEventArgs)e).Handled = true;
	}

	private void OnChartKeyDown(object sender, KeyEventArgs e)
	{
		if ((int)e.Key == 32 && selectedRegion != null)
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
			((RoutedEventArgs)e).Handled = true;
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
		if ((Keyboard.Modifiers & 4) == 4)
		{
			return true;
		}
		if (!Keyboard.IsKeyDown((Key)116))
		{
			return Keyboard.IsKeyDown((Key)117);
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
			if ((property != null) && property.GetValue(anchor, null) is int num3 && num3 >= 0)
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
		Bars primaryBarsSeries = GetPrimaryBarsSeries();
		if (primaryBarsSeries == null)
		{
			return 0;
		}
		return primaryBarsSeries.Count;
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
		((DispatcherObject)ChartControl).Dispatcher.InvokeAsync((Action)delegate
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
				else if (value.StartBarIndex != num11 || value.EndBarIndex != num12 || MathExtentions.ApproxCompare(value.HighPrice, num13) != 0 || MathExtentions.ApproxCompare(value.LowPrice, num14) != 0)
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
			region.POCLabelCached = "POC " + Instrument.MasterInstrument.FormatPrice(region.POCPrice, true);
		}
		if (region.VAHLabelCached == null || vAHIndex != region.VAHIndex)
		{
			region.VAHLabelCached = "VAH " + Instrument.MasterInstrument.FormatPrice(region.VAHPrice, true);
		}
		if (region.VALLabelCached == null || vALIndex != region.VALIndex)
		{
			region.VALLabelCached = "VAL " + Instrument.MasterInstrument.FormatPrice(region.VALPrice, true);
		}
		region.IsDirty = false;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		OnRender(chartControl, chartScale);
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
		int num = Math.Max(region.StartBarIndex, firstBarOnChart);
		int num2 = Math.Min(region.EndBarIndex, lastBarOnChart);
		float num3 = chartControl.GetXByBarIndex(ChartBars, num);
		float num4 = chartControl.GetXByBarIndex(ChartBars, num2);
		float num5 = chartScale.GetYByValue(region.HighPrice);
		float num6 = chartScale.GetYByValue(region.LowPrice);
		RectangleF val = default(RectangleF);
		val = new RectangleF(Math.Min(num3, num4), Math.Min(num5, num6), Math.Abs(num4 - num3), Math.Abs(num6 - num5));
		RenderTarget.FillRectangle(val, (Brush)(object)dxRectFillBrush);
		SolidColorBrush val2 = ((region == selectedRegion) ? dxSelectedBorderBrush : dxRectBorderBrush);
		float num7 = ((region == selectedRegion) ? 2f : 1f);
		RenderTarget.DrawRectangle(val, (Brush)(object)val2, num7, dxSolidStroke);
	}

	private void RenderAnchors(ChartControl chartControl, ChartScale chartScale, AnchoredRegion region, int firstBarOnChart, int lastBarOnChart)
	{
		int num = Math.Max(region.StartBarIndex, firstBarOnChart);
		int num2 = Math.Min(region.EndBarIndex, lastBarOnChart);
		float num3 = chartControl.GetXByBarIndex(ChartBars, num);
		float num4 = chartControl.GetXByBarIndex(ChartBars, num2);
		float num5 = chartScale.GetYByValue(region.HighPrice);
		float num6 = chartScale.GetYByValue(region.LowPrice);
		float radius = 4f;
		RenderAnchorDot(num3, num5, radius);
		RenderAnchorDot(num4, num5, radius);
		RenderAnchorDot(num3, num6, radius);
		RenderAnchorDot(num4, num6, radius);
		float x = (num3 + num4) / 2f;
		float y = (num5 + num6) / 2f;
		RenderAnchorDot(num3, y, radius);
		RenderAnchorDot(num4, y, radius);
		RenderAnchorDot(x, num5, radius);
		RenderAnchorDot(x, num6, radius);
	}

	private void RenderAnchorDot(float x, float y, float radius)
	{
		Ellipse val = default(Ellipse);
		val = new Ellipse(new Vector2(x, y), radius, radius);
		RenderTarget.FillEllipse(val, (Brush)(object)dxAnchorBrush);
		RenderTarget.DrawEllipse(val, (Brush)(object)dxRectBorderBrush, 1f);
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
		int num = Math.Max(region.StartBarIndex, firstBarOnChart);
		int num2 = Math.Min(region.EndBarIndex, lastBarOnChart);
		float num3 = chartControl.GetXByBarIndex(ChartBars, num);
		float num4 = chartControl.GetXByBarIndex(ChartBars, num2);
		float num5 = Math.Abs(num4 - num3);
		if (num5 < 5f)
		{
			return;
		}
		float num6 = num5 * 0.85f;
		float num7 = Math.Min(num3, num4);
		float num8 = Math.Max(num3, num4);
		float num9 = ((ProfileAlignment == VPAlignment.Left) ? num7 : ((ProfileAlignment != VPAlignment.Right) ? (num7 + (num5 - num6) / 2f) : (num8 - num6)));
		double num10 = volumeGroupSize * 0.5;
		if (ShowDeltaBars && region.MaxAbsDelta > 0 && dxDeltaPositiveBrush != null && dxDeltaNegativeBrush != null)
		{
			float num11 = Math.Max(5f, Math.Min(DeltaProfileWidth, num6));
			RectangleF val = default(RectangleF);
			for (int i = 0; i < region.RenderLevelCount; i++)
			{
				PriceLevel priceLevel = region.RenderLevels[i];
				long num12 = Math.Abs(priceLevel.Delta);
				if (num12 >= DeltaThreshold && num12 != 0L)
				{
					double num13 = priceLevel.Price - num10;
					double num14 = priceLevel.Price + num10;
					if (!(num14 < region.LowPrice) && !(num13 > region.HighPrice))
					{
						double num15 = Math.Min(num14, region.HighPrice);
						double num16 = Math.Max(num13, region.LowPrice);
						float num17 = chartScale.GetYByValue(num15);
						float num18 = chartScale.GetYByValue(num16);
						float num19 = Math.Min(num17, num18);
						float num20 = Math.Max(1f, Math.Abs(num18 - num17));
						float num21 = Math.Max(1f, (float)((double)num12 / (double)region.MaxAbsDelta * (double)num11));
						float num22 = num9 - num21;
						val = new RectangleF(num22, num19, num21, num20);
						SolidColorBrush val2 = ((priceLevel.Delta > 0) ? dxDeltaPositiveBrush : dxDeltaNegativeBrush);
						RenderTarget.FillRectangle(val, (Brush)(object)val2);
					}
				}
			}
		}
		int num23 = 0;
		RectangleF val4 = default(RectangleF);
		for (int j = 0; j < region.RenderLevelCount; j++)
		{
			PriceLevel priceLevel2 = region.RenderLevels[j];
			if (priceLevel2.Volume >= VolumeThreshold)
			{
				double num24 = priceLevel2.Price - num10;
				double num25 = priceLevel2.Price + num10;
				if (!(num25 < region.LowPrice) && !(num24 > region.HighPrice))
				{
					double num26 = Math.Min(num25, region.HighPrice);
					double num27 = Math.Max(num24, region.LowPrice);
					float num28 = chartScale.GetYByValue(num26);
					float num29 = chartScale.GetYByValue(num27);
					float num30 = Math.Min(num28, num29);
					float num31 = Math.Max(1f, Math.Abs(num29 - num28));
					float num32 = Math.Max(2f, (float)((double)priceLevel2.Volume / (double)maxVolume * (double)num6));
					SolidColorBrush val3 = (SolidColorBrush)(priceLevel2.Zone == 2 ? dxPOCBrush : (priceLevel2.Zone == 1 ? dxValueAreaBrush : dxProfileBrush));
					val4 = new RectangleF(num9, num30, num32, num31);
					RenderTarget.FillRectangle(val4, (Brush)(object)val3);
					num23++;
				}
			}
		}
		if (num23 != 0)
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
		StrokeStyle val = dxDashStroke ?? dxSolidStroke;
		int num = Math.Max(region.StartBarIndex, firstBarOnChart);
		int num2 = Math.Min(region.EndBarIndex, lastBarOnChart);
		float val2 = chartControl.GetXByBarIndex(ChartBars, num);
		float val3 = chartControl.GetXByBarIndex(ChartBars, num2);
		float num3 = Math.Min(val2, val3);
		float num4 = Math.Max(val2, val3);
		float num5 = ChartPanel.Y;
		float num6 = ChartPanel.Y + ChartPanel.H;
		float num7 = ChartPanel.X + ChartPanel.W;
		if (ShowPOCLine && region.POCPrice > 0.0 && region.POCPrice >= region.LowPrice && region.POCPrice <= region.HighPrice)
		{
			float num8 = chartScale.GetYByValue(region.POCPrice);
			RenderTarget.DrawLine(new Vector2(num3, num8), new Vector2(num4, num8), (Brush)(object)dxPOCLineBrush, POCLineWidth, dxSolidStroke);
			if (ShowLabels && num8 >= num5 && num8 <= num6 && num4 + (float)LabelOffset <= num7)
			{
				RenderCachedLabel(num4, num8, region.POCLabelCached, dxPOCLabelBrush);
			}
		}
		if (!ShowVALines)
		{
			return;
		}
		if (region.VAHPrice > 0.0 && region.VAHPrice >= region.LowPrice && region.VAHPrice <= region.HighPrice)
		{
			float num9 = chartScale.GetYByValue(region.VAHPrice);
			RenderTarget.DrawLine(new Vector2(num3, num9), new Vector2(num4, num9), (Brush)(object)dxVAHLineBrush, VAHLineWidth, val);
			if (ShowLabels && num9 >= num5 && num9 <= num6 && num4 + (float)LabelOffset <= num7)
			{
				RenderCachedLabel(num4, num9, region.VAHLabelCached, dxVAHLabelBrush);
			}
		}
		if (region.VALPrice > 0.0 && region.VALPrice >= region.LowPrice && region.VALPrice <= region.HighPrice)
		{
			float num10 = chartScale.GetYByValue(region.VALPrice);
			RenderTarget.DrawLine(new Vector2(num3, num10), new Vector2(num4, num10), (Brush)(object)dxVALLineBrush, VALLineWidth, val);
			if (ShowLabels && num10 >= num5 && num10 <= num6 && num4 + (float)LabelOffset <= num7)
			{
				RenderCachedLabel(num4, num10, region.VALLabelCached, dxVALLabelBrush);
			}
		}
	}

	private void RenderCachedLabel(float x, float y, string cachedText, SolidColorBrush brush)
	{
		if (dxLabelTextFormat != null && cachedText != null)
		{
			RectangleF val = default(RectangleF);
			val = new RectangleF(x + (float)LabelOffset, y - (float)LabelFontSize - 2f, 200f, (float)(LabelFontSize + 6));
			RenderTarget.DrawText(cachedText, dxLabelTextFormat, val, (Brush)(object)brush);
		}
	}

	private Color GetSafeBrushColor(Brush brush, Color fallbackColor)
	{
		SolidColorBrush val = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
		if (val == null)
		{
			return fallbackColor;
		}
		return val.Color;
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
				Color safeBrushColor = GetSafeBrushColor(ProfileColor, Colors.DodgerBlue);
				dxProfileBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor.R / 255f, (float)(int)safeBrushColor.G / 255f, (float)(int)safeBrushColor.B / 255f, (float)ProfileOpacity / 100f));
				Color safeBrushColor2 = GetSafeBrushColor(ValueAreaColor, Colors.SteelBlue);
				dxValueAreaBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor2.R / 255f, (float)(int)safeBrushColor2.G / 255f, (float)(int)safeBrushColor2.B / 255f, (float)ValueAreaOpacity / 100f));
				Color safeBrushColor3 = GetSafeBrushColor(POCBarColor, Colors.Orange);
				dxPOCBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor3.R / 255f, (float)(int)safeBrushColor3.G / 255f, (float)(int)safeBrushColor3.B / 255f, (float)POCBarOpacity / 100f));
				Color safeBrushColor4 = GetSafeBrushColor(DeltaPositiveColor, Colors.LimeGreen);
				dxDeltaPositiveBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor4.R / 255f, (float)(int)safeBrushColor4.G / 255f, (float)(int)safeBrushColor4.B / 255f, (float)DeltaOpacity / 100f));
				Color safeBrushColor5 = GetSafeBrushColor(DeltaNegativeColor, Colors.IndianRed);
				dxDeltaNegativeBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor5.R / 255f, (float)(int)safeBrushColor5.G / 255f, (float)(int)safeBrushColor5.B / 255f, (float)DeltaOpacity / 100f));
				Color safeBrushColor6 = GetSafeBrushColor(POCLineColor, Colors.Orange);
				dxPOCLineBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor6.R / 255f, (float)(int)safeBrushColor6.G / 255f, (float)(int)safeBrushColor6.B / 255f, 1f));
				Color safeBrushColor7 = GetSafeBrushColor(VAHLineColor, Colors.DodgerBlue);
				dxVAHLineBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor7.R / 255f, (float)(int)safeBrushColor7.G / 255f, (float)(int)safeBrushColor7.B / 255f, 1f));
				Color safeBrushColor8 = GetSafeBrushColor(VALLineColor, Colors.DodgerBlue);
				dxVALLineBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor8.R / 255f, (float)(int)safeBrushColor8.G / 255f, (float)(int)safeBrushColor8.B / 255f, 1f));
				Color safeBrushColor9 = GetSafeBrushColor(POCLabelColor, Colors.Orange);
				dxPOCLabelBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor9.R / 255f, (float)(int)safeBrushColor9.G / 255f, (float)(int)safeBrushColor9.B / 255f, 1f));
				Color safeBrushColor10 = GetSafeBrushColor(VAHLabelColor, Colors.DodgerBlue);
				dxVAHLabelBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor10.R / 255f, (float)(int)safeBrushColor10.G / 255f, (float)(int)safeBrushColor10.B / 255f, 1f));
				Color safeBrushColor11 = GetSafeBrushColor(VALLabelColor, Colors.DodgerBlue);
				dxVALLabelBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor11.R / 255f, (float)(int)safeBrushColor11.G / 255f, (float)(int)safeBrushColor11.B / 255f, 1f));
				Color safeBrushColor12 = GetSafeBrushColor(RectangleBorderColor, Colors.White);
				dxRectBorderBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor12.R / 255f, (float)(int)safeBrushColor12.G / 255f, (float)(int)safeBrushColor12.B / 255f, 0.7f));
				dxRectFillBrush = new SolidColorBrush(RenderTarget, new Color4((float)(int)safeBrushColor12.R / 255f, (float)(int)safeBrushColor12.G / 255f, (float)(int)safeBrushColor12.B / 255f, (float)RectangleFillOpacity / 100f));
				dxSelectedBorderBrush = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 0f, 1f));
				dxAnchorBrush = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 0.9f));
				dxSolidStroke = new StrokeStyle(((Resource)RenderTarget).Factory, new StrokeStyleProperties
				{
					DashStyle = (DashStyle)0
				});
				try
				{
					dxDashStroke = new StrokeStyle(((Resource)RenderTarget).Factory, new StrokeStyleProperties
					{
						DashStyle = (DashStyle)1
					});
				}
				catch
				{
					dxDashStroke = null;
				}
				try
				{
					dxLabelTextFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", (float)LabelFontSize)
					{
						TextAlignment = (TextAlignment)0,
						ParagraphAlignment = (ParagraphAlignment)2
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
		OnRenderTargetChanged();
	}

	private void DisposeDx()
	{
		SolidColorBrush obj = dxProfileBrush;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		dxProfileBrush = null;
		SolidColorBrush obj2 = dxValueAreaBrush;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		dxValueAreaBrush = null;
		SolidColorBrush obj3 = dxPOCBrush;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
		dxPOCBrush = null;
		SolidColorBrush obj4 = dxDeltaPositiveBrush;
		if (obj4 != null)
		{
			((DisposeBase)obj4).Dispose();
		}
		dxDeltaPositiveBrush = null;
		SolidColorBrush obj5 = dxDeltaNegativeBrush;
		if (obj5 != null)
		{
			((DisposeBase)obj5).Dispose();
		}
		dxDeltaNegativeBrush = null;
		SolidColorBrush obj6 = dxPOCLineBrush;
		if (obj6 != null)
		{
			((DisposeBase)obj6).Dispose();
		}
		dxPOCLineBrush = null;
		SolidColorBrush obj7 = dxVAHLineBrush;
		if (obj7 != null)
		{
			((DisposeBase)obj7).Dispose();
		}
		dxVAHLineBrush = null;
		SolidColorBrush obj8 = dxVALLineBrush;
		if (obj8 != null)
		{
			((DisposeBase)obj8).Dispose();
		}
		dxVALLineBrush = null;
		SolidColorBrush obj9 = dxPOCLabelBrush;
		if (obj9 != null)
		{
			((DisposeBase)obj9).Dispose();
		}
		dxPOCLabelBrush = null;
		SolidColorBrush obj10 = dxVAHLabelBrush;
		if (obj10 != null)
		{
			((DisposeBase)obj10).Dispose();
		}
		dxVAHLabelBrush = null;
		SolidColorBrush obj11 = dxVALLabelBrush;
		if (obj11 != null)
		{
			((DisposeBase)obj11).Dispose();
		}
		dxVALLabelBrush = null;
		SolidColorBrush obj12 = dxRectBorderBrush;
		if (obj12 != null)
		{
			((DisposeBase)obj12).Dispose();
		}
		dxRectBorderBrush = null;
		SolidColorBrush obj13 = dxRectFillBrush;
		if (obj13 != null)
		{
			((DisposeBase)obj13).Dispose();
		}
		dxRectFillBrush = null;
		SolidColorBrush obj14 = dxSelectedBorderBrush;
		if (obj14 != null)
		{
			((DisposeBase)obj14).Dispose();
		}
		dxSelectedBorderBrush = null;
		SolidColorBrush obj15 = dxAnchorBrush;
		if (obj15 != null)
		{
			((DisposeBase)obj15).Dispose();
		}
		dxAnchorBrush = null;
		StrokeStyle obj16 = dxSolidStroke;
		if (obj16 != null)
		{
			((DisposeBase)obj16).Dispose();
		}
		dxSolidStroke = null;
		StrokeStyle obj17 = dxDashStroke;
		if (obj17 != null)
		{
			((DisposeBase)obj17).Dispose();
		}
		dxDashStroke = null;
		TextFormat obj18 = dxLabelTextFormat;
		if (obj18 != null)
		{
			((DisposeBase)obj18).Dispose();
		}
		dxLabelTextFormat = null;
		dxResourcesValid = false;
	}
}
}
