namespace Durak.Core;

/// <summary>
/// Card ranks used in Durak. The game uses a 36-card deck, so ranks start at Six.
/// Explicit numeric values let us compare ranks with plain <c>&lt;</c> / <c>&gt;</c>
/// and match the face value printed on the card.
/// </summary>
public enum Rank
{
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14,
}
