namespace Fenceless.UI.Widgets
{
    public readonly struct FenceWidgetRenderResult
    {
        public FenceWidgetRenderResult(int maxScrollOffset)
        {
            MaxScrollOffset = maxScrollOffset;
        }

        public int MaxScrollOffset { get; }
    }
}
