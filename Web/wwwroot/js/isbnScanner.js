var version = "0.21.3";
var script_url = "https://unpkg.com/@zxing/library@" + version + "/umd/index.min.js";

// Helper to load ZXing if not already loaded
async function ensureZXingLoaded() {
    if (!globalThis?.ZXing) {
      try {
          await new Promise((resolve, reject) => {
            var script = globalThis?.document?.createElement('script');
            script.src = script_url;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
          });
      } catch (e) {
        console.log(e);
      }
        
    }
}

var codeReader = null;
var video = null;
var scanning = false;

function init() {
    if (!codeReader && globalThis?.ZXing) {
        codeReader = new globalThis.ZXing.BrowserMultiFormatReader();
    }
}

export function stopScan() {
  scanning = false;
  if (codeReader) codeReader.reset();
  if (video && video.srcObject) {
    var stream = video.srcObject;
    stream.getTracks().forEach(function (track) { return track.stop() });
    video.srcObject = null;
  }
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

export async function startScan(videoElementId, dotNetObjectRef) {
    // Prevent concurrent scans
    if (scanning) {
        console.warn('Scan already in progress');
        return;
    }

    await ensureZXingLoaded();
    init();

    video = globalThis?.document?.getElementById(videoElementId);
    if (!video) throw new Error('Video element not found');
    scanning = true;

    try {
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
            stream.getTracks().forEach(function (track) { return track.stop(); });
            throw new Error('Video element removed or scan stopped during initialization');
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
                    console.log('ISBN extracted:', isbn);
                    var isValid = await dotNetObjectRef.invokeMethodAsync('ValidateIsbn', isbn);
                    if (isValid) {
                        console.log('Valid ISBN confirmed:', isbn);
                        stopScan();
                      dotNetObjectRef.invokeMethodAsync('OnIsbnScannedFromJsAsync', isbn);
                    } else {
                        console.log('ISBN checksum validation failed:', isbn);
                    }
                } else {
                    console.log('No valid ISBN format found in:', rawText);
                }
            }
            if (err && !(err instanceof globalThis.ZXing.NotFoundException)) {
                console.error('Barcode scan error:', err);
            }
        });
    } catch (error) {
        console.error('Failed to start barcode scanning:', error);
        scanning = false;
        dotNetObjectRef.invokeMethodAsync('OnScanErrorFromJs', error.message);
        throw error;
    }
}




