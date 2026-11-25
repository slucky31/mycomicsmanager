// Back on the feeds (#1386): the card left is brought back on screen.
export function scrollToCard(id) {
    document.getElementById(id)?.scrollIntoView({ block: 'center' });
}
