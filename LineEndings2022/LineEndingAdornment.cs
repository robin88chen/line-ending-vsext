using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LineEndings2022
{
    /// <summary>
    /// LineEndingAdornment places red boxes behind all the "a"s in the editor window
    /// </summary>
    internal sealed class LineEndingAdornment
    {
        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer layer;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private readonly IWpfTextView view;

        private Brush glyphBrush;

        int font_size;
        ImageSource image_source_crlf;
        ImageSource image_source_lf;
        float pixelsPerDip;

        string lf_mark = "$";
        string crlf_mark = "8";

        private SVsServiceProvider service_provider;
        /// <summary>
        /// Initializes a new instance of the <see cref="LineEndingAdornment"/> class.
        /// </summary>
        /// <param name="view">Text view to create the adornment for</param>
        public LineEndingAdornment(SVsServiceProvider provider, IWpfTextView view)
        {
            service_provider = provider;

            ThreadHelper.ThrowIfNotOnUIThread();

            pixelsPerDip = (float)VisualTreeHelper.GetDpi(new Button()).PixelsPerDip;
            RetrieveMarks();
            GetTextFont();
            image_source_crlf = GenerateTextImageSource(crlf_mark, glyphBrush, FontStyles.Normal, FontWeights.Light, FontStretches.Normal);
            image_source_lf = GenerateTextImageSource(lf_mark, glyphBrush, FontStyles.Normal, FontWeights.Light, FontStretches.Normal);
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            this.layer = view.GetAdornmentLayer("LineEndingAdornment");

            this.view = view;
            this.view.LayoutChanged += this.OnLayoutChanged;
        }

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                this.CreateVisuals(line);
            }
        }

        private void RetrieveMarks()
        {
            OptionPageGrid page = (OptionPageGrid)LineEndings2022Package.Instance.GetDialogPage(typeof(OptionPageGrid));
            page.LoadSettingsFromStorage();
            lf_mark = $"{Convert.ToChar((byte)page.LfMark)}";
            crlf_mark = $"{Convert.ToChar((byte)page.CrLfMark)}";
        }
        private void GetTextFont()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE vsEnvironment = (DTE)service_provider.GetService(typeof(DTE));
            if (vsEnvironment == null) throw new InvalidOperationException("No DTE service");
            EnvDTE.Properties propertiesList = vsEnvironment.get_Properties("FontsAndColors", "TextEditor");
            Property prop = propertiesList.Item("FontSize");
            font_size = (System.Int16)prop.Value;
            var fontColorItems = propertiesList.Item("FontsAndColorsItems").Object as FontsAndColorsItems;
            var colorableItems = fontColorItems.Item("Comment");
            byte[] color_bytes = BitConverter.GetBytes(colorableItems.Foreground);
            glyphBrush = new SolidColorBrush(Color.FromArgb(0x80, color_bytes[2], color_bytes[1], color_bytes[0]));
        }

        /// <summary>
        /// Adds the scarlet box behind the 'a' characters within the given line
        /// </summary>
        /// <param name="line">Line to add the adornments</param>
        private void CreateVisuals(ITextViewLine line)
        {
            IWpfTextViewLineCollection textViewLines = this.view.TextViewLines;

            int charIndex = line.EndIncludingLineBreak - 1;

            if (this.view.TextSnapshot[charIndex] == '\n')
            {
                SnapshotSpan span = new SnapshotSpan(this.view.TextSnapshot, Span.FromBounds(charIndex, charIndex + 1));
                Geometry geometry = textViewLines.GetMarkerGeometry(span);
                if (geometry != null)
                {
                    //var drawing = new GeometryDrawing(line.LineBreakLength == 2 ? this.brush_for_crlf : this.brush_for_lf, this.pen, geometry);
                    //drawing.Freeze();

                    //var drawingImage = new DrawingImage(drawing);
                    //drawingImage.Freeze();
                    Image image;
                    if (line.LineBreakLength == 2)
                    {
                        image_source_crlf.Freeze();
                        image = new Image { Source = image_source_crlf };
                    }
                    else
                    {
                        image_source_lf.Freeze();
                        image = new Image { Source = image_source_lf };

                    }

                    // Align the image with the top of the bounds of the text geometry
                    Canvas.SetLeft(image, geometry.Bounds.Left + 1);
                    Canvas.SetTop(image, geometry.Bounds.Top + 2);

                    this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
                }
            }
        }
        private ImageSource GenerateTextImageSource(string text, Brush foreBrush, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch)
        {
            var fontFamily = new FontFamily("Wingdings 3"); // font_family);
            if (fontFamily != null && !String.IsNullOrEmpty(text))
            {
                Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);

                GlyphTypeface glyphTypeface;
                if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
                {
                    //typeface = new Typeface(new FontFamily(new Uri("pack://application:,,,"), fontFamily.Source), fontStyle, fontWeight, fontStretch);
                    //if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
                        throw new InvalidOperationException("No glyphtypeface found");
                }

                ushort[] glyphIndexes = new ushort[text.Length];
                double[] advanceWidths = new double[text.Length];

                for (int n = 0; n < text.Length; n++)
                {
                    ushort glyphIndex;
                    try
                    {
                        glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

                    }
                    catch (Exception)
                    {
                        glyphIndex = 42;
                    }
                    glyphIndexes[n] = glyphIndex;

                    double width = glyphTypeface.AdvanceWidths[glyphIndex] * 1.0;
                    advanceWidths[n] = width;
                }

                try
                {

                    GlyphRun gr = new GlyphRun(glyphTypeface, 0, false, font_size * 96.0 / 72.0, pixelsPerDip, glyphIndexes,
                                                                         new Point(0, 0), advanceWidths, null, null, null, null, null, null);

                    GlyphRunDrawing glyphRunDrawing = new GlyphRunDrawing(foreBrush, gr);
                    return new DrawingImage(glyphRunDrawing);
                }
                catch (Exception ex)
                {
                    // ReSharper disable LocalizableElement
                    Console.WriteLine("Error in generating Glyphrun : " + ex.Message);
                    // ReSharper restore LocalizableElement
                }
            }
            return null;
        }

    }
}
