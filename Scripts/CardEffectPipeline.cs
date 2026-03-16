using System;
using System.Collections.Generic;

public interface ICardEffectRuntime
{
    void ExecuteDamage(CardData card, CardEffectData effect);
    void ExecuteGainBlock(CardData card, CardEffectData effect);
    void ExecuteApplyVulnerable(CardData card, CardEffectData effect);
    void ExecuteGainStrength(CardData card, CardEffectData effect);
    void ExecuteGainEnergy(CardData card, CardEffectData effect);
    void ExecuteHeal(CardData card, CardEffectData effect);
}

public delegate void CardEffectHandler(
    CardData card,
    CardEffectData effect,
    CardEffectExecutionResult result,
    ICardEffectRuntime runtime);

public sealed class CardEffectExecutionResult
{
    public int DrawCount { get; private set; }

    public void AddDraw(int amount)
    {
        if (amount > 0)
        {
            DrawCount += amount;
        }
    }
}

public static class CardEffectPipeline
{
    private static readonly Dictionary<CardEffectType, CardEffectHandler> Handlers = new();

    static CardEffectPipeline()
    {
        RegisterOrReplaceHandler(CardEffectType.Damage, (card, effect, _, runtime) => runtime.ExecuteDamage(card, effect));
        RegisterOrReplaceHandler(CardEffectType.GainBlock, (card, effect, _, runtime) => runtime.ExecuteGainBlock(card, effect));
        RegisterOrReplaceHandler(CardEffectType.ApplyVulnerable, (card, effect, _, runtime) => runtime.ExecuteApplyVulnerable(card, effect));
        RegisterOrReplaceHandler(CardEffectType.GainStrength, (card, effect, _, runtime) => runtime.ExecuteGainStrength(card, effect));
        RegisterOrReplaceHandler(CardEffectType.GainEnergy, (card, effect, _, runtime) => runtime.ExecuteGainEnergy(card, effect));
        RegisterOrReplaceHandler(CardEffectType.Heal, (card, effect, _, runtime) => runtime.ExecuteHeal(card, effect));
        RegisterOrReplaceHandler(CardEffectType.DrawCards, (_, effect, result, _) => result.AddDraw(effect.Amount));
    }

    public static void RegisterOrReplaceHandler(CardEffectType effectType, CardEffectHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        Handlers[effectType] = handler;
    }

    public static CardEffectExecutionResult Execute(CardData card, ICardEffectRuntime runtime)
    {
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        var result = new CardEffectExecutionResult();

        foreach (var effect in card.Effects)
        {
            if (!Handlers.TryGetValue(effect.Type, out var handler))
            {
                throw new InvalidOperationException($"No handler registered for card effect type: {effect.Type}");
            }

            for (var i = 0; i < effect.Repeat; i++)
            {
                handler(card, effect, result, runtime);
            }
        }

        return result;
    }
}
