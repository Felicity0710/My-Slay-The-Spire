using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CardBrowserFilterState
{
    public string SearchText { get; init; } = string.Empty;
    public CardKind? Kind { get; init; }
    public int? Cost { get; init; }
    public CardEffectType? EffectType { get; init; }
}

public static class CardBrowserFilter
{
    public static List<CardData> Apply(IEnumerable<CardData> cards, CardBrowserFilterState state)
    {
        var safeState = state ?? new CardBrowserFilterState();
        var query = cards;

        if (safeState.Kind.HasValue)
        {
            query = query.Where(card => card.Kind == safeState.Kind.Value);
        }

        if (safeState.Cost.HasValue)
        {
            query = query.Where(card => card.Cost == safeState.Cost.Value);
        }

        if (safeState.EffectType.HasValue)
        {
            query = query.Where(card => card.HasEffect(safeState.EffectType.Value));
        }

        if (!string.IsNullOrWhiteSpace(safeState.SearchText))
        {
            var keyword = safeState.SearchText.Trim();
            query = query.Where(card =>
                card.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                card.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                card.DescriptionZh.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderBy(card => card.Cost)
            .ThenBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
