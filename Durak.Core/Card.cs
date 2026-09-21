namespace Durak.Core;

/// <summary>
/// A single playing card. Declared as a <c>record</c> so it is immutable and two cards
/// with the same suit and rank are considered equal (value semantics) — exactly what
/// we want when checking "is this card already on the table?".
/// </summary>
/// <param name="Suit">The card's suit.</param>
/// <param name="Rank">The card's rank.</param>
public sealed record Card(Suit Suit, Rank Rank) : IComparable<Card>
{
    /// <summary>
    /// Orders cards by rank, then by suit. This is a plain ordering for sorting a hand;
    /// it deliberately knows nothing about trumps — that is a game rule, not a card
    /// property, and belongs in the game logic that knows the current trump suit.
    /// </summary>
    public int CompareTo(Card? other)
    {
        if (other is null)
        {
            return 1; // by convention, any value sorts after null
        }

        int byRank = Rank.CompareTo(other.Rank);
        return byRank != 0 ? byRank : Suit.CompareTo(other.Suit);
    }

    public override string ToString() => $"{Rank} of {Suit}";
}
