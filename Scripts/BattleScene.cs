using Godot;
using System;
using System.Collections.Generic;

public partial class BattleScene : Control
{
    private const int MaxEnergy = 3;
    private const int HandLimit = 10;

    private readonly Random _rng = new();
    private readonly List<CardData> _drawPile = new();
    private readonly List<CardData> _discardPile = new();
    private readonly List<CardData> _hand = new();

    private Label _playerHpLabel = null!;
    private Label _playerBlockLabel = null!;
    private Label _enemyHpLabel = null!;
    private Label _enemyIntentLabel = null!;
    private Label _turnLabel = null!;
    private Label _energyLabel = null!;
    private Label _handCountLabel = null!;
    private RichTextLabel _logText = null!;
    private HBoxContainer _handContainer = null!;

    private int _turn = 1;
    private int _playerHp = 80;
    private int _playerBlock;
    private int _enemyHp = 65;
    private int _enemyIntentDamage;
    private int _energy;
    private bool _battleEnded;

    public override void _Ready()
    {
        _playerHpLabel = GetNode<Label>("%PlayerHpLabel");
        _playerBlockLabel = GetNode<Label>("%PlayerBlockLabel");
        _enemyHpLabel = GetNode<Label>("%EnemyHpLabel");
        _enemyIntentLabel = GetNode<Label>("%EnemyIntentLabel");
        _turnLabel = GetNode<Label>("%TurnLabel");
        _energyLabel = GetNode<Label>("%EnergyLabel");
        _handCountLabel = GetNode<Label>("%HandCountLabel");
        _logText = GetNode<RichTextLabel>("%LogText");
        _handContainer = GetNode<HBoxContainer>("%HandContainer");

        GetNode<Button>("%EndTurnButton").Pressed += EndTurn;
        GetNode<Button>("%RestartButton").Pressed += RestartBattle;
        GetNode<Button>("%BackButton").Pressed += BackToMenu;

        BuildStarterDeck();
        Shuffle(_drawPile);

        Log("Battle start.");
        StartPlayerTurn();
    }

    private void BuildStarterDeck()
    {
        for (var i = 0; i < 5; i++)
        {
            _drawPile.Add(CardData.Strike());
            _drawPile.Add(CardData.Defend());
        }

        _drawPile.Add(CardData.HeavySlash());
    }

    private void StartPlayerTurn()
    {
        if (_battleEnded)
        {
            return;
        }

        _energy = MaxEnergy;
        DrawCards(5);
        _enemyIntentDamage = _rng.Next(6, 13);
        Log($"Turn {_turn}: enemy plans {_enemyIntentDamage} damage.");

        RefreshUi();
    }

    private void EndTurn()
    {
        if (_battleEnded)
        {
            return;
        }

        foreach (var card in _hand)
        {
            _discardPile.Add(card);
        }
        _hand.Clear();

        var blocked = Math.Min(_playerBlock, _enemyIntentDamage);
        var damageTaken = _enemyIntentDamage - blocked;
        _playerBlock -= blocked;
        _playerHp -= damageTaken;
        _playerBlock = 0;

        Log($"Enemy attacks for {_enemyIntentDamage}. You block {blocked}, take {damageTaken}.");

        if (_playerHp <= 0)
        {
            _playerHp = 0;
            _battleEnded = true;
            Log("You are defeated.");
            RefreshUi();
            return;
        }

        _turn += 1;
        StartPlayerTurn();
    }

    private void DrawCards(int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (_hand.Count >= HandLimit)
            {
                Log("Hand is full.");
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
                Log("Shuffle discard into draw pile.");
            }

            var card = _drawPile[0];
            _drawPile.RemoveAt(0);
            _hand.Add(card);
        }

        RenderHand();
    }

    private void PlayCard(CardData card)
    {
        if (_battleEnded)
        {
            return;
        }

        if (!_hand.Contains(card))
        {
            return;
        }

        if (card.Cost > _energy)
        {
            Log($"Not enough energy for {card.Name}.");
            return;
        }

        _energy -= card.Cost;

        switch (card.Kind)
        {
            case CardKind.Attack:
                _enemyHp -= card.Damage;
                Log($"Play {card.Name}: deal {card.Damage} damage.");
                break;
            case CardKind.Skill:
                _playerBlock += card.Block;
                Log($"Play {card.Name}: gain {card.Block} Block.");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _hand.Remove(card);
        _discardPile.Add(card);

        if (_enemyHp <= 0)
        {
            _enemyHp = 0;
            _battleEnded = true;
            Log("Enemy defeated. Victory!");
        }

        RefreshUi();
    }

    private void RenderHand()
    {
        foreach (Node child in _handContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var card in _hand)
        {
            var button = new Button
            {
                Text = card.ToCardText(),
                CustomMinimumSize = new Vector2(170, 170),
                ClipText = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };

            button.Pressed += () => PlayCard(card);
            _handContainer.AddChild(button);
        }
    }

    private void RestartBattle()
    {
        GetTree().ReloadCurrentScene();
    }

    private void BackToMenu()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void RefreshUi()
    {
        _playerHpLabel.Text = $"HP: {_playerHp}";
        _playerBlockLabel.Text = $"Block: {_playerBlock}";
        _enemyHpLabel.Text = $"Enemy HP: {_enemyHp}";
        _enemyIntentLabel.Text = _battleEnded ? "Intent: -" : $"Intent: Attack {_enemyIntentDamage}";
        _turnLabel.Text = $"Turn: {_turn}";
        _energyLabel.Text = $"Energy: {_energy}/{MaxEnergy}";
        _handCountLabel.Text = $"Hand: {_hand.Count} | Draw: {_drawPile.Count} | Discard: {_discardPile.Count}";

        RenderHand();
    }

    private void Log(string line)
    {
        _logText.AppendText($"{line}\n");
        _logText.ScrollToLine(Math.Max(_logText.GetLineCount() - 1, 0));
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