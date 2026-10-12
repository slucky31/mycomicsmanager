// Book reader: taps, swipes, keyboard, visible row tracking, fullscreen and the gesture help.
// A row ([data-row]) is one page, or two pages side by side in the "Two pages" layout.
const TAP_ZONE = 0.3;
// A tap moves one screen minus this overlap, so the end of the previous screen stays visible.
const PHONE_MAX_HEIGHT = 500;
const PHONE_OVERLAP = 40;
const TABLET_OVERLAP = 80;
// Unloaded pages use this height / width ratio (see .reader-page-pending in BookReader.razor.css).
const PLACEHOLDER_RATIO = 1.5;
// The visible row is the one at this height of the screen.
const VISIBLE_ROW_PROBE = 1 / 3;
const END_TOLERANCE = 2;
const SWIPE_MIN_DISTANCE = 50;
const GESTURE_HELP_KEY = "mcm.reader.gestureHelpSeen";

let state = null;

// options: { paged, rightToLeft, fitWidth, key }
export function attach(scroller, dotNetObjectRef, initialRow, options) {
    detach();

    const current = {
        scroller,
        dotNetObjectRef,
        options,
        visibleRow: initialRow,
        atEnd: false,
        frame: 0,
        touchStart: null,
        anchor: { row: initialRow, ratio: 0, width: 0 },
    };

    resetPosition(current, initialRow);

    current.onScroll = () => {
        if (current.frame === 0) {
            current.frame = requestAnimationFrame(() => {
                current.frame = 0;
                reportPosition(current);
            });
        }
    };

    current.onClick = (event) => {
        const position = event.clientX / window.innerWidth;
        if (position > TAP_ZONE && position < 1 - TAP_ZONE) {
            dotNetObjectRef.invokeMethodAsync("OnCenterTap");
            return;
        }
        // Mangas are read from right to left: the next page is on the left.
        const onRight = position >= 1 - TAP_ZONE;
        const forward = current.options.rightToLeft ? !onRight : onRight;
        scrollScreen(current, forward ? 1 : -1);
    };

    // A page loading above the screen grows or shrinks from its placeholder: the scroll position
    // follows, so the page being read does not move. Only pages fitted to the width change height.
    current.onImageLoad = (event) => {
        const image = event.target;
        if (!(image instanceof HTMLImageElement) || !current.options.fitWidth) {
            return;
        }
        const row = image.closest("[data-row]");
        const placeholderHeight = row.clientWidth * PLACEHOLDER_RATIO;
        if (row.offsetTop + placeholderHeight <= scroller.scrollTop) {
            scroller.scrollTop += row.offsetHeight - placeholderHeight;
        }
    };

    // Rotating the tablet changes the width, hence the height of every page: the same scroll offset
    // would land on another page, or past the end of the book once the pages get shorter.
    current.onResize = () => {
        if (isConfiguredForRows(current)) {
            restoreAnchorIfResized(current);
        }
    };

    current.onKey = (event) => {
        if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
            return;
        }
        if (event.key === " ") {
            event.preventDefault();
            scrollScreen(current, event.shiftKey ? -1 : 1);
        } else if (event.key === "f" || event.key === "F") {
            event.preventDefault();
            toggleFullscreen();
        } else if (event.key === "ArrowLeft" || event.key === "ArrowRight" || event.key === "Escape") {
            event.preventDefault();
            dotNetObjectRef.invokeMethodAsync("OnNavigationKey", event.key);
        }
    };

    // Page by page, a horizontal swipe turns the page (towards the left for mangas).
    current.onTouchStart = (event) => {
        current.touchStart = { x: event.changedTouches[0].clientX, y: event.changedTouches[0].clientY };
    };
    current.onTouchEnd = (event) => {
        const start = current.touchStart;
        current.touchStart = null;
        if (!start || !current.options.paged) {
            return;
        }
        const deltaX = event.changedTouches[0].clientX - start.x;
        const deltaY = event.changedTouches[0].clientY - start.y;
        if (Math.abs(deltaX) < SWIPE_MIN_DISTANCE || Math.abs(deltaX) <= Math.abs(deltaY)) {
            return;
        }
        const towardsLeft = deltaX < 0;
        const forward = current.options.rightToLeft ? !towardsLeft : towardsLeft;
        dotNetObjectRef.invokeMethodAsync("OnTurnPage", forward ? 1 : -1);
    };

    scroller.addEventListener("scroll", current.onScroll, { passive: true });
    scroller.addEventListener("click", current.onClick);
    scroller.addEventListener("load", current.onImageLoad, true);
    scroller.addEventListener("touchstart", current.onTouchStart, { passive: true });
    scroller.addEventListener("touchend", current.onTouchEnd, { passive: true });
    document.addEventListener("keydown", current.onKey);
    window.addEventListener("resize", current.onResize);
    state = current;

    return { canUseFullscreen: document.fullscreenEnabled === true, gestureHelpSeen: isGestureHelpSeen() };
}

// The display settings changed and the rows were rendered again: shows the given row.
export function configure(options, row) {
    if (state) {
        state.options = options;
        resetPosition(state, row);
    }
}

export function scrollToRow(row) {
    if (state) {
        scrollToRowOf(state, row);
    }
}

export function markGestureHelpSeen() {
    try {
        localStorage.setItem(GESTURE_HELP_KEY, "true");
    } catch {
        // Storage disabled (private browsing): the help shows again next time.
    }
}

export function toggleFullscreen() {
    if (!document.fullscreenEnabled) {
        return;
    }
    if (document.fullscreenElement) {
        void document.exitFullscreen();
    } else {
        void document.documentElement.requestFullscreen();
    }
}

export function detach() {
    if (!state) {
        return;
    }

    const current = state;
    state = null;
    cancelAnimationFrame(current.frame);
    current.scroller.removeEventListener("scroll", current.onScroll);
    current.scroller.removeEventListener("click", current.onClick);
    current.scroller.removeEventListener("load", current.onImageLoad, true);
    current.scroller.removeEventListener("touchstart", current.onTouchStart);
    current.scroller.removeEventListener("touchend", current.onTouchEnd);
    document.removeEventListener("keydown", current.onKey);
    window.removeEventListener("resize", current.onResize);
    if (document.fullscreenElement) {
        void document.exitFullscreen();
    }
}

function isGestureHelpSeen() {
    try {
        return localStorage.getItem(GESTURE_HELP_KEY) === "true";
    } catch {
        // Without storage the help would show at every opening: better not to show it at all.
        return true;
    }
}

function resetPosition(current, row) {
    scrollToRowOf(current, row);
    current.visibleRow = row;
    current.atEnd = isAtEnd(current.scroller);
    current.anchor = readAnchor(current);
}

function isConfiguredForRows(current) {
    return current.scroller.dataset.layout === current.options.key;
}

// The height of the pages follows their width: a new width means the scroll offsets changed.
function pagesWidth(current) {
    return current.scroller.firstElementChild?.clientWidth ?? current.scroller.clientWidth;
}

// The rows are read again at each use: Blazor may replace an element when it renders the rows again.
function rowsOf(current) {
    return current.scroller.querySelectorAll("[data-row]");
}

function rowElement(current, row) {
    return current.scroller.querySelector(`[data-row="${row}"]`);
}

function rowOf(element) {
    return Number(element.dataset.row);
}

function scrollToRowOf(current, row, behavior = "auto") {
    const element = rowElement(current, row);
    if (element) {
        current.scroller.scrollTo({ top: element.offsetTop, behavior });
    }
}

function scrollScreen(current, direction) {
    const scroller = current.scroller;
    const { paged, fitWidth } = current.options;

    if (!paged && !fitWidth) {
        // Each row fills the screen: a tap shows the next or previous one.
        scrollRow(current, direction);
        return;
    }
    if (direction > 0 && isAtEnd(scroller)) {
        // Past the bottom of the last page the book is finished; page by page, the next page shows.
        notifyEnd(current);
        return;
    }
    if (direction < 0 && scroller.scrollTop <= 0) {
        if (paged) {
            current.dotNetObjectRef.invokeMethodAsync("OnTurnPage", -1);
        }
        return;
    }
    const overlap = scroller.clientHeight <= PHONE_MAX_HEIGHT ? PHONE_OVERLAP : TABLET_OVERLAP;
    scroller.scrollBy({ top: direction * (scroller.clientHeight - overlap), behavior: "smooth" });
}

function scrollRow(current, direction) {
    const target = current.visibleRow + direction;
    if (target < 0) {
        return;
    }
    if (target >= rowsOf(current).length) {
        current.dotNetObjectRef.invokeMethodAsync("OnReachedEnd");
        return;
    }
    scrollToRowOf(current, target, "smooth");
}

function notifyEnd(current) {
    if (current.options.paged) {
        current.dotNetObjectRef.invokeMethodAsync("OnTurnPage", 1);
    } else {
        current.dotNetObjectRef.invokeMethodAsync("OnReachedEnd");
    }
}

function isAtEnd(scroller) {
    return scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - END_TOLERANCE;
}

function reportPosition(current) {
    // New settings were rendered but not yet sent to the script: the rows on screen changed under it
    // (switching to page by page leaves one row, scrolled to its bottom). It is not the reader moving.
    if (!isConfiguredForRows(current)) {
        return;
    }

    // Browsers may report the scroll caused by a rotation before the resize event:
    // it is not the reader moving, the page being read is put back instead.
    if (restoreAnchorIfResized(current)) {
        return;
    }
    current.anchor = readAnchor(current);

    // Page by page, the reader turns the pages itself: scrolling stays within the page shown.
    if (current.options.paged) {
        return;
    }

    const visibleRow = findVisibleRow(current);
    if (visibleRow !== current.visibleRow) {
        current.visibleRow = visibleRow;
        current.dotNetObjectRef.invokeMethodAsync("OnVisibleRowChanged", visibleRow);
    }

    const atEnd = isAtEnd(current.scroller);
    if (atEnd && !current.atEnd) {
        current.dotNetObjectRef.invokeMethodAsync("OnReachedEnd");
    }
    current.atEnd = atEnd;
}

// At the bottom of the book, a short last row may never reach the probe line: it is still the one read.
function findVisibleRow(current) {
    const rows = rowsOf(current);
    if (isAtEnd(current.scroller)) {
        return rowOf(rows[rows.length - 1]);
    }
    const probe = current.scroller.scrollTop + current.scroller.clientHeight * VISIBLE_ROW_PROBE;
    return rowOf(rows[rowIndexAt(rows, probe)]);
}

// The rows are in order: the last one starting above the line y.
function rowIndexAt(rows, y) {
    let low = 0;
    let high = rows.length - 1;
    while (low < high) {
        const middle = Math.ceil((low + high) / 2);
        if (rows[middle].offsetTop <= y) {
            low = middle;
        } else {
            high = middle - 1;
        }
    }
    return low;
}

// The row at the top of the screen, and how far into it the screen starts (0 = its top, 1 = its bottom).
function readAnchor(current) {
    const rows = rowsOf(current);
    if (rows.length === 0) {
        return current.anchor;
    }
    const top = current.scroller.scrollTop;
    const element = rows[rowIndexAt(rows, top)];
    const ratio = element.offsetHeight > 0 ? (top - element.offsetTop) / element.offsetHeight : 0;
    return { row: rowOf(element), ratio, width: pagesWidth(current) };
}

// Returns true when the width changed since the anchor was read, after putting its row back on screen.
function restoreAnchorIfResized(current) {
    const width = pagesWidth(current);
    if (width === current.anchor.width) {
        return false;
    }
    const element = rowElement(current, current.anchor.row);
    if (element) {
        current.scroller.scrollTop = element.offsetTop + current.anchor.ratio * element.offsetHeight;
    }
    current.anchor = { ...current.anchor, width };
    return true;
}
