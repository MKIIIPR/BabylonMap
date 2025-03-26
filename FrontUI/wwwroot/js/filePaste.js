export function initializeFilePaste(fileDropContainer, inputFile) {

    fileDropContainer.addEventListener('paste', onPaste);

    function onPaste(event) {
        inputFile.files = event.clipboardData.files;
        const changeEvent = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(changeEvent);
    }

    return {
        dispose: () => {
            fileDropContainer.removeEventListener('paste', onPaste);
        }
    }

}
export async function getClipboardImage(dotNetHelper) {
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
