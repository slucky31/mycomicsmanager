var version = "0.21.3";
var script_url = "https://unpkg.com/@zxing/library@" + version + "/umd/index.min.js";

var codeReader = null;
var video = null;
var scanning = false;

// Helper functions defined first
function sanitizeInput(text) {
    if (!text) return '';
    return text.trim();
}

function extractISBN(text) {
    if (!text) return null;
    
    // Clean the text (remove spaces, dashes, prefixes)
    var cleaned = text.replace(/[-\s]/g, '');
    
    // Remove "ISBN" prefix if present
    cleaned = cleaned.replace(/^ISBN/i, '');
    
    // Extract ISBN-13 or ISBN-10 (without checksum validation)
    // Full validation will be done in C#
    var isbn13Match = cleaned.match(/(\d{13})/);
    var isbn10Match = cleaned.match(/(\d{9}[\dX])/i);
    
    if (isbn13Match) {
        return isbn13Match[1];
    }
    if (isbn10Match) {
        return isbn10Match[1].toUpperCase();
    }
    
    return null;
}

// Helper to load ZXing if not already loaded
async function ensureZXingLoaded() {
    if (!globalThis?.ZXing) {
        try {
            await new Promise((resolve, reject) => {
                var script = document.createElement('script');
                script.src = script_url;
                script.onload = resolve;
                script.onerror = (error) => {
                    console.error('[ISBN Scanner] Failed to load ZXing library', error);
                    reject(error);
                };
                document.head.appendChild(script);
            });
        } catch (e) {
            console.error('[ISBN Scanner] Error loading ZXing:', e);
            throw e;
        }
    }
}

export function stopScan() {
    scanning = false;
    if (codeReader) {
        codeReader.reset();
        codeReader = null;
    }
    if (video && video.srcObject) {
        var stream = video.srcObject;
        stream.getTracks().forEach(function (track) { 
            track.stop();
        });
        video.srcObject = null;
    }
}

export async function startScan(videoElementId, dotNetObjectRef) {
    // Prevent concurrent scans
    if (scanning) {
        console.warn('[ISBN Scanner] Scan already in progress');
        return;
    }

    try {
        await ensureZXingLoaded();
        
        // Always create a fresh code reader instance
        codeReader = new globalThis.ZXing.BrowserMultiFormatReader();

        video = document.getElementById(videoElementId);
        if (!video) {
            var error = 'Video element not found: ' + videoElementId;
            console.error('[ISBN Scanner]', error);
            throw new Error(error);
        }
        
        scanning = true;

        // Request rear camera explicitly via getUserMedia
        var stream = await navigator.mediaDevices.getUserMedia({
            video: { 
                facingMode: { ideal: 'environment' },
                width: { ideal: 1920 },
                height: { ideal: 1080 }
            }
        });
        
        // Re-verify video element is still in DOM and scanning hasn't been stopped
        if (!scanning || !video || !video.isConnected) {
            stream.getTracks().forEach(function (track) { track.stop(); });
            var error = 'Video element removed or scan stopped during initialization';
            console.error('[ISBN Scanner]', error);
            throw new Error(error);
        }

        video.srcObject = stream;
        await video.play();

        // Enable autofocus on video track if supported
        var videoTrack = stream.getVideoTracks()[0];
        var capabilities = videoTrack.getCapabilities();
        
        if (capabilities.focusMode && capabilities.focusMode.includes('continuous')) {
            await videoTrack.applyConstraints({
                advanced: [{ focusMode: 'continuous' }]
            });
        }

        codeReader.decodeFromStream(stream, video, async (result, err) => {
            if (result && scanning) {
                var rawText = sanitizeInput(result.getText());                                
                var isbn = extractISBN(rawText);
                
                if (isbn) {
                    try {
                        var isValid = await dotNetObjectRef.invokeMethodAsync('ValidateIsbn', isbn);
                        if (isValid) {
                            stopScan();
                            await dotNetObjectRef.invokeMethodAsync('OnIsbnScannedFromJsAsync', isbn);
                        }
                    } catch (validationError) {
                        console.error('[ISBN Scanner] Error during ISBN validation:', validationError);
                    }
                }
            }
            if (err && !(err instanceof globalThis.ZXing.NotFoundException)) {
                console.error('[ISBN Scanner] Barcode scan error:', err);
            }
        });
        
    } catch (error) {
        console.error('[ISBN Scanner] Failed to start barcode scanning:', error);
        scanning = false;
        try {
            await dotNetObjectRef.invokeMethodAsync('OnScanErrorFromJs', error.message);
        } catch (callbackError) {
            console.error('[ISBN Scanner] Failed to call OnScanErrorFromJs:', callbackError);
        }
        throw error;
    }
}




