# CBP Launcher
A launcher/installer for the Rise of Nations [Community Balance/Bugfix Patch](https://steamcommunity.com/sharedfiles/filedetails/?id=2287791153).

- **CBP Setup GUI** is a small installer/updater (has separate language files to allow for translations). Unless mentioned otherwise, "CBP Setup" refers to CBP Setup GUI.
- **CBP Launcher** is the main program and contains the vast majority of overall logic. It's a launcher for CBP (patch/unpatch RoN:EE), an updater for CBP Setup GUI, and essentially provides its own mod manager via a [basic plugin system](https://github.com/MHLoppy/CBP-SDK), separate from RoN's mod manager.

Both are C# WPF apps using .NET Framework. As of v0.5, CBP Launcher haphazardly implements elements of MVVM architecture.

_Unused/Legacy (likely to be removed but spun out to their own project pages for posterity):_
- **CBP Setup** (note no GUI) is the predecessor to CBP Setup GUI and is a console application that ran without user interaction. Some anti-virus programs found this behavious deeply suspicious, so it was converted into CBP Setup GUI (and development after that point continued on CBP Setup GUI, not CBP Setup non-GUI).
- **CBP patriots** was briefly trialled as a replacement for RoN's own launcher (patriots.exe) that would just launch CBPS/CBPL as needed. This responsibility was instead integrated directly into CBP Setup GUI to avoid having to use three separate programs.

_The Rules.xml Editor plugin bundled with CBP as of Alpha 8 can be found [here](https://github.com/MHLoppy/CBP-RE-Plugin)._

_The Sound.xml Editor plugin bundled with CBP as of Alpha 8 can be found [here](https://github.com/MHLoppy/CBP-SE-Plugin)._

![CBP Launcher v0.5 classic plus](https://i.imgur.com/EurwlsN.png)
![CBP Launcher v0.5 spartan v1](https://i.imgur.com/LH8muOp.png)
![CBP Setup GUI v0.5](https://i.imgur.com/Scf5pH7.png)

*screenshots show v0.5*

### TODO
- Mirror repo (e.g. GitLab)
- Able to function even if source of update checks becomes unavailable (target v0.7 - clunky implementation present in 0.6.6.1)
- Integrate all first-time-run prompts into single window (target v0.7-v0.8)
- Improved offline functionality (target v0.8)
- Move CBPLauncher.exe location away from CBPSetupGUI.exe to reduce user error from misreading instructions (target v0.8-v0.9)
- Option to load archived versions of CBP (target v0.8-v0.9)
- Minimal translations of essential portions only of CBP Setup GUI and CBP Launcher (target v0.8-v0.9)
- Select loading of secondary CBP files individually, rather than as group (target v0.8-v0.9)
- Complete translations of all text in CBP Setup GUI and CBP Launcher (target: when the multi-lingual multi-national RoN community gets off its collectively lazy ass and contributes)
- Option to configure max number of logs in UI (wishlist)
- Load / unload Workshop mod alongside local mod files (wishlist)
- Hide hotfix versioning on primary version display (wishlist)

### License
- Code and documentation are licensed under the [European Union Public License v. 1.2 (EUPL-1.2) or later](https://joinup.ec.europa.eu/collection/eupl/eupl-text-eupl-12).
- Most images and logos are -- or are based on -- assets owned by Microsoft and are not covered under the aforementioned EUPL license. These images are not licensed for further re-use outside of RoN mods and similar RoN-related works.

### Credits
- The basic structure and functionality of the launcher (although now only a small minority of the code) is ported from Tom Weiland's excellent [launcher tutorial](https://github.com/tom-weiland/csharp-game-launcher).
- [Huan Tran](https://github.com/dotnal) provided some guidance across a large swathe of my persistent coding questions.
- A slim few members of the RoN community actually bothered to spend a few minutes testing the launcher and providing feedback. A number of bugs were only fixed because they were willing to do so.
- The background image for the Spartan V1 skin is from [Tingey Injury Law Firm via Unsplash](https://unsplash.com/photos/yCdPU73kGSc).
- [Costura.Fody](https://github.com/Fody/Costura) is used to embed some dependencies into a single exe ([PostSharp.Community.Packer](https://github.com/postsharp/PostSharp.Community.Packer) can do the same thing).
- [PoLaKoSz's fork](https://github.com/PoLaKoSz/CodeKicker.BBCode) of [Pablissimo's fork](https://github.com/Pablissimo/CodeKicker.BBCode-Mod) of [CodeKicker's BBCode parser](https://web.archive.org/web/20210629143751/https://archive.codeplex.com/?p=bbcode) is used to parse BBCode patch notes into HTML.
- [Nathan Harrenstein's port](https://www.nuget.org/packages/HtmlToXamlConverter) of [Microsoft's XAML FlowDocument to HTML Conversion Prototype](https://web.archive.org/web/20160312013954/http://blogs.msdn.com/b/wpfsdk/archive/2006/05/25/xaml-flowdocument-to-html-conversion-prototype.aspx) is used to convert pre-parsed patch notes into a viewable flow document.
- [TgaLib](https://github.com/shns/TgaLib) is used to preview TGA textures in the Optional Changes window.
- [NLog](https://nlog-project.org/) is used for logging, and the fancy log viewer is a lightly modified version of [Sentinel.NLogViewer](https://github.com/dojo90/NLogViewer).
- [tips'n tricks' C# XML tutorials](https://www.youtube.com/channel/UCtkgMa4i4HBE_vZW7EwYYXQ/search?query=c%23%20xml) were invaluable as a starting point for reading and modifying XML files.
- Video tutorials by [ToskersCorner](https://www.youtube.com/c/ToskersCorner) and [BinaryBunny](https://www.youtube.com/c/BinaryBunny) helped with a few aspects of WPF development.
- mjn33's [automated workaround utility](https://github.com/mjn33/ron-objmask-workaround) for the object masks bug isn't used by CBP Launcher, but was a key part of creating CBP itself.
