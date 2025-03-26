window.validateFileSize = (file, maxSize) => {
    if (file.size > maxSize) {
        return false; // Dateigröße überschreitet das Limit
    }
    return true; // Dateigröße ist innerhalb des Limits
};