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
        // Demander explicitement la caméra arrière via getUserMedia
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { 
                facingMode: { ideal: 'environment' },
                width: { ideal: 1920 },
                height: { ideal: 1080 }
            }
        });
        
        video.srcObject = stream;
        await video.play();

        // Activer l'autofocus sur la piste vidéo si supporté
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
                    // Valider avec le validateur C# côté serveur
                    const isValid = await dotNetObjectRef.invokeMethodAsync('ValidateIsbn', isbn);
                    if (isValid) {
                        console.log('Valid ISBN found:', isbn);
                        stopScan();
                        dotNetObjectRef.invokeMethodAsync('OnIsbnScannedFromJs', isbn);
                    } else {
                        console.log('ISBN format valid but failed server validation:', isbn);
                    }
                } else {
                    console.log('Not a valid ISBN format:', rawText);
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
    
    // Nettoyer le texte (enlever espaces, tirets, préfixes)
    let cleaned = text.replace(/[-\s]/g, '');
    
    // Enlever le préfixe "ISBN" si présent
    cleaned = cleaned.replace(/^ISBN/i, '');
    
    // Vérifier si c'est un ISBN-10 ou ISBN-13 valide
    const isbn13Match = cleaned.match(/(\d{13})/);
    const isbn10Match = cleaned.match(/(\d{10})/);
    
    if (isbn13Match) {
        return isbn13Match[1];
    }
    if (isbn10Match) {
        return isbn10Match[1];
    }
    
    return null;
}
