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
    }
};