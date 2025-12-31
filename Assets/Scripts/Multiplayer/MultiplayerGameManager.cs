using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Steamworks;


public class SeatRuntime
{
    public PlayerData player;
    public PlayerSeatUI ui;
    public ulong ownerSteamId; // 0 for bots/offline
}

public class MultiplayerGameManager : MonoBehaviour
{
    public CardManager cardManager;
    public TMPro.TextMeshProUGUI statusText;

    public List<PlayerSeatUI> seatUIs = new();

    // runtime state
    public List<PlayerData> players = new();
    public List<SeatRuntime> seats = new();   // <- single definition

    private int currentPlayerIndex = 0;
    private List<Card> dealerHand = new();
    private int dealerScore = 0;
    public List<TMPro.TextMeshProUGUI> playerCashTexts = new();
    private bool roundInProgress = false;

    private const int MIN_BET = 25;
    private bool HasMinBet(PlayerData p) => p.wager >= MIN_BET;
    private int GetIndex(PlayerData p) => players.IndexOf(p);

    public SharedSeatControls sharedControls;

    public TMPro.TextMeshProUGUI localCashBig;

    [Header("Networking")]
    [Tooltip("Auto-detected. Host = lobby owner. In solo/offline this will be true.")]
    public bool isHost = false;

    private BJSteamP2PBus bus;
    private CSteamID lobbyId = CSteamID.Nil;
    private ulong hostSteamId = 0UL;


    void Start()
    {
        // Build an empty table (SeatAssigner will populate ownership/humans vs. bots)
        BuildEmptyTable(seatUIs.Count);

        // Networking wiring (Steam P2P messages)
        bus = BJSteamP2PBus.Instance ?? new GameObject("BJSteamP2PBus").AddComponent<BJSteamP2PBus>();
        bus.OnJsonMessage += OnNetJson;

        // Detect host from lobby owner (fallback to solo)
        if (SteamManager.Initialized && LobbyBridge.Instance && LobbyBridge.Instance.HasLobby)
        {
            lobbyId = LobbyBridge.Instance.LobbyId;
            hostSteamId = SteamMatchmaking.GetLobbyOwner(lobbyId).m_SteamID;
            isHost = SteamUser.GetSteamID().m_SteamID == hostSteamId;
        }
        else
        {
            isHost = true;
        }

        statusText.text = isHost ? "Waiting for wagers..." : "Waiting for host...";
        UpdateCashUI();
    }

    private void OnDestroy()
    {
        if (bus != null) bus.OnJsonMessage -= OnNetJson;
    }

    public void BuildEmptyTable(int seatCount)
    {
        players.Clear();
        seats.Clear();

        for (int i = 0; i < seatCount; i++)
        {
            var p = new PlayerData($"Seat{i + 1}", true, 1_000_000); // default bot until SetSeatOwner
            players.Add(p);

            var s = new SeatRuntime
            {
                player = p,
                ui = seatUIs[i],
                ownerSteamId = 0UL
            };
            seats.Add(s);

            s.ui.Init(this, i, s.ownerSteamId, true);
            s.ui.UpdateMoneyUI(p.cash, p.wager);
        }
    }


    // Called by SeatAssigner when ownership is known
    public void SetSeatOwner(int index, ulong ownerId, string displayName, bool isBot)
    {
        var s = seats[index];
        s.ownerSteamId = ownerId;
        s.player.isBot = isBot;
        if (!string.IsNullOrEmpty(displayName))
            s.player.playerName = displayName;

        s.ui.Init(this, index, s.ownerSteamId, s.player.isBot);
        s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
    }

    public void BeginBettingPhase()
    {
        statusText.text = "Waiting for wagers...";
        //enable wagers for local seat only
        if (sharedControls)
            sharedControls.SetBettingEnabled(LocalSeatIndex >= 0);
    }

    private int LocalSeatIndex
    => seats.FindIndex(s => s.ownerSteamId == (SteamAPI.IsSteamRunning() ? SteamUser.GetSteamID().m_SteamID : 0)
                         && !s.player.isBot);

    private List<Card> ActiveHand(PlayerData p)
        => (p.hasSplit && p.playingSplit) ? p.splitHand : p.hand;

    private void SetTurnButtons(int activeSeat)
    {
        if (!sharedControls) return;
        sharedControls.SetTurnEnabled(activeSeat == LocalSeatIndex);
    }


    public void OnWager(PlayerData player, int amount)
    {
        if (!isHost) return; // only host mutates authoritative money/wagers
        if (amount <= 0) return;

        if (player.cash >= amount)
        {
            player.wager += amount;
            player.cash -= amount;

            statusText.text = (player.wager < MIN_BET)
                ? $"{player.playerName} wagered ${amount:N0} (min ${MIN_BET:N0} to play)"
                : $"{player.playerName} wagered ${amount:N0}";

            UpdateCashUI();
            BroadcastState();
        }
        else
        {
            statusText.text = $"{player.playerName} doesn't have enough cash!";
            BroadcastState();
        }
    }

    public void StartRound()
    {
        if (!isHost) return; // only host deals / advances the round
        if (roundInProgress) return;

        bool anyEligible = false;
        foreach (var p in players) if (HasMinBet(p)) { anyEligible = true; break; }
        if (!anyEligible) { statusText.text = $"Need at least one player to wager min: ${MIN_BET:N0}."; return; }

        cardManager.ClearPlayerAreas();
        cardManager.ClearDealerArea();

        foreach (var p in players)
        {
            p.hand.Clear();
            p.splitHand.Clear();
            p.hasSplit = false;
            p.playingSplit = false;
            p.splitWager = 0;
            p.isDone = false;

        }

        dealerHand.Clear(); dealerScore = 0;

        for (int i = 0; i < players.Count; i++)
        {
            if (HasMinBet(players[i]))
            {
                players[i].hand.Add(cardManager.DealCardToPlayer(i, true, false));
                players[i].hand.Add(cardManager.DealCardToPlayer(i, true, false));
            }
            else players[i].isDone = true;
        }

        dealerHand.Add(cardManager.DealCardToDealer(false));
        dealerHand.Add(cardManager.DealCardToDealer(true));

        roundInProgress = true;

        if (sharedControls) sharedControls.SetBettingEnabled(false); //lock wagers during the hand

        currentPlayerIndex = 0;
        while (currentPlayerIndex < players.Count &&
               (players[currentPlayerIndex].isDone || players[currentPlayerIndex].hand.Count == 0))
            currentPlayerIndex++;

        if (currentPlayerIndex >= players.Count)
        {
            statusText.text = $"No eligible players this round (min ${MIN_BET}).";
            roundInProgress = false; return;
        }

        statusText.text = $"{players[currentPlayerIndex].playerName}'s turn!";
        StartCoroutine(HandleTurn(players[currentPlayerIndex]));
        UpdateCashUI();
        UpdateTurnControls();

        BroadcastState();
    }

    private IEnumerator HandleTurn(PlayerData player)
    {
        yield return new WaitForSeconds(1f);

        if (player.hand.Count == 0) { NextPlayer(); yield break; }

        if (player.isBot)
        {
            int seatIndex = GetIndex(player);
            yield return StartCoroutine(BotTurn(player, seatIndex));
            NextPlayer();
        }
        else
        {
            statusText.text = $"{player.playerName}, choose an action!";
            UpdateTurnControls();
            BroadcastState();

        }
    }

    public void OnHit()
    {
        if (!roundInProgress) { StartRound(); return; }
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        var p = players[currentPlayerIndex];
        var c = cardManager.DealCardToPlayer(currentPlayerIndex, true, false);
        p.hand.Add(c);

        if (CalculateHandScore(p.hand) > 21)
        {
            p.isDone = true;
            statusText.text = $"{p.playerName} busted!";
            NextPlayer();
        }
    }

    public void OnStand()
    {
        if (!roundInProgress) return;
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        var player = players[currentPlayerIndex];
        player.isDone = true;
        statusText.text = $"{player.playerName} stands.";
        NextPlayer();
    }

    private bool CanSplit(PlayerData p)
    {
        //one split max
        if (p.hasSplit) return false;

        //two cards, same rank, and enough cash to match the wager
        if (p.hand.Count != 2) return false;

        bool sameRank = p.hand[0].value == p.hand[1].value;
        return sameRank && p.wager > 0 && p.cash >= p.wager; // >= (not >)
    }

    private void UpdateTurnControls()
    {
        if (!sharedControls) return;
        int local = LocalSeatIndex;
        bool myTurn = roundInProgress && currentPlayerIndex == local;
        sharedControls.SetTurnEnabled(myTurn);

        //toggle split visibility / interactability when its your turn
        bool showSplit = false;
        if (myTurn && local >= 0 && local < seats.Count)
            showSplit = CanSplit(seats[local].player);

        sharedControls.SetSplitVisible(showSplit);
        sharedControls.SetSplitInteractable(showSplit);   //clickable only if legal
    }

    private void NextPlayer()
    {
        if (!isHost) return;
        currentPlayerIndex++;

        while (currentPlayerIndex < players.Count &&
               (players[currentPlayerIndex].isDone || players[currentPlayerIndex].hand.Count == 0))
            currentPlayerIndex++;

        if (currentPlayerIndex >= players.Count)
        {
            if (roundInProgress && dealerHand.Count >= 1)
                StartCoroutine(DealerTurn());
            else
                Debug.LogWarning("Tried to start DealerTurn without a valid dealer hand.");
        }
        else
        {
            StartCoroutine(HandleTurn(players[currentPlayerIndex]));
        }

        BroadcastState();
    }


    public void OnNextHandButton()
    {
        if (roundInProgress) return; //Don't allow during a hand
        if (!isHost)
        {
            SendRequest("REQ_START", -1, 0);
            return;
        }

        StartRound(); // StartRound already enforces MIN_BET
    }

    private IEnumerator DealerTurn()
    {
        if (!isHost) yield break;
        if (dealerHand.Count == 0) { Debug.LogError("DealerTurn called with empty dealerHand."); yield break; }

        dealerHand[0].ShowBack(false);
        dealerScore = CalculateHandScore(dealerHand);
        BroadcastState();

        while (dealerScore < 17)
        {
            dealerHand.Add(cardManager.DealCardToDealer(true));
            dealerScore = CalculateHandScore(dealerHand);
            BroadcastState();
            yield return new WaitForSeconds(1f);
        }

        ResolveBets();
        UpdateCashUI();
        BroadcastState();

        roundInProgress = false;
        statusText.text = "Round over. Place wagers!";
        BeginBettingPhase(); //re-enable local betting UI
        BroadcastState();
    }

    private int PayoutFor(List<Card> hand, int wager, int dealerFinal)
    {
        if (hand.Count == 0) return 0;
        int score = CalculateHandScore(hand);
        if (score > 21) return 0;
        if (dealerFinal > 21 || score > dealerFinal) return wager * 2;
        if (score == dealerFinal) return wager;   // push
        return 0;
    }

    private void ResolveBets()
    {
        int dealerFinal = CalculateHandScore(dealerHand);

        foreach (var p in players)
        {
            // main hand
            p.cash += PayoutFor(p.hand, p.wager, dealerFinal);

            // split hand (if any)
            if (p.hasSplit)
                p.cash += PayoutFor(p.splitHand, p.splitWager, dealerFinal);

            // reset wagers and split state for next round
            p.wager = 0;
            p.splitWager = 0;
            p.hasSplit = false;
            p.playingSplit = false;
            p.splitHand.Clear();
        }
    }

    private int CalculateHandScore(List<Card> hand)
    {
        int total = 0, ace = 0;
        foreach (var c in hand) { total += c.realValue; if (c.value == 1) ace++; }
        while (total > 21 && ace > 0) { total -= 10; ace--; }
        return total;
    }

    private void UpdateLocalCashBig()
    {
        if (!localCashBig) return;
        int idx = LocalSeatIndex;
        localCashBig.text = (idx >= 0) ? $"{players[idx].cash:N0}" : "-";
    }


    private void UpdateCashUI()
    {
        for (int i = 0; i < players.Count && i < playerCashTexts.Count; i++)
            playerCashTexts[i].text = $"{players[i].playerName}: ${players[i].cash:N0}";

        UpdateLocalCashBig();
    }

    private IEnumerator BotTurn(PlayerData bot, int seatIndex)
    {
        statusText.text = $"{bot.playerName} thinking...";
        yield return new WaitForSeconds(1f);

        int dealerUp = (dealerHand.Count >= 2) ? dealerHand[1].realValue : 10;
        bool done = false;

        while (!done)
        {
            switch (GetBotDecision(bot.hand, dealerUp))
            {
                case BotAction.Hit:
                    bot.hand.Add(cardManager.DealCardToPlayer(seatIndex, true, false));
                    if (CalculateHandScore(bot.hand) > 21) { statusText.text = $"{bot.playerName} busted!"; done = true; }
                    break;

                case BotAction.Stand:
                    statusText.text = $"{bot.playerName} stands."; done = true;
                    break;

                case BotAction.Double:
                    if (bot.cash >= bot.wager)
                    {
                        bot.cash -= bot.wager; bot.wager *= 2;
                        bot.hand.Add(cardManager.DealCardToPlayer(seatIndex, true, false));
                        statusText.text = $"{bot.playerName} doubles.";
                    }
                    done = true;
                    break;

                case BotAction.Split:
                    done = true; // TODO later
                    break;
            }
            yield return new WaitForSeconds(1f);
        }
        bot.isDone = true;
    }

    private enum BotAction { Hit, Stand, Double, Split }
    private BotAction GetBotDecision(List<Card> hand, int dealerUpCardValue)
    {
        int score = CalculateHandScore(hand);
        bool isSoft = hand.Exists(c => c.realValue == 11);

        if (isSoft)
        {
            if (score >= 19) return BotAction.Stand;
            if (score == 18 && dealerUpCardValue >= 9) return BotAction.Hit;
            return BotAction.Stand;
        }
        if (score >= 17) return BotAction.Stand;
        if (score >= 13 && dealerUpCardValue <= 6) return BotAction.Stand;
        if (score == 12 && dealerUpCardValue is >= 4 and <= 6) return BotAction.Stand;
        if (score >= 10) return BotAction.Double;
        return BotAction.Hit;
    }

    // ---- Requests from UI (ownership/turn checks) ----

    private bool IsMySeat(int seatIndex, ulong caller)
        => seatIndex >= 0 && seatIndex < seats.Count && seats[seatIndex].ownerSteamId == caller;

    private bool IsCurrentTurn(int seatIndex) => seatIndex == currentPlayerIndex;

    public void RequestWager(int seatIndex, int amount, ulong callerSteamId)
    {
        // Clients forward requests to host. Host applies + broadcasts.
        if (!isHost)
        {
            SendRequest("REQ_WAGER", seatIndex, amount);
            return;
        }

        if (roundInProgress) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        if (amount == -1) amount = p.cash; // all-in
        OnWager(p, amount);
        seats[seatIndex].ui.UpdateMoneyUI(p.cash, p.wager);
        BroadcastState();
    }

    public void RequestHit(int seatIndex, ulong callerSteamId)
    {
        if (!isHost)
        {
            SendRequest("REQ_HIT", seatIndex, 0);
            return;
        }

        if (!roundInProgress) { StartRound(); return; }
        if (!IsCurrentTurn(seatIndex)) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        var hand = ActiveHand(p);

        var c = cardManager.DealCardToPlayer(seatIndex, true, p.hasSplit && p.playingSplit);
        hand.Add(c);

        if (CalculateHandScore(hand) > 21)
        {
            // Busts this hand only
            if (p.hasSplit && !p.playingSplit)
            {
                // main hand busted; play the split hand now
                p.playingSplit = true;
                statusText.text = $"{p.playerName} busted main hand - now playing split hand.";
                UpdateTurnControls();
                BroadcastState();
                return; // stay on same player
            }
            else
            {
                p.isDone = true;
                statusText.text = $"{p.playerName} busted!";
                NextPlayer();
                UpdateTurnControls();
                BroadcastState();
            }
        }
        else
        {
            BroadcastState();
        }
    }

    public void RequestStand(int seatIndex, ulong callerSteamId)
    {
        if (!isHost)
        {
            SendRequest("REQ_STAND", seatIndex, 0);
            return;
        }

        if (!roundInProgress) return;
        if (!IsCurrentTurn(seatIndex)) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;

        if (p.hasSplit && !p.playingSplit)
        {
            // Finished main hand; switch to split hand
            p.playingSplit = true;
            statusText.text = $"{p.playerName} stands (main). Now playing split hand.";
            UpdateTurnControls();
            BroadcastState();
            return; // stay on same player
        }

        // No split pending OR we were already on split hand
        p.isDone = true;
        statusText.text = $"{p.playerName} stands.";
        NextPlayer();
        UpdateTurnControls();
        BroadcastState();
    }

    public void RequestDouble(int seatIndex, ulong callerSteamId)
    {
        if (!isHost)
        {
            SendRequest("REQ_DOUBLE", seatIndex, 0);
            return;
        }

        if (!roundInProgress || !IsCurrentTurn(seatIndex) || !IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        bool onSplit = (p.hasSplit && p.playingSplit);
        var hand = ActiveHand(p);

        int wagerForThisHand = onSplit ? p.splitWager : p.wager;
        if (p.cash >= wagerForThisHand && wagerForThisHand > 0)
        {
            p.cash -= wagerForThisHand;
            if (onSplit) p.splitWager *= 2; else p.wager *= 2;

            var c = cardManager.DealCardToPlayer(seatIndex, true, onSplit);
            hand.Add(c);

            // Double = one card then stand
            if (p.hasSplit && !p.playingSplit)
            {
                p.playingSplit = true;
                statusText.text = $"{p.playerName} - play your split hand!";
                UpdateTurnControls();
                BroadcastState();
                return;
            }

            p.isDone = true;
            NextPlayer();
        }
        UpdateCashUI();
        UpdateTurnControls();
        BroadcastState();
    }

    public void RequestSplit(int seatIndex, ulong callerSteamId)
    {
        if (!isHost)
        {
            SendRequest("REQ_SPLIT", seatIndex, 0);
            return;
        }

        if (!roundInProgress || !IsCurrentTurn(seatIndex) || !IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        if (!CanSplit(p)) return;

        // Pay the second bet
        p.cash -= p.wager;
        p.splitWager = p.wager;

        //Move the existing card visually
        var moved = p.hand[1];
        p.hand.RemoveAt(1);
        p.splitHand.Clear();
        p.splitHand.Add(moved);
        cardManager.MoveCardBetweenHands(seatIndex, moved, toSplit: true);

        // Mark split state: we start by continuing the MAIN hand
        p.hasSplit = true;
        p.playingSplit = false;

        // Deal one card to each hand
        p.hand.Add(cardManager.DealCardToPlayer(seatIndex, true, false));  // main lane
        p.splitHand.Add(cardManager.DealCardToPlayer(seatIndex, true, true)); // split lane

        statusText.text = $"{p.playerName} splits.";
        seats[seatIndex].ui.UpdateMoneyUI(p.cash, p.wager);
        UpdateCashUI();

        // Split is no longer legal now
        UpdateTurnControls();
        BroadcastState();
    }

    // ---------------- Networking ----------------

    private void SendRequest(string type, int seatIndex, int amount)
    {
        if (!SteamManager.Initialized || !lobbyId.IsValid() || bus == null) return;

        var env = new BJNetEnvelope
        {
            type = type,
            senderSteamId = SteamUser.GetSteamID().m_SteamID,
            seatIndex = seatIndex,
            amount = amount,
            state = null
        };

        string json = JsonUtility.ToJson(env);
        // always send to host
        bus.SendToUser(hostSteamId, json);
    }

    private void BroadcastState()
    {
        if (!isHost || bus == null) return;

        var env = new BJNetEnvelope
        {
            type = "STATE",
            senderSteamId = SteamManager.Initialized ? SteamUser.GetSteamID().m_SteamID : 0UL,
            seatIndex = -1,
            amount = 0,
            state = BuildState()
        };

        string json = JsonUtility.ToJson(env);
        if (SteamManager.Initialized && lobbyId.IsValid())
            bus.BroadcastToLobby(lobbyId, json);
    }

    private BJTableDTO BuildState()
    {
        var dto = new BJTableDTO
        {
            currentPlayerIndex = currentPlayerIndex,
            roundInProgress = roundInProgress,
            statusText = statusText ? statusText.text : ""
        };

        // players
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            var pd = new BJPlayerDTO
            {
                playerName = p.playerName,
                isBot = p.isBot,
                cash = p.cash,
                wager = p.wager,
                splitWager = p.splitWager,
                hasSplit = p.hasSplit,
                playingSplit = p.playingSplit,
                isDone = p.isDone
            };

            // hands
            foreach (var c in p.hand)
            {
                if (c == null) continue;
                pd.hand.Add(new BJCardDTO { suit = c.suit, value = c.value, faceUp = c.IsFaceUp() });
            }
            foreach (var c in p.splitHand)
            {
                if (c == null) continue;
                pd.splitHand.Add(new BJCardDTO { suit = c.suit, value = c.value, faceUp = c.IsFaceUp() });
            }

            dto.players.Add(pd);
        }

        // dealer
        foreach (var c in dealerHand)
        {
            if (c == null) continue;
            dto.dealerHand.Add(new BJCardDTO { suit = c.suit, value = c.value, faceUp = c.IsFaceUp() });
        }

        return dto;
    }

    private void ApplyState(BJTableDTO dto)
    {
        if (dto == null) return;

        // Clients are purely renderers. Stop any local coroutines.
        StopAllCoroutines();

        roundInProgress = dto.roundInProgress;
        currentPlayerIndex = dto.currentPlayerIndex;
        if (statusText) statusText.text = dto.statusText ?? "";

        // Defensive: keep seat count stable; just overwrite existing PlayerData objects.
        int n = Mathf.Min(players.Count, dto.players.Count);
        for (int i = 0; i < n; i++)
        {
            var p = players[i];
            var pd = dto.players[i];

            p.playerName = pd.playerName;
            p.isBot = pd.isBot;
            p.cash = pd.cash;
            p.wager = pd.wager;
            p.splitWager = pd.splitWager;
            p.hasSplit = pd.hasSplit;
            p.playingSplit = pd.playingSplit;
            p.isDone = pd.isDone;

            p.hand.Clear();
            p.splitHand.Clear();
        }

        // Rebuild visuals from snapshot
        if (cardManager)
        {
            cardManager.ClearPlayerAreas();
            cardManager.ClearDealerArea();

            for (int i = 0; i < n; i++)
            {
                var p = players[i];
                var pd = dto.players[i];

                foreach (var c in pd.hand)
                {
                    var card = cardManager.SpawnCardToPlayerFromData(i, c.suit, c.value, c.faceUp, toSplit: false);
                    if (card != null) p.hand.Add(card);
                }
                foreach (var c in pd.splitHand)
                {
                    var card = cardManager.SpawnCardToPlayerFromData(i, c.suit, c.value, c.faceUp, toSplit: true);
                    if (card != null) p.splitHand.Add(card);
                }
            }

            dealerHand.Clear();
            foreach (var c in dto.dealerHand)
            {
                var card = cardManager.SpawnCardToDealerFromData(c.suit, c.value, c.faceUp);
                if (card != null) dealerHand.Add(card);
            }
        }

        UpdateCashUI();
        UpdateTurnControls();
    }

    private void OnNetJson(ulong senderSteamId, string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        BJNetEnvelope env;
        try { env = JsonUtility.FromJson<BJNetEnvelope>(json); }
        catch { return; }
        if (env == null) return;

        // Host receives REQ_* and executes them using the caller's steamId for validation.
        if (isHost && env.type != null && env.type.StartsWith("REQ_"))
        {
            switch (env.type)
            {
                case "REQ_WAGER": RequestWager(env.seatIndex, env.amount, senderSteamId); break;
                case "REQ_HIT": RequestHit(env.seatIndex, senderSteamId); break;
                case "REQ_STAND": RequestStand(env.seatIndex, senderSteamId); break;
                case "REQ_DOUBLE": RequestDouble(env.seatIndex, senderSteamId); break;
                case "REQ_SPLIT": RequestSplit(env.seatIndex, senderSteamId); break;
                case "REQ_START": StartRound(); break;
            }
            return;
        }

        // Clients receive authoritative snapshots.
        if (!isHost && env.type == "STATE")
        {
            ApplyState(env.state);
        }
    }
}