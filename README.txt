This build rolls back toward the earlier look and addresses the issues you called out.

Changes:
- no flicker layer
- animated number lines are back, but only the number region is invalidated
- bottom-left and bottom-right stray orange corner lines removed
- button layout clamps earlier and minimum size is larger so the start buttons stop getting cut off
- window/tray icon forcing is retained from the stronger icon-fix path
- hide-to-tray still avoids minimizing the game

v24 changes:
- intercepts minimize before the window actually minimizes, to stop BO minimizing with it
- removes AppUserModelID and goes back to simpler icon handling closer to the earlier working icon behavior
- dark backing under the animated number region to reduce white-ish redraw artifacts
- top-right corner accent realigned and bottom stray orange corners remain removed
- larger minimum size and safer button positioning to avoid start-button clipping


v25 changes:
- minimize-to-tray is now immediate with no delayed hide/glitching
- number animation repaint is restricted to the text itself and no longer redraws the dark backing rectangle that could appear as a flashing white-ish box


v26 snappy polish:
- window now appears first, then hostmode starts a moment later so startup feels faster
- tray hide is immediate after launch
- number animation updates less often to reduce UI churn
- close path is trimmed down for faster shutdown
- restore from tray does less extra work so it feels lighter


v27 changes:
- minimize-to-tray path trimmed down further for instant hide
- top-right orange corner realigned slightly
- number redraw region tightened and shadow removed to reduce white flash artifacts
- faint orange coded texture added behind the three main start buttons


v28 changes:
- instant minimize-to-tray via Visible=false path
- number animation sped up again
- button background code texture removed
- number repaint clipped and backed with dark fill to suppress white flash artifacts
- top-right corner accent realigned
- minimum size increased and button layout moved up more to avoid clipping


v29 changes:
- minimize is intercepted directly for instant tray hide
- removed the number-region transition block so numbers now just swap with no visible flash box
- restored faint coded texture behind the three main buttons, tied directly to button bounds so it resizes cleanly


v31 cleanup:
- animated number cluster is isolated in its own double-buffered control so it does not flash the whole launcher
- button textures are painted inside each custom launch button, so they always stay aligned and the dedicated one stretches correctly
- minimize-to-tray is intercepted directly and hides immediately
- top-right corner uses the exact same margin/length math as top-left


v32 polish:
- minimize-to-tray now hides the Win32 window directly and hides the inner card first to prevent white textbox fade
- removed the top-right orange corner entirely
- number ticker background simplified to remove the weird bottom tile
- button number texture now tiles across the full width so resized buttons do not leave blank space
- minimum size increased and button area moved up further to prevent cutoff


v33 polish:
- rebuilt the top-right number ticker to use fewer rows, tighter spacing, and a fixed pixel font so lines stop overlapping
- removed the extra bottom artifact by shrinking and repositioning the ticker box
- made the ticker cycle faster
- extended the button texture tiling so it fills the whole button width more cleanly


v34 fix:
- keeps tray mode active when the initial launched process exits quickly and reattaches to a live game process with the same executable name
- prevents the launcher from instantly reappearing after launch when BO spins up through a short-lived bootstrap process


v35 changes:
- added -sp as an alias for -zm
- launch options text updated to the exact wording requested
- added a short launch guard so campaign/zombies cannot be triggered twice during crash-recovery safe mode popup scenarios
