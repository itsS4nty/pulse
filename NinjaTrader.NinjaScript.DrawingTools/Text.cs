using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class Text : DrawingTool
{
	private Brush areaBrush;

	private DeviceBrush areaBrushDevice = new DeviceBrush();

	private int areaOpacity;

	private TextAlignment alignment;

	[CLSCompliant(false)]
	protected TextLayout cachedTextLayout;

	private SimpleFont font;

	private Rect layoutRect;

	private bool needsLayoutUpdate;

	private readonly float outlinePadding = GetPadding();

	private Brush textBrush;

	private DeviceBrush textBrushDevice = new DeviceBrush();

	private string text;

	public override object Icon => Icons.DrawText;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextAlignment", GroupName = "NinjaScriptGeneral", Order = 7)]
	public TextAlignment Alignment
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return alignment;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			if (alignment != value)
			{
				alignment = value;
				needsLayoutUpdate = true;
			}
		}
	}

	[XmlIgnore]
	[Browsable(false)]
	public bool UseChartTextBrush { get; set; }

	[Browsable(false)]
	public bool UseChartTextBrushSerialize
	{
		get
		{
			if (UseChartTextBrush)
			{
				if (LastBrush != null && TextBrush != null)
				{
					return ((object)LastBrush).ToString() == ((object)TextBrush).ToString();
				}
				return true;
			}
			return false;
		}
		set
		{
			UseChartTextBrush = value;
		}
	}

	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool ManuallyDrawn { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public Brush LastBrush { get; set; }

	public ChartAnchor Anchor { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { Anchor };

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush AreaBrush
	{
		get
		{
			return areaBrush;
		}
		set
		{
			areaBrush = value;
			Brush val = areaBrush;
			if (val != null && ((Freezable)val).CanFreeze)
			{
				((Freezable)areaBrush).Freeze();
			}
		}
	}

	[Browsable(false)]
	public string AreaBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(AreaBrush);
		}
		set
		{
			AreaBrush = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
	public int AreaOpacity
	{
		get
		{
			return areaOpacity;
		}
		set
		{
			areaOpacity = Math.Max(0, Math.Min(100, value));
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextFont", GroupName = "NinjaScriptGeneral", Order = 4)]
	public SimpleFont Font
	{
		get
		{
			return font;
		}
		set
		{
			font = value;
			needsLayoutUpdate = true;
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 3)]
	public Stroke OutlineStroke { get; set; }

	[ExcludeFromTemplate]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolText", GroupName = "NinjaScriptGeneral", Order = 5)]
	[PropertyEditor("NinjaTrader.Gui.Tools.MultilineEditor")]
	public string DisplayText
	{
		get
		{
			return text;
		}
		set
		{
			if (!(text == value))
			{
				text = value;
				needsLayoutUpdate = true;
			}
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush TextBrush
	{
		get
		{
			return textBrush;
		}
		set
		{
			textBrush = value;
			Brush val = textBrush;
			if (val != null && ((Freezable)val).CanFreeze)
			{
				((Freezable)textBrush).Freeze();
			}
		}
	}

	[Browsable(false)]
	public string TextBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(TextBrush);
		}
		set
		{
			TextBrush = Serialize.StringToBrush(value);
		}
	}

	[Browsable(false)]
	public int YPixelOffset { get; set; }

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		TextLayout obj = cachedTextLayout;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		if (textBrushDevice != null)
		{
			textBrushDevice.RenderTarget = null;
		}
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
		cachedTextLayout = null;
		textBrushDevice = null;
		areaBrushDevice = null;
	}

	private void DrawText(ChartControl chartControl)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		if (Font == null || string.IsNullOrEmpty(DisplayText))
		{
			return;
		}
		Rect currentRect = GetCurrentRect(layoutRect, outlinePadding);
		RectangleF val = default(RectangleF);
		((RectangleF)(ref val))._002Ector((float)((Rect)(ref currentRect)).X, (float)((Rect)(ref currentRect)).Y, (float)((Rect)(ref currentRect)).Width, (float)((Rect)(ref currentRect)).Height);
		Stroke outlineStroke = OutlineStroke;
		textBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		if (AreaBrush != null)
		{
			Brush obj = AreaBrush;
			SolidColorBrush val2 = (SolidColorBrush)(object)((obj is SolidColorBrush) ? obj : null);
			if (val2 != null)
			{
				Brush brush = areaBrushDevice.Brush;
				SolidColorBrush val3 = (SolidColorBrush)(object)((brush is SolidColorBrush) ? brush : null);
				if (val3 != null && !(val3.Color != val2.Color) && !(Math.Abs(((Brush)val3).Opacity - (double)areaOpacity / 100.0) > 0.1))
				{
					goto IL_0128;
				}
			}
			Brush val4 = AreaBrush.Clone();
			val4.Opacity = (double)areaOpacity / 100.0;
			areaBrushDevice.Brush = val4;
			goto IL_0128;
		}
		areaBrushDevice.RenderTarget = null;
		goto IL_0170;
		IL_023d:
		Brush val5 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : textBrushDevice.BrushDX);
		((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2(((RectangleF)(ref val)).X + outlinePadding, ((RectangleF)(ref val)).Y + outlinePadding), cachedTextLayout, val5, (DrawTextOptions)1);
		return;
		IL_0128:
		areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		val5 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : areaBrushDevice.BrushDX);
		((ChartObject)this).RenderTarget.FillRectangle(val, val5);
		goto IL_0170;
		IL_0170:
		if (outlineStroke.StrokeStyle != null && (outlineStroke.Brush != null || !BrushExtensions.IsTransparent(outlineStroke.Brush)))
		{
			val5 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			if (val5 != null)
			{
				((ChartObject)this).RenderTarget.DrawRectangle(val, val5, outlineStroke.Width, outlineStroke.StrokeStyle);
			}
		}
		textBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		Brush obj2 = TextBrush;
		SolidColorBrush val6 = (SolidColorBrush)(object)((obj2 is SolidColorBrush) ? obj2 : null);
		if (val6 != null)
		{
			Brush brush2 = textBrushDevice.Brush;
			SolidColorBrush val7 = (SolidColorBrush)(object)((brush2 is SolidColorBrush) ? brush2 : null);
			if (val7 != null && !(val7.Color != val6.Color) && !(Math.Abs(((Brush)val7).Opacity - ((Brush)val6).Opacity) > 0.1))
			{
				goto IL_023d;
			}
		}
		textBrushDevice.Brush = TextBrush;
		goto IL_023d;
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if ((int)((UIElement)chartControl.GetTextEntryBox()).Visibility == 0)
			{
				return null;
			}
			return Cursors.IBeam;
		}
		if ((int)((DrawingTool)this).DrawingState == 3)
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		}
		Rect currentRect = GetCurrentRect(layoutRect, outlinePadding);
		if (!((Rect)(ref currentRect)).IntersectsWith(new Rect(((Point)(ref point)).X, ((Point)(ref point)).Y, 4.0, 4.0)))
		{
			return null;
		}
		if (!((DrawingTool)this).IsLocked)
		{
			return Cursors.SizeAll;
		}
		return Cursors.Arrow;
	}

	protected virtual Rect GetCurrentRect(Rect pLayoutRect, double pOutlinePadding)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (ManuallyDrawn)
		{
			return new Rect(((Rect)(ref pLayoutRect)).X - pOutlinePadding, ((Rect)(ref pLayoutRect)).Y - pOutlinePadding, ((Rect)(ref pLayoutRect)).Width + pOutlinePadding * 2.0, ((Rect)(ref pLayoutRect)).Height + pOutlinePadding * 2.0);
		}
		return new Rect(((Rect)(ref pLayoutRect)).X - pOutlinePadding, ((Rect)(ref pLayoutRect)).Y - ((Rect)(ref pLayoutRect)).Height / 2.0 - pOutlinePadding, ((Rect)(ref pLayoutRect)).Width + pOutlinePadding * 2.0, ((Rect)(ref pLayoutRect)).Height + pOutlinePadding * 2.0);
	}

	private static float GetPadding()
	{
		return (Application.Current.FindResource((object)"FontModalTitleMargin") as float?) ?? 3f;
	}

	protected virtual Point GetTextDrawingPosition(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected I4, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		Point point = Anchor.GetPoint(chartControl, chartPanel, chartScale, true);
		if (cachedTextLayout == null)
		{
			return point;
		}
		TextAlignment val = Alignment;
		return (Point)((int)val switch
		{
			2 => new Point(((Point)(ref point)).X - (double)(cachedTextLayout.MaxWidth / 2f), ((Point)(ref point)).Y), 
			1 => new Point(((Point)(ref point)).X - (double)cachedTextLayout.MaxWidth, ((Point)(ref point)).Y), 
			0 => new Point(((Point)(ref point)).X + (double)outlinePadding, ((Point)(ref point)).Y), 
			_ => point, 
		});
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0 || layoutRect == default(Rect) || (int)((UIElement)chartControl.GetTextEntryBox()).Visibility == 0)
		{
			return Array.Empty<Point>();
		}
		Rect currentRect = GetCurrentRect(layoutRect, outlinePadding);
		return (Point[])(object)new Point[4]
		{
			((Rect)(ref currentRect)).TopLeft,
			((Rect)(ref currentRect)).TopRight,
			((Rect)(ref currentRect)).BottomLeft,
			((Rect)(ref currentRect)).BottomRight
		};
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		float num = (float)chartControl.GetXByTime(Anchor.Time) + ((cachedTextLayout == null) ? 0f : cachedTextLayout.Metrics.Width);
		DateTime timeByX = chartControl.GetTimeByX((int)num);
		if (Anchor.Time > lastTimeOnChart || timeByX < firstTimeOnChart)
		{
			return false;
		}
		if (((ChartObject)this).IsAutoScale)
		{
			return true;
		}
		if (needsLayoutUpdate || cachedTextLayout == null)
		{
			return true;
		}
		float num2 = chartScale.GetYByValue(Anchor.Price);
		float height = cachedTextLayout.Metrics.Height;
		if (!(chartScale.GetValueByY(num2 + height) > chartScale.MaxValue))
		{
			return !(Anchor.Price < chartScale.MinValue);
		}
		return false;
	}

	public override void OnCalculateMinMax()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible && (int)((DrawingTool)this).DrawingState != 0)
		{
			((ChartObject)this).MinValue = Anchor.Price;
			((ChartObject)this).MaxValue = Anchor.Price;
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolText;
			Alignment = (TextAlignment)0;
			Anchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				DisplayName = Resource.NinjaScriptDrawingToolAnchor
			};
			Font = new SimpleFont
			{
				Size = 14.0
			};
			OutlineStroke = new Stroke((Brush)(object)Brushes.Transparent, 2f);
			TextBrush = textBrush;
			AreaBrush = (Brush)(object)Brushes.Transparent;
			AreaOpacity = 100;
			YPixelOffset = 0;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			TextBrush = null;
			textBrush = null;
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Expected O, but got Unknown
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Expected O, but got Unknown
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Expected O, but got Unknown
		TextBox tb;
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			dataPoint.CopyDataValues(Anchor);
			Anchor.IsEditing = false;
			Point mouseDownPoint = chartControl.MouseDownPoint;
			DisplayText = string.Empty;
			tb = chartControl.GetTextEntryBox();
			tb.Text = string.Empty;
			((TextBoxBase)tb).AcceptsReturn = true;
			((TextBoxBase)tb).AcceptsTab = true;
			((Control)tb).Background = (Brush)new SolidColorBrush(Color.FromArgb((byte)4, (byte)0, (byte)0, (byte)0));
			((Control)tb).BorderBrush = chartControl.Properties.AxisPen.Brush;
			((Control)tb).FontFamily = Font.Family;
			((Control)tb).FontSize = Font.Size;
			((Control)tb).FontStyle = (Font.Italic ? FontStyles.Italic : FontStyles.Normal);
			((Control)tb).FontWeight = (Font.Bold ? FontWeights.Bold : FontWeights.Normal);
			((Control)tb).Foreground = TextBrush ?? chartControl.Properties.ChartText;
			TextBox obj = tb;
			object obj2 = Application.Current.FindResource((object)"TextBoxNoEffects");
			((FrameworkElement)obj).Style = (Style)((obj2 is Style) ? obj2 : null);
			((FrameworkElement)tb).Margin = new Thickness(((Point)(ref mouseDownPoint)).X, ((Point)(ref mouseDownPoint)).Y, 0.0, 0.0);
			if (TextBrush == null)
			{
				UseChartTextBrush = true;
			}
			((UIElement)tb).PreviewKeyDown += new KeyEventHandler(OnTbOnPreviewKeyDown);
			((UIElement)chartControl).PreviewMouseDown += new MouseButtonEventHandler(OnTbPreviewMouseDown);
			((UIElement)tb).IsVisibleChanged += new DependencyPropertyChangedEventHandler(OnTbOnIsVisibleChanged);
			ManuallyDrawn = true;
			((UIElement)tb).Visibility = (Visibility)0;
			((UIElement)tb).Focus();
		}
		else
		{
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			Rect currentRect = GetCurrentRect(layoutRect, outlinePadding);
			if (((Rect)(ref currentRect)).IntersectsWith(new Rect(((Point)(ref point)).X, ((Point)(ref point)).Y, 2.0, 2.0)))
			{
				Anchor.IsEditing = true;
				((DrawingTool)this).DrawingState = (DrawingState)3;
			}
			else
			{
				((ChartObject)this).IsSelected = false;
			}
		}
		void OnTbOnIsVisibleChanged(object _, DependencyPropertyChangedEventArgs __)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Expected O, but got Unknown
			if ((int)((UIElement)tb).Visibility != 0)
			{
				((UIElement)tb).PreviewKeyDown -= new KeyEventHandler(OnTbOnPreviewKeyDown);
				((UIElement)tb).PreviewMouseDown -= new MouseButtonEventHandler(OnTbPreviewMouseDown);
				((UIElement)tb).IsVisibleChanged -= new DependencyPropertyChangedEventHandler(OnTbOnIsVisibleChanged);
				DisplayText = tb.Text;
				((DrawingTool)this).DrawingState = (DrawingState)2;
				((ChartObject)this).IsSelected = false;
				chartControl.InvalidateVisual();
				if (chartControl.IsStayInDrawMode)
				{
					chartControl.TryStartDrawing(((object)this).GetType().FullName);
				}
				if (((DrawingTool)this).IsGlobalDrawingTool)
				{
					GlobalDrawingToolManager.RaiseGlobalDrawingObjectChanged(chartControl, (Operation)1, (DrawingTool)(object)this);
				}
			}
		}
		void OnTbOnPreviewKeyDown(object _, KeyEventArgs args)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Invalid comparison between Unknown and I4
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Invalid comparison between Unknown and I4
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Invalid comparison between Unknown and I4
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Invalid comparison between Unknown and I4
			Key key = args.Key;
			if (((int)key == 3 || (int)key == 6) ? true : false)
			{
				((UIElement)tb).Visibility = (Visibility)2;
				((RoutedEventArgs)args).Handled = true;
			}
			else if ((int)args.Key == 156 && (int)args.SystemKey == 6)
			{
				int caretIndex = tb.CaretIndex;
				string text = tb.Text.Substring(0, caretIndex);
				string text2 = tb.Text.Substring(caretIndex);
				tb.Text = text + Environment.NewLine + text2;
				tb.CaretIndex = caretIndex + Environment.NewLine.Length;
				((RoutedEventArgs)args).Handled = true;
			}
		}
		void OnTbPreviewMouseDown(object _, MouseButtonEventArgs __)
		{
			if (!((UIElement)tb).IsMouseDirectlyOver)
			{
				((UIElement)tb).Visibility = (Visibility)2;
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		bool flag = !((DrawingTool)this).IsLocked;
		if (flag)
		{
			DrawingState drawingState = ((DrawingTool)this).DrawingState;
			bool flag2 = (((int)drawingState == 1 || (int)drawingState == 3) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			Anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		((DrawingTool)this).DrawingState = (DrawingState)2;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return;
		}
		if (UseChartTextBrush)
		{
			if (LastBrush != TextBrush && LastBrush != chartControl.Properties.ChartText && LastBrush != null)
			{
				LastBrush = TextBrush;
				UseChartTextBrush = false;
			}
			else
			{
				TextBrush = chartControl.Properties.ChartText;
				LastBrush = TextBrush;
			}
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		UpdateTextLayout(val.W);
		Point textDrawingPosition = GetTextDrawingPosition(chartControl, val, chartScale);
		float num = (float)((Point)(ref textDrawingPosition)).X;
		float num2 = (float)((Point)(ref textDrawingPosition)).Y;
		num2 -= (float)YPixelOffset;
		layoutRect = new Rect((double)num, (double)num2, (double)cachedTextLayout.MaxWidth, (double)cachedTextLayout.MaxHeight);
		DrawText(chartControl);
	}

	private void UpdateTextLayout(float maxWidth)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Invalid comparison between Unknown and I4
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Invalid comparison between Unknown and I4
		if (needsLayoutUpdate)
		{
			needsLayoutUpdate = false;
			cachedTextLayout = null;
			if (Font != null)
			{
				TextFormat val = Font.ToDirectWriteTextFormat();
				cachedTextLayout = new TextLayout(Globals.DirectWriteFactory, DisplayText ?? string.Empty, val, maxWidth, val.FontSize);
				cachedTextLayout.MaxWidth = cachedTextLayout.Metrics.Width;
				cachedTextLayout.MaxHeight = cachedTextLayout.Metrics.Height;
				((TextFormat)cachedTextLayout).TextAlignment = (TextAlignment)(((int)Alignment == 2) ? 2 : (((int)Alignment == 1) ? 1 : 0));
				needsLayoutUpdate = false;
				((DisposeBase)val).Dispose();
			}
		}
	}
}
