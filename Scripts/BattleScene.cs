using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum EnemyIntentType
{
    Attack,
    Defend,
    Buff
}

public partial class BattleScene : Control
{
    private enum EnemyAnimState
    {
        Idle,
        Hit,
        Dying
    }

    private const int MaxEnergy = 3;
    private const int HandLimit = 10;

    private readonly Random _rng = new();
    private readonly List<CardData> _drawPile = new();
    private readonly List<CardData> _discardPile = new();
    private readonly List<CardData> _hand = new();

    private Label _playerHpLabel = null!;
    private Label _playerBlockLabel = null!;
    private Label _playerStatusLabel = null!;
    private Label _enemyHpLabel = null!;
    private Label _enemyBlockLabel = null!;
    private Label _enemyStatusLabel = null!;
    private Label _enemyIntentLabel = null!;
    private Label _enemyNameLabel = null!;
    private Label _turnLabel = null!;
    private Label _energyLabel = null!;
    private Label _topHpLabel = null!;
    private Label _handCountLabel = null!;
    private Label _relicBarLabel = null!;
    private RichTextLabel _logText = null!;
    private HBoxContainer _relicIcons = null!;

    private Control _mainMargin = null!;
    private Control _handContainer = null!;
    private Control _enemyDropArea = null!;
    private Label _dropHintLabel = null!;
    private Label _enemyBodyLabel = null!;
    private ColorRect _enemySprite = null!;
    private TextureRect _enemyPortrait = null!;
    private ProgressBar _enemyHpBar = null!;
    private Control _enemyIntentBadge = null!;
    private TextureRect _enemyIntentIcon = null!;
    private Label _enemyIntentValueLabel = null!;
    private Control _playerPanel = null!;
    private Control _enemyPanel = null!;
    private Control _turnBanner = null!;
    private Label _turnBannerLabel = null!;
    private Control _drawAnchor = null!;
    private Control _effectsLayer = null!;
    private ColorRect _arenaFarBg = null!;
    private ColorRect _arenaMidBg = null!;
    private ColorRect _arenaFrontFog = null!;
    private PanelContainer _keywordTooltip = null!;
    private RichTextLabel _keywordTooltipText = null!;

    private Button _endTurnButton = null!;

    private readonly StyleBoxFlat _dropNormalStyle = new();
    private readonly StyleBoxFlat _dropHotStyle = new();

    private int _turn = 1;
    private int _playerHp;
    private int _playerMaxHp;
    private int _playerBlock;
    private int _playerStrength;
    private int _playerVulnerable;

    private int _enemyHp;
    private int _enemyMaxHp;
    private int _enemyBlock;
    private int _enemyStrength;
    private int _enemyVulnerable;

    private string _enemyName = "Enemy";
    private string _enemyVisualId = "cultist";
    private bool _isElite;

    private EnemyIntentType _enemyIntentType;
    private int _enemyIntentValue;

    private int _energy;
    private bool _battleEnded;
    private int _inputLockDepth;
    private string _relicUiSignature = string.Empty;
    private readonly Dictionary<string, PanelContainer> _relicChipById = new();
    private readonly Dictionary<string, ColorRect> _relicTriggerDotById = new();
    private readonly HashSet<string> _triggeredRelicsThisTurn = new();
    private readonly Dictionary<string, Texture2D> _iconCache = new();
    private Vector2 _playerPanelBasePos;
    private Vector2 _enemyDropAreaBasePos;
    private Vector2 _enemyDropAreaBaseScale;
    private Vector2 _enemyShadowBaseSize;
    private Vector2 _playerShadowBaseSize;
    private ColorRect _enemyShadow = null!;
    private ColorRect _playerShadow = null!;
    private float _animTime;
    private EnemyAnimState _enemyAnimState = EnemyAnimState.Idle;
    private float _enemyAnimTimer;
    private bool _enemyEntrancePlayed;
    private float _playerPunchX;
    private float _enemyPunchX;
    private bool _deferredHandLayoutPending;
    private bool _dropZoneHighlighted;

    private CardView _hoveredCard = null!;

    public override void _Ready()
    {
        _playerHpLabel = GetNode<Label>("%PlayerHpLabel");
        _playerBlockLabel = GetNode<Label>("%PlayerBlockLabel");
        _playerStatusLabel = GetNode<Label>("%PlayerStatusLabel");
        _enemyNameLabel = GetNode<Label>("%EnemyNameLabel");
        _enemyHpLabel = GetNode<Label>("%EnemyHpLabel");
        _enemyBlockLabel = GetNode<Label>("%EnemyBlockLabel");
        _enemyStatusLabel = GetNode<Label>("%EnemyStatusLabel");
        _enemyIntentLabel = GetNode<Label>("%EnemyIntentLabel");
        _turnLabel = GetNode<Label>("%TurnLabel");
        _energyLabel = GetNode<Label>("%EnergyLabel");
        _topHpLabel = GetNode<Label>("%TopHpLabel");
        _handCountLabel = GetNode<Label>("%HandCountLabel");
        _relicBarLabel = GetNode<Label>("%RelicBarLabel");
        _relicIcons = GetNode<HBoxContainer>("%RelicIcons");
        _logText = GetNode<RichTextLabel>("%LogText");

        _mainMargin = GetNode<Control>("%MainMargin");
        _handContainer = GetNode<Control>("%HandContainer");
        _enemyDropArea = GetNode<Control>("%EnemyDropArea");
        _dropHintLabel = GetNode<Label>("%DropHintLabel");
        _enemyBodyLabel = GetNode<Label>("%EnemyBodyLabel");
        _enemySprite = GetNode<ColorRect>("%EnemySprite");
        _enemyPortrait = GetNode<TextureRect>("%EnemyPortrait");
        _enemyHpBar = GetNode<ProgressBar>("%EnemyHpBar");
        _enemyIntentBadge = GetNode<Control>("%EnemyIntentBadge");
        _enemyIntentIcon = GetNode<TextureRect>("%EnemyIntentIcon");
        _enemyIntentValueLabel = GetNode<Label>("%EnemyIntentValue");
        _playerPanel = GetNode<Control>("%PlayerPanel");
        _enemyPanel = GetNode<Control>("%EnemyPanel");
        _turnBanner = GetNode<Control>("%TurnBanner");
        _turnBannerLabel = GetNode<Label>("%TurnBannerLabel");
        _drawAnchor = GetNode<Control>("%DrawAnchor");
        _effectsLayer = GetNode<Control>("%EffectsLayer");
        _arenaFarBg = GetNode<ColorRect>("%ArenaFarBg");
        _arenaMidBg = GetNode<ColorRect>("%ArenaMidBg");
        _arenaFrontFog = GetNode<ColorRect>("%ArenaFrontFog");
        _keywordTooltip = GetNode<PanelContainer>("%KeywordTooltip");
        _keywordTooltipText = GetNode<RichTextLabel>("%KeywordTooltipText");
        _enemyShadow = GetNode<ColorRect>("MainMargin/MainVBox/Arena/EnemyShadow");
        _playerShadow = GetNode<ColorRect>("MainMargin/MainVBox/Arena/PlayerShadow");

        _endTurnButton = GetNode<Button>("%EndTurnButton");

        SetupDropZoneStyles();

        _endTurnButton.Pressed += EndTurn;
        GetNode<Button>("%BackButton").Pressed += BackToMap;
        _handContainer.Resized += () => LayoutHandCards(false);

        SetupFromGameState();
        _playerPanelBasePos = _playerPanel.Position;
        _enemyDropAreaBasePos = _enemyDropArea.Position;
        _enemyDropAreaBaseScale = _enemyDropArea.Scale;
        _enemyShadowBaseSize = _enemyShadow.Size;
        _playerShadowBaseSize = _playerShadow.Size;

        Log("Battle start", "#cbd5e1");
        StartBattleFlow();
    }

    public override void _Process(double delta)
    {
        _animTime += (float)delta;

        var viewport = GetViewportRect().Size;
        if (viewport.X <= 1 || viewport.Y <= 1)
        {
            return;
        }

        var mouse = GetViewport().GetMousePosition();
        var nx = (mouse.X / viewport.X - 0.5f) * 2f;
        var ny = (mouse.Y / viewport.Y - 0.5f) * 2f;

        _arenaFarBg.Position = new Vector2(nx * -6f, ny * -4f);
        _arenaMidBg.Position = new Vector2(nx * -10f, ny * -6f);
        _arenaFrontFog.Position = new Vector2(nx * -14f, ny * -8f);

        var breathePlayer = Mathf.Sin(_animTime * 1.2f) * 2.2f;
        var breatheEnemy = Mathf.Sin(_animTime * 1.1f + 1.3f) * 2.8f;
        _playerPunchX = Mathf.Lerp(_playerPunchX, 0f, (float)delta * 16f);
        _enemyPunchX = Mathf.Lerp(_enemyPunchX, 0f, (float)delta * 16f);
        _playerPanel.Position = _playerPanelBasePos + new Vector2(_playerPunchX, breathePlayer);

        var enemyAnimOffset = Vector2.Zero;
        var enemyAnimScaleMul = 1f;
        switch (_enemyAnimState)
        {
            case EnemyAnimState.Hit:
                _enemyAnimTimer -= (float)delta;
                enemyAnimOffset = new Vector2(Mathf.Sin(_animTime * 60f) * 5f, -2f);
                enemyAnimScaleMul = 1.02f;
                if (_enemyAnimTimer <= 0f)
                {
                    _enemyAnimState = EnemyAnimState.Idle;
                }
                break;
            case EnemyAnimState.Dying:
                enemyAnimOffset = new Vector2(0f, 12f);
                enemyAnimScaleMul = 0.9f;
                break;
        }

        _enemyDropArea.Position = _enemyDropAreaBasePos + new Vector2(_enemyPunchX, breatheEnemy) + enemyAnimOffset;
        _enemyDropArea.Scale = _enemyDropAreaBaseScale * ((1f + Mathf.Sin(_animTime * 1.1f + 1.3f) * 0.01f) * enemyAnimScaleMul);

        var shadowScale = 1f + Mathf.Sin(_animTime * 1.1f + 1.3f) * 0.03f;
        _enemyShadow.Size = _enemyShadowBaseSize * shadowScale;
        _playerShadow.Size = _playerShadowBaseSize * (1f + Mathf.Sin(_animTime * 1.2f) * 0.02f);
    }

    private async void StartBattleFlow()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _playerPanelBasePos = _playerPanel.Position;
        _enemyDropAreaBasePos = _enemyDropArea.Position;
        _enemyDropAreaBaseScale = _enemyDropArea.Scale;
        _enemyShadowBaseSize = _enemyShadow.Size;
        _playerShadowBaseSize = _playerShadow.Size;
        await PlayEnemyEntrance();
        StartPlayerTurn();
    }

    private async Task PlayEnemyEntrance()
    {
        if (_enemyEntrancePlayed)
        {
            return;
        }

        _enemyEntrancePlayed = true;
        _enemyDropArea.Scale = _enemyDropAreaBaseScale * 0.86f;
        _enemyDropArea.Modulate = new Color(1, 1, 1, 0f);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_enemyDropArea, "scale", _enemyDropAreaBaseScale, 0.22f);
        tween.Parallel().TweenProperty(_enemyDropArea, "modulate:a", 1f, 0.2f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void SetupDropZoneStyles()
    {
        _dropNormalStyle.BgColor = new Color("2b3445");
        _dropNormalStyle.BorderWidthLeft = 2;
        _dropNormalStyle.BorderWidthTop = 2;
        _dropNormalStyle.BorderWidthRight = 2;
        _dropNormalStyle.BorderWidthBottom = 2;
        _dropNormalStyle.BorderColor = new Color("4b5563");
        _dropNormalStyle.CornerRadiusTopLeft = 8;
        _dropNormalStyle.CornerRadiusTopRight = 8;
        _dropNormalStyle.CornerRadiusBottomLeft = 8;
        _dropNormalStyle.CornerRadiusBottomRight = 8;

        _dropHotStyle.BgColor = new Color("1f4d3b");
        _dropHotStyle.BorderWidthLeft = 2;
        _dropHotStyle.BorderWidthTop = 2;
        _dropHotStyle.BorderWidthRight = 2;
        _dropHotStyle.BorderWidthBottom = 2;
        _dropHotStyle.BorderColor = new Color("34d399");
        _dropHotStyle.CornerRadiusTopLeft = 8;
        _dropHotStyle.CornerRadiusTopRight = 8;
        _dropHotStyle.CornerRadiusBottomLeft = 8;
        _dropHotStyle.CornerRadiusBottomRight = 8;

        SetDropZoneHighlight(false);
    }

    private void SetupFromGameState()
    {
        var state = GetNode<GameState>("/root/GameState");

        _playerHp = state.PlayerHp;
        _playerMaxHp = state.MaxHp;

        _isElite = state.PendingEncounterType == MapNodeType.EliteBattle;
        if (_isElite)
        {
            _enemyVisualId = "elite_sentinel";
            _enemyHp = 85 + state.Floor * 12;
            _enemyMaxHp = _enemyHp;
            _enemyStrength = Math.Max(state.Floor / 2, 1);
        }
        else
        {
            _enemyVisualId = "cultist";
            _enemyHp = 50 + state.Floor * 9;
            _enemyMaxHp = _enemyHp;
            _enemyStrength = Math.Max(state.Floor - 1, 0);
        }

        var visual = CombatVisualCatalog.GetEnemyVisual(_enemyVisualId);
        _enemyName = visual.DisplayName;
        _enemySprite.Color = visual.StageTint;
        _enemyPortrait.Texture = LoadTextureCached(visual.PortraitPath);

        _drawPile.Clear();
        _discardPile.Clear();
        _hand.Clear();

        _drawPile.AddRange(state.CreateDeckCards());
        Shuffle(_drawPile);
    }

    private async void StartPlayerTurn()
    {
        if (_battleEnded)
        {
            return;
        }

        PushInputLock();
        ClearRelicTurnMarkers();
        await ShowTurnBanner("Player Turn", new Color("38bdf8"));

        var state = GetNode<GameState>("/root/GameState");

        _energy = MaxEnergy;
        if (_turn == 1 && state.HasRelic("lantern"))
        {
            _energy += 1;
            Log("Lantern grants +1 energy", "#facc15");
            FlashRelic("lantern");
        }

        _playerBlock = 0;
        if (_turn == 1 && state.HasRelic("anchor"))
        {
            _playerBlock += 8;
            Log("Anchor grants 8 block", "#60a5fa");
            FlashRelic("anchor");
        }

        await DrawCards(5);
        RollEnemyIntent();

        RefreshUi();
        PopInputLock();
    }

    private void RollEnemyIntent()
    {
        var roll = _rng.Next(100);
        if (roll < (_isElite ? 70 : 60))
        {
            _enemyIntentType = EnemyIntentType.Attack;
            _enemyIntentValue = _isElite ? _rng.Next(9, 15) : _rng.Next(6, 11);
        }
        else if (roll < 85)
        {
            _enemyIntentType = EnemyIntentType.Defend;
            _enemyIntentValue = _isElite ? 10 : 6;
        }
        else
        {
            _enemyIntentType = EnemyIntentType.Buff;
            _enemyIntentValue = _isElite ? 3 : 2;
        }

        Log($"Turn {_turn}: Enemy intent -> {IntentText()}", "#94a3b8");
    }

    private string IntentText()
    {
        return _enemyIntentType switch
        {
            EnemyIntentType.Attack => $"Attack {_enemyIntentValue + _enemyStrength}",
            EnemyIntentType.Defend => $"Gain {_enemyIntentValue} Block",
            EnemyIntentType.Buff => $"Gain {_enemyIntentValue} Strength",
            _ => "-"
        };
    }

    private string IntentBadgeText()
    {
        return _enemyIntentType switch
        {
            EnemyIntentType.Attack => $"{_enemyIntentValue + _enemyStrength}",
            EnemyIntentType.Defend => $"{_enemyIntentValue}",
            EnemyIntentType.Buff => $"+{_enemyIntentValue}",
            _ => "-"
        };
    }

    private string IntentIconText()
    {
        return CombatVisualCatalog.GetIntentIconPath(_enemyIntentType);
    }

    private async void EndTurn()
    {
        if (_battleEnded || IsInputLocked())
        {
            return;
        }

        PushInputLock();

        foreach (var card in _hand)
        {
            _discardPile.Add(card);
        }
        _hand.Clear();

        await RenderHand();

        await ShowTurnBanner("Enemy Turn", new Color("f87171"));

        ExecuteEnemyTurn();
        if (_battleEnded)
        {
            RefreshUi();
            PopInputLock();
            return;
        }

        _turn += 1;
        TickStatuses();
        PopInputLock();
        StartPlayerTurn();
    }

    private void ExecuteEnemyTurn()
    {
        switch (_enemyIntentType)
        {
            case EnemyIntentType.Attack:
            {
                var rawDamage = _enemyIntentValue + _enemyStrength;
                var finalDamage = ApplyVulnerable(rawDamage, _playerVulnerable);
                var blocked = Math.Min(_playerBlock, finalDamage);
                var taken = finalDamage - blocked;
                _playerBlock -= blocked;
                _playerHp -= taken;
                Log($"Enemy attacks {finalDamage}, blocked {blocked}, took {taken}", "#f87171");
                if (taken > 0)
                {
                    SpawnFloatingText(_playerPanel, $"-{taken}", new Color("fca5a5"));
                    SpawnSlashEffect(_playerPanel, new Color("fecaca"));
                    FlashPanel(_playerPanel, new Color(1f, 0.5f, 0.5f, 1f));
                    PunchPanel(_playerPanel, -8f);
                    ShakeMain(6f, 7);
                }

                break;
            }
            case EnemyIntentType.Defend:
                _enemyBlock += _enemyIntentValue;
                Log($"Enemy gains {_enemyIntentValue} Block", "#60a5fa");
                SpawnFloatingText(_enemyPanel, $"+{_enemyIntentValue} Block", new Color("93c5fd"));
                SpawnShieldEffect(_enemyDropArea, new Color("93c5fd"));
                break;
            case EnemyIntentType.Buff:
                _enemyStrength += _enemyIntentValue;
                Log($"Enemy gains {_enemyIntentValue} Strength", "#c084fc");
                SpawnFloatingText(_enemyPanel, $"+{_enemyIntentValue} STR", new Color("d8b4fe"));
                SpawnRuneEffect(_enemyDropArea, new Color("d8b4fe"));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_playerHp <= 0)
        {
            _playerHp = 0;
            _battleEnded = true;
            Log("Defeat", "#ef4444");
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        }
    }

    private void TickStatuses()
    {
        _playerVulnerable = Math.Max(_playerVulnerable - 1, 0);
        _enemyVulnerable = Math.Max(_enemyVulnerable - 1, 0);
        _enemyBlock = 0;
    }

    private async Task DrawCards(int count)
    {
        var entering = new HashSet<CardData>();

        for (var i = 0; i < count; i++)
        {
            if (_hand.Count >= HandLimit)
            {
                Log("Hand is full", "#f59e0b");
                break;
            }

            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0)
                {
                    break;
                }

                _drawPile.AddRange(_discardPile);
                _discardPile.Clear();
                Shuffle(_drawPile);
                Log("Shuffled discard into draw pile", "#94a3b8");
            }

            var card = _drawPile[0];
            _drawPile.RemoveAt(0);
            _hand.Add(card);
            entering.Add(card);
        }

        await RenderHand(entering);
    }

    private async Task<bool> TrySpendAndApplyCard(CardData card)
    {
        if (_battleEnded || !_hand.Contains(card))
        {
            return false;
        }

        if (card.Cost > _energy)
        {
            Log($"Not enough energy for {card.Name}", "#f59e0b");
            return false;
        }

        _energy -= card.Cost;

        var state = GetNode<GameState>("/root/GameState");
        var relicAttackBonus = state.HasRelic("whetstone") ? 1 : 0;

        if (card.Damage > 0)
        {
            var rawDamage = card.Damage + _playerStrength + relicAttackBonus;
            var finalDamage = ApplyVulnerable(rawDamage, _enemyVulnerable);
            var damageToHp = Math.Max(finalDamage - _enemyBlock, 0);
            _enemyBlock = Math.Max(_enemyBlock - finalDamage, 0);
            _enemyHp -= damageToHp;
            Log($"Play {card.Name}: damage {finalDamage} ({damageToHp} HP)", "#f87171");
            if (damageToHp > 0)
            {
                SpawnFloatingText(_enemyDropArea, $"-{damageToHp}", new Color("fda4af"));
                SpawnSlashEffect(_enemyDropArea, new Color("fda4af"));
                TriggerEnemyHit();
                FlashPanel(_enemyDropArea, new Color(1f, 0.55f, 0.55f, 1f));
                PunchPanel(_enemyDropArea, 8f);
                ShakeMain(4f, 5);
            }
        }

        if (card.Block > 0)
        {
            _playerBlock += card.Block;
            Log($"Play {card.Name}: gain {card.Block} Block", "#60a5fa");
            SpawnFloatingText(_playerPanel, $"+{card.Block}", new Color("93c5fd"));
            SpawnShieldEffect(_playerPanel, new Color("93c5fd"));
        }

        if (card.ApplyVulnerable > 0)
        {
            _enemyVulnerable += card.ApplyVulnerable;
            Log($"Play {card.Name}: apply {card.ApplyVulnerable} Vulnerable", "#c084fc");
            SpawnFloatingText(_enemyDropArea, $"VUL+{card.ApplyVulnerable}", new Color("d8b4fe"));
            SpawnRuneEffect(_enemyDropArea, new Color("d8b4fe"));
        }

        _hand.Remove(card);
        _discardPile.Add(card);

        if (card.DrawCount > 0)
        {
            Log($"Play {card.Name}: draw {card.DrawCount}", "#93c5fd");
            await DrawCards(card.DrawCount);
        }
        else
        {
            await RenderHand();
        }

        if (_enemyHp <= 0)
        {
            _enemyHp = 0;
            OnVictory();
            return true;
        }

        RefreshUi();
        return true;
    }

    private async void OnCardDropAttempt(CardView view, Vector2 mouseGlobal)
    {
        if (_battleEnded || IsInputLocked())
        {
            SetDropZoneHighlight(false);
            if (IsInstanceValid(view))
            {
                await view.AnimateBackToHand();
            }

            LayoutHandCards(true);
            return;
        }

        SetDropZoneHighlight(false);

        var requiresEnemyTarget = CardRequiresEnemyTarget(view.Card);
        if (!requiresEnemyTarget)
        {
            PushInputLock();
            var playedSelf = await TrySpendAndApplyCard(view.Card);
            if (!playedSelf && IsInstanceValid(view))
            {
                await view.AnimateBackToHand();
                LayoutHandCards(true);
            }
            PopInputLock();
            return;
        }

        var canPlayZone = _enemyDropArea.GetGlobalRect().HasPoint(mouseGlobal);
        if (!canPlayZone)
        {
            await view.AnimateBackToHand();
            LayoutHandCards(true);
            return;
        }

        if (view.Card.Cost > _energy)
        {
            Log($"Not enough energy for {view.Card.Name}", "#f59e0b");
            await view.AnimateBackToHand();
            LayoutHandCards(true);
            return;
        }

        PushInputLock();

        var dropRect = _enemyDropArea.GetGlobalRect();
        var fromPos = view.GlobalPosition + view.Size * 0.5f;
        var target = new Vector2(
            dropRect.Position.X + dropRect.Size.X * 0.5f - view.Size.X * 0.5f,
            dropRect.Position.Y + dropRect.Size.Y * 0.5f - view.Size.Y * 0.5f);

        await view.AnimateToTarget(target);

        SpawnCardTrail(fromPos, target + view.Size * 0.5f);
        var played = await TrySpendAndApplyCard(view.Card);
        if (!played && IsInstanceValid(view))
        {
            await view.AnimateBackToHand();
            LayoutHandCards(true);
        }
        PopInputLock();
    }

    private async void OnCardClicked(CardView view)
    {
        if (_battleEnded || IsInputLocked())
        {
            return;
        }

        if (CardRequiresEnemyTarget(view.Card))
        {
            Log($"Drag {view.Card.Name} to enemy to play", "#94a3b8");
            if (IsInstanceValid(view))
            {
                await view.AnimateBackToHand(0.08f);
            }
            return;
        }

        PushInputLock();
        var toRect = _enemyDropArea.GetGlobalRect();
        var fromPos = view.GlobalPosition + view.Size * 0.5f;
        var toPos = new Vector2(toRect.Position.X + toRect.Size.X * 0.5f, toRect.Position.Y + toRect.Size.Y * 0.5f);
        if (view.Card.Cost <= _energy)
        {
            SpawnCardTrail(fromPos, toPos);
        }
        if (!await TrySpendAndApplyCard(view.Card) && IsInstanceValid(view))
        {
            await view.AnimateBackToHand();
            LayoutHandCards(true);
        }

        PopInputLock();
    }

    private void OnCardDragMoved(CardView _, Vector2 mouseGlobal)
    {
        var hot = _enemyDropArea.GetGlobalRect().HasPoint(mouseGlobal);
        SetDropZoneHighlight(hot);
    }

    private void OnCardDragStarted(CardView card)
    {
        _hoveredCard = null;
        HideKeywordTooltip();
        SetDropZoneHighlight(false);
        card.Scale = Vector2.One;
    }

    private void OnCardDragEnded(CardView card)
    {
        SetDropZoneHighlight(false);
        if (IsInstanceValid(card) && _hand.Contains(card.Card))
        {
            RequestHandLayout(true);
        }
    }

    private void OnCardHoverChanged(CardView card, bool hovered)
    {
        if (card.IsDragging)
        {
            return;
        }

        _hoveredCard = hovered ? card : (_hoveredCard == card ? null : _hoveredCard);
        if (hovered)
        {
            ShowKeywordTooltip(card);
        }
        else if (_hoveredCard == null)
        {
            HideKeywordTooltip();
        }
    }

    private bool CardRequiresEnemyTarget(CardData card)
    {
        return card.Damage > 0 || card.ApplyVulnerable > 0;
    }

    private void SetDropZoneHighlight(bool highlight)
    {
        if (_dropZoneHighlighted == highlight)
        {
            return;
        }

        _dropZoneHighlighted = highlight;
        _enemyDropArea.AddThemeStyleboxOverride("panel", highlight ? _dropHotStyle : _dropNormalStyle);
        _dropHintLabel.Modulate = highlight ? new Color("bbf7d0") : new Color("d1d5db");
    }

    private void FlashPanel(Control panel, Color flashColor)
    {
        if (!IsInstanceValid(panel))
        {
            return;
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(panel, "modulate", flashColor, 0.07f);
        tween.TweenProperty(panel, "modulate", Colors.White, 0.16f);
    }

    private void TriggerEnemyHit()
    {
        if (_enemyAnimState == EnemyAnimState.Dying)
        {
            return;
        }

        _enemyAnimState = EnemyAnimState.Hit;
        _enemyAnimTimer = 0.14f;
    }

    private async Task TriggerEnemyDeath()
    {
        if (_enemyAnimState == EnemyAnimState.Dying)
        {
            return;
        }

        _enemyAnimState = EnemyAnimState.Dying;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_enemyDropArea, "modulate:a", 0.15f, 0.28f);
        tween.Parallel().TweenProperty(_enemyDropArea, "scale", _enemyDropAreaBaseScale * 0.78f, 0.28f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void SpawnSlashEffect(Control target, Color color)
    {
        var rect = new ColorRect
        {
            Color = color,
            Size = new Vector2(86, 10),
            TopLevel = true,
            ZIndex = 180
        };
        _effectsLayer.AddChild(rect);

        var area = target.GetGlobalRect();
        rect.GlobalPosition = new Vector2(area.Position.X + area.Size.X * 0.5f - 43f, area.Position.Y + area.Size.Y * 0.45f);
        rect.RotationDegrees = -22f;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(rect, "scale", new Vector2(1.3f, 1f), 0.12f);
        tween.Parallel().TweenProperty(rect, "modulate:a", 0f, 0.18f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(rect))
            {
                rect.QueueFree();
            }
        };
    }

    private void SpawnShieldEffect(Control target, Color color)
    {
        var ring = new ColorRect
        {
            Color = new Color(color.R, color.G, color.B, 0.36f),
            Size = new Vector2(58, 58),
            TopLevel = true,
            ZIndex = 180
        };
        _effectsLayer.AddChild(ring);

        var area = target.GetGlobalRect();
        ring.GlobalPosition = new Vector2(area.Position.X + area.Size.X * 0.5f - 29f, area.Position.Y + area.Size.Y * 0.5f - 29f);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(ring, "scale", new Vector2(1.5f, 1.5f), 0.22f);
        tween.Parallel().TweenProperty(ring, "modulate:a", 0f, 0.22f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(ring))
            {
                ring.QueueFree();
            }
        };
    }

    private void SpawnRuneEffect(Control target, Color color)
    {
        var rune = new Label
        {
            Text = "✦",
            Modulate = color,
            TopLevel = true,
            ZIndex = 180
        };
        _effectsLayer.AddChild(rune);

        var area = target.GetGlobalRect();
        rune.GlobalPosition = new Vector2(area.Position.X + area.Size.X * 0.5f - 6f, area.Position.Y + area.Size.Y * 0.5f - 8f);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(rune, "global_position", rune.GlobalPosition + new Vector2(0f, -28f), 0.26f);
        tween.Parallel().TweenProperty(rune, "modulate:a", 0f, 0.26f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(rune))
            {
                rune.QueueFree();
            }
        };
    }

    private void PunchPanel(Control panel, float offsetX)
    {
        if (panel == _playerPanel)
        {
            _playerPunchX += offsetX;
            return;
        }

        if (panel == _enemyDropArea || panel == _enemyPanel)
        {
            _enemyPunchX += offsetX;
        }
    }

    private async void ShakeMain(float intensity, int steps)
    {
        if (!IsInstanceValid(_mainMargin))
        {
            return;
        }

        var original = _mainMargin.Position;
        for (var i = 0; i < steps; i++)
        {
            var x = (float)(_rng.NextDouble() * 2.0 - 1.0) * intensity;
            var y = (float)(_rng.NextDouble() * 2.0 - 1.0) * intensity * 0.5f;
            _mainMargin.Position = original + new Vector2(x, y);
            await ToSignal(GetTree().CreateTimer(0.012f), SceneTreeTimer.SignalName.Timeout);
        }

        _mainMargin.Position = original;
        // Cards are positioned in global space; after screen shake, force a re-layout
        // so they snap back to the correct fan positions.
        LayoutHandCards(false);
    }

    private async Task ShowTurnBanner(string text, Color tint)
    {
        _turnBannerLabel.Text = text;
        _turnBanner.Modulate = new Color(tint, 0f);
        _turnBanner.Visible = true;
        _turnBanner.Position = new Vector2(_turnBanner.Position.X, 20);

        var tweenIn = CreateTween();
        tweenIn.SetEase(Tween.EaseType.Out);
        tweenIn.SetTrans(Tween.TransitionType.Cubic);
        tweenIn.TweenProperty(_turnBanner, "position:y", 34f, 0.15f);
        tweenIn.Parallel().TweenProperty(_turnBanner, "modulate:a", 1f, 0.15f);
        await ToSignal(tweenIn, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(0.18f), SceneTreeTimer.SignalName.Timeout);

        var tweenOut = CreateTween();
        tweenOut.SetEase(Tween.EaseType.Out);
        tweenOut.SetTrans(Tween.TransitionType.Cubic);
        tweenOut.TweenProperty(_turnBanner, "modulate:a", 0f, 0.2f);
        await ToSignal(tweenOut, Tween.SignalName.Finished);
        _turnBanner.Visible = false;
    }

    private void SpawnFloatingText(Control target, string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            Modulate = color,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TopLevel = true,
            ZIndex = 200
        };

        _effectsLayer.AddChild(label);

        var targetRect = target.GetGlobalRect();
        var start = new Vector2(targetRect.Position.X + targetRect.Size.X * 0.5f - 40f, targetRect.Position.Y + 18f);
        label.GlobalPosition = start;

        var isDamage = text.StartsWith("-");
        var value = 0;
        if (isDamage)
        {
            int.TryParse(text.Replace("-", string.Empty), out value);
        }
        var isCritStyle = isDamage && value >= 12;
        label.Scale = isCritStyle ? new Vector2(1.25f, 1.25f) : Vector2.One;
        if (isCritStyle)
        {
            label.AddThemeColorOverride("font_color", new Color("fecaca"));
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(label, "scale", label.Scale * (isCritStyle ? 1.15f : 1.07f), 0.12f);
        tween.Parallel().TweenProperty(label, "global_position", start + new Vector2(0f, -18f), 0.12f);
        tween.TweenProperty(label, "scale", isCritStyle ? new Vector2(1.12f, 1.12f) : Vector2.One, 0.1f);
        tween.Parallel().TweenProperty(label, "global_position", start + new Vector2(0f, -42f), 0.34f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.34f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(label))
            {
                label.QueueFree();
            }
        };
    }

    private void SpawnCardTrail(Vector2 from, Vector2 to)
    {
        var dir = to - from;
        var len = Math.Max(dir.Length(), 1f);
        var trail = new ColorRect
        {
            Color = new Color("7dd3fc"),
            Size = new Vector2(len, 3),
            TopLevel = true,
            ZIndex = 170
        };
        _effectsLayer.AddChild(trail);
        trail.GlobalPosition = from;
        trail.Rotation = dir.Angle();

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(trail, "modulate:a", 0f, 0.18f);
        tween.Parallel().TweenProperty(trail, "scale:y", 0.2f, 0.18f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(trail))
            {
                trail.QueueFree();
            }
        };
    }

    private async void OnVictory()
    {
        _battleEnded = true;
        PushInputLock();
        await TriggerEnemyDeath();
        Log("Victory", "#22c55e");

        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = _playerHp;
        var hpBeforeResolve = state.PlayerHp;
        state.ResolveBattleVictory();
        if (state.PlayerHp > hpBeforeResolve && state.HasRelic("charm"))
        {
            Log($"Lucky Charm heals {state.PlayerHp - hpBeforeResolve} HP", "#86efac");
            FlashRelic("charm");
        }
        state.RollRewardOptions(3);

        if (_isElite)
        {
            state.RollRelicOptions(3);
            GetTree().ChangeSceneToFile("res://Scenes/RelicRewardScene.tscn");
            return;
        }

        GetTree().ChangeSceneToFile("res://Scenes/RewardScene.tscn");
    }

    private int ApplyVulnerable(int baseDamage, int vulnerableTurns)
    {
        if (vulnerableTurns <= 0)
        {
            return baseDamage;
        }

        return Mathf.CeilToInt(baseDamage * 1.5f);
    }

    private async Task RenderHand(HashSet<CardData> entering = null)
    {
        foreach (Node child in _handContainer.GetChildren())
        {
            child.QueueFree();
        }

        _hoveredCard = null;
        var entrants = new List<CardView>();

        foreach (var card in _hand)
        {
            var cardView = new CardView();
            cardView.Setup(card);
            cardView.SetPlayable(!IsInputLocked() && card.Cost <= _energy);
            cardView.DropAttempted += OnCardDropAttempt;
            cardView.Clicked += OnCardClicked;
            cardView.DragMoved += OnCardDragMoved;
            cardView.DragStarted += OnCardDragStarted;
            cardView.DragEnded += OnCardDragEnded;
            cardView.HoverChanged += OnCardHoverChanged;
            _handContainer.AddChild(cardView);

            if (entering != null && entering.Contains(card))
            {
                entrants.Add(cardView);
            }
        }

        LayoutHandCards(false);

        if (entrants.Count == 0)
        {
            return;
        }

        var drawRect = _drawAnchor.GetGlobalRect();
        var basePos = new Vector2(drawRect.Position.X + drawRect.Size.X * 0.5f, drawRect.Position.Y + drawRect.Size.Y * 0.5f);
        var tasks = new List<Task>();
        for (var i = 0; i < entrants.Count; i++)
        {
            var offset = new Vector2(i * 8f, -i * 6f);
            tasks.Add(entrants[i].AnimateFromDraw(basePos + offset));
        }

        await Task.WhenAll(tasks);
        LayoutHandCards(true);
    }

    private void LayoutHandCards(bool animate)
    {
        var cards = new List<CardView>();
        foreach (Node node in _handContainer.GetChildren())
        {
            if (node is CardView card)
            {
                cards.Add(card);
            }
        }

        var count = cards.Count;
        if (count == 0)
        {
            HideKeywordTooltip();
            return;
        }

        CardView hovered = null;
        if (IsInstanceValid(_hoveredCard))
        {
            hovered = _hoveredCard;
        }

        var hoveredIndex = -1;
        var anyDragging = false;
        for (var i = 0; i < count; i++)
        {
            if (cards[i].IsDragging)
            {
                anyDragging = true;
            }
            if (hovered != null && cards[i] == hovered && !cards[i].IsDragging)
            {
                hoveredIndex = i;
            }
        }

        var width = _handContainer.Size.X;
        var baseY = _handContainer.GlobalPosition.Y + _handContainer.Size.Y - 230f;
        var spread = Mathf.Min(1000f, 170f * Math.Max(count - 1, 1));
        var startX = _handContainer.GlobalPosition.X + width * 0.5f - spread * 0.5f - 90f;

        for (var i = 0; i < count; i++)
        {
            var t = count == 1 ? 0.5f : i / (float)(count - 1);
            var x = startX + spread * t;
            var normalized = t * 2f - 1f;
            var curveY = Mathf.Abs(normalized) * 26f;
            var rot = normalized * 14f;

            if (!anyDragging && hoveredIndex >= 0)
            {
                var distance = Math.Abs(i - hoveredIndex);
                var direction = i < hoveredIndex ? -1f : 1f;
                if (i == hoveredIndex)
                {
                    curveY -= 14f;
                    rot *= 0.22f;
                }
                else if (distance == 1)
                {
                    x += direction * 20f;
                    rot *= 0.65f;
                }
                else if (distance == 2)
                {
                    x += direction * 8f;
                }
            }

            var pos = new Vector2(x, baseY + curveY);
            var scale = Vector2.One;
            var finalRot = rot;

            if (!anyDragging && hoveredIndex == i)
            {
                pos.Y -= 18f;
                scale = new Vector2(1.08f, 1.08f);
                finalRot = 0f;
            }

            cards[i].ZIndex = hoveredIndex == i ? count + 10 : i;
            cards[i].SetPose(pos, finalRot, scale, animate);
        }
    }

    private void BackToMap()
    {
        if (IsInputLocked())
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = _playerHp;
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void RefreshUi()
    {
        _playerHpLabel.Text = $"HP: {_playerHp}";
        _playerBlockLabel.Text = $"Block: {_playerBlock}";
        _playerStatusLabel.Text = $"Status: STR {_playerStrength}, VUL {_playerVulnerable}";

        _enemyNameLabel.Text = _enemyName;
        _enemyBodyLabel.Text = _enemyName.ToUpperInvariant();
        _enemyHpLabel.Text = $"Enemy HP: {_enemyHp}";
        _enemyBlockLabel.Text = $"Enemy Block: {_enemyBlock}";
        _enemyStatusLabel.Text = $"Enemy Status: STR {_enemyStrength}, VUL {_enemyVulnerable}";
        _enemyHpBar.MaxValue = Math.Max(_enemyMaxHp, 1);
        _enemyHpBar.Value = Math.Max(_enemyHp, 0);
        _enemyIntentIcon.Texture = _battleEnded ? null : LoadTextureCached(IntentIconText());
        _enemyIntentValueLabel.Text = _battleEnded ? "-" : IntentBadgeText();
        _enemyIntentBadge.Modulate = _enemyIntentType switch
        {
            EnemyIntentType.Attack => new Color("fecaca"),
            EnemyIntentType.Defend => new Color("bfdbfe"),
            EnemyIntentType.Buff => new Color("e9d5ff"),
            _ => Colors.White
        };

        _enemyIntentLabel.Text = _battleEnded ? "Intent: -" : $"Intent: {IntentText()}";
        _topHpLabel.Text = $"HP {_playerHp}/{_playerMaxHp}";
        _turnLabel.Text = $"Turn {_turn}";
        _energyLabel.Text = $"Energy {_energy}/{MaxEnergy}";
        _handCountLabel.Text = $"Hand: {_hand.Count} | Draw: {_drawPile.Count} | Discard: {_discardPile.Count}";
        _relicBarLabel.Text = BuildRelicBarText();
        RefreshRelicIcons();

        UpdateInputControls();
        RefreshCardPlayableStates();
    }

    private string BuildRelicBarText()
    {
        var state = GetNode<GameState>("/root/GameState");
        if (state.RelicIds.Count == 0)
        {
            return "Relics: None";
        }

        var names = state.RelicIds.Select(id => RelicData.CreateById(id).Name);
        return $"Relics: {string.Join(" | ", names)}";
    }

    private void RefreshRelicIcons()
    {
        var state = GetNode<GameState>("/root/GameState");
        var signature = string.Join("|", state.RelicIds);
        if (signature == _relicUiSignature)
        {
            return;
        }

        _relicUiSignature = signature;

        foreach (Node child in _relicIcons.GetChildren())
        {
            child.QueueFree();
        }
        _relicChipById.Clear();
        _relicTriggerDotById.Clear();
        foreach (var relicId in state.RelicIds)
        {
            var relic = RelicData.CreateById(relicId);
            var chip = new PanelContainer
            {
                CustomMinimumSize = new Vector2(48, 28),
                TooltipText = relic.ToRelicText()
            };
            var chipStyle = new StyleBoxFlat
            {
                BgColor = new Color("1b2936"),
                BorderColor = new Color("6ea0c8"),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6
            };
            chip.AddThemeStyleboxOverride("panel", chipStyle);

            var icon = new TextureRect
            {
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                Texture = LoadTextureCached(CombatVisualCatalog.GetRelicIconPath(relicId))
            };
            chip.AddChild(icon);

            var triggerDot = new ColorRect
            {
                Color = new Color("facc15"),
                CustomMinimumSize = new Vector2(8, 8),
                Size = new Vector2(8, 8),
                Visible = _triggeredRelicsThisTurn.Contains(relicId)
            };
            triggerDot.Position = new Vector2(38, 2);
            chip.AddChild(triggerDot);

            _relicIcons.AddChild(chip);
            _relicChipById[relicId] = chip;
            _relicTriggerDotById[relicId] = triggerDot;
        }
    }

    private void ShowKeywordTooltip(CardView card)
    {
        var lines = new List<string>();
        if (card.Card.Damage > 0)
        {
            lines.Add("[color=#fda4af]Damage[/color]: reduced by enemy Block.");
        }

        if (card.Card.Block > 0)
        {
            lines.Add("[color=#93c5fd]Block[/color]: prevents incoming damage this turn.");
        }

        if (card.Card.ApplyVulnerable > 0)
        {
            lines.Add("[color=#e9d5ff]Vulnerable[/color]: target takes 50% more damage.");
        }

        if (card.Card.DrawCount > 0)
        {
            lines.Add("[color=#a5f3fc]Draw[/color]: draw extra cards now.");
        }

        if (lines.Count == 0)
        {
            lines.Add("[color=#cbd5e1]No keywords.[/color]");
        }

        _keywordTooltipText.Text = string.Join("\n", lines);
        var pos = card.GlobalPosition + new Vector2(card.Size.X + 12f, -10f);
        var viewport = GetViewportRect().Size;
        var tipSize = _keywordTooltip.Size;
        if (tipSize.X < 10f || tipSize.Y < 10f)
        {
            tipSize = new Vector2(270f, 150f);
        }
        pos.X = Mathf.Clamp(pos.X, 8f, viewport.X - tipSize.X - 8f);
        pos.Y = Mathf.Clamp(pos.Y, 8f, viewport.Y - tipSize.Y - 8f);
        _keywordTooltip.Position = pos;
        _keywordTooltip.Visible = true;
    }

    private void HideKeywordTooltip()
    {
        _keywordTooltip.Visible = false;
    }

    private void FlashRelic(string relicId)
    {
        if (!_relicChipById.TryGetValue(relicId, out var chip) || !IsInstanceValid(chip))
        {
            return;
        }

        _triggeredRelicsThisTurn.Add(relicId);
        if (_relicTriggerDotById.TryGetValue(relicId, out var dot) && IsInstanceValid(dot))
        {
            dot.Visible = true;
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(chip, "modulate", new Color("fef08a"), 0.08f);
        tween.TweenProperty(chip, "modulate", Colors.White, 0.18f);
    }

    private void ClearRelicTurnMarkers()
    {
        if (_triggeredRelicsThisTurn.Count == 0)
        {
            return;
        }

        _triggeredRelicsThisTurn.Clear();
        foreach (var kv in _relicTriggerDotById)
        {
            if (IsInstanceValid(kv.Value))
            {
                kv.Value.Visible = false;
            }
        }
    }

    private Texture2D LoadTextureCached(string path)
    {
        if (_iconCache.TryGetValue(path, out var texture) && IsInstanceValid(texture))
        {
            return texture;
        }

        var loaded = GD.Load<Texture2D>(path);
        if (loaded != null)
        {
            _iconCache[path] = loaded;
            return loaded;
        }

        return GD.Load<Texture2D>("res://icon.svg");
    }

    private void Log(string line, string colorHex = "#cbd5e1")
    {
        _logText.AppendText($"[color={colorHex}]{line}[/color]\n");
        _logText.ScrollToLine(Math.Max(_logText.GetLineCount() - 1, 0));
    }

    private bool IsInputLocked()
    {
        return _battleEnded || _inputLockDepth > 0;
    }

    private void PushInputLock()
    {
        _inputLockDepth += 1;
        UpdateInputControls();
    }

    private void PopInputLock()
    {
        _inputLockDepth = Math.Max(0, _inputLockDepth - 1);
        UpdateInputControls();
    }

    private void UpdateInputControls()
    {
        _endTurnButton.Disabled = IsInputLocked();
        RefreshCardPlayableStates();
    }

    private void RefreshCardPlayableStates()
    {
        foreach (Node node in _handContainer.GetChildren())
        {
            if (node is CardView cardView)
            {
                cardView.SetPlayable(!IsInputLocked() && cardView.Card.Cost <= _energy);
            }
        }
    }

    private void RequestHandLayout(bool animate)
    {
        if (animate)
        {
            LayoutHandCards(true);
            return;
        }

        if (_deferredHandLayoutPending)
        {
            return;
        }

        _deferredHandLayoutPending = true;
        CallDeferred(nameof(DeferredHandLayout));
    }

    private void DeferredHandLayout()
    {
        _deferredHandLayoutPending = false;
        LayoutHandCards(false);
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
