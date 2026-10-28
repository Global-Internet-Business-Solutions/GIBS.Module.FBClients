/* Module Script */
window.GIBS = window.GIBS || {};
var GIBS = window.GIBS;

GIBS.FBClients = GIBS.FBClients || {};

GIBS.FBClients._cameraStream = GIBS.FBClients._cameraStream || null;

GIBS.FBClients.shoppingLabelXml = '<?xml version="1.0" encoding="utf-8"?><DesktopLabel Version="1">  <DYMOLabel Version="3"><Description>DYMO Label</Description><Orientation>Landscape</Orientation><LabelName>Address</LabelName><InitialLength>0</InitialLength><BorderStyle>SolidLine</BorderStyle><DYMORect><DYMOPoint><X>0.23</X><Y>0.06</Y></DYMOPoint><Size><Width>3.21</Width><Height>0.9966666</Height></Size></DYMORect><BorderColor><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></BorderColor><BorderThickness>1</BorderThickness><Show_Border>False</Show_Border><DynamicLayoutManager><RotationBehavior>ClearObjects</RotationBehavior><LabelObjects><TextObject><Name>Line_1</Name><Brushes><BackgroundBrush><SolidColorBrush><Color A="0" R="1" G="1" B="1"></Color></SolidColorBrush></BackgroundBrush><BorderBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></BorderBrush><StrokeBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></StrokeBrush><FillBrush><SolidColorBrush>  <Color A="0" R="1" G="1" B="1"></Color></SolidColorBrush></FillBrush></Brushes><Rotation>Rotation0</Rotation><OutlineThickness>1</OutlineThickness><IsOutlined>False</IsOutlined><BorderStyle>SolidLine</BorderStyle><Margin><DYMOThickness Left="0" Top="0" Right="0" Bottom="0" /></Margin><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><FitMode>AlwaysFit</FitMode><IsVertical>False</IsVertical><FormattedText><FitMode>AlwaysFit</FitMode><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><IsVertical>False</IsVertical><LineTextSpan><TextSpan>  <Text>Enter Text</Text>  <FontInfo><FontName>Arial</FontName><FontSize>23.4</FontSize><IsBold>False</IsBold><IsItalic>False</IsItalic><IsUnderline>False</IsUnderline><FontBrush>  <SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color>  </SolidColorBrush></FontBrush>  </FontInfo></TextSpan></LineTextSpan></FormattedText><ObjectLayout><DYMOPoint><X>0.3867604</X><Y>0.0716153</Y></DYMOPoint><Size><Width>2.726479</Width><Height>0.3758107</Height></Size></ObjectLayout></TextObject><TextObject><Name>Line_2</Name><Brushes><BackgroundBrush><SolidColorBrush>  <Color A="0" R="0" G="0" B="0"></Color></SolidColorBrush></BackgroundBrush><BorderBrush><SolidColorBrush>  <Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></BorderBrush><StrokeBrush><SolidColorBrush>  <Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></StrokeBrush><FillBrush><SolidColorBrush>  <Color A="0" R="0" G="0" B="0"></Color></SolidColorBrush></FillBrush></Brushes><Rotation>Rotation0</Rotation><OutlineThickness>1</OutlineThickness><IsOutlined>False</IsOutlined><BorderStyle>SolidLine</BorderStyle><Margin><DYMOThickness Left="0" Top="0" Right="0" Bottom="0" /></Margin><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><FitMode>AlwaysFit</FitMode><IsVertical>False</IsVertical><FormattedText><FitMode>AlwaysFit</FitMode><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><IsVertical>False</IsVertical><LineTextSpan><TextSpan>  <Text>ABC</Text>  <FontInfo><FontName>Segoe UI</FontName><FontSize>19.3</FontSize><IsBold>False</IsBold><IsItalic>False</IsItalic><IsUnderline>False</IsUnderline><FontBrush>  <SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color>  </SolidColorBrush></FontBrush>  </FontInfo></TextSpan></LineTextSpan></FormattedText><ObjectLayout><DYMOPoint><X>0.2437118</X><Y>0.3820833</Y></DYMOPoint><Size><Width>3.046935</Width><Height>0.3600001</Height></Size></ObjectLayout></TextObject><TextObject><Name>Line_3</Name><Brushes><BackgroundBrush><SolidColorBrush>  <Color A="0" R="0" G="0" B="0"></Color></SolidColorBrush></BackgroundBrush><BorderBrush><SolidColorBrush>  <Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></BorderBrush><StrokeBrush><SolidColorBrush>  <Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></StrokeBrush><FillBrush><SolidColorBrush>  <Color A="0" R="0" G="0" B="0"></Color></SolidColorBrush></FillBrush></Brushes><Rotation>Rotation0</Rotation><OutlineThickness>1</OutlineThickness><IsOutlined>False</IsOutlined><BorderStyle>SolidLine</BorderStyle><Margin><DYMOThickness Left="0" Top="0" Right="0" Bottom="0" /></Margin><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><FitMode>AlwaysFit</FitMode><IsVertical>False</IsVertical><FormattedText><FitMode>AlwaysFit</FitMode><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><IsVertical>False</IsVertical><LineTextSpan><TextSpan>  <Text>ABC</Text>  <FontInfo><FontName>Segoe UI</FontName><FontSize>21.2</FontSize><IsBold>False</IsBold><IsItalic>False</IsItalic><IsUnderline>False</IsUnderline><FontBrush>  <SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color>  </SolidColorBrush></FontBrush>  </FontInfo></TextSpan></LineTextSpan></FormattedText><ObjectLayout><DYMOPoint><X>0.5063462</X><Y>0.6558337</Y></DYMOPoint><Size><Width>2.438336</Width><Height>0.3979165</Height></Size></ObjectLayout></TextObject></LabelObjects></DynamicLayoutManager></DYMOLabel><LabelApplication>Blank</LabelApplication><DataTable><Columns></Columns><Rows></Rows></DataTable></DesktopLabel>';

GIBS.FBClients.barcodeLabelXml = '<?xml version="1.0" encoding="utf-8"?><DesktopLabel Version="1"><DYMOLabel Version="3"><Description>DYMO Label</Description><Orientation>Landscape</Orientation><LabelName>Address</LabelName><InitialLength>0</InitialLength><BorderStyle>SolidLine</BorderStyle><DYMORect><DYMOPoint><X>0.23</X><Y>0.06</Y></DYMOPoint><Size><Width>3.21</Width><Height>0.9966666</Height></Size></DYMORect><BorderColor><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></BorderColor><BorderThickness>1</BorderThickness><Show_Border>False</Show_Border><DynamicLayoutManager><RotationBehavior>ClearObjects</RotationBehavior><LabelObjects><TextObject><Name>TextObject0</Name><Brushes><BackgroundBrush><SolidColorBrush><Color A="0" R="0" G="0" B="0"></Color></SolidColorBrush></BackgroundBrush><BorderBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></BorderBrush><StrokeBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></StrokeBrush><FillBrush><SolidColorBrush><Color A="0" R="0" G="0" B="0"></Color></SolidColorBrush></FillBrush></Brushes><Rotation>Rotation0</Rotation><OutlineThickness>1</OutlineThickness><IsOutlined>False</IsOutlined><BorderStyle>SolidLine</BorderStyle><Margin><DYMOThickness Left="0" Top="0" Right="0" Bottom="0" /></Margin><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><FitMode>AlwaysFit</FitMode><IsVertical>False</IsVertical><FormattedText><FitMode>AlwaysFit</FitMode><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><IsVertical>False</IsVertical><LineTextSpan><TextSpan><Text>ABC</Text><FontInfo><FontName>Segoe UI</FontName><FontSize>25.1</FontSize><IsBold>False</IsBold><IsItalic>False</IsItalic><IsUnderline>False</IsUnderline><FontBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></FontBrush></FontInfo></TextSpan></LineTextSpan></FormattedText><ObjectLayout><DYMOPoint><X>0.25</X><Y>0.06041637</Y></DYMOPoint><Size><Width>3.006255</Width><Height>0.4683344</Height></Size></ObjectLayout></TextObject><BarcodeObject><Name>BarcodeObject0</Name><Brushes><BackgroundBrush><SolidColorBrush><Color A="1" R="1" G="1" B="1"></Color></SolidColorBrush></BackgroundBrush><BorderBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></BorderBrush><StrokeBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></StrokeBrush><FillBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></FillBrush></Brushes><Rotation>Rotation0</Rotation><OutlineThickness>1</OutlineThickness><IsOutlined>False</IsOutlined><BorderStyle>SolidLine</BorderStyle><Margin><DYMOThickness Left="0" Top="0" Right="0" Bottom="0" /></Margin><BarcodeFormat>Code128Auto</BarcodeFormat><Data><DataString>123</DataString></Data><HorizontalAlignment>Center</HorizontalAlignment><VerticalAlignment>Middle</VerticalAlignment><Size>SmallMedium</Size><TextPosition>Bottom</TextPosition><FontInfo><FontName>Arial</FontName><FontSize>12</FontSize><IsBold>False</IsBold><IsItalic>False</IsItalic><IsUnderline>False</IsUnderline><FontBrush><SolidColorBrush><Color A="1" R="0" G="0" B="0"></Color></SolidColorBrush></FontBrush></FontInfo><ObjectLayout><DYMOPoint><X>0.2595834</X><Y>0.5287508</Y></DYMOPoint><Size><Width>3.03293</Width><Height>0.527916</Height></Size></ObjectLayout></BarcodeObject></LabelObjects></DynamicLayoutManager></DYMOLabel><LabelApplication>Blank</LabelApplication><DataTable><Columns></Columns><Rows></Rows></DataTable></DesktopLabel>';

GIBS.FBClients.startCamera = function (videoElementId, httpsMessageId, noDeviceMessageId, noSupportMessageId) {
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
};

GIBS.FBClients.captureSnapshot = function (videoElementId, canvasElementId) {
    var stream = document.getElementById(videoElementId);
    var capture = document.getElementById(canvasElementId);
    if (!stream || !capture) {
        return '';
    }

    var ctx = capture.getContext('2d');
    ctx.drawImage(stream, 0, 0, capture.width, capture.height);
    return capture.toDataURL('image/png');
};

GIBS.FBClients.stopCamera = function (videoElementId) {
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
};

GIBS.FBClients._patchDymoXmlSerializer = function () {
    if (typeof dymo !== 'undefined' && dymo.xml && typeof dymo.xml.serialize === 'function' && !dymo.xml._gibsPatched) {
        dymo.xml.serialize = function (doc) {
            var serialized = new XMLSerializer().serializeToString(doc);
            return serialized.replace(/<Color ([^>]+)\/>/g, '<Color $1> </Color>');
        };
        dymo.xml._gibsPatched = true;
    }
};

GIBS.FBClients.ensureDymoLoaded = function () {
    if (typeof dymo !== 'undefined' && dymo.label && dymo.label.framework) {
        GIBS.FBClients._patchDymoXmlSerializer();
        return Promise.resolve();
    }

    return new Promise(function (resolve, reject) {
        var existing = document.querySelector('script[src*="dymo.connect.framework.js"]');
        if (existing) {
            var attempts = 0;
            var checkInterval = setInterval(function () {
                attempts++;
                if (typeof dymo !== 'undefined' && dymo.label && dymo.label.framework) {
                    clearInterval(checkInterval);
                    GIBS.FBClients._patchDymoXmlSerializer();
                    resolve();
                } else if (attempts >= 50) {
                    clearInterval(checkInterval);
                    reject(new Error('Timed out waiting for DYMO Connect Framework script to initialize.'));
                }
            }, 100);
            return;
        }

        var script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = '/_content/GIBS.Module.FBClients/dymo.connect.framework.js';
        script.setAttribute('data-gibs-dymo', 'true');
        script.onload = function () {
            GIBS.FBClients._patchDymoXmlSerializer();
            resolve();
        };
        script.onerror = function () {
            reject(new Error('Failed to load DYMO Connect Framework script.'));
        };
        document.head.appendChild(script);
    });
};

GIBS.FBClients.printShoppingLabel = async function (line1, line2, line3, howmanycopies, xmlLabel) {
    try {
        await GIBS.FBClients.ensureDymoLoaded();
        var printers = dymo.label.framework.getLabelWriterPrinters();
        if (!printers || printers.length === 0) {
            return 'No DYMO LabelWriter printers were found. Please ensure DYMO Connect is running and your printer is connected.';
        }

        var printerName = printers[0].name;
        var labelXml = (xmlLabel || GIBS.FBClients.shoppingLabelXml || '').toString();
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
};

GIBS.FBClients.printBarCodeLabel = async function (line1, barcodenumber, howmanycopies, xmlLabel) {
    try {
        await GIBS.FBClients.ensureDymoLoaded();
        var printers = dymo.label.framework.getLabelWriterPrinters();
        if (!printers || printers.length === 0) {
            return 'No DYMO LabelWriter printers were found. Please ensure DYMO Connect is running and your printer is connected.';
        }

        var printerName = printers[0].name;
        var labelXml = (xmlLabel || GIBS.FBClients.barcodeLabelXml || '').toString();
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
};

window.printLabel = function (line1, line2, line3, howmanycopies, xmlLabel) {
    return GIBS.FBClients.printShoppingLabel(line1, line2, line3, howmanycopies, xmlLabel);
};

window.printBarCodeLabel = function (line1, barcodenumber, howmanycopies, xmlLabel) {
    return GIBS.FBClients.printBarCodeLabel(line1, barcodenumber, howmanycopies, xmlLabel);
};