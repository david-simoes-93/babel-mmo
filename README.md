# Babel

Check out our Babel [playlist](https://www.youtube.com/playlist?list=PLDLzwYVrKJNHCbL-mJy7_uNHo7Txkg3-B)!

## What is Babel?

At the moment, Babel is a proof-of-concept: a cooperative dungeon for (ideally) 4 players. They must coordinate and defeat the four bosses. With time and funding, Babel would be a full fledged MMO, the first of its kind (no levels, no XP, unique classes).

There is no concept of XP or levels in Babel. There are different worlds, the floors of the Tower of Babel, and each world has some specific theme, and requirements for a player to ascend to the next floor. 
- The first world would be a simple tutorial world, cyber-retro style. The requirements to ascend would be completing the quests to get the core abilities of your character, some gear, and reaching top 10 of a 50-player arena match.
- The second world would be a Nordic world, full of ice, snow, and appropriate monsters. The players could explore several different villages and the wilderness. The requirements to ascent would be defeating the Dungeon of Valhalla (the currently implemented dungeon) and to have accumulated 500 PvP kills.
- The third world would be a Lovecraftian horror region. Zombies, vampires, and other monsters of the dark roam the lands, haunted manors, mysteryous lights, and the region is eternally surrounded by fog (toroidal map, walking into the fog would make a character appear on the other side of the map)
...

There could even be floors representing different times of the same place (like floor 10 you would meet some NPCs on a city, floor 21 you would find the city in ruins after some attack, half the NPCs dead, and the other asking for your help).

When players ascend to the next floor, a copy of their character is made that ascends, while another remains. For example, Daniela might want to return to Floor 1. The character she plays there is exactly the same as it was when she ascended. If she gets new gear or abilities, they won't be reflected on her character on floor 2. Carefully done requirements must be made to ensure that any abilities the characters would need on floor 2 are set as requirements on floor 1. Any gear or abilities obtained in floor 2 will also not be reflected on her floor 1 character. She essentially has 2 characters by that point. Players may wish to return to lower floors for completionism (achievements, etc), events (seasonal or special events on specific floors), or to help friends ascent. Gear and abilities is the only thing that distinguishes all the characters in a world, which on one hand prevents high-level players from killing low-level players, but might also lead to overgeared players doing the same (so designing proper combat and safe areas is clearly mandatory).

For server health, we would want to keep a decent amount of players per floor. Because most players accumulate on the top floors, we would probably have lower floors with a single server, and top floors with multiple ones. Players could choose their server per floor, and once the maximum floor changed (as more floors are added), servers would be merged. Bots could also be considered to fill events requiring a specific amount of people (like the 50-player arena on Floor 1). It is important to keep the amount of servers fluid: react quickly to player-base trends, split and merge as necessary.

What classes are there in Babel?
- The Fighter: third person camera, plays similarly to a JRPG, with simple controls but complex combos. The fighter acts like a melee brawler or warrior, having high health and dealing good melee single target damage. The fighter is also able to enter a defensive stance, dealing less damage, but also healing himself and taking reduced damage.
- The Sniper: first person camera, plays similarly to a FPS, with simple controls and high-skill ceiling. The sniper is a ranged attacker with long, medium, and close range options. The sniper might also heal himself or his allies with a medi-gun.
- The Strategist: third person camera, plays similarly to a MOBA, with simple controls but complex combos. The strategist is an AoE attacker with short and medium range directional abilities. He excels at controlling the battlefield (freezing enemies, shielding allies, creating physical barriers, pushing targets away).
- The Mage: third person camera, plays similarly to an MMO, with complex controls. The mage is a ranged attacker with long and medium range options. The mage targets his enemies/allies and casts spells (some helpful, others harmful) at them, often having low mobility.

![image](https://user-images.githubusercontent.com/9117323/202024348-81f82675-c7f2-401c-b6dc-f07463af6d60.png)

Each class gives a party (a group of players that have allied with one another) a different HUD benefit:
- The Fighter: ?
- The Sniper: can spot enemies, allowing them to be tracked through walls
- The Strategist: a minimap with allies and enemies spotted
Players in a party without a Strategist, for example, have no minimap.
- The Mage: health bars visible above everyone's head, allowing people to know the HP % of targets

How to keep class balance?
- Track win-rate statistics in PvP, DPS and HPS statistics in PvE
- Have specific talents for classes to improve them in PvP or PvE (like "Do more 15% damage to NPCs." and "Reduce damage taken from Fighters by 20%")
- Have a rotational advantage system (similar to how Pokemon has Fire-Water-Grass): Fighters are good against Snipers, bad against Mages, neutral against Strategists. Mages are good against Fighters, bad against Strategists, neutral against Snipers. Etc.

What bosses are currently implemented?
- Freyja: spawns Wildfire Mushrooms that explode on death (must be killed from afar); summons Valkyries that carry players into the abyss (must be killed before they drop their target); creates poison clouds on damage-taken beneath the players' feet (players must move out of these); attacks melee;
- Thor: has a thundercloud rotating around the map dealing tons of damage (players must avoid this); casts a Chain Lightning (players must be spread out); summons magnets that allow the Chain Lightning to bounce to others (must be killed quickly); does a Thunder Slam on damage electrifying the floor (players must jump when Thor lands); attacks melee;
- Loki: creates clones of himself on damage-taken, with less HP but dealing the same damage; creates a Cursed Wall that sweeps the map killing everone (must be avoided by taking the teleporters on the platform to cross the map); attacks ranged
- Odin: not yet implemented. The idea is that, at some point, Odin destroys the map and everyone falls into a small planet where they move around with 3d gravity. And other stuff worthy of final boss fight

![image](https://user-images.githubusercontent.com/9117323/202024925-92993e95-d544-4b64-a6f8-5239caa39adb.png)

## Building Babel

To deploy, first build the server and then the client (or run the client inside Unity). 

For the server, on Unity, go to `File` > `Build Settings` and build `InitialScene`, `MainMenu`, and `NorseMap` with the `Server Build` option enabled. Pick a folder to build it on and run `BabelUnity.exe` to start the server.

For the client, either build Babel without the `Server Build` option or just run things from within the Unity Editor.

You need a server running before running the client. You can run multiple clients on the same server. Download compiled binares [here](https://www.dropbox.com/s/w9c7ops67lojzrl/babel.zip).

## TODO

Long list of TODOs. Because this is only a Proof of Concept, I am not implementing everything I could. I have created a working implementation to show how the game could work. I have implemented 3 player classes, and 3 bosses. Previous issue tracker [here](https://babel-mmo.myjetbrains.com/youtrack/issues).

There are many things to add:
- Rewrite networking code. See [this](https://bitbucket.org/Unity-Technologies/networking/src) and the [basis](https://github.com/Unity-Technologies/FPSSample) of Babel code. When Babel was started, Unity had just scrapped its networking framework, and there was no other decent option.
- Finish log-in code, with accounts, secure connections, etc
- Document (diagrams!) the networking flows
- Currently we trust positional data from clients, but at the very least, we would require a filter to check for hacks. This filter could simply check validity randomly for 1 out of N packets, and a report system would make it more aggressive for reported players.
- Chat system
- Strategist class
- Odin boss. At half HP, Odin would destroy the map and characters would fall on a 3d World with gravity where they could move around in a mind-boggling final boss fight.
- Implement [health bars](https://github.com/fholm/unityassets), minimap, and other perks
- Implement gear
- Implement sound (sound FX and background music)
- Implement persistent databases, storing positional data, gear, and whatever else
- Rework animations. Legs and torso should be separate, such that movement doesn't affect punches or other anymations.
- Implement a friendly NPC system, who could be interacted with as a vendor or quest giver
- Implement more skills and talents for all classes
- Make things pretty (we're just adding the same laser over and over, and using colored circles for visual effects)
- Add CI: unit and integration tests
- Add containers (dockerfiles)

Bugs:
- Balance bosses for 4 players
- Many others, surely

## Want to contribute?

Consider joining our [Discord](https://discord.gg/TypvwNW) channel, or emailing me.

Install [Unity](https://store.unity.com/download), [Git](https://git-scm.com/download/win), and [Git LFS](https://git-lfs.github.com/). Use Git Bash for everything from now on.

Set up your account and name with

    git config --global user.email "bluemoon93"
	git config --global user.name "David"
    git clone https://gitlab.com/bluemoon93/babel
	
Open Unity and `Open` the folder `babel` you just created. `File` > `Open Scene` > `Scenes/Map_Arena.unity` should get you playing.

----

Always work within your own branch, not on master. Merges will be handled later. To check your branch

    git branch
	
If it says `master`, you either have not created and branch, or have not switched to it.

    git branch david-new-feature
	git checkout david-new-feature

When you add new files, decide whether they should be in Git or Git LFS (basically, anything graphical and large should go on LFS). At the moment, it's `Materials`, `Scenes`, and `Resources`. If your files should go on LFS,

    git lfs track Assets/Materials*
	
Regardless of whether your files go on LFS or not, you now add them and commit your changes

    git add Assets/Materials*
	git commit -m "Added Materials folder"
	
Check if all files have been added and everything is correct, and push your files into your branch in the repository

    git status
	git push origin david-new-feature
	
You can then check if everything is working, and ask for a merge onto the `master` branch.
