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
    public partial class ClassicPlusOldAnnouncements : UserControl
    {
        public ClassicPlusOldAnnouncements()
        {
            InitializeComponent();
        }

        FlowDocument oldAnnouncementsFlowDoc = new FlowDocument();
        readonly string fileName = "old_announcements.txt"; // TODO I absolutely hate how I've duplicated the entire component just to change one string

        private void OldAnnFlowDoc_Initialized(object sender, EventArgs e)
        {
            if (IsInDesignMode() == false)
            {
                try
                {
                    LoadFormattedOldAnnouncements();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading old announcements: " + ex);
                }
            }
            else
            {
                //designtime baybeee
                oldAnnouncementsFlowDoc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
                OldAnnouncementsFlowDocument.Document = oldAnnouncementsFlowDoc;
            }
        }

        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }

        void OldAnnouncements_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void LoadFormattedOldAnnouncements()
        {
            //first check if file exists
            string txtPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "CBP", fileName));
            if (File.Exists(txtPath))
            {
                try
                {
                    // manually adding the hyperlink (and associated text) at the top (because flowdocuments don't handle URLs by default, even though they do display as if they do)
                    // https://stackoverflow.com/questions/2288999/how-can-i-get-a-flowdocument-hyperlink-to-launch-browser-and-go-to-url-in-a-wpf
                    //Paragraph paragraph = new Paragraph();
                    //announcementsFlowDoc.Blocks.Add(paragraph);
                    //Run normaltext1 = new Run("These are only summaries with the most important details. Check the ");
                    //paragraph.Inlines.Add(normaltext1);
                    //Run linktext = new Run("Steam Workshop");
                    //Hyperlink workshoplink = new Hyperlink(linktext);
                    //workshoplink.NavigateUri = new Uri("https://steamcommunity.com/sharedfiles/filedetails/changelog/2287791153");
                    //workshoplink.RequestNavigate += new RequestNavigateEventHandler(Workshoplink_RequestNavigate);
                    //paragraph.Inlines.Add(workshoplink); //ensure to add linkname, not linktextname
                    //Run normaltext2 = new Run(" for links to the full patch notes.");
                    //paragraph.Inlines.Add(normaltext2);

                    // announcements - the main part of the flowdocument
                    string announcements = txtPath;//relies on CBP Launcher being in root folder (as expected)
                    string formattedAnnouncements = "<html><body style='background-color: #000000; font-family: sans-serif; color: #C8C8C8;'>" + ProcessBBCodeFromTxtFile(announcements) + "</body></html>";

                    string xaml = HtmlToXamlConverter.ConvertHtmlToXaml(formattedAnnouncements, false);
                    oldAnnouncementsFlowDoc.Blocks.Add((Section)XamlReader.Parse(xaml));

                    //because the document is larger than the pure HTML page was (in terms of visual space), the background needs to be set a bit differentl in order to cover the whole area:
                    oldAnnouncementsFlowDoc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#000000");
                    oldAnnouncementsFlowDoc.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#000000");
                    oldAnnouncementsFlowDoc.FontFamily = new FontFamily("Segoe UI");
                    oldAnnouncementsFlowDoc.FontSize = 15;
                    oldAnnouncementsFlowDoc.PagePadding = new Thickness(5, 5, 5, 5);
                    oldAnnouncementsFlowDoc.TextAlignment = TextAlignment.Left;

                    OldAnnouncementsFlowDocument.Document = oldAnnouncementsFlowDoc;
                }
                catch (Exception ex)
                {
                    // not sure if this catch will work, but it doesn't hurt to try
                    FlowDocument announcementsFlowDoc = new FlowDocument();

                    Paragraph myParagraph = new Paragraph();
                    myParagraph.Inlines.Add(new Run("Old announcements could not be loaded.\n\n" + ex));
                    oldAnnouncementsFlowDoc.Blocks.Add(myParagraph);
                    oldAnnouncementsFlowDoc.TextAlignment = TextAlignment.Left;

                    OldAnnouncementsFlowDocument.Document = oldAnnouncementsFlowDoc;
                }
            }
            else
            {
                Paragraph paragraph = new Paragraph();
                oldAnnouncementsFlowDoc.Blocks.Add(paragraph);
                // Run normaltext1 = new Run("Unable to load announcements file (maybe CBP isn't loaded).");
                Run normaltext1 = new Run("Unable to load old announcements file.");
                paragraph.Inlines.Add(normaltext1);

                oldAnnouncementsFlowDoc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#000000");
                oldAnnouncementsFlowDoc.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#EEEEEE");
                oldAnnouncementsFlowDoc.FontFamily = new FontFamily("Segoe UI");
                oldAnnouncementsFlowDoc.FontSize = 14;
                oldAnnouncementsFlowDoc.PagePadding = new Thickness(5, 5, 5, 5);
                oldAnnouncementsFlowDoc.TextAlignment = TextAlignment.Left;

                OldAnnouncementsFlowDocument.Document = oldAnnouncementsFlowDoc;
            }
        }

        private void Workshoplink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
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
