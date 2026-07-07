# MCUnityContinued
A continuation of IronArthur's MCUnity 
https://github.com/IronArthur/MCUnity

This is an attempt to get Mech Commander working in Unity based on the masterful work of Iron Arthur!
He basically wrote most of what was needed to get all the art assets from Mech Commander and that code still lies at the heart of this project.

What we can do now?
1. Load all assets from the game into Unity
2. Map terrain can be read from their respective FST and FIT files and terrain can be loaded as they appear in the game
3. Mechs and Vehicles can be loaded into Unity at least to view
4. A buggy animation system for mechs is in place (press play and use keyboard) but it is far from finished

What we hope to get done
1. A working port of Mech Commander in Unity
2. A working editor for the original game in Unity

Challenges
1. To get Mech Commander ported, we need to be able to recreate the systems in game
2. To get the editor working, we need to be able to decode the GMM file which was the last binary not decoded by IronArthur

Known Issues:
1. The map overlay tiles are misplaced. The positioning is not yet figured out.
2. The Mech animations will have problems when mirroring (among other things like wrong animation transitions etc)
3. Loading Mech Assets take too long! It is faster if we extract the sprite sheets as png files first, but the project starts to balloon in size!

Quick Tutorial:
Download the files in the repo as a zip and extract to an existing unity project.

First we load the game files using the GameFilesLoader mono
Pressing Copy MCG Files needs to be done so we can work with a local copy of the game files:

<img width="731" height="596" alt="image" src="https://github.com/user-attachments/assets/8baf4bcf-5904-4f40-982e-a3ae70dd21e0" />

The Map Loader mono will be able to load maps (with errors in overlay tiles!). 
<img width="1908" height="765" alt="image" src="https://github.com/user-attachments/assets/3bd33153-e7fc-4c1d-9b9c-78d36853a205" />

Press play and the game will load (slowly) whatever mech you put into the MechSample mono 
<img width="712" height="232" alt="image" src="https://github.com/user-attachments/assets/e831b844-eb74-404c-9d88-408925a12968" />

Press the arrow keys to make the mech move! Behold its buggy glory! 
<img width="1383" height="740" alt="image" src="https://github.com/user-attachments/assets/679e4d5e-d998-4e33-b39d-cca88951640c" />


