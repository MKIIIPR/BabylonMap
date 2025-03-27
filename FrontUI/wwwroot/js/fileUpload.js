// Definiere die Funktion initializeFilePaste
function initializeFilePaste(fileDropContainer, inputFile) {
    fileDropContainer.addEventListener('paste', onPaste);

    function onPaste(event) {
        inputFile.files = event.clipboardData.files;
        const changeEvent = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(changeEvent);
    }

    return {
        dispose: function () {
            fileDropContainer.removeEventListener('paste', onPaste);
        }
    };
}

// Definiere die Funktion getClipboardImage
async function getClipboardImage(dotNetHelper) {
    try {
        const clipboardItems = await navigator.clipboard.read();

        for (const item of clipboardItems) {
            for (const type of item.types) {
                if (type.startsWith("image/")) {
                    const blob = await item.getType(type);
                    const reader = new FileReader();

                    reader.onload = function (event) {
                        const base64String = event.target.result.split(',')[1];
                        dotNetHelper.invokeMethodAsync('OnImagePasted', base64String);
                    };

                    reader.readAsDataURL(blob);
                    return;
                }
            }
        }
    } catch (err) {
        console.error("Clipboard access error:", err);
    }
}

// Definiere validateFileSize als normale Funktion
function validateFileSize(file, maxSize) {
    if (file.size > maxSize) {
        return false; // Dateigröße überschreitet das Limit
    }
    return true; // Dateigröße ist innerhalb des Limits
}

// Mache die Funktionen global verfügbar, indem du sie an das window-Objekt anhängst
window.initializeFilePaste = initializeFilePaste;
window.getClipboardImage = getClipboardImage;
window.validateFileSize = validateFileSize;