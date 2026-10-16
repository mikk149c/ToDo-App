using Windows.Foundation;

namespace ToDo_App.UI.Controls
{
    /// <summary>
    /// Lays children out left to right and wraps to a new line when the next child doesn't fit.
    /// </summary>
    public sealed class WrapPanel : Panel
    {
        public double HorizontalSpacing { get; set; } = 4;
        public double VerticalSpacing { get; set; } = 4;

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (UIElement child in Children)
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            }

            return layout(availableSize.Width, arrange: false);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            layout(finalSize.Width, arrange: true);
            return finalSize;
        }

        // Shared by measure and arrange so both agree on where each child goes
        private Size layout(double maxWidth, bool arrange)
        {
            double x = 0, y = 0, lineHeight = 0, usedWidth = 0;

            foreach (UIElement child in Children)
            {
                Size size = child.DesiredSize;
                if (x > 0 && x + size.Width > maxWidth)
                {
                    x = 0;
                    y += lineHeight + VerticalSpacing;
                    lineHeight = 0;
                }

                if (arrange)
                {
                    child.Arrange(new Rect(x, y, size.Width, size.Height));
                }

                usedWidth = Math.Max(usedWidth, x + size.Width);
                lineHeight = Math.Max(lineHeight, size.Height);
                x += size.Width + HorizontalSpacing;
            }

            return new Size(usedWidth, y + lineHeight);
        }
    }
}
