using CodeKicker.BBCode;
using HTMLConverter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;

namespace CBPLauncher.Skins
{
    public partial class SpartanV1OldAnnouncements : UserControl
    {
        public SpartanV1OldAnnouncements()
        {
            InitializeComponent();
        }

        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }

        readonly string fileName = "old_announcements.txt"; // TODO I absolutely hate how I've duplicated the entire component just to change one string

        void RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void OldAnnFlowDoc_Initialized(object sender, EventArgs e)
        {
            if (IsInDesignMode() == false)
            {
                // Announcements aren't visible at initial load, so no placeholder should be needed
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ev) =>
                {
                    try
                    {
                        // The doc (?) is fussy about being done on a background thread, so just do what we can in background
                        string oldAnnouncements = Path.Combine(Directory.GetCurrentDirectory(), "CBP", fileName);

                        if (File.Exists(oldAnnouncements))
                        {
                            string formattedOldAnnouncements = "<html><body style='background-color: #DFF3F3F3; font-family: sans-serif; color: #FF1A1A1A;'>" + ProcessBBCodeFromTxtFile(oldAnnouncements) + "</body></html>";
                            string xaml = HtmlToXamlConverter.ConvertHtmlToXaml(formattedOldAnnouncements, false);
                            ev.Result = xaml;
                        }
                        else
                        {
                            ev.Result = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        ev.Result = ex;
                    }
                };
                worker.RunWorkerCompleted += (s, ev) =>
                {
                    // All UI element creation on UI thread
                    try
                    {
                        if (ev.Result is Exception ex)
                        {
                            OldAnnouncementsFlowDocument.Document = CreateErrorDocument("Error loading old announcements: " + ex);
                        }
                        else if (ev.Result is string xaml)
                        {
                            OldAnnouncementsFlowDocument.Document = LoadFormattedOldAnnouncements(xaml);
                        }
                        else
                        {
                            OldAnnouncementsFlowDocument.Document = LoadFormattedOldAnnouncements(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        OldAnnouncementsFlowDocument.Document = CreateErrorDocument("Error building document: " + ex);
                    }
                };
                worker.RunWorkerAsync();
            }
            else
            {
                //designtime baybeee
                var doc = new FlowDocument();
                doc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
                OldAnnouncementsFlowDocument.Document = doc;
            }
        }

        private FlowDocument LoadFormattedOldAnnouncements(string xaml)
        {
            var doc = new FlowDocument();

            // add old announcements - the main part of the flowdocument
            if (xaml != null)
            {
                doc.Blocks.Add((Section)XamlReader.Parse(xaml));
            }
            else
            {
                Paragraph errorParagraph = new Paragraph();
                errorParagraph.Inlines.Add(new Run("Unable to load old announcements."));
                doc.Blocks.Add(errorParagraph);
            }

            doc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#EEF9F9F9");
            doc.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF1A1A1A");
            doc.FontFamily = (FontFamily)Application.Current.Resources["DefaultFont"];
            doc.FontSize = 15;
            doc.PagePadding = new Thickness(5, 5, 5, 5);
            doc.TextAlignment = TextAlignment.Left;

            return doc;
        }

        private FlowDocument CreateErrorDocument(string errorMessage)
        {
            var doc = new FlowDocument();
            Paragraph myParagraph = new Paragraph();
            myParagraph.Inlines.Add(new Run(errorMessage));
            doc.Blocks.Add(myParagraph);
            return doc;
        }

        private string ProcessBBCodeFromTxtFile(string txtfile)
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
    }
}
