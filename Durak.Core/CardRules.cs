namespace Durak.Core;

public static class CardRules
{
    public static bool CanBeat(this Card defender, Card attacker, Suit trumpSuit)
    {
        ArgumentNullException.ThrowIfNull(defender);
        ArgumentNullException.ThrowIfNull(attacker);

        if (defender.Suit == attacker.Suit)
        {
            return defender.Rank > attacker.Rank;
        }

        // different suits - only a trump can beat it
        return defender.Suit == trumpSuit;
    }

    public static IEnumerable<Card> CardsThatBeat(this IEnumerable<Card> hand, Card attacker, Suit trumpSuit)
    {
        ArgumentNullException.ThrowIfNull(hand);
        ArgumentNullException.ThrowIfNull(attacker);

        return hand.Where(card => card.CanBeat(attacker, trumpSuit));
    }
}
