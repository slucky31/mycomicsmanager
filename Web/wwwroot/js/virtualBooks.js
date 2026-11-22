// Virtualized library (#1344): the number of columns of the grid, followed as the screen is resized, and the first
// book on screen, kept for the next visit.
const POSITION_DELAY_MS = 250;
const states = new Map();

function columnsOf(probe) {
    const grid = probe.firstElementChild;
    const tracks = grid ? getComputedStyle(grid).gridTemplateColumns : 'none';
    return tracks && tracks !== 'none' ? tracks.trim().split(/\s+/).length : 1;
}

function scrollerOf(element) {
    for (let node = element.parentElement; node; node = node.parentElement) {
        const overflow = getComputedStyle(node).overflowY;
        if (overflow === 'auto' || overflow === 'scroll') {
            return node;
        }
    }
    return document.scrollingElement;
}

function firstVisibleBook(scroller) {
    const top = scroller.getBoundingClientRect().top;
    for (const row of scroller.querySelectorAll('[data-first-book]')) {
        if (row.getBoundingClientRect().bottom > top + 1) {
            return Number(row.dataset.firstBook);
        }
    }
    return null;
}

// Returns the number of columns; dotNetRef is told when it changes and where the scroll stops.
export function observe(id, probe, dotNetRef) {
    dispose(id);
    if (!probe) {
        return 1;
    }

    let columns = columnsOf(probe);
    const resizeObserver = new ResizeObserver(() => {
        const next = columnsOf(probe);
        if (next !== columns) {
            columns = next;
            dotNetRef.invokeMethodAsync('ColumnsChanged', next).catch(() => { /* circuit gone */ });
        }
    });
    resizeObserver.observe(probe);

    const scroller = scrollerOf(probe);
    let timer = null;
    const onScroll = () => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            const first = firstVisibleBook(scroller);
            if (first !== null) {
                dotNetRef.invokeMethodAsync('PositionChanged', first).catch(() => { /* circuit gone */ });
            }
        }, POSITION_DELAY_MS);
    };
    scroller.addEventListener('scroll', onScroll, { passive: true });

    states.set(id, { resizeObserver, scroller, onScroll, stop: () => clearTimeout(timer) });
    return columns;
}

export function dispose(id) {
    const state = states.get(id);
    if (!state) {
        return;
    }
    state.stop();
    state.resizeObserver.disconnect();
    state.scroller.removeEventListener('scroll', state.onScroll);
    states.delete(id);
}
