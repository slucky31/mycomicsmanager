// Continuous vertical reader: taps, keyboard, visible page tracking and fullscreen.
const TAP_ZONE = 0.3;
// A tap moves one screen minus this overlap, so the end of the previous screen stays visible.
const PHONE_MAX_HEIGHT = 500;
const PHONE_OVERLAP = 40;
const TABLET_OVERLAP = 80;
// Unloaded pages use this height / width ratio (see .reader-page in BookReader.razor.css).
const PLACEHOLDER_RATIO = 1.5;
// The visible page is the one at this height of the screen.
const VISIBLE_PAGE_PROBE = 1 / 3;
const END_TOLERANCE = 2;

let state = null;

export function attach(scroller, dotNetObjectRef, initialPage) {
    detach();

    const current = {
        scroller,
        dotNetObjectRef,
        visiblePage: initialPage,
        atEnd: false,
        frame: 0,
    };

    scrollToPageOf(current, initialPage);
    current.atEnd = isAtEnd(scroller);

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
        if (position < TAP_ZONE) {
            scrollScreen(current, -1);
        } else if (position > 1 - TAP_ZONE) {
            scrollScreen(current, 1);
        } else {
            dotNetObjectRef.invokeMethodAsync("OnCenterTap");
        }
    };

    // A page loading above the screen grows or shrinks from its placeholder: the scroll position
    // follows, so the page being read does not move.
    current.onImageLoad = (event) => {
        const image = event.target;
        if (!(image instanceof HTMLImageElement)) {
            return;
        }
        const page = image.closest("[data-page]");
        const placeholderHeight = page.clientWidth * PLACEHOLDER_RATIO;
        if (page.offsetTop + placeholderHeight <= scroller.scrollTop) {
            scroller.scrollTop += page.offsetHeight - placeholderHeight;
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

    scroller.addEventListener("scroll", current.onScroll, { passive: true });
    scroller.addEventListener("click", current.onClick);
    scroller.addEventListener("load", current.onImageLoad, true);
    document.addEventListener("keydown", current.onKey);
    state = current;

    return document.fullscreenEnabled === true;
}

export function scrollToPage(pageIndex) {
    if (state) {
        scrollToPageOf(state, pageIndex);
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
    document.removeEventListener("keydown", current.onKey);
    if (document.fullscreenElement) {
        void document.exitFullscreen();
    }
}

// The page elements are read again at each use: Blazor may replace an element when it re-renders the pages.
function pagesOf(current) {
    return current.scroller.querySelectorAll("[data-page]");
}

function scrollToPageOf(current, pageIndex) {
    const page = pagesOf(current)[pageIndex];
    if (page) {
        current.scroller.scrollTop = page.offsetTop;
    }
}

function scrollScreen(current, direction) {
    const scroller = current.scroller;
    if (direction > 0 && isAtEnd(scroller)) {
        // Going further than the bottom of the last page finishes the book.
        current.dotNetObjectRef.invokeMethodAsync("OnReachedEnd");
        return;
    }
    const overlap = scroller.clientHeight <= PHONE_MAX_HEIGHT ? PHONE_OVERLAP : TABLET_OVERLAP;
    scroller.scrollBy({ top: direction * (scroller.clientHeight - overlap), behavior: "smooth" });
}

function isAtEnd(scroller) {
    return scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - END_TOLERANCE;
}

function reportPosition(current) {
    const visiblePage = findVisiblePage(current);
    if (visiblePage !== current.visiblePage) {
        current.visiblePage = visiblePage;
        current.dotNetObjectRef.invokeMethodAsync("OnVisiblePageChanged", visiblePage);
    }

    const atEnd = isAtEnd(current.scroller);
    if (atEnd && !current.atEnd) {
        current.dotNetObjectRef.invokeMethodAsync("OnReachedEnd");
    }
    current.atEnd = atEnd;
}

// The pages are in order: the last one starting above the probe line is the visible one.
// At the bottom of the book, a short last page may never reach that line: it is still the one read.
function findVisiblePage(current) {
    const pages = pagesOf(current);
    if (isAtEnd(current.scroller)) {
        return pages.length - 1;
    }
    const probe = current.scroller.scrollTop + current.scroller.clientHeight * VISIBLE_PAGE_PROBE;
    let low = 0;
    let high = pages.length - 1;
    while (low < high) {
        const middle = Math.ceil((low + high) / 2);
        if (pages[middle].offsetTop <= probe) {
            low = middle;
        } else {
            high = middle - 1;
        }
    }
    return low;
}
