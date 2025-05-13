# Game Design Document: Underflow

**Date: May 5, 2025**
**Author: James Fu**

## 1. Overview

### 1.1 Game Title

Underflow

### 1.2 Tagline

Fight to the last byte.

### 1.3 Game Summary

Underflow is a single-player, cyberpunk rogue-like deck-builder where survival hinges on managing the player's deck, which doubles as their life force. Set within a corrupted digital Grid, the player, a Runner, must navigate hostile systems and dismantle security Processes (AI, ICE, Daemons). However, the hostile environment has scrambled the player's code! However, they have just enough time and computing power to reorder the top few frames of your program's stack just before the next execution cycle hits.

The core gameplay revolves around the "Execution Window": the top cards of the player's deck are visible and can be reordered. On turn end, the game automatically executes affordable cards from the top of the deck based on available "Energy". Running out of cards triggers a fatal "Underflow" error, which causes the player to lose the run. Players win encounters by depleting the opponent's "Execution Stack" (their action queue/deck) using offensive cards ("Deleting" enemy code), while defending by injecting disruptive cards into the enemy's stack. Strategic sequencing, resource management, and taking calculated risks are key to survival.

**Please note that since the narrative and aesthetic components of the game is almost entirely secondary to the core gameplay / puzzle, the above material, as well as any material relating to it, is very much subject to change, depending on the availability of suitable assets.** However, this is not currently a point of concern as is it easy to mould a given set a assets to the gameplay.

### 1.4 Genre & Platform

- **Primary Genre:** Rogue-like Deckbuilder
- **Secondary Genres:** Strategy, Puzzle (Sequencing)
- **Target Platform:** PC (Windows, Mac) - Designed to be playable on lower-end machines.
- **Engine:** Unity (URP)

### 1.5 Target Audience

Players aged 16+ who enjoy strategic card games (*Slay the Spire*, *Balatro*), rogue-likes with high stakes and permanent consequences, puzzle-like optimization challenges (*Into the Breach*), and gritty cyberpunk aesthetics (*System Shock*, *Blade Runner*).

### 1.6 Tone & Theme

- **Tone:** Cyberpunk grit, high-tech tension, tactical planning under pressure, oppressive digital atmosphere, moments of clever execution followed by resource scarcity dread.
- **Core Theme:** "Code is Life / Deletion is Death". This is reinforced through:
  - The deck representing player health/integrity.
  - The "Underflow" loss condition.
  - Deleting cards from the opponent's stack as the primary win condition.
  - Injection of cards disrupting the execution of stacks.

## 2. Gameplay Mechanics

### 2.1 Core Gameplay Loop

A single combat encounter follows this loop:

1. **Player Turn**
    - Player observes their Execution Window and the Opponent's next potential actions.
    - Player reorders cards within the Execution Window.
2. **End Player Turn / Automatic Execution:**
    - Player ends their turn.
    - The game iterates through the player's deck from the top (index 0).
    - For each card:
        - Check if current Energy is sufficient for the card's Energy cost.
        - If YES: Deduct cost, execute card effect.
        - If NO: Stop execution for the turn.
3. **Enemy Turn:** Enemy executes actions from its stack based on available Energy and card costs.
4. **Game End:** Repeat steps 1 - 3 until either the player or the enemy runs out of cards. If the player wins, they gain some rewards (detailed in 2.4) and move on to the next challenge. If the player loses, it's game over and they have to restart from the very beginning.

### 2.2 Core Mechanics

- **Execution Window:**
  - A fixed number (`executionWindowSize`, e.g., 4) of cards from the top of the player's deck are constantly visible and represent the potential actions for the turn.
  - This replaces the traditional hand.
  - **Interaction:** The player's primary interaction is reordering these cards via 3D drag-and-drop to optimize the execution sequence.
- **Energy:**
  - The primary resource limiting actions per turn.
  - Player gains a base amount each turn (`playerEnergyPerTurn`).
  - Cards have an `energyCost`. Execution stops when the next card's cost exceeds available Energy.
- **Deck as Life / Underflow:**
  - The combined cards in the player's draw and discard piles represent remaining system integrity/life.
  - If the player attempts to draw (via reshuffle during Delete) or is forced to Delete when both piles are empty, the game ends immediately (Underflow).
- **Deleting (Offense):**
  - Player attack cards primarily function by deleting cards from the top of the opponent's deck.

### 2.3. Card Design Philosophy

- **Target Pool:** Aim for approximately 40 unique player cards.
- **Core Effects:** It is important to note that the potential design space involving the existing core mechanics is already immense. Below are just examples of what is possible
  - **Deletion:** Delete cards from the stack
  - **Disruption:** Adding cards that otherwise disrupt the execution of a stack
  - **Utility:** Retrieve from discard, gain temporary energy / benefits
  - **Setup/Combo:** Cards that bolster the effects of other cards
  - **Transformation:** Cards that change other cards
  - **Duplication:** Cards that copy the effect of other cards
  - **On Delete Effects:** Cards with effects upon being deleted
- **Keywords / Symbols:** A UI goal is to introduce keywords or symbols for common effects so that there is less text on the screen

### 2.4. Progression Systems

- **Card Rewards:** Choose 1 set out of a few sets of cards post-combat (added to player's deck). Card's have different rarities that effect their chances of being present in the pool.
- **Artifacts:** Passive, permanent buffs from Elites, Shops, Events. (e.g. Execution Window Size +1, Enemy +1)
- **Consumables:** Cards that can be added to the player's queue at any time. When used, they produce a powerful one-time effect that is meant to help the player get though a tough challenge. They are gone forever after being used.
- **Node Map:** Rogue-like map navigation through Grid sectors.
- **Shops/Data Caches:** Spend currency ("Data Fragments" or something similar) to buy Cards, Artifacts, Consumables, or remove cards.
- **Events/Corrupted Nodes:** (Stretch Goal) Text-based choices with risk/reward outcomes. Might involve card transformations or gaining negative cards.
- **Meta-Progression:** (Stretch Goal) Unlock starting cards, artifacts, character variations across runs.

## 3. User Interface (UI) & User Experience (UX)

### 3.1. Target Look and Feel

- **Aesthetic:** Dark, gritty cyberpunk. Neon highlights. Glitch effects. UI resembles futuristic debuggers/process managers.
- **Core Interaction:** Intuitive 3D drag-and-drop for reordering Execution Window cards. Clear visual feedback. Ending Turn (starting execution) and .
- **Readability:** High-contrast text, clear icons for game state info. Tooltips essential.
- **Game Feel:** Impactful execution (VFX/SFX), fluid reordering, clear disruption feedback.

### 3.2. Key UI Elements

- **3D Execution Window:** Primary focus. Displays 3D cards (Title, Energy Cost, some Description of Effects). Allows drag-and-drop.
- **Player Status:** Displays current Energy, Stack Count, Discard count.
- **Enemy Status:** Displays Enemy Stack count, Enemy Energy, Clear indication of enemy's *next* intended action(s).
- **Tooltip System:** On hover over 3D cards, show full description, keywords / symbols explained.
- **Text Console:** (Toggleable) Detailed logs. Primarily for debug/cheat mode in final version.
- **End Turn Button:** Clear button to initiate execution.
- **Menus:** Start (Start, Credits, Quit), Pause (Return, Main Menu, Quit, Settings), Settings (Volume, Resolution), Display Decks (Full Deck, Remaining Cards during battle (out of order), Discard Piles).
- **Progression:** Map screen, Card Reward selection, Shop interface, Artifact / Consumables display.

## 4. Art & Audio

### 4.1. Art Direction

- **Style:** Stylized 3D, clean info display, cyberpunk grit. Abstract digital avatars. Distinct card visuals.
- **Visual References:** *Inscryption* (Act 3), * *Blade Runner*, *Ghost in the Shell*, *System Shock*, *Cyberpunk 2077*.
- **Color Palette:** Dark base, neon accents (Player: cyan/green, Enemy: magenta/red). High contrast.
- **Effects:** Glitch shaders, particles.

### 4.2. Audio Direction

- **Music:** Dynamic synthwave, dark ambient electronic, glitch-hop. Tension ramps up with low deck count. Unique tracks for different nodes.
- **Sound Design:** Crucial feedback: Drag/drop/snap, Energy gain/spend, deletion (player vs enemy), disruption effects, win/loss stings. Digital, synthetic, glitchy sounds.

## 5. Level & Encounter Design

- **Structure:** Node-based map. Target playtime: ~20-30 mins/run.
- **Node Types:** Standard Combat, Elite Combat, Shop/Data Cache, Event/Corrupted Node, Sector Boss.
- **Encounter Design:** Focus on variety using 5-6 distinct enemy archetypes:
  - Aggro Deletion
  - Disruption focused
  - Setup / Burst Attacker
  - Defensive / Repair focused
  - Mixes / Elite variations
- **Difficulty Scaling:** Increase enemy stack size, Energy, card costs, effect potency in later sectors. Introduce tougher enemy types. Bosses have unique mechanics.

## 6. Production Plan (Post-POC#2)

1. **Sprint 3 (Proto#1):**
    - Implement functional 3D drag-and-drop reordering.
    - Implement remaining UI to make the game functional without having to interact with text elements
    - Basic animation of card movements / effects
    - Implement remaining core effects (transformation, duplication, on delete effects)
    - Implement a property of a card such that its position cannot be changed (for disruption-type effects)
    - Add at least one player card for each of those effects.
    - Design a few battles with premade decks and begin balancing the card designs
2. **Sprint 4 (Proto#2):**
    - Implement basic Artifacts system.
    - Implement basic Consumables system
    - Implement basic End of Combat Reward system.
    - Implement currency
    - Implement menus for plays to view their remaining cards (out of order), their discard pile, and the opponent's discard pile.
    - Add more content (aim for around 20 unique cards, 5 artifacts, 5 consumables, ~50% of total content).
    - Refine UI feedback (drag/drop, execution). Basic SFX integration.
3. **Sprint 5 (Pre-Alpha):**
    - Implement Node Map structure and basic navigation.
    - Implement Shop node functionality.
    - Implement basic Event node system.
    - Add more content (cards, artifacts, 1-2 enemy types).
    - Basic Menus (Start, Pause, Win/Loss). Playable loop  (15 mins).
    - Basic saving / loading
    - Expand cheat mode functionality
4. **Sprint 6 (Alpha):**
    - Implement the final boss.
    - Implement an interactive tutorial level that explains the basic objectives / mechanics.
    - Expand content significantly (~75% target cards/artifacts/consumables).
    - Refine UI towards final look. Placeholder music. Full loop (25-30 mins). Incorporate feedback.
5. **Sprint 7 (Beta):**
    - Implement remaining content (target total ~40 cards, 10 artifacts, 10 consumables, 5-6 enemies, 1 final boss). Full loop (~30+ mins).
    - Integrate final/near-final art/audio. Heavy balancing / testing. Finalize menus/credits.
6. **Sprint 8 (Gold Master):**
    - Bug fixing, polish, final balancing based on Beta feedback. Optimization. Final builds/marketing materials.

## 7. Backlog Links

- [Product Backlog](https://docs.google.com/spreadsheets/d/14uFZ29ERYC4IcAdrFOzYVS3qwQkC9e0gpKk8yRjiKuQ/edit?usp=sharing)
- [Sprint Backlogs](https://docs.google.com/spreadsheets/d/108kpZEfzmqhiw2oPdZHrZqlA2tKmVVScg21tdIpOmlo/edit?usp=sharing)
