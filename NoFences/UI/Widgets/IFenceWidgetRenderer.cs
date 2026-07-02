using System.Drawing;

namespace Fenceless.UI.Widgets
{
    public interface IFenceWidgetRenderer
    {
        FenceWidgetRenderResult Render(FenceWidgetRenderContext context);
        string HitTest(FenceWidgetRenderContext context, Point point);
    }
}
