/* Module Script */
var GIBS = GIBS || {};

GIBS.FBClients = {
    _cameraStream: null,

    startCamera: function (videoElementId, httpsMessageId, noDeviceMessageId, noSupportMessageId) {
        var player = document.getElementById(videoElementId);
        var httpsMessageBox = document.getElementById(httpsMessageId);
        var noDeviceMessageBox = document.getElementById(noDeviceMessageId);
        var noSupportMessageBox = document.getElementById(noSupportMessageId);

        if (!player) {
            return;
        }

        var isSecure = window.location.protocol === 'https:' ||
            window.location.hostname === 'localhost' ||
            window.location.hostname === '127.0.0.1';

        if (!isSecure) {
            if (httpsMessageBox) {
                httpsMessageBox.classList.remove('camera-hidden');
            }
            return;
        }

        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            navigator.mediaDevices.getUserMedia({ audio: false, video: true })
                .then(function (mediaStream) {
                    GIBS.FBClients._cameraStream = mediaStream;
                    player.classList.remove('camera-hidden');
                    if ('srcObject' in player) {
                        player.srcObject = mediaStream;
                    } else {
                        player.src = window.URL.createObjectURL(mediaStream);
                    }
                    player.onloadedmetadata = function () {
                        player.play();
                    };
                })
                .catch(function (err) {
                    console.error('Unable to access camera:', err);
                    if (noDeviceMessageBox) {
                        noDeviceMessageBox.classList.remove('camera-hidden');
                    }
                });
        } else {
            if (noSupportMessageBox) {
                noSupportMessageBox.classList.remove('camera-hidden');
            }
        }
    },

    captureSnapshot: function (videoElementId, canvasElementId) {
        var stream = document.getElementById(videoElementId);
        var capture = document.getElementById(canvasElementId);
        if (!stream || !capture) {
            return '';
        }

        var ctx = capture.getContext('2d');
        ctx.drawImage(stream, 0, 0, capture.width, capture.height);
        return capture.toDataURL('image/png');
    },

    stopCamera: function (videoElementId) {
        if (GIBS.FBClients._cameraStream) {
            var tracks = GIBS.FBClients._cameraStream.getTracks();
            for (var i = 0; i < tracks.length; i++) {
                tracks[i].stop();
            }
            GIBS.FBClients._cameraStream = null;
        }
        if (videoElementId) {
            var player = document.getElementById(videoElementId);
            if (player && 'srcObject' in player) {
                player.srcObject = null;
            }
        }
    },

    ensureDymoLoaded: function () {
        if (typeof dymo !== 'undefined' && dymo.label && dymo.label.framework) {
            return Promise.resolve();
        }

        return new Promise(function (resolve, reject) {
            var existing = document.querySelector('script[data-gibs-dymo="true"]');
            if (existing) {
                existing.addEventListener('load', function () { resolve(); });
                existing.addEventListener('error', function () { reject(new Error('Failed to load DYMO Connect Framework script.')); });
                return;
            }

            var script = document.createElement('script');
            script.type = 'text/javascript';
            script.src = '/_content/GIBS.Module.FBClients/dymo.connect.framework.js';
            script.setAttribute('data-gibs-dymo', 'true');
            script.onload = function () { resolve(); };
            script.onerror = function () { reject(new Error('Failed to load DYMO Connect Framework script.')); };
            document.head.appendChild(script);
        });
    },

    printShoppingLabel: async function (line1, line2, line3, howmanycopies, xmlLabel) {
        try {
            await GIBS.FBClients.ensureDymoLoaded();
            var printers = dymo.label.framework.getLabelWriterPrinters();
            if (!printers || printers.length === 0) {
                return 'No DYMO LabelWriter printers were found. Please ensure DYMO Connect is running and your printer is connected.';
            }

            var printerName = printers[0].name;
            var labelXml = (xmlLabel || '').toString();
            var label = dymo.label.framework.openLabelXml(labelXml);
            label.setObjectText('Line_1', (line1 || '').toString());
            label.setObjectText('Line_2', (line2 || '').toString());
            label.setObjectText('Line_3', (line3 || '').toString());

            var copies = parseInt(howmanycopies, 10);
            if (isNaN(copies) || copies < 1) {
                copies = 1;
            }
            var params = dymo.label.framework.createLabelWriterPrintParamsXml({ copies: copies });
            await dymo.label.framework.printLabel(printerName, params, label);
            console.log('Address label printed successfully!');
            return '';
        } catch (error) {
            console.error('Error printing label:', error);
            return error && error.message ? error.message : String(error);
        }
    },

    printBarCodeLabel: async function (line1, barcodenumber, howmanycopies, xmlLabel) {
        try {
            await GIBS.FBClients.ensureDymoLoaded();
            var printers = dymo.label.framework.getLabelWriterPrinters();
            if (!printers || printers.length === 0) {
                return 'No DYMO LabelWriter printers were found. Please ensure DYMO Connect is running and your printer is connected.';
            }

            var printerName = printers[0].name;
            var labelXml = (xmlLabel || '').toString();
            var label = dymo.label.framework.openLabelXml(labelXml);
            label.setObjectText('TextObject0', (line1 || '').toString());
            label.setObjectText('BarcodeObject0', (barcodenumber || '').toString());

            var copies = parseInt(howmanycopies, 10);
            if (isNaN(copies) || copies < 1) {
                copies = 1;
            }
            var params = dymo.label.framework.createLabelWriterPrintParamsXml({ copies: copies });
            await dymo.label.framework.printLabel(printerName, params, label);
            console.log('Barcode label printed successfully!');
            return '';
        } catch (error) {
            console.error('Error printing barcode label:', error);
            return error && error.message ? error.message : String(error);
        }
    }
};

window.printLabel = function (line1, line2, line3, howmanycopies, xmlLabel) {
    return GIBS.FBClients.printShoppingLabel(line1, line2, line3, howmanycopies, xmlLabel);
};

window.printBarCodeLabel = function (line1, barcodenumber, howmanycopies, xmlLabel) {
    return GIBS.FBClients.printBarCodeLabel(line1, barcodenumber, howmanycopies, xmlLabel);
};