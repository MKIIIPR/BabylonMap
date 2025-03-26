async function createZip(files) {
    const zip = new JSZip();

    for (const file of files) {
        const folder = zip.folder(file.folder);
        folder.file(file.name, file.content, { base64: true });
    }

    const blob = await zip.generateAsync({ type: "blob" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "tiles.zip";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

window.downloadTilesAsZip = createZip;
