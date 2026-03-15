using System;
using System.Collections.Generic;

public readonly struct DeckDrawResult<T>
{
    public IReadOnlyList<T> DrawnCards { get; }
    public bool HandLimitReached { get; }
    public int ReshuffleCount { get; }

    public DeckDrawResult(IReadOnlyList<T> drawnCards, bool handLimitReached, int reshuffleCount)
    {
        DrawnCards = drawnCards;
        HandLimitReached = handLimitReached;
        ReshuffleCount = reshuffleCount;
    }
}

public static class DeckFlowResolver
{
    public static DeckDrawResult<T> DrawIntoHand<T>(
        IList<T> drawPile,
        IList<T> discardPile,
        IList<T> hand,
        int drawCount,
        int handLimit,
        Random rng)
    {
        var drawn = new List<T>();
        var handLimitReached = false;
        var reshuffleCount = 0;

        for (var i = 0; i < drawCount; i++)
        {
            if (hand.Count >= handLimit)
            {
                handLimitReached = true;
                break;
            }

            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    break;
                }

                for (var j = 0; j < discardPile.Count; j++)
                {
                    drawPile.Add(discardPile[j]);
                }
                discardPile.Clear();
                ShuffleInPlace(drawPile, rng);
                reshuffleCount++;
            }

            var card = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(card);
            drawn.Add(card);
        }

        return new DeckDrawResult<T>(drawn, handLimitReached, reshuffleCount);
    }

    public static void ShuffleInPlace<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
