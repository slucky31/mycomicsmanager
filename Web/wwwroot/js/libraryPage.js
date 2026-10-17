// Library page: a long press (touch) or a right click (mouse) on a book opens its actions, and the menus of a
// tablet open next to the button that shows them.
const LONG_PRESS_MS = 500;
const MOVE_TOLERANCE = 10;
const BOOK_SELECTOR = '[data-book-id]';

const handlers = new WeakMap();

function viewport() {
    return { viewportWidth: window.innerWidth, viewportHeight: window.innerHeight };
}

function rectAnchor(element) {
    const rect = element.getBoundingClientRect();
    return { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom, ...viewport() };
}

function pointAnchor(item, x, y) {
    // The context menu key of a keyboard gives no position: the menu opens next to the book.
    if (x === 0 && y === 0) {
        return rectAnchor(item);
    }
    return { left: x, top: y, right: x, bottom: y, ...viewport() };
}

export function anchorOf(elementId) {
    const element = document.getElementById(elementId);
    return element ? rectAnchor(element) : null;
}

export function attach(container, dotNetRef) {
    if (!container || handlers.has(container)) {
        return;
    }

    let timer = null;
    let start = null;
    // A long press is followed by a click on release: it must not open the book.
    let swallowClick = false;

    const bookOf = target => {
        const item = target.closest(BOOK_SELECTOR);
        return item && container.contains(item) ? item : null;
    };
    const cancel = () => {
        if (timer) {
            clearTimeout(timer);
            timer = null;
        }
    };
    const open = (item, x, y) => {
        dotNetRef.invokeMethodAsync('OpenBookActionsAt', item.dataset.bookId, pointAnchor(item, x, y))
            .catch(() => { /* circuit gone: nothing to open */ });
    };

    const onPointerDown = e => {
        swallowClick = false;
        if (e.pointerType === 'mouse' || e.target.closest('button')) {
            return;
        }
        const item = bookOf(e.target);
        if (!item) {
            return;
        }
        cancel();
        start = { x: e.clientX, y: e.clientY };
        timer = setTimeout(() => {
            timer = null;
            swallowClick = true;
            open(item, start.x, start.y);
        }, LONG_PRESS_MS);
    };
    const onPointerMove = e => {
        if (timer && Math.hypot(e.clientX - start.x, e.clientY - start.y) > MOVE_TOLERANCE) {
            cancel();
        }
    };
    const onClick = e => {
        if (swallowClick) {
            swallowClick = false;
            e.preventDefault();
            e.stopPropagation();
        }
    };
    const onContextMenu = e => {
        const item = bookOf(e.target);
        if (!item) {
            return;
        }
        e.preventDefault();
        // Android fires contextmenu on a long press too: open the actions once.
        if (swallowClick) {
            return;
        }
        if (timer) {
            cancel();
            swallowClick = true;
        }
        open(item, e.clientX, e.clientY);
    };

    const listeners = {
        pointerdown: onPointerDown,
        pointermove: onPointerMove,
        pointerup: cancel,
        pointercancel: cancel,
        scroll: cancel,
        click: onClick,
        contextmenu: onContextMenu
    };
    for (const [type, listener] of Object.entries(listeners)) {
        container.addEventListener(type, listener, type === 'click');
    }
    handlers.set(container, { listeners, cancel });
}

// Another library is shown in the same page (side bar): its books start at the top.
export function scrollToTop(container) {
    if (container) {
        container.scrollTop = 0;
    }
}

export function detach(container) {
    const state = container && handlers.get(container);
    if (!state) {
        return;
    }
    state.cancel();
    for (const [type, listener] of Object.entries(state.listeners)) {
        container.removeEventListener(type, listener, type === 'click');
    }
    handlers.delete(container);
}
