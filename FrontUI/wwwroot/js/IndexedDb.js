window.indexedDb = {
    dbName: "BabylonDb",
    dbVersion: 2, // Starten Sie mit einer Versionsnummer, erhöhen Sie sie, wenn Sie die Datenbankstruktur ändern

    // Funktion, um die Datenbank zu öffnen oder zu erstellen
    openDatabase: function (tableName) {
        return new Promise((resolve, reject) => {
            // Verwenden Sie die Versionsnummer beim Öffnen der Datenbank
            const request = indexedDB.open(this.dbName, this.dbVersion);

            request.onupgradeneeded = function (event) {
                const db = event.target.result;
                // Erstellen Sie den Object Store nur, wenn er noch nicht existiert
                if (!db.objectStoreNames.contains(tableName)) {
                    db.createObjectStore(tableName);
                }
            };

            request.onsuccess = function (event) {
                resolve(event.target.result);
            };

            request.onerror = function (event) {
                reject("Database error: " + event.target.errorCode);
            };
        });
    },

    // Funktion, um Daten in der angegebenen Tabelle zu speichern
    saveData: async function (tableName, data) {
        try {
            await removeNullProperties(data);

            const db = await this.openDatabase(tableName);
            const transaction = db.transaction([tableName], "readwrite");
            const store = transaction.objectStore(tableName);

            let putRequest = store.put(data, tableName);

            putRequest.onsuccess = function () {
                console.log("Daten erfolgreich in " + tableName + " gespeichert oder aktualisiert");
            };

            putRequest.onerror = function (event) {
                console.error("Fehler beim Speichern oder Aktualisieren der Daten in " + tableName + ": ", event.target.error);
            };
        } catch (error) {
            console.error("Fehler beim Öffnen der Datenbank: ", error);
        }
    },

    loadData: async function (tableName) {
        try {
            const db = await this.openDatabase(tableName);
            const transaction = db.transaction([tableName], "readonly");
            const store = transaction.objectStore(tableName);
            let getRequest = store.getAll();

            return new Promise((resolve, reject) => {
                getRequest.onsuccess = function () {
                    resolve(getRequest.result);
                };

                getRequest.onerror = function (event) {
                    reject("Fehler beim Laden der Daten aus " + tableName + ": ", event.target.error);
                };
            });
        } catch (error) {
            console.error("Fehler beim Öffnen der Datenbank: ", error);
            return Promise.reject(error);
        }
    }


};

async function removeNullProperties(obj) {
    for (const prop in obj) {
        if (obj[prop] === null || obj[prop] === undefined) {
            /*console.log(`Wert für ${prop} ist null oder undefiniert, wird entfernt.`);*/
            delete obj[prop];
        } else if (typeof obj[prop] === 'object') {
            // Rekursiver Aufruf für untergeordnete Objekte
            await removeNullProperties(obj[prop]);
        }
    }
}

window.indexedDbInit = {
    dbName: "BabylonDb",
    dbVersion: 2,
    initializeDatabase:  function (version) {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, version);

            request.onupgradeneeded = function (event) {
                const db = event.target.result;

                // Erstellen Sie hier die benötigten Object Stores
                const objectStores = ["AllRSIShips", "PersonalHangar", "AppStateChanges", "CCUChains", "Localization", "ShopContext", "ShopContent","life","ptu"];
                objectStores.forEach(storeName => {
                    if (!db.objectStoreNames.contains(storeName)) {
                        db.createObjectStore(storeName);
                    }
                });
                let localizationStore;
                if (!db.objectStoreNames.contains("Localization")) {
                    localizationStore = db.createObjectStore("Localization");
                } else {
                    localizationStore = event.target.transaction.objectStore("Localization");
                }

                // Initialisieren Sie die Lokalisierungsdaten für jede Sprache
                localizationStore.put({ greeting: "Hello", farewell: "Goodbye" }, "en");
                localizationStore.put({ greeting: "Hallo", farewell: "Auf Wiedersehen" }, "ger");
                localizationStore.put({ greeting: "Bonjour", farewell: "Au revoir" }, "fr");
                localizationStore.put({ greeting: "你好", farewell: "再见" }, "cn");
                localizationStore.put({ greeting: "Ciao", farewell: "Arrivederci" }, "it");
                localizationStore.put({ greeting: "Hello", farewell: "Goodbye" }, "en_ig");
                localizationStore.put({ greeting: "Hallo", farewell: "Auf Wiedersehen" }, "ger_ig");
                localizationStore.put({ greeting: "Bonjour", farewell: "Au revoir" }, "fr_ig");
                localizationStore.put({ greeting: "你好", farewell: "再见" }, "cn_ig");
                localizationStore.put({ greeting: "Ciao", farewell: "Arrivederci" }, "it_ig");

            };

            request.onsuccess = function (event) {
                console.log("Datenbank initialisiert mit Version " + version);
                resolve(event.target.result);
            };

            request.onerror = function (event) {
                console.error("Fehler beim Initialisieren der Datenbank: ", event.target.errorCode);
                reject(event.target.errorCode);
            };
        });
    },
    initializeLocalizationData: function () {
        const dbRequest = indexedDB.open("BabylonDb", 3);

        dbRequest.onupgradeneeded = function (event) {
            const db = event.target.result;
            const localizationStore = db.ObjectStore("Localization");

            // Initialisieren Sie die Lokalisierungsdaten für jede Sprache
            localizationStore.put({ /* Ihre englischen Lokalisierungsdaten */ }, "en");
            localizationStore.put({ /* Ihre deutschen Lokalisierungsdaten */ }, "ger");
            localizationStore.put({ /* Ihre französischen Lokalisierungsdaten */ }, "fr");
            localizationStore.put({ /* Ihre chinesischen Lokalisierungsdaten */ }, "cn");
            localizationStore.put({ /* Ihre italienischen Lokalisierungsdaten */ }, "it");
            localizationStore.put({ /* Ihre englischen Lokalisierungsdaten */ }, "en_ig");
            localizationStore.put({ /* Ihre deutschen Lokalisierungsdaten */ }, "ger_ig");
            localizationStore.put({ /* Ihre französischen Lokalisierungsdaten */ }, "fr_ig");
            localizationStore.put({ /* Ihre chinesischen Lokalisierungsdaten */ }, "cn_ig");
            localizationStore.put({ /* Ihre italienischen Lokalisierungsdaten */ }, "it_ig");
        };

        dbRequest.onerror = function (event) {
            console.log("Fehler beim Initialisieren der Lokalisierungsdaten", event);
        };
    }

}
// indexedInterop.js

window.indexedInterop = {
    saveData: function (tableName, data) {
        return new Promise((resolve, reject) => {
            const worker = new Worker('backgroundWorker.js');
            worker.postMessage({ action: 'saveData', tableName, data });

            worker.addEventListener('message', (event) => {
                if (event.data.status === 'success') {
                    resolve(event.data.message);
                } else {
                    reject(event.data.message);
                }
            });
        });
    },

    loadData: function (tableName, key) {
        return new Promise((resolve, reject) => {
            const worker = new Worker('pwa/js/workerDB.js');
            worker.postMessage({ action: 'loadData', tableName, key });

            worker.addEventListener('message', (event) => {
                if (event.data.status === 'success') {
                    resolve(event.data.data);
                } else {
                    reject(event.data.message);
                }
            });
        });
    }
};
