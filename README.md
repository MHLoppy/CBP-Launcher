# CBP Launcher
A launcher/installer for the Rise of Nations [Community Balance Patch](https://steamcommunity.com/sharedfiles/filedetails/?id=2287791153).

![CBP Launcher v0.5 classic plus](https://i.imgur.com/EurwlsN.png)
![CBP Launcher v0.5 spartan v1](https://i.imgur.com/LH8muOp.png)
![CBP Setup GUI v0.5](https://i.imgur.com/Scf5pH7.png)

*screenshots show v0.5*

### TODO
- Option to remove previous versions of CBP, instead of archiving (target: v0.6)
- Option to override file / registry paths (target v0.6-v0.7)
- Option to load archived versions of CBP (target v0.7-v0.9)
- Load / unload Workshop mod alongside local mod files (wishlist)

### License
- Code is licensed under the [Mozilla Public License Version 2.0 (MPL 2.0)](https://www.mozilla.org/en-US/MPL/2.0/).
- Some images and logos are -- or are based on -- assets owned by Microsoft and are not licensed for further re-use outside of RoN mods.

### Credits
- The basic structure and functionality of the launcher (although now only a small minority of the code) is ported from Tom Weiland's excellent [launcher tutorial](https://github.com/tom-weiland/csharp-game-launcher).
- [Huan Tran](https://github.com/dotnal) provided some guidance across a large swathe of my persistent coding questions.
- A slim few members of the RoN community actually bothered to spend a few minutes testing the launcher and providing feedback. A number of bugs were only fixed because they were willing to do so.
- The background image for the Spartan V1 skin is from [Tingey Injury Law Firm via Unsplash](https://unsplash.com/photos/yCdPU73kGSc).
- [Costura.Fody](https://github.com/Fody/Costura) is used to embed some dependencies into a single exe ([PostSharp.Community.Packer](https://github.com/postsharp/PostSharp.Community.Packer) can do the same thing).
- [PoLaKoSz's fork](https://github.com/PoLaKoSz/CodeKicker.BBCode) of [Pablissimo's fork](https://github.com/Pablissimo/CodeKicker.BBCode-Mod) of [CodeKicker's BBCode parser](https://web.archive.org/web/20210629143751/https://archive.codeplex.com/?p=bbcode) is used to parse BBCode patch notes into HTML.
- [Nathan Harrenstein's port](https://www.nuget.org/packages/HtmlToXamlConverter) of [Microsoft's XAML FlowDocument to HTML Conversion Prototype](https://web.archive.org/web/20160312013954/http://blogs.msdn.com/b/wpfsdk/archive/2006/05/25/xaml-flowdocument-to-html-conversion-prototype.aspx) is used to convert pre-parsed patch notes into a viewable flow document.
- [TgaLib](https://github.com/shns/TgaLib) is used to preview TGA textures in the Optional Changes window.
- [NLog](https://nlog-project.org/) is used for logging, and the fancy log viewer is a lightly modified version of [Sentinel.NLogViewer](https://github.com/dojo90/NLogViewer)
- [tips'n tricks' C# XML tutorials](https://www.youtube.com/channel/UCtkgMa4i4HBE_vZW7EwYYXQ/search?query=c%23%20xml) were invaluable as a starting point for reading and modifying XML files.
- Video tutorials by [ToskersCorner](https://www.youtube.com/c/ToskersCorner) and [BinaryBunny](https://www.youtube.com/c/BinaryBunny) helped with a few aspects of WPF development.
