// Keyboard and swipe navigation for the book reader.
const SWIPE_MIN_DISTANCE = 50;

let keyHandler = null;
let touchStartHandler = null;
let touchEndHandler = null;
let surface = null;

export function attach(element, dotNetObjectRef) {
    detach();
    surface = element;

    keyHandler = (event) => {
        if (event.key === "ArrowLeft" || event.key === "ArrowRight" || event.key === "Escape") {
            event.preventDefault();
            dotNetObjectRef.invokeMethodAsync("OnNavigationKey", event.key);
        }
    };

    let startX = 0;
    let startY = 0;
    touchStartHandler = (event) => {
        startX = event.changedTouches[0].clientX;
        startY = event.changedTouches[0].clientY;
    };
    touchEndHandler = (event) => {
        const deltaX = event.changedTouches[0].clientX - startX;
        const deltaY = event.changedTouches[0].clientY - startY;
        if (Math.abs(deltaX) >= SWIPE_MIN_DISTANCE && Math.abs(deltaX) > Math.abs(deltaY)) {
            dotNetObjectRef.invokeMethodAsync("OnNavigationKey", deltaX < 0 ? "ArrowRight" : "ArrowLeft");
        }
    };

    document.addEventListener("keydown", keyHandler);
    surface.addEventListener("touchstart", touchStartHandler, { passive: true });
    surface.addEventListener("touchend", touchEndHandler, { passive: true });
}

export function detach() {
    if (keyHandler) {
        document.removeEventListener("keydown", keyHandler);
    }
    if (surface && touchStartHandler) {
        surface.removeEventListener("touchstart", touchStartHandler);
        surface.removeEventListener("touchend", touchEndHandler);
    }
    keyHandler = null;
    touchStartHandler = null;
    touchEndHandler = null;
    surface = null;
}
