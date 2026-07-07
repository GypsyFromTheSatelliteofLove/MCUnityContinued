HEYO From GypsyFromTheSatelliteOfLove

To start, let me say I am aware how terrible the code is right now :( it is barely working and is super inefficient (meaning terribly terribly slow). But I might not be able to work on this as much as I want to so I decided to upload whats here now.

I plan to implement a faster way to get the sprites and use them in game using IronArthurs methods but i just ran out of time. Will come back to this defo but I am also well aware that this may be beyond my abilities right now :) in order to really get a serious port going, this is going to need a ton of Data Oriented Optimizations that i just am not capable of executing yet :(

How to Use:
1. Click on the GameFilesLoader and Browse to your Mech Commander installation folder

2. Click on the Copy Files button to begin copying the asset and data files 

3. To load a mission, you have to click on the MapLoader and use the MapTilesLoader
- The Map Loader is supposed to be IronArthurs version of the loader but its not finished

4. Click on Setup Map List to get the list of maps from the data files

5. The buttons for loading maps will now be available BUT only the Select Map to Load from Pak works if we dont load the sprites

6. To load the sprites, we first click Check Valid Indices (this loads the data needed for printing the sprite sheets)

7. Then click Make Blank Sprite Sheets as it is recommended to print blanks before the actual sprites; go to the Resources/GroundTiles and in the Originals and Expansion folders there will be the blank sprites; i never finished automating this step so its recommended to set all the blank sheets to MaxSize 4096, Format Alpha8, and set to Point Filter

8. Then click Make Sprite Sheets and this will print the sheets unto the blanks in the proper format; it may take awhile as there are around 5000 tiles

9. You have to then go thru the process of creating the tile sheets by going thru the sprite editor per sheet and slicing the sheets; USE TOP as pivot on map tiles
- If you want to see sample palettes, you can press the print palette button

10. After all this, you can now print the sprites from the sheets using the Select Map to Load with Tiles 


11. To load a mech, similar process, click the SpriteLoader

12. Press the Create Directories button if you need to create the file structure; but the project so far comes with the directories set

13. Press the Create Blank Sprite Sheets, and then set the MaxSize 4096, Format Alpha8, and set to Point Filter

14. Press the Create Mech Sprite Files button and it will create a mech sheet for the selected mech (mech Index) (decided to do this 1 at a time for now since it can take a while, mechs have up to 26k images each)
- Create Mech Anim Files just creates the info for animations which is also created when sprites are 

15. Go thru the resources/mechs/<mechname>/<mechPart> to use the sprite editor to create the tile sheets by slicing and USING TOP LEFT pivot for sprites - these can take up to 10mins for big sheets with 8k sprites
- Print Mech Bmp is an individual printer for the last loaded file

15. Go to the AwesomeSample (which can load any mech, awesome is just the first on the list)

16. Select the mech name you want to load and press Load Resources which should automatically find the sprites and animation files based on the mech name

17. Press play and use the arrow keys to test the animation

Thats about it ... its very limited but its a step into maybe building this game in Unity for real