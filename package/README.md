# DvergrForHire

Got a pile of coins and nothing to spend it on? Hire some muscle. Walk up to a peaceful Dvergr, pay, and it follows
you as your mercenary: it fights for you, never turns on you, and leaves your base standing. Client-side, and
everything other players see is vanilla: no new creatures, items or effects.

## Quick Start

1. Install with a mod manager (r2modman, Thunderstore Mod Manager, Gale): BepInEx comes along automatically.
2. Find a Dvergr you haven't angered and look at it: it tells you its price.
3. Press **E** with enough coins in your inventory. It's yours and follows you.

## Prices

| Dvergr | Price |
|---|---|
| Mistlands rogue or mage | 250 coins |
| Ashlands | 500 coins |
| Deep North (inside Mørkhalla) | 250 coins |

Starred Dvergr cost more: one star doubles the price, two stars triple it. You pay once: no wages, no food. A hired
Dvergr is yours until it dies.

## Features

- **Every Dvergr is for hire:** crossbow rogues, fire, frost and support mages, and the tough Ashlands and Deep North
  Dvergr.
- **Follow, stay and rename** just like vanilla tamed animals.
- **They fight for you:** anything hostile near you gets a bolt or a fireball. Support mages heal and buff you.
- **They never attack players, and their bolts and spells don't damage your buildings or ships.**
- **Your weapons don't hurt them**, like any tamed animal (only PvP and the butcher knife do).

## How Things Behave

- **Angry Dvergr won't work for you.** Hit one, or anger their camp, and those Dvergr stay angry until they die, like
  in vanilla. Their camp is off the market until new Dvergr show up.
- **Hiring a Dvergr away from its camp leaves its spot empty:** the camp gets no new Dvergr there while yours is alive.
- **Your hired Dvergr get along with peaceful Dvergr.** If a camp gets angry, they fight it.
- **Like any tamed animal, hired Dvergr don't follow you through dungeon doors or portals.** Deep North Dvergr only
  live inside Mørkhalla, so hire them to help you clear that dungeon.
- When a hired Dvergr dies, it drops its usual loot.

## Multiplayer

DvergrForHire uses the game's own Dvergr instead of adding new mod-only ones, so friends without the mod still see your
mercenaries (green health bar, their names), and uninstalling the mod keeps them.

Valheim lets one player's game control each creature at a time, usually whoever got there first. If that's a friend
without the mod, your game takes your hired Dvergr near you under its control, so following and staying work. The first
time, your game checks for up to half a minute that the friend really doesn't have the mod, so a Dvergr you hire while a
friend's game controls it may stand still for that long before it follows you.

Friends without the mod can hurt your hired Dvergr with any weapon, and if their game controls it at that moment, the
hit angers the Dvergr nearby. Until your game takes control, a hired Dvergr can also still damage buildings. Best:
everyone in your group has the mod, same version.

## Compatibility

- Built and tested for Valheim 1.0 (Deep North). Tested on Linux; there's no OS-specific code.
- **If another mod already made the Dvergr tameable when a world loads**, DvergrForHire leaves them alone and says why
  in `BepInEx/LogOutput.log`.
- Mods that change the hover text or E of tamed animals might clash. If Dvergr act strangely next to another mod, please
  open an issue.
- The few texts the mod adds (the price, "Hired", "Not enough coins") are in English; everything else is the game's own.

## Uninstalling

Nothing is stored in your characters. Hired Dvergr stay in your world, friendly and with their names, but they stop
following and wander around where they are, so tell them to **stay** at home first. Without the mod any weapon can hurt them
again, and hitting one angers the Dvergr nearby. Reinstalling picks up where you left off.

## Links

- Source and bug reports: https://github.com/algo7/DvergrForHire (issues welcome)
- Changes: the Changelog tab
- Made with AI assistance.
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and HarmonyX. MIT license.
