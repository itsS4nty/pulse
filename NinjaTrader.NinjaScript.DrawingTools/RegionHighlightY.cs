using System;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

[CLSCompliant(false)]
public class RegionHighlightY : RegionHighlightBase
{
	public override object Icon => Icons.DrawRegionHighlightY;

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRegionHiglightY;
			base.Mode = RegionHighlightMode.Price;
			base.StartAnchor.IsXPropertiesVisible = false;
			base.EndAnchor.IsXPropertiesVisible = false;
		}
	}
}
