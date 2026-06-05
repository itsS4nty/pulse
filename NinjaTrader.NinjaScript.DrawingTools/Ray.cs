using NinjaTrader.Custom;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class Ray : Line
{
	public override object Icon => Icons.DrawRay;

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			base.LineType = ChartLineType.Ray;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRay;
		}
	}
}
