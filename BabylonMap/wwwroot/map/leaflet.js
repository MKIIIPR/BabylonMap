var data = {
    mapSize: 1500000,
    tileSize: 512,
    transformB: 403,
    transformD: -6.5,
    scale: 1.252
};
let markers = [];
let activeTimers = {}
var map;

function initMap() {
    // Prüfe, ob das Container-DIV vorhanden ist
    const mapDiv = document.getElementById('map');
    if (!mapDiv) {
        console.error("initMap: Div mit der ID 'map' wurde nicht gefunden. Initialisierung wird abgebrochen.");
        return;
    } else {
        console.log("initMap: Div 'map' gefunden.");
    }

    // Karte initialisieren
    map = L.map('map', {
        crs: L.extend({}, L.CRS.Simple, {
            transformation: new L.Transformation(
                1 / (data.mapSize / data.tileSize),
                data.transformB,
                1 / (data.mapSize / data.tileSize),
                data.transformD
            ),
            scale: function (zoom) { return Math.pow(2, zoom) * data.scale; },
            zoom: function (zoom) { return Math.log(zoom / data.scale) / Math.LN2; }
        }),
        zoomControl: false,
        minZoom: 3,
        maxZoom: 6,
        zoom: 4,
        attributionControl: false,
        layers: [
            L.tileLayer('/map/{z}/{x}/{y}.png', { tileSize: data.tileSize })
        ]
    }).setView([207998, -879827]);
    console.log("initMap: Karte initialisiert.");


    // Attribution hinzufügen
    L.control.attribution({ prefix: false })
        .addAttribution('<a href="https://deine-seite.com">Deine Spielkarte</a>')
        .addTo(map);
    console.log("initMap: Attribution hinzugefügt.");

    
    // Event-Listener für das Kontextmenü (Rechtsklick)
    map.addEventListener('contextmenu', function (ev) {
        console.log("contextmenu-Event ausgelöst:", ev);

        const coordElem = document.getElementById('map-coordinates');
        if (coordElem) {
            coordElem.innerHTML =
                ev.latlng.lng.toFixed(0) + ', ' + ev.latlng.lat.toFixed(0);
            console.log("contextmenu: 'map-coordinates' aktualisiert.");
        } else {
            console.error("contextmenu: Element mit ID 'map-coordinates' nicht gefunden.");
        }

        // Sende die Koordinaten an Blazor, wenn verfügbar
        if (typeof DotNet !== "undefined" && DotNet.invokeMethodAsync) {
            DotNet.invokeMethodAsync('BabylonMap', 'UpdateCoordinates', ev.latlng.lat, ev.latlng.lng)
                .then(() => console.log("contextmenu: Koordinaten an Blazor gesendet."))
                .catch(err => console.error("contextmenu: Fehler beim Senden der Koordinaten an Blazor:", err));
        } else {
            console.error("contextmenu: DotNet.invokeMethodAsync ist nicht verfügbar.");
        }
    });
}

function addMarker(lat, lng, text) {
    if (!map) {
        console.error("addMarker: Karte ist nicht initialisiert. Marker kann nicht hinzugefügt werden.");
        return;
    }
    L.marker([lat, lng]).addTo(map)
        .bindPopup(text)
        .openPopup();
    console.log(`addMarker: Marker bei [${lat}, ${lng}] mit Text "${text}" hinzugefügt.`);
}

function CenterOnMap(lat, lng) {
    if (map && typeof map.setView === 'function') {
        map.setView([lat, lng], map.getZoom());
        console.log(`CenterOnMap: Karte auf [${lat}, ${lng}] zentriert.`);
    } else {
        console.error("CenterOnMap: Karte ist nicht initialisiert oder setView existiert nicht.");
    }
}

function removeMarker(id) {
    if (!map) {
        console.error("removeMarker: Karte ist nicht initialisiert.");
        return;
    }
    let markerObj = markers.find(m => m.id === id);
    if (markerObj) {
        map.removeLayer(markerObj.marker);
        markers = markers.filter(m => m.id !== id);
        console.log("removeMarker: Marker mit ID", id, "entfernt.");
    } else {
        console.warn("removeMarker: Kein Marker mit ID " + id + " gefunden.");
    }
}


function addCustomMarker(lat, lng, node, timeleft) {
    console.log("addCustomMarker aufgerufen:", { lat, lng, node });
    if (!node) {
        console.error("addCustomMarker: 'node' ist null oder undefined.");
        return;
    }
    if (!node.id) {
        console.error("addCustomMarker: node.id ist null oder undefined.");
        return;
    }
    if (!node.node || !node.node.name) {
        console.error("addCustomMarker: node.node.name fehlt.");
        return;
    }
    if (!map) {
        console.error("addCustomMarker: Karte ist nicht initialisiert.");
        return;
    }

    var imgUrl = node.node.nodeImageUrl|| "https://via.placeholder.com/50"; // Fallback-URL, falls kein Bild vorhanden ist

    var svgIcon =
        L.divIcon({
        className: "custom-icon",
        html: `
            <svg xmlns="http://www.w3.org/2000/svg" width="60" height="100" viewBox="0 0 60 100">
                <!-- Rechteck für den Timer oben -->
                <rect x="5" y="5" width="50" height="20" fill="black" stroke="white" stroke-width="2" rx="5"/>
                <text id="timer-${node.id}" x="30" y="20" text-anchor="middle" font-size="14" fill="white">10</text>
                
               

                <!-- Dreieck als Marker -->
                <polygon points="30,90 10,60 50,60" fill="#${node.rarity}" stroke="#${node.rarity}" stroke-width="2"/>

                 <!-- Rechteck für das Bild -->
                <rect x="10" y="30" width="40" height="40" fill="#${node.rarity}" stroke="#${node.rarity}" stroke-width="2" rx="5"/>
                <image x="10" y="32" width="40"  height="40" href="${imgUrl}" />
            </svg>`,
        iconSize: [60, 100],
        iconAnchor: [30, 90]
    });

    var marker = L.marker([lat, lng], { icon: svgIcon }).addTo(map)
        .bindPopup(node.node.name);
    markers.push({ id: node.id, marker: marker });
    console.log("addCustomMarker: Benutzerdefinierter Marker mit ID", node.id, "hinzugefügt.");

    // Starte den Timer für diesen Marker
    startTimer(node.id, timeleft);
}

//AddCustomMarkerOhneBild
///Ohnebild
//function addCustomMarker(lat, lng, node, timeleft) {
//    console.log("addCustomMarker aufgerufen:", { lat, lng, node });
//    if (!node) {
//        console.error("addCustomMarker: 'node' ist null oder undefined.");
//        return;
//    }
//    if (!node.id) {
//        console.error("addCustomMarker: node.id ist null oder undefined.");
//        return;
//    }
//    if (!node.node || !node.node.name) {
//        console.error("addCustomMarker: node.node.name fehlt.");
//        return;
//    }
//    if (!map) {
//        console.error("addCustomMarker: Karte ist nicht initialisiert.");
//        return;
//    }

//    var svgIcon = L.divIcon({
//        className: "custom-icon",
//        html: `
//            <svg xmlns="http://www.w3.org/2000/svg" width="60" height="80" viewBox="0 0 60 80">
//                <!-- Rechteck für den Timer oben -->
//                <rect x="5" y="5" width="50" height="20" fill="black" stroke="white" stroke-width="2" rx="5"/>
//                <text id="timer-${node.id}" x="30" y="20" text-anchor="middle" font-size="14" fill="white">10</text>
                
//                <!-- Dreieck als Marker -->
//                <polygon points="30,70 10,40 50,40" fill="red" stroke="black" stroke-width="2"/>
//            </svg>`,
//        iconSize: [60, 80],
//        iconAnchor: [30, 70]
//    });

//    var marker = L.marker([lat, lng], { icon: svgIcon }).addTo(map)
//        .bindPopup(node.node.name);
//    markers.push({ id: node.id, marker: marker });
//    console.log("addCustomMarker: Benutzerdefinierter Marker mit ID", node.id, "hinzugefügt.");

//    // Starte den Timer für diesen Marker
//    startTimer(node.id, timeleft);
//}

function formatTime(seconds) {
    let hours = Math.floor(seconds / 3600);
    let minutes = Math.floor((seconds % 3600) / 60);
    let secs = seconds % 60;
    return `${hours}:${minutes.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`;
}

function startTimer(id, timeRemaining) {
    let timeLeft = timeRemaining;
    const timerElement = document.getElementById(`timer-${id}`);
    if (!timerElement) {
        console.warn("startTimer: Timer-Element für ID", id, "nicht gefunden.");
        return;
    }
    console.log("startTimer: Timer gestartet für ID", id, "mit", timeRemaining, "Sekunden.");

    // Falls es schon einen Timer für diese ID gibt, zuerst stoppen
    if (activeTimers[id]) {
        clearInterval(activeTimers[id]);
        delete activeTimers[id];
    }

    const interval = setInterval(() => {
        if (timeLeft <= 0) {
            clearInterval(interval);
            timerElement.textContent = "🔥"; // Timer abgelaufen
            console.log("startTimer: Timer abgelaufen für ID", id);
        } else {
            let formatted = formatTime(timeLeft);
            timerElement.textContent = formatted;
            console.log(`startTimer: Update für ID ${id} – verbleibende Zeit: ${timeLeft} Sek.`);
            timeLeft--;
        }
    }, 1000);

    activeTimers[id] = interval; // Speichere den Timer in activeTimers
}

function removeCustomMarkers() {
    if (!map) {
        console.error("removeCustomMarkers: Karte ist nicht initialisiert.");
        return;
    }
    if (markers.length === 0) {
        console.warn("removeCustomMarkers: Keine Custom-Marker gefunden.");
        return;
    }

    markers = markers.filter(markerObj => {
        if (markerObj.marker.options.icon && markerObj.marker.options.icon.options.className === "custom-icon") {
            map.removeLayer(markerObj.marker);

            // Timer für diesen Marker stoppen
            if (activeTimers[markerObj.id]) {
                clearInterval(activeTimers[markerObj.id]); // Timer stoppen
                delete activeTimers[markerObj.id]; // Entfernen aus Speicher
                console.log(`removeCustomMarkers: Timer für Marker ${markerObj.id} gestoppt.`);
            }

            return false; // Entferne Marker aus `markers`-Array
        }
        return true; // Behalte andere Marker
    });

    console.log("removeCustomMarkers: Alle benutzerdefinierten Marker entfernt.");
}

window.initMap = initMap;
window.addMarker = addMarker;
window.CenterOnMap = CenterOnMap;
