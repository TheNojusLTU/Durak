namespace Durak.Core;

/// <summary>
/// The 36-card Durak draw pile. Created full and ordered; call <see cref="Shuffle"/>
/// before dealing. The bottom card is revealed as the trump and is drawn last.
/// </summary>
public sealed class Deck
{
    // A Stack is the natural fit: cards are only ever taken from the top.
    private readonly Stack<Card> _cards;

    /// <summary>Creates a full, ordered 36-card deck.</summary>
    public Deck()
    {
        // LINQ: for every suit, pair it with every rank => 4 * 9 = 36 cards.
        IEnumerable<Card> allCards =
            from suit in Enum.GetValues<Suit>()
            from rank in Enum.GetValues<Rank>()
            select new Card(suit, rank);

        _cards = new Stack<Card>(allCards);
    }

    /// <summary>Number of cards still in the pile.</summary>
    public int Count => _cards.Count;

    /// <summary>True when there is nothing left to draw.</summary>
    public bool IsEmpty => _cards.Count == 0;

    /// <summary>
    /// The card at the bottom of the pile. In Durak it is turned face up after the
    /// shuffle: its suit is the trump suit for the whole game. It stays in the pile
    /// and is the very last card drawn. Null once the pile is empty.
    /// </summary>
    public Card? TrumpCard => _cards.Count == 0 ? null : _cards.Last();

    /// <summary>Trump suit for this game, or null once the pile is empty.</summary>
    public Suit? TrumpSuit => TrumpCard?.Suit;

    /// <summary>
    /// Randomises card order. The <paramref name="random"/> source is injected rather
    /// than created here so tests can pass a seeded <see cref="Random"/> and get the
    /// same shuffle every run (the course requires predictable tests).
    /// </summary>
    public void Shuffle(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        // Stack has no in-place shuffle, so copy out, shuffle, and rebuild.
        Card[] cards = _cards.ToArray();
        random.Shuffle(cards); // Fisher-Yates, built into .NET 8+

        _cards.Clear();
        foreach (Card card in cards)
        {
            _cards.Push(card);
        }
    }

    /// <summary>
    /// Takes up to <paramref name="count"/> cards from the top of the pile. Returns
    /// fewer when the pile runs out, which is normal late in a game.
    /// </summary>
    public IReadOnlyList<Card> Draw(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var drawn = new List<Card>(capacity: count);
        while (drawn.Count < count && _cards.Count > 0)
        {
            drawn.Add(_cards.Pop());
        }

        return drawn;
    }

    /// <summary>Read-only view of the remaining cards, top of the pile first.</summary>
    public IEnumerable<Card> RemainingCards => _cards;
}
