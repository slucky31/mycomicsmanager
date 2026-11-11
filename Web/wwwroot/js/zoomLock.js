// No zoom in the app: pinching is blocked everywhere but inside an element marked data-allow-zoom (the
// book reader, where pinching is the only way to read a small balloon). Double-tap zoom is turned off by
// touch-action in theme.css and the zoom on a focused field by maximum-scale in App.razor.
(function () {
    function isZoomAllowed(event) {
        // Already zoomed (pinched in the reader, then left it): pinching back out must stay possible.
        if (window.visualViewport && window.visualViewport.scale > 1) {
            return true;
        }
        var target = event.target;
        return target instanceof Element && target.closest("[data-allow-zoom]") !== null;
    }

    // Safari and the app added to the iPhone Home Screen ignore user-scalable=no: the pinch is stopped here.
    document.addEventListener("gesturestart", function (event) {
        if (!isZoomAllowed(event)) {
            event.preventDefault();
        }
    }, { passive: false });

    // Other browsers: a move with two fingers is a pinch.
    document.addEventListener("touchmove", function (event) {
        if (event.touches.length > 1 && !isZoomAllowed(event)) {
            event.preventDefault();
        }
    }, { passive: false });
})();
