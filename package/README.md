# DvergrForHire

![Hired Dvergr fighting a Seeker inside a palisade pen](https://raw.githubusercontent.com/algo7/DvergrForHire/main/images/header.jpg)

Got a pile of coins and nothing to spend it on? Hire some muscle. Pay a peaceful Dvergr and it follows you as your
mercenary: it fights for you, never turns on you, and leaves your base standing. Or build a **hiring post** at home and
pick exactly the Dvergr you want, Deep North included. Nothing to install on the server.

> **⚠️ It's highly recommended that everyone on the server has this mod, same version.**
>
> In multiplayer, one player's game controls each creature. While a friend's game *without* the mod controls your hired
> Dvergr (nobody with the mod nearby, or for 30 seconds after you enter an area that friend loaded first), they **stay
> hired and friendly**, but:
>
> - their shots and spells **can damage your buildings**,
> - they don't follow you, and E does nothing,
> - if that friend hits one, the wild Dvergr nearby (if any) get angry.
>
> Friends without the mod can always hurt your hired Dvergr.

## Quick Start

1. Install with a mod manager (r2modman, Thunderstore Mod Manager, Gale).
2. Look at a Dvergr you haven't angered: it tells you its price.
3. Press **E** with enough coins. It's yours and **follows you right away**.

![A wild Dvergr: "[E] Hire: 200 coins"; the same Dvergr after paying: "Dvergr Rogue ( Hired )", [E] Follow / Stay](https://raw.githubusercontent.com/algo7/DvergrForHire/main/images/hire.jpg)

## Prices

| Dvergr | Price | ★ | ★★ |
|---|---|---|---|
| Mistlands rogue or mage | 100 coins | 150 | 225 |
| Ashlands | 200 coins | 300 | 450 |
| Deep North (in the Mørkhalla dungeon) | 100 coins | 150 | 225 |

Each star costs 50% more. You pay once: no wages, no food, and they heal on their own.

Ashlands and Deep North Dvergr look the same in the game, but the Deep North one has better stats.

Deep North Dvergr only live in the Mørkhalla dungeon and won't follow you out, which is why they're cheap. For one that
follows you around the world, build a Deep North hiring post.

## Hiring Posts

Rather hire at home? Build a **hiring post** (hammer, Defence section, next to a black forge): a Dvergr lantern pole with
a recruiter by it. It costs the pole's usual materials plus the extras below.

![The hammer's Defence section with the six hiring posts, and what the rogue post costs](https://raw.githubusercontent.com/algo7/DvergrForHire/main/images/build-menu.jpg)

| Post | Hire price | ★ | ★★ | Extra materials |
|---|---|---|---|---|
| Rogue, fire mage, ice mage, support mage | 120 coins | 180 | 270 | 10 yggdrasil wood, 5 black marble |
| Ashlands | 240 coins | 360 | 540 | 10 ashwood, 5 grausten |
| Deep North | 480 coins | 720 | 1080 | 10 frostwood, 5 norn thread |

- **E on the post** picks the stars for your next hire.
- **E on the recruiter** hires: the new Dvergr appears next to you and follows you. Hire as many as you can pay for.
- The recruiter guards its post. **If it dies, the post breaks** and drops the pole's materials. Remove a post with the
  hammer and its recruiter disappears with it.
- Friends without the mod see a lantern pole with a Dvergr next to it.

![Picking two stars on a hiring post, and the two-star rogue it hires next to the recruiter](https://raw.githubusercontent.com/algo7/DvergrForHire/main/images/stars.jpg)

### Pick Your Dvergr

There's a post for every kind of Dvergr, so you choose exactly who you hire. Build them side by side and mix your crew.

![The six recruiters: rogue, fire mage, ice mage and support mage (120 coins), Ashlands (240) and Deep North (480)](https://raw.githubusercontent.com/algo7/DvergrForHire/main/images/recruiters.jpg)

## Features

- **Every Dvergr is for hire** (except the ones you've angered): crossbow rogues, fire, frost and support mages, and the
  tough Ashlands and Deep North Dvergr.
- **Follow, stay and rename** like tamed animals.
- **They fight for you.** Support mages heal and buff you.
- **They never attack players or damage your buildings and ships.**
- **Your weapons don't hurt them** (only PvP and the butcher knife do, like with tamed animals).

![Hired Dvergr fighting a two-star Seeker Soldier at dusk](https://raw.githubusercontent.com/algo7/DvergrForHire/main/images/combat.jpg)

## How Things Behave

- **A new hire follows you right away**, so hiring one while you flee means it flees with you. Press **E** to make it stay.
- **Angry Dvergr won't work for you**, and they stay angry until they die, like in vanilla.
- **Hiring a Dvergr leaves its camp spot empty** while your hire is alive.
- **They get along with peaceful Dvergr** and fight angry ones.
- **Like tamed animals, they don't follow you through portals or dungeon doors.**

## Why It Works This Way

- **Cheap enough to lose.** A hire costs about what a dozen Dvergr drop. Tougher Dvergr and stars cost more.
- **Posts cost 20% more than camps:** you pay for not walking to a camp.
- **Dvergr from a post drop no loot**, so posts can't turn coins into gemstones or trophies. Camp hires keep their loot.
- **The post is the game's own lantern pole:** friends without the mod can't see custom models, so this way everyone
  sees the same post. Build any hut you like around it.
- **No settings file:** everyone with the mod sees the same prices.

## Multiplayer

Your mercenaries are the game's own Dvergr, so friends without the mod still see them (green health bar, names). When a
friend's game without the mod controls your hired Dvergr, your game takes control of the ones near you so they follow
you again. The first time, this takes up to 30 seconds; until then, see the warning at the top.

## Compatibility

- Built and tested for Valheim 1.0 (Deep North), on Linux; no OS-specific code.
- If another mod already makes Dvergr tameable, this one steps aside and says so in `BepInEx/LogOutput.log`.
- Mods that change tamed animals' hover text or E might clash. Please open an issue if Dvergr act strangely.
- The mod's few texts are in English.

## Uninstalling

Hired Dvergr stay in your world, friendly and named, but stop following, so tell them to **stay** at home first. Any
weapon can hurt them again, and hitting one angers the wild Dvergr nearby (if any). The hiring posts disappear from
the building menu; posts you already built stay as lantern poles with a friendly Dvergr next to them. Reinstalling
picks up where you left off.

## Links

- Source and bug reports: https://github.com/algo7/DvergrForHire (issues welcome)
- Changes: the Changelog tab
- Made with AI assistance.
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and HarmonyX. MIT license.
