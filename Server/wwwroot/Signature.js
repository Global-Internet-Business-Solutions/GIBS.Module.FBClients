window.GIBS = window.GIBS || {};
GIBS.FBClients = GIBS.FBClients || {};

GIBS.FBClients.signature = (function () {
    var sdkPromise;
    var session;

    function loadSdk() {
        if (typeof window.IsSigWebInstalled === 'function') {
            return Promise.resolve();
        }
        if (!sdkPromise) {
            sdkPromise = new Promise(function (resolve, reject) {
                var script = document.createElement('script');
                var timeout = setTimeout(function () {
                    script.remove();
                    sdkPromise = null;
                    reject(new Error('Timed out loading the Topaz SigWeb SDK. Check your internet connection and content security policy.'));
                }, 10000);
                script.src = 'https://www.sigplusweb.com/SigWebTablet.js';
                script.onload = function () {
                    clearTimeout(timeout);
                    if (typeof window.IsSigWebInstalled !== 'function') {
                        script.remove();
                        sdkPromise = null;
                        reject(new Error('The Topaz SigWeb SDK did not initialize.'));
                        return;
                    }
                    resolve();
                };
                script.onerror = function () {
                    clearTimeout(timeout);
                    script.remove();
                    sdkPromise = null;
                    reject(new Error('Unable to load the Topaz SigWeb SDK. Check your internet connection and content security policy.'));
                };
                document.head.appendChild(script);
            });
        }
        return sdkPromise;
    }

    function requireSession(canvas) {
        if (!session || session.canvas !== canvas) {
            throw new Error('Signature capture is no longer active. Start signing again.');
        }
        if (!IsSigWebInstalled()) {
            throw new Error('Unable to communicate with SigWeb. Check that the Topaz pad is connected and SigWeb is installed and running on this PC.');
        }
    }

    function stop(canvas) {
        if (!session || session.canvas !== canvas) {
            return;
        }
        var current = session;
        session = null;
        clearInterval(current.timer);
        current.observer.disconnect();
        window.removeEventListener('pagehide', current.onPageHide);
        SetTabletState(0, current.timer);
    }

    async function start(canvas) {
        await loadSdk();
        if (!canvas || !canvas.isConnected) {
            throw new Error('The signature canvas is no longer available.');
        }
        if (session) {
            throw new Error('Another signature capture is active. Finish or cancel it first.');
        }
        if (!IsSigWebInstalled()) {
            throw new Error('Unable to communicate with SigWeb. Check that the Topaz pad is connected and SigWeb is installed and running on this PC.');
        }
        var context = canvas.getContext('2d');
        if (!context) {
            throw new Error('Your browser does not support the signature canvas.');
        }
        SetTabletState(0);
        SetDisplayXSize(canvas.width);
        SetDisplayYSize(canvas.height);
        SetJustifyMode(0);
        ClearTablet();
        var current = { canvas: canvas };
        current.onPageHide = function () { stop(canvas); };
        current.observer = new MutationObserver(function () {
            if (!canvas.isConnected) {
                stop(canvas);
            }
        });
        session = current;
        try {
            current.timer = SetTabletState(1, context, 50);
            if (!current.timer) {
                throw new Error('SigWeb could not start the signature pad. Check the pad connection and try again.');
            }
            current.observer.observe(document.body, { childList: true, subtree: true });
            window.addEventListener('pagehide', current.onPageHide);
        } catch (error) {
            stop(canvas);
            throw error;
        }
    }

    function clear(canvas) {
        requireSession(canvas);
        ClearTablet();
        canvas.getContext('2d').clearRect(0, 0, canvas.width, canvas.height);
    }

    async function finish(canvas) {
        var timeout;
        try {
            requireSession(canvas);
            var points = Number(NumberOfTabletPoints());
            if (!Number.isFinite(points) || points <= 0) {
                throw new Error('Please sign before continuing.');
            }
            // Stop polling before exporting, but retain ownership until the callback completes.
            clearInterval(session.timer);
            SetTabletState(0, session.timer);
            SetImageXSize(canvas.width);
            SetImageYSize(canvas.height);
            SetImagePenWidth(5);
            return await new Promise(function (resolve, reject) {
                timeout = setTimeout(function () {
                    reject(new Error('Timed out reading the signature from SigWeb. Start signing again.'));
                }, 10000);
                GetSigImageB64(function (image) {
                    if (typeof image !== 'string' || !image.length) {
                        reject(new Error('SigWeb returned an empty signature image.'));
                        return;
                    }
                    resolve(image);
                });
            });
        } finally {
            clearTimeout(timeout);
            stop(canvas);
        }
    }

    return { start: start, clear: clear, finish: finish, stop: stop };
})();
