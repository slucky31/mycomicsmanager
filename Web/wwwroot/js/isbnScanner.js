// ISBN Barcode Scanner using ZXing-js
(function() {
    'use strict';
    
    // Guard against SSR
    if (typeof window === 'undefined') {
        return;
    }

    window.isbnScanner = {
        video: null,
        canvas: null,
        context: null,
        codeReader: null,
        scanning: false,

        init: function() {
            // Load ZXing library if not already loaded
            if (typeof window.ZXing === 'undefined') {
                var script = document.createElement('script');
                script.src = 'https://unpkg.com/@zxing/library@latest/umd/index.min.js';
                var self = this;
                script.onload = function() {
                    self.codeReader = new window.ZXing.BrowserMultiFormatReader();
                };
                document.head.appendChild(script);
            } else {
                this.codeReader = new window.ZXing.BrowserMultiFormatReader();
            }
        },

        startScan: function(videoElementId, dotNetObjectRef) {
            var self = this;
            
            return new Promise(function(resolve, reject) {
                try {
                    if (!self.codeReader) {
                        self.init();
                        // Wait for ZXing to load
                        setTimeout(function() {
                            self.startScanInternal(videoElementId, dotNetObjectRef);
                            resolve();
                        }, 1000);
                    } else {
                        self.startScanInternal(videoElementId, dotNetObjectRef);
                        resolve();
                    }
                } catch (error) {
                    console.error('Failed to start barcode scanning:', error);
                    dotNetObjectRef.invokeMethodAsync('OnScanErrorFromJs', error.message);
                    reject(error);
                }
            });
        },

        startScanInternal: function(videoElementId, dotNetObjectRef) {
            var self = this;
            
            this.video = document.getElementById(videoElementId);
            if (!this.video) {
                throw new Error('Video element not found');
            }

            this.scanning = true;

            // Get available video devices
            this.codeReader.listVideoInputDevices().then(function(videoInputDevices) {
                if (videoInputDevices.length === 0) {
                    throw new Error('No video input devices found');
                }

                // Start scanning with the first available camera
                var selectedDeviceId = videoInputDevices[0].deviceId;
                
                self.codeReader.decodeFromVideoDevice(selectedDeviceId, self.video, function(result, err) {
                    if (result && self.scanning) {
                        // Found a barcode
                        var isbn = result.getText();
                        if (self.isValidISBN(isbn)) {
                            self.stopScan();
                            dotNetObjectRef.invokeMethodAsync('OnIsbnScannedFromJs', isbn);
                        }
                    }
                    if (err && !(err instanceof window.ZXing.NotFoundException)) {
                        console.error('Barcode scan error:', err);
                    }
                });
            }).catch(function(error) {
                console.error('Failed to list video devices:', error);
                dotNetObjectRef.invokeMethodAsync('OnScanErrorFromJs', error.message);
            });
        },

        stopScan: function() {
            this.scanning = false;
            if (this.codeReader) {
                this.codeReader.reset();
            }
            if (this.video && this.video.srcObject) {
                var stream = this.video.srcObject;
                var tracks = stream.getTracks();
                tracks.forEach(function(track) {
                    track.stop();
                });
                this.video.srcObject = null;
            }
        },

        isValidISBN: function(isbn) {
            if (!isbn) {
                return false;
            }
            var cleanIsbn = isbn.replace(/[-\s]/g, '');
            return /^\d{10}$/.test(cleanIsbn) || /^\d{13}$/.test(cleanIsbn);
        }
    };
})();