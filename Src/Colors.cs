using ICSharpCode.AvalonEdit.Highlighting;

namespace Backlang.Ilspy
{
    public static class Colors
    {
        public static readonly HighlightingColor TypeColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(System.Windows.Media.Colors.LightBlue) };
        public static readonly HighlightingColor KeywordColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(System.Windows.Media.Colors.Blue) };
        public static readonly HighlightingColor StringColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(System.Windows.Media.Colors.PaleVioletRed) };
        public static readonly HighlightingColor AnnotationColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(System.Windows.Media.Colors.Green) };

    }
}