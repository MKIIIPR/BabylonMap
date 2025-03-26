// backgroundWorker.js

const dbName = 'BabylonDb'; // Hier den gewünschten DB-Namen eintragen
const dbVersion = 2; // Hier die gewünschte DB-Version eintragen

self.addEventListener('message', async (event) => {
    const { action, tableName, data, key } = event.data;

    if (action === 'saveData') {
        try {
            await saveDataToIndexedDB(dbName, dbVersion, tableName, data, key);
            self.postMessage({ status: 'success', message: 'Daten erfolgreich gespeichert.' });
        } catch (error) {
            console.error(`Fehler beim Speichern der Daten: ${key}`, error);
            self.postMessage({ status: 'error', message: 'Fehler beim Speichern der Daten.' });
        }
    } else if (action === 'loadData') {
        try {
            if (!tableName) {
                throw new Error('Der Tabellenname fehlt.');
            }

            if (!key) {
                throw new Error('Der Schlüssel fehlt.');
            }

            const loadedData = await loadDataFromIndexedDB(dbName, dbVersion, tableName, key);

            if (!loadedData) {
                console.error(`Fehler beim Laden der Daten: ${key}`, 'Geladene Daten sind null oder undefiniert.');
                self.postMessage({ status: 'error', message: 'Fehler beim Laden der Daten.' });
                return;
            }

            console.log(`Daten erfolgreich geladen: ${key}`);
            self.postMessage({ status: 'success', message: 'Daten erfolgreich geladen.', data: loadedData });
        } catch (error) {
            console.error(`Fehler beim Laden der Daten: ${key}`, error);
            self.postMessage({ status: 'error', message: 'Fehler beim Laden der Daten.' });
        }
    }
});

async function saveDataToIndexedDB(dbName, dbVersion, tableName, data, key) {
    try {
        if (!dbName || !dbVersion) {
            throw new Error('DB-Name oder DB-Version fehlt.');
        }

        if (!tableName) {
            throw new Error('Der Tabellenname fehlt.');
        }

        if (!key) {
            throw new Error('Der Schlüssel fehlt.');
        }

        if (!data) {
            throw new Error('Die Daten fehlen.');
        }

        const db = await openIndexedDB(dbName, dbVersion);
        if (!db.objectStoreNames.contains(tableName)) {
            throw new Error(`Die Tabelle ${tableName} existiert nicht.`);
        }

        const transaction = db.transaction(tableName, 'readwrite');
        const store = transaction.objectStore(tableName);

        return new Promise((resolve, reject) => {
            const request = store.put(data, key);

            request.onsuccess = () => {
                console.log(`Daten erfolgreich gespeichert: ${key}`);
                resolve();
            };
            request.onerror = () => {
                console.error(`Fehler beim Speichern der Daten: ${key}`, request.error);
                reject(request.error);
            };
        });
    } catch (error) {
        console.error(`Fehler beim Speichern der Daten: ${key}`, error);
        throw error;
    }
}

async function loadDataFromIndexedDB(dbName, dbVersion, tableName, key) {
    try {
        if (!dbName || !dbVersion) {
            throw new Error('DB-Name oder DB-Version fehlt.');
        }

        if (!tableName) {
            throw new Error('Der Tabellenname fehlt.');
        }

        if (!key) {
            throw new Error('Der Schlüssel fehlt.');
        }

        const db = await openIndexedDB(dbName, dbVersion);
        if (!db.objectStoreNames.contains(tableName)) {
            throw new Error(`Die Tabelle ${tableName} existiert nicht.`);
        }

        const transaction = db.transaction(tableName, 'readonly');
        const store = transaction.objectStore(tableName);
        const request = store.get(key);

        return new Promise((resolve, reject) => {
            request.onsuccess = () => {
                const result = request.result;
                if (!result) {
                    console.error(`Fehler beim Laden der Daten: ${key}`, 'Geladene Daten sind null oder undefiniert.');
                    reject(new Error('Geladene Daten sind null oder undefiniert.'));
                    return;
                }
                resolve(result);
            };
            request.onerror = () => {
                console.error(`Fehler beim Laden der Daten: ${key}`, request.error);
                reject(request.error);
            };
        });
    } catch (error) {
        console.error(`Fehler beim Laden der Daten: ${key}`, error);
        throw error;
    }
}

async function openIndexedDB(dbName, dbVersion) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, dbVersion);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            // Hier könnten weitere Initialisierungen für verschiedene Tabellen erfolgen
        };

        request.onsuccess = (event) => {
            resolve(event.target.result);
        };

        request.onerror = () => {
            reject(request.error);
        };
    });
}
