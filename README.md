BunnySplit
==========

An AutoSplit component which works with Bunnymod XT (BXT). Written in C# 6 / .NET Framework 4.5.

#Download
- [Here](http://play.sourceruns.org/BunnySplit/LiveSplit.BunnySplit.dll)
- From supported games in LiveSplit (soon).

#Usage
- You will need to run the game with [Bunnymod XT](https://github.com/YaLTeR/BunnymodXT).
- BunnySplit reads the game time from the BXT timer (which you can control with `bxt_timer_start`, `bxt_timer_stop`, `bxt_timer_reset`).
- AutoReset and AutoStart resets and starts the LiveSplit timer on `bxt_timer_reset` and `bxt_timer_start`, respectively.
- AutoSplit on game end supports Half-Life, Opposing Force, Blue Shift and Gunman Chronicles.
- AutoSplit on chapters or maps splits when you enter some chapter or map for the first time.

#Building
Use Visual Studio 2015 with the latest LiveSplit.