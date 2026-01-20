# 🃏 Blackjack (Multiplayer + Card Counting)

**Engine:** Unity  
**Language:** C#  
**Platform:** PC  
**Multiplayer:** Yes (Steam Networking)  
**Status:** In active development  
**Steam Page:** Created (currently unpublished while refining gameplay)

---

## 📌 Project Overview

This project is a **fully playable Blackjack game built in Unity** that supports **multiplayer gameplay** and incorporates **real casino mechanics**, including **card counting (Hi–Lo system)**.  

The goal of this project was to design a realistic Blackjack experience while exploring **game state management, networking, probability, and system architecture** in a real-time multiplayer environment.

Players can join the same table, place wagers, play through full rounds, and experience how card distribution and counting influence decision-making over time.

---

## 🎮 Core Features

- Standard Blackjack rules (Hit / Stand)
- **Multiplayer support using Steam networking**
- **Hi–Lo card counting system**
  - Running count updated per card dealt
- Dealer AI with correct hit/stand logic
- Turn-based flow synced across players
- Centralized game manager controlling rounds and payouts
- Steam lobby integration (Steam page prepared but unpublished)

---

## 🧠 Technical Implementation

### Multiplayer Architecture
- Uses **Steamworks networking** for peer connectivity
- Player actions (hit, stand, wager) are synchronized across clients
- Server-authoritative logic ensures:
  - Consistent card order
  - Fair dealing
  - Correct round resolution
- Handles player join/leave events gracefully

### Game Systems
- **Deck & Card System**
  - Full 52-card deck with shuffle and draw logic
- **Card Counting**
  - Hi–Lo values assigned per card
  - Running count updated in real time
- **Round Management**
  - Betting phase → player turns → dealer turn → payout
- **State Control**
  - Prevents invalid actions outside turn windows

---

## 📂 Code Structure

- `GameManager`
  - Controls round flow, betting, dealing, and resolution
- `Deck / Card`
  - Card generation, shuffle, draw, and count updates
- `PlayerController`
  - Handles player actions, wagers, and hand logic
- `DealerController`
  - Dealer AI behavior and rules
- `MultiplayerManager`
  - Steam lobby setup and network synchronization

*(Scripts are included in the repository for review.)*

---

## 📚 Skills Learned

This project significantly strengthened my understanding of:

- **Multiplayer Game Development**
  - Networked state synchronization
  - Handling latency and shared game logic
- **C# Object-Oriented Design**
  - Modular, reusable systems for cards, players, and rounds
- **Game State Management**
  - Turn-based logic across multiple clients
- **Probability & Decision Systems**
  - Implementing and validating a real card counting model
- **Steamworks Integration**
  - Lobby creation, session management, and deployment preparation
- **Debugging Distributed Systems**
  - Tracking desync issues and edge cases in multiplayer scenarios

---

## 🚀 Future Improvements

If I continue developing this project, I plan to:

- Publish the **Steam store page**
- Polish Core mechanics:
  - Bot difficulty
  - Insurance
  - Visuals
- Improve UI/UX and animations
- Add player statistics:
  - Win rate
  - Count accuracy
  - Expected value over time
- Implement spectator mode
- Improve matchmaking and lobby filters

---

## 📫 Author

**Daron Baltazar**  
GitHub: https://github.com/Tigranasouras  

---

## ⚠️ Disclaimer

This project is for educational and entertainment purposes only and is **not intended for real-money gambling**.
