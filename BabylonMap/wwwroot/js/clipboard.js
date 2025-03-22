export async function getClipboardImage(dotNetHelper) {
    try {
        // Diese Funktion sollte durch einen direkten Benutzerklick ausgelöst werden!
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

        const text = await navigator.clipboard.readText();
        if (text.startsWith("data:image")) {
            const base64String = text.split(',')[1];
            dotNetHelper.invokeMethodAsync('OnImagePasted', base64String);
        } else {
            console.warn("Kein Bild in der Zwischenablage gefunden.");
        }
    } catch (err) {
        console.error("Clipboard access error:", err);
    }
}
