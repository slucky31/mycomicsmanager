// Reads the text of a book page in the browser, so the ISBN printed on it can be extracted.
// The OCR engine (Tesseract.js) is only downloaded the first time a page is read, and the
// page never leaves the browser: only the recognized text is sent back to the server.
const TESSERACT_URL = "https://cdn.jsdelivr.net/npm/tesseract.js@7.0.0/dist/tesseract.min.js";

// Small print (the copyright notice) is only readable once enlarged. The cap keeps the
// canvas below the size limit of mobile browsers.
const MAX_UPSCALE = 2;
const MAX_LONG_SIDE = 4096;

let scriptPromise = null;
let workerPromise = null;

function loadTesseract() {
    if (globalThis.Tesseract) {
        return Promise.resolve();
    }

    scriptPromise ??= new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = TESSERACT_URL;
        script.onload = resolve;
        script.onerror = () => {
            // Allows a retry once the network is back.
            scriptPromise = null;
            script.remove();
            reject(new Error("Unable to load the OCR library"));
        };
        document.head.appendChild(script);
    });
    return scriptPromise;
}

function getWorker() {
    workerPromise ??= loadTesseract()
        .then(() => globalThis.Tesseract.createWorker("eng"))
        .catch((error) => {
            workerPromise = null;
            throw error;
        });
    return workerPromise;
}

function enlarge(image) {
    if (!image?.complete || image.naturalWidth === 0) {
        throw new Error("The page is not loaded yet");
    }

    const longSide = Math.max(image.naturalWidth, image.naturalHeight);
    const scale = Math.min(MAX_UPSCALE, MAX_LONG_SIDE / longSide);
    const canvas = document.createElement("canvas");
    canvas.width = Math.round(image.naturalWidth * scale);
    canvas.height = Math.round(image.naturalHeight * scale);

    const context = canvas.getContext("2d");
    context.imageSmoothingQuality = "high";
    context.drawImage(image, 0, 0, canvas.width, canvas.height);
    return canvas;
}

async function recognizeImage(image) {
    const canvas = enlarge(image);
    const worker = await getWorker();
    const { data } = await worker.recognize(canvas);
    return data.text ?? "";
}

// Reads the page displayed by the reader.
export function recognize(image) {
    return recognizeImage(image);
}

// Reads a page without displaying it (scan of the first and last pages).
export async function recognizeUrl(url) {
    const image = new Image();
    image.src = url;
    await image.decode();
    return recognizeImage(image);
}

export async function terminate() {
    const pending = workerPromise;
    workerPromise = null;
    if (!pending) {
        return;
    }

    try {
        const worker = await pending;
        await worker.terminate();
    } catch {
        // The worker never started: there is nothing to release.
    }
}
