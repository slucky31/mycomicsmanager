let version = '0.21.3';
let script_url = `https://unpkg.com/@zxing/library@${version}/umd/index.min.js`;

// Helper to load ZXing if not already loaded
async function ensureZXingLoaded() {
    if (!window.ZXing) {
        await new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = script_url;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }
}

let codeReader = null;
let video = null;
let scanning = false;

function init() {
    if (!codeReader && window.ZXing) {
        codeReader = new window.ZXing.BrowserMultiFormatReader();
    }
}

export async function startScan(videoElementId, dotNetObjectRef) {
    await ensureZXingLoaded();
    init();

    video = document.getElementById(videoElementId);
    if (!video) throw new Error('Video element not found');
    scanning = true;

    try {
        // Request rear camera explicitly via getUserMedia
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { 
                facingMode: { ideal: 'environment' },
                width: { ideal: 1920 },
                height: { ideal: 1080 }
            }
        });
        
        video.srcObject = stream;
        await video.play();

        // Enable autofocus on video track if supported
        const videoTrack = stream.getVideoTracks()[0];
        const capabilities = videoTrack.getCapabilities();
        
        if (capabilities.focusMode && capabilities.focusMode.includes('continuous')) {
            await videoTrack.applyConstraints({
                advanced: [{ focusMode: 'continuous' }]
            });
        }

        codeReader.decodeFromStream(stream, video, async (result, err) => {
            if (result && scanning) {
                const rawText = result.getText();
                console.log('Barcode detected:', rawText);
                
                const isbn = extractISBN(rawText);
                if (isbn) {
                    console.log('ISBN extracted:', isbn);
                    const isValid = await dotNetObjectRef.invokeMethodAsync('ValidateIsbn', isbn);
                    if (isValid) {
                        console.log('Valid ISBN confirmed:', isbn);
                        stopScan();
                        dotNetObjectRef.invokeMethodAsync('OnIsbnScannedFromJs', isbn);
                    } else {
                        console.log('ISBN checksum validation failed:', isbn);
                    }
                } else {
                    console.log('No valid ISBN format found in:', rawText);
                }
            }
            if (err && !(err instanceof window.ZXing.NotFoundException)) {
                console.error('Barcode scan error:', err);
            }
        });
    } catch (error) {
        console.error('Failed to start barcode scanning:', error);
        dotNetObjectRef.invokeMethodAsync('OnScanErrorFromJs', error.message);
        throw error;
    }
}

export function stopScan() {
    scanning = false;
    if (codeReader) codeReader.reset();
    if (video && video.srcObject) {
        const stream = video.srcObject;
        stream.getTracks().forEach(track => track.stop());
        video.srcObject = null;
    }
}

function extractISBN(text) {
    if (!text) return null;
    
    // Clean the text (remove spaces, dashes, prefixes)
    let cleaned = text.replace(/[-\s]/g, '');
    
    // Remove "ISBN" prefix if present
    cleaned = cleaned.replace(/^ISBN/i, '');
    
    // Extract ISBN-13 or ISBN-10 (without checksum validation)
    // Full validation will be done in C#
    const isbn13Match = cleaned.match(/(\d{13})/);
    const isbn10Match = cleaned.match(/(\d{9}[\dX])/i);
    
    if (isbn13Match) {
        return isbn13Match[1];
    }
    if (isbn10Match) {
        return isbn10Match[1].toUpperCase();
    }
    
    return null;
}
