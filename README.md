# CBP Launcher
A launcher/installer for the Rise of Nations [Community Balance Patch](https://steamcommunity.com/sharedfiles/filedetails/?id=2287791153).

![CBP Launcher v0.1 demo](https://i.imgur.com/m8inuTy.png)
*screenshot shows v0.1*

This is still in development, and more features and broader error handling is expected to be added in future versions. Right now it's only expected to work if you *don't* have CBP installed already, but feel free to test it anyway!

Please let me know whether the launcher works / doesn't work for you - the bugs can't get fixed until someone notices them.

### TODO
- Use Steam Workshop files as primary* (target: v0.3)
- Load / unload Workshop mod alongside local mod files (target: v0.3)
- Work with existing CBP installs (target: v0.4)
- Remove / archive previous versions of CBP (target: v0.4)
- Enable / Disable optional changes (target: v0.3-v0.5)
- Launcher can self-update using Steam Workshop files (target v0.5)
- Optional changes addressed individually, not as group (target: v0.3-0.6)
- Improved offline capabilities** (target v0.4-v0.6)
- Less.. "everywhere" UI and manually provide / override install path(s) (target v0.4-v0.6)
- Backup then replace default launcher (target v0.5-v0.7)
- Option to override file / registry paths (target v0.4-v0.8)

\* Right now it still sources files externally even though it "knows" where the Workshop files are - I just prioritised the load/unload functionality because I considered it more important.  
\*\* The current version can function offline if you're not using CBP, but it can't use or install CBP yet without an active connection.

### Credits
- The basic structure and functionality of the launcher (although increasing a minority of the code) is ported from Tom Weiland's excellent [launcher tutorial](https://github.com/tom-weiland/csharp-game-launcher).
- The background image is from [Tingey Injury Law Firm via Unsplash](https://unsplash.com/photos/yCdPU73kGSc).
