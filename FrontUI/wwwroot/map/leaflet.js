var data = {
    mapSize: 1500000,
    tileSize: 512,
    transformB: 403,
    transformD: -6.5,
    scale: 1.252
};
let markers = [];
let activeTimers = {};
var map;

function initMap() {
    // Prüfe, ob das Container-DIV vorhanden ist
    const mapDiv = document.getElementById('map');
    if (!mapDiv) {
        console.error("initMap: Div mit der ID 'map' wurde nicht gefunden. Initialisierung wird abgebrochen.");
        return;
    }
    console.log("initMap: Div 'map' gefunden.");

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
        maxZoom: 7,
        zoom: 4,
        attributionControl: false,
        layers: [
            L.tileLayer('_content/FrontUI/map/{z}/{x}/{y}.webp', { tileSize: data.tileSize })
        ]
    }).setView([207998, -879827]);
    console.log("initMap: Karte initialisiert.");

    // Event-Listener für das Kontextmenü (Rechtsklick)
    map.addEventListener('contextmenu', async function (ev) {
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
            try {
                await DotNet.invokeMethodAsync('FrontUI', 'UpdateCoordinates', ev.latlng.lat, ev.latlng.lng);
                console.log("contextmenu: Koordinaten an Blazor gesendet.");
            } catch (err) {
                console.error("contextmenu: Fehler beim Senden der Koordinaten an Blazor:", err);
            }
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
    if (!node || !node.id || !node.node || !node.node.name) {
        console.error("addCustomMarker: Ungültige Daten für 'node'.");
        return;
    }
    if (!map) {
        console.error("addCustomMarker: Karte ist nicht initialisiert.");
        return;
    }

    var imgUrl = node.node.nodeImageUrl || "https://via.placeholder.com/50";

    var svgIcon = L.divIcon({
        className: "custom-icon",
        html: `
            <svg xmlns="http://www.w3.org/2000/svg" width="60" height="100" viewBox="0 0 60 100">
                <rect x="5" y="5" width="50" height="20" fill="black" stroke="white" stroke-width="2" rx="5"/>
                <text id="timer-${node.id}" x="30" y="20" text-anchor="middle" font-size="14" fill="white"></text>
                <polygon points="30,90 10,60 50,60" fill="#${node.rarity}" stroke="#${node.rarity}" stroke-width="2"/>
                <rect x="10" y="30" width="40" height="40" fill="#${node.rarity}" stroke="#${node.rarity}" stroke-width="2" rx="5"/>
                <image x="10" y="32" width="40" height="40" href="${imgUrl}" />
            </svg>`,
        iconSize: [60, 100],
        iconAnchor: [30, 90]
    });

    var marker = L.marker([lat, lng], { icon: svgIcon }).addTo(map)
        .bindPopup(node.node.name)
        .on("click", function () {
            console.log("Marker geklickt:", node.id);
            sendNodeToBlazor(node.id); // Blazor informieren
        });

    markers.push({ id: node.id, marker: marker, node: node });
    console.log("addCustomMarker: Benutzerdefinierter Marker mit ID", node.id, "hinzugefügt.");

    startTimer(node.id, timeleft);
}

function sendNodeToBlazor(nodeId) {
    if (typeof DotNet !== "undefined" && DotNet.invokeMethodAsync) {
        let markerObj = markers.find(m => m.id === nodeId);
        if (markerObj) {
            console.log("sendNodeToBlazor: Sende Node ID an Blazor", markerObj.id);
            // Sende nur die ID an Blazor
            DotNet.invokeMethodAsync('FrontUI', 'ReceiveNodeData', markerObj.id)
                .then(() => console.log("Node ID erfolgreich an Blazor gesendet"))
                .catch(err => console.error("Fehler beim Senden der Node ID an Blazor", err));
        } else {
            console.warn("sendNodeToBlazor: Kein Marker mit ID", nodeId, "gefunden.");
        }
    } else {
        console.error("sendNodeToBlazor: Blazor Interop nicht verfügbar.");
    }
}

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
        
        return;
    }
    

    // Falls es schon einen Timer für diese ID gibt, zuerst stoppen
    if (activeTimers[id]) {
        clearInterval(activeTimers[id]);
        delete activeTimers[id];
    }

    const interval = setInterval(() => {
        let formatted = formatTime(timeLeft);
        if (timeLeft <= 0) {
            clearInterval(interval);
            timerElement.textContent = "🔥"; // Timer abgelaufen
            
        } else {
            timerElement.textContent = formatted;
            
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

// Blazor Interop: Globale Funktionalität verfügbar machen
window.initMap = initMap;
window.addCustomMarker = addCustomMarker;
window.removeMarker = removeMarker;
window.removeCustomMarkers = removeCustomMarkers;
window.CenterOnMap = CenterOnMap;
