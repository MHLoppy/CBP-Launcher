using CodeKicker.BBCode;
using HTMLConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;

namespace CBPLauncher.Logic
{
    ///
    /// TODO: test that *anything* in this class actually works
    ///
    public class FlowDocumentBuilder
    {
        public class ColorScheme
        {
            public string BackgroundColor { get; set; }
            public string TextColor { get; set; }
            public string HtmlBackgroundColor { get; set; }
            public string HtmlTextColor { get; set; }
        }

        public static FlowDocument CreatePatchnotesPlaceholder(ColorScheme colors)
        {
            var placeholder = new FlowDocument();
            placeholder.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(colors.BackgroundColor);
            placeholder.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(colors.BackgroundColor);//make the text illegible in the placeholder

            placeholder.FontFamily = (FontFamily)Application.Current.Resources["DefaultFont"];
            placeholder.FontSize = 15;
            placeholder.PagePadding = new Thickness(10, 10, 10, 5); // hyperlink height is higher lol
            placeholder.TextAlignment = TextAlignment.Left;

            // replicate top so it looks similar
            Paragraph paragraph = new Paragraph();
            placeholder.Blocks.Add(paragraph);
            Run text1 = new Run("For explanations and more details about these changes, check the full patch notes.");
            paragraph.Inlines.Add(text1);
            string placeholderHtml = $"<html><body style='background-color: {colors.HtmlBackgroundColor}; font-family: sans-serif; color: {colors.HtmlTextColor};'>";

            // Add blank lines to fill space
            for (int i = 0; i < 50; i++)
            {
                placeholderHtml += "<br>&nbsp;</br>";
            }
            placeholderHtml += "</body></html>";

            string placeholderXaml = HtmlToXamlConverter.ConvertHtmlToXaml(placeholderHtml, false);
            placeholder.Blocks.Add((Section)XamlReader.Parse(placeholderXaml));

            return placeholder;
        }

        // announcements are light (low compute) enough that it might not be worth using a placeholder?
        public static FlowDocument CreateAnnouncementsPlaceholder(ColorScheme colors)
        {
            var placeholder = new FlowDocument();
            placeholder.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(colors.BackgroundColor);
            placeholder.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(colors.BackgroundColor);//make the text illegible in the placeholder

            //because the document is larger than the pure HTML page was (in terms of visual space), the background needs to be set a bit differently in order to cover the whole area:
            placeholder.FontFamily = (FontFamily)Application.Current.Resources["DefaultFont"];
            placeholder.FontSize = 15;
            placeholder.PagePadding = new Thickness(5, 5, 5, 5);
            placeholder.TextAlignment = TextAlignment.Left;

            Paragraph paragraph = new Paragraph();
            placeholder.Blocks.Add(paragraph);
            string placeholderHtml = $"<html><body style='background-color: {colors.HtmlBackgroundColor}; font-family: sans-serif; color: {colors.HtmlTextColor};'>";

            // Add blank lines to fill space
            for (int i = 0; i < 10; i++)
            {
                placeholderHtml += "<br>&nbsp;</br>";
            }
            placeholderHtml += "</body></html>";

            string placeholderXaml = HtmlToXamlConverter.ConvertHtmlToXaml(placeholderHtml, false);
            placeholder.Blocks.Add((Section)XamlReader.Parse(placeholderXaml));

            return placeholder;
        }

        public static FlowDocument LoadFormattedDocumentAnnouncements(string filePath, ColorScheme colors)
        {
            // this version is used when rendering directly in HTML (e.g. webbrowser control or cefsharp)
            //string formattedPatchNotes = "<html><body style='background-color: #DFF3F3F3; font-family: sans-serif; color: #FF1A1A1A; font-size:90%;'>" + ProcessBBCodeFromTxtFile(patchnotes) + "</body></html>";

            //WebBrowserControl.NavigateToString(formattedPatchNotes);//this is the integrated browser, which unfortunately doesn't render properly in transparent windows:
            //https://web.archive.org/web/20150415020527/http://blogs.msdn.com/b/changov/archive/2009/01/19/webbrowser-control-on-transparent-wpf-window.aspx
            //hence the unfortunate need to swap to another option e.g. CEF (much, much heavier than the integrated one though)

            var doc = new FlowDocument();

            if (File.Exists(filePath))
            {
                // announcements - the main part of the flowdocument
                string formattedAnnouncements = $"<html><body style='background-color: {colors.HtmlBackgroundColor}; font-family: sans-serif; color: {colors.HtmlTextColor};'>" + ProcessBBCodeFromTxtFileAnnouncements(filePath) + "</body></html>";

                string xaml = HtmlToXamlConverter.ConvertHtmlToXaml(formattedAnnouncements, false);
                doc.Blocks.Add((Section)XamlReader.Parse(xaml));
            }
            else
            {
                Paragraph errorParagraph = new Paragraph();
                errorParagraph.Inlines.Add(new Run("Unable to load announcements file."));
                doc.Blocks.Add(errorParagraph);
            }

            //because the document is larger than the pure HTML page was (in terms of visual space), the background needs to be set a bit differently in order to cover the whole area:
            doc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(colors.BackgroundColor);
            doc.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(colors.TextColor);
            doc.FontFamily = (FontFamily)Application.Current.Resources["DefaultFont"];
            doc.FontSize = 15;
            doc.PagePadding = new Thickness(5, 5, 5, 5);
            doc.TextAlignment = TextAlignment.Left;

            return doc;
        }

        // WIP [see ddg convo]
        //private static FlowDocument LoadFormattedDocumentPatchnotes(string filePath, ColorScheme colors)
        //{
        //    // this version is used when rendering directly in HTML (e.g. webbrowser control or cefsharp)
        //    //string formattedPatchNotes = "<html><body style='background-color: #DFF3F3F3; font-family: sans-serif; color: #FF1A1A1A; font-size:90%;'>" + ProcessBBCodeFromTxtFile(patchnotes) + "</body></html>";

        //    //WebBrowserControl.NavigateToString(formattedPatchNotes);//this is the integrated browser, which unfortunately doesn't render properly in transparent windows:
        //    //https://web.archive.org/web/20150415020527/http://blogs.msdn.com/b/changov/archive/2009/01/19/webbrowser-control-on-transparent-wpf-window.aspx
        //    //hence the unfortunate need to swap to another option e.g. CEF (much, much heavier than the integrated one though)

        //    var doc = new FlowDocument();

        //    if (File.Exists(filePath))
        //    {
        //        // manually adding the hyperlink (and associated text) at the top (because flowdocuments don't handle URLs by default, even though they do display as if they do)
        //        // https://stackoverflow.com/questions/2288999/how-can-i-get-a-flowdocument-hyperlink-to-launch-browser-and-go-to-url-in-a-wpf
        //        Paragraph paragraph = new Paragraph();
        //        doc.Blocks.Add(paragraph);
        //        Run normaltext1 = new Run("For explanations and more details about these changes, check the ");
        //        paragraph.Inlines.Add(normaltext1);
        //        Run linktext = new Run("full patch notes");
        //        Hyperlink workshoplink = new Hyperlink(linktext);
        //        workshoplink.NavigateUri = new Uri("https://mhloppy.com/category/rise-of-nations/cbp-patch-notes/");
        //        workshoplink.RequestNavigate += new RequestNavigateEventHandler(RequestNavigate);
        //        paragraph.Inlines.Add(workshoplink);
        //        Run normaltext2 = new Run(".");
        //        paragraph.Inlines.Add(normaltext2);
        //    }
        //    else
        //    {

        //    }


        //    // add patch notes - the main part of the flowdocument
        //    if (xaml != null)
        //    {
        //        doc.Blocks.Add((Section)XamlReader.Parse(xaml));
        //    }
        //    else
        //    {
        //        Paragraph errorParagraph = new Paragraph();
        //        errorParagraph.Inlines.Add(new Run("Unable to load patch notes file (maybe CBP isn't loaded)."));
        //        doc.Blocks.Add(errorParagraph);
        //    }

        //    doc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#EEF9F9F9");
        //    doc.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF1A1A1A");
        //    doc.FontFamily = (FontFamily)Application.Current.Resources["DefaultFont"];
        //    doc.FontSize = 15;
        //    doc.PagePadding = new Thickness(10, 5, 10, 5);
        //    doc.TextAlignment = TextAlignment.Left;

        //    return doc;
        //}

        private static void RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private static string ProcessBBCodeFromTxtFileAnnouncements(string txtfile)
        {
            string text = File.ReadAllText(txtfile);

            // Make newlines work
            //text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            //text = Regex.Replace(text, @"\n{2,}", "\n\n");
            //text = Regex.Replace(text, @"(?<!\n)\n(?!\n)", "<br/>");

            //text = Regex.Replace(text, @"- ", @"[*] ");
            //text = Regex.Replace(text, @"--> ", @"[*] --> ");

            var bbTags = new List<BBTag>()
            {
                /*new BBTag("h1", "<h1>", "</h1>"),
                new BBTag("h2", "<h2>", "</h2>"),
                new BBTag("h3", "<h3>", "</h3>"),
                new BBTag("h4", "<h4>", "</h4>"),*/

                // headers are too big with defaults in flowdocument since it doesn't accept the 90% text scaling
                new BBTag("h1", "<h2>", "</h2>"),
                new BBTag("h2", "<h3>", "</h3>"),
                new BBTag("h3", "<h4>", "</h4>"),
                new BBTag("h4", "<h5>", "</h5>"),//does h5 exist?

                // Make newlines work with "custom" BBCode
                new BBTag("br", "", "<br><br>"),

                new BBTag("b", "<strong>", "</strong><br>"),
                new BBTag("i", "<em>", "</em>"),
                new BBTag("u", "<span style=\"text-decoration: line-underline\">", "</span>"),//doesn't seem to work, I guess maybe not supported by the built-in browser
                new BBTag("s", "<span style=\"text-decoration: line-through\">", "</span>"),

                new BBTag("list", "<ul>", "</ul>") { SuppressFirstNewlineAfter = true },//the true/false new line suppression doesn't seem to have any effect for my specific formatting
                new BBTag("li", "<li>", "</li>", true, false),
                new BBTag("*", "<li>", "</li>", true, false),

                new BBTag("url", "<a href=\"${url}\">", "</a>",
                    new BBAttribute("url", "")),

                new BBTag("code", "<pre class=\"prettyprint\">", "</pre>")
                {
                    StopProcessing = true,
                    SuppressFirstNewlineAfter = true
                },

                new BBTag("hr", "<hr>", "</hr>"),
            };
            var parser = new BBCodeParser(bbTags);

            text = parser.ToHtml(text);

            return text;
        }

        private static string ProcessBBCodeFromTxtFilePatchnotes(string txtfile)
        {
            string text = File.ReadAllText(txtfile);

            text = Regex.Replace(text, @"- ", @"[*] ");
            text = Regex.Replace(text, @"--> ", @"[*] --> ");

            var bbTags = new List<BBTag>()
            {
                /*new BBTag("h1", "<h1>", "</h1>"),
                new BBTag("h2", "<h2>", "</h2>"),
                new BBTag("h3", "<h3>", "</h3>"),
                new BBTag("h4", "<h4>", "</h4>"),*/

                // headers are too big with defaults in flowdocument since it doesn't accept the 90% text scaling
                new BBTag("h1", "<h2>", "</h2>"),
                new BBTag("h2", "<h3>", "</h3>"),
                new BBTag("h3", "<h4>", "</h4>"),
                new BBTag("h4", "<h5>", "</h5>"),//does h5 exist?

                new BBTag("b", "<strong>", "</strong>"),
                new BBTag("i", "<em>", "</em>"),
                new BBTag("u", "<span style=\"text-decoration: line-underline\">", "</span>"),//doesn't seem to work, I guess maybe not supported by the built-in browser
                new BBTag("s", "<span style=\"text-decoration: line-through\">", "</span>"),

                new BBTag("list", "<ul>", "</ul>") { SuppressFirstNewlineAfter = true },//the true/false new line suppression doesn't seem to have any effect for my specific formatting
                new BBTag("li", "<li>", "</li>", true, false),
                new BBTag("*", "<li>", "</li>", true, false),

                new BBTag("url", "<a href=\"${url}\">", "</a>",
                    new BBAttribute("url", "")),

                new BBTag("code", "<pre class=\"prettyprint\">", "</pre>")
                {
                    StopProcessing = true,
                    SuppressFirstNewlineAfter = true
                },

                new BBTag("hr", "<hr>", "</hr>"),
            };
            var parser = new BBCodeParser(bbTags);

            text = parser.ToHtml(text);

            return text;
        }
    }
}
