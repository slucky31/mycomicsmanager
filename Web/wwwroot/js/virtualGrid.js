// Virtualized grids (#1344: the library, #1366: the series and the volumes of a series): the number of columns of the
// grid, followed as the screen is resized, and the first item on screen, kept for the next visit.
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
    // The page itself scrolls (the series pages): its scroll events reach the window.
    return null;
}

function firstVisibleItem(grid, scroller) {
    const top = scroller ? scroller.getBoundingClientRect().top : 0;
    for (const row of grid.querySelectorAll('[data-first-item]')) {
        if (row.getBoundingClientRect().bottom > top + 1) {
            return Number(row.dataset.firstItem);
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
    const target = scroller ?? window;
    const grid = probe.parentElement;
    let timer = null;
    const onScroll = () => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            const first = firstVisibleItem(grid, scroller);
            if (first !== null) {
                dotNetRef.invokeMethodAsync('PositionChanged', first).catch(() => { /* circuit gone */ });
            }
        }, POSITION_DELAY_MS);
    };
    target.addEventListener('scroll', onScroll, { passive: true });

    states.set(id, { resizeObserver, target, onScroll, stop: () => clearTimeout(timer) });
    return columns;
}

export function dispose(id) {
    const state = states.get(id);
    if (!state) {
        return;
    }
    state.stop();
    state.resizeObserver.disconnect();
    state.target.removeEventListener('scroll', state.onScroll);
    states.delete(id);
}
