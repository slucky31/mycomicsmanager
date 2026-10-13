// The position of the button that opens a menu, so that a tablet opens the menu next to it.
export function anchorOf(elementId) {
    const element = document.getElementById(elementId);
    if (!element) {
        return null;
    }

    const rect = element.getBoundingClientRect();
    return {
        left: rect.left,
        top: rect.top,
        right: rect.right,
        bottom: rect.bottom,
        viewportWidth: window.innerWidth,
        viewportHeight: window.innerHeight
    };
}
