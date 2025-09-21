// ISBN Barcode Scanner using ZXing-js
window.isbnScanner = {
    video: null,
    canvas: null,
    context: null,
    codeReader: null,
    scanning: false,

    init: function() {
        // Load ZXing library if not already loaded
        if (!window.ZXing) {
            const script = document.createElement('script');
            script.src = 'https://unpkg.com/@zxing/library@latest/umd/index.min.js';
            script.onload = () => {
                this.codeReader = new ZXing.BrowserMultiFormatReader();
            };
            document.head.appendChild(script);
        } else {
            this.codeReader = new ZXing.BrowserMultiFormatReader();
        }
    },

    startScan: async function(videoElementId, dotNetObjectRef) {
        try {
            if (!this.codeReader) {
                this.init();
                // Wait for ZXing to load
                await new Promise(resolve => setTimeout(resolve, 1000));
            }

            this.video = document.getElementById(videoElementId);
            if (!this.video) {
                throw new Error('Video element not found');
            }

            this.scanning = true;

            // Get available video devices
            const videoInputDevices = await this.codeReader.listVideoInputDevices();
            if (videoInputDevices.length === 0) {
                throw new Error('No video input devices found');
            }

            // Start scanning with the first available camera
            const selectedDeviceId = videoInputDevices[0].deviceId;
            
            this.codeReader.decodeFromVideoDevice(selectedDeviceId, this.video, (result, err) => {
                if (result && this.scanning) {
                    // Found a barcode
                    const isbn = result.getText();
                    if (this.isValidISBN(isbn)) {
                        this.stopScan();
                        dotNetObjectRef.invokeMethodAsync('OnIsbnScannedFromJs', isbn);
                    }
                }
                if (err && !(err instanceof ZXing.NotFoundException)) {
                    console.error('Barcode scan error:', err);
                }
            });

        } catch (error) {
            console.error('Failed to start barcode scanning:', error);
            dotNetObjectRef.invokeMethodAsync('OnScanErrorFromJs', error.message);
        }
    },

    stopScan: function() {
        this.scanning = false;
        if (this.codeReader) {
            this.codeReader.reset();
        }
        if (this.video && this.video.srcObject) {
            const stream = this.video.srcObject;
            const tracks = stream.getTracks();
            tracks.forEach(track => track.stop());
            this.video.srcObject = null;
        }
    },

    isValidISBN: function(isbn) {
        if (!isbn) return false;
        const cleanIsbn = isbn.replace(/[-\s]/g, '');
        return /^\d{10}$/.test(cleanIsbn) || /^\d{13}$/.test(cleanIsbn);
    }
};