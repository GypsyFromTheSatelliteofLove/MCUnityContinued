**MC UNITY CONTINUED**

A continuation of IronArthur's MCUnity https://github.com/IronArthur/MCUnity

This is an attempt to get Mech Commander working in Unity based on the masterful work of Iron Arthur! He basically wrote most of what was needed to get all the art assets from Mech Commander and that code still lies at the heart of this project.

Quick Tutorial:

1. Download the files in the repo as a zip or git clone it.
2. Open the SpriteLoader scene
3. Find the GameFilesLoader gameobject and locate your MCG installation folder (where the MCG.exe is)

<img width="662" height="649" alt="image" src="https://github.com/user-attachments/assets/d36622cd-b389-4905-8c12-e2a704a9b781" />

   
5. Press Copy Files to make a local copy of the asset files
6. You can load maps with the map loader

<img width="1880" height="893" alt="image" src="https://github.com/user-attachments/assets/8e8e6a9c-eecd-4d54-a0d1-f6fd794c23ef" />

6. Press play and the mechsample can be moved with the arrow keys (in all its buggy buggy glory!)

What we can do now?

1. Load all assets from the game into Unity
2. Map terrain can be read from their respective FST and FIT files and terrain can be loaded as they appear in the game
3. Mechs can be loaded into Unity at least to view (theoretically possible for vehicles as well but the script isnt written yet)
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

OLD STUFF:
1. Open the scene: SpriteLoader_Old.scene
2. Make sure that the game files were copied into the local folder (if you already did so, no need to do again)
3. To create traditional sprite sheets, locate the MechSpriteLoader_OLD mono, press Cache Data for Mech button
4. Press Print Cached Mech - this will create sprite sheets in the Resources/Sprites/Mechs/<mechname> folder (in their own part folder)
5. Navigate to the sprite sheet, make sure to set to 4096 max size, alpha 8 format, and point no filter 
6. then click the sprite editor and just auto slice with the pivot at TOP LEFT... do this for all the sprite sheets (THESE WILL TAKE A LOT OF TIME: up to 10 min for each sheet?)
7. Locate the MechSample gameobject and click the Load Resources button on the MechAnimator_OLD mono
8. Press play and watch the jank filled monstrosity fill your eyeballs

SOME INFO:
1. The main thing the game could do to load the assets is to use a custom texture + indexing system which works (kind of) but is slow to load, and the sprite sheets that take a ton of setup but usually load quicker (dunno about runtime memory effeciency tho)
2. IF the loading problem can be solved (which might be because the indexing uses big arrays of vector2s), its probably better to generate meshes instead of sprite sheets?
3. The current version is still basically more OOP but a DOD approach should be possible... but the structs have to be streamlined for better memory effeciency
4. Author is aware the code is bad! Mostly hacked together during whatever free time was had :/
5. Author is also aware coding is not his strongest suite hehe so any words of advice will be appreciated
