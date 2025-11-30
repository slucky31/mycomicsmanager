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
        const videoInputDevices = await codeReader.listVideoInputDevices();
        if (videoInputDevices.length === 0) throw new Error('No video input devices found');
        const selectedDeviceId = videoInputDevices[0].deviceId;

        codeReader.decodeFromVideoDevice(selectedDeviceId, video, (result, err) => {
            if (result && scanning) {
                const isbn = result.getText();
                if (isValidISBN(isbn)) {
                    stopScan();
                    dotNetObjectRef.invokeMethodAsync('OnIsbnScannedFromJs', isbn);
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

function isValidISBN(isbn) {
    if (!isbn) return false;
    const cleanIsbn = isbn.replace(/[-\s]/g, '');
    return /^\d{10}$/.test(cleanIsbn) || /^\d{13}$/.test(cleanIsbn);
}
