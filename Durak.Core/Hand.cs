using System.Collections;

namespace Durak.Core;

public class Hand : IEnumerable<Card>
{
    private readonly List<Card> _cards = new();

    public int Count => _cards.Count;

    public bool IsEmpty => _cards.Count == 0;

    public void Add(IEnumerable<Card> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        _cards.AddRange(cards);
    }

    public void Remove(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (!_cards.Remove(card))
        {
            throw new InvalidOperationException($"{card} is not in the hand.");
        }
    }

    public void Refill(Deck deck, int handSize = 6)
    {
        ArgumentNullException.ThrowIfNull(deck);

        var missing = handSize - _cards.Count;
        if (missing > 0)
        {
            Add(deck.Draw(missing));
        }
    }

    public IEnumerator<Card> GetEnumerator() => _cards.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
