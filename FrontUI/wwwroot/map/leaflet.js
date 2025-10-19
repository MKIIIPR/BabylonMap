var data = {
    mapSize: 1500000,
    tileSize: 512,
    transformB: 403,
    transformD: -6.5,
    scale: 1.252
};
let markers = [];
let activeTimers = {};
let activeBlinkers = {}; // id -> { interval, stopTimeout, iconDiv, prev, prevOffset, markerObj }
let raisedMarkers = {}; // id -> { timeout, iconDiv, prev, prevOffset, markerObj }
let selectedMarker = null; // { id, iconDiv, prev, prevOffset, markerObj }
var map;

function initMap() {
    const mapDiv = document.getElementById('map');
    if (!mapDiv) {
        console.error("initMap: Div mit der ID 'map' wurde nicht gefunden. Initialisierung wird abgebrochen.");
        return;
    }

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
        minZoom: 1,
        maxZoom: 9,
        zoom: 2,
        attributionControl: false,
        layers: [
            L.tileLayer('https://cdn.ashescodex.com/map/20250826/{z}/{x}/{y}.webp', { tileSize: data.tileSize })
        ]
    }).setView([550000, -950000]);

    map.addEventListener('contextmenu', async function (ev) {
        const coordElem = document.getElementById('map-coordinates');
        if (coordElem) {
            coordElem.innerHTML =
                ev.latlng.lng.toFixed(0) + ', ' + ev.latlng.lat.toFixed(0);
        }

        if (typeof DotNet !== "undefined" && DotNet.invokeMethodAsync) {
            try {
                await DotNet.invokeMethodAsync('FrontUI', 'UpdateCoordinates', ev.latlng.lat, ev.latlng.lng);
            } catch (err) {
                console.error("contextmenu: Fehler beim Senden der Koordinaten an Blazor:", err);
            }
        }
    });
}

function addMarker(lat, lng, text) {
    if (!map) {
        console.error("addMarker: Karte ist nicht initialisiert. Marker kann nicht hinzugefügt werden.");
        return;
    }
    const m = L.marker([lat, lng]).addTo(map);
    if (text && text.length) {
        m.bindPopup(text, { autoPan: false });
    }
}

function CenterOnMap(lat, lng) {
    if (map && typeof map.setView === 'function') {
        map.setView([lat, lng], map.getZoom());
    }
}

function removeMarker(id) {
    if (!map) return;
    let markerObj = markers.find(m => m.id === id);
    if (markerObj) {
        map.removeLayer(markerObj.marker);
        markers = markers.filter(m => m.id !== id);
        stopMarkerBlink(id);
        stopActiveTimer(id);
    }
}

function addCustomMarker(lat, lng, node, timeleft) {
    if (!node || !node.id || !node.node || !node.node.name) return;
    if (!map) return;

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

    var marker = L.marker([lat, lng], { icon: svgIcon }).addTo(map);

    if (node.description && node.description.trim().length > 0) {
        marker.bindPopup(node.description, { autoPan: false });
    }

    marker.on("click", function () {
        sendNodeToBlazor(node.id);
    });

    markers.push({ id: node.id, marker: marker, node: node });

    startTimer(node.id, timeleft);
}

function sendNodeToBlazor(nodeId) {
    if (typeof DotNet !== "undefined" && DotNet.invokeMethodAsync) {
        let markerObj = markers.find(m => m.id === nodeId);
        if (markerObj) {
            DotNet.invokeMethodAsync('FrontUI', 'ReceiveNodeData', markerObj.id)
                .catch(err => console.error("Fehler beim Senden der Node ID an Blazor", err));
        }
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

    if (activeTimers[id]) {
        clearInterval(activeTimers[id]);
        delete activeTimers[id];
    }

    const interval = setInterval(() => {
        let formatted = formatTime(timeLeft);
        if (timeLeft <= 0) {
            clearInterval(interval);
            timerElement.textContent = "🔥";
        } else {
            timerElement.textContent = formatted;
            timeLeft--;
        }
    }, 1000);

    activeTimers[id] = interval;
}

function stopActiveTimer(id) {
    if (activeTimers[id]) {
        clearInterval(activeTimers[id]);
        delete activeTimers[id];
    }
}

function startMarkerBlink(id, durationMs = 60000, periodMs = 500) {
    try {
        const markerObj = markers.find(m => m.id === id);
        if (!markerObj) return;
        const iconDiv = markerObj.marker._icon;
        if (!iconDiv) return;

        stopMarkerBlink(id);

        const prev = {
            zIndex: iconDiv.style.zIndex,
            boxShadow: iconDiv.style.boxShadow,
            opacity: iconDiv.style.opacity,
            filter: iconDiv.style.filter
        };
        const prevOffset = markerObj.marker.options.zIndexOffset || 0;

        markerObj.marker.setZIndexOffset(1000000);
        iconDiv.style.zIndex = '1000000';
        if (iconDiv.parentElement) {
            iconDiv.parentElement.appendChild(iconDiv);
        }

        iconDiv.style.transition = 'opacity 0.15s linear';
        iconDiv.style.opacity = '1';

        let visible = true;
        const interval = setInterval(() => {
            visible = !visible;
            iconDiv.style.opacity = visible ? '1' : '0.55';
        }, periodMs);

        const stopTimeout = setTimeout(() => {
            stopMarkerBlink(id);
        }, durationMs);

        activeBlinkers[id] = { interval, stopTimeout, iconDiv, prev, prevOffset, markerObj };
    } catch (e) {
        console.warn('startMarkerBlink failed', e);
    }
}

function stopMarkerBlink(id) {
    const b = activeBlinkers[id];
    if (!b) return;
    try {
        clearInterval(b.interval);
        clearTimeout(b.stopTimeout);
        if (b.iconDiv) {
            b.iconDiv.style.opacity = b.prev.opacity || '1';
            b.iconDiv.style.filter = b.prev.filter || 'none';
            b.iconDiv.style.boxShadow = b.prev.boxShadow || 'none';
            b.iconDiv.style.zIndex = b.prev.zIndex || '';
        }
        if (b.markerObj && b.markerObj.marker) {
            b.markerObj.marker.setZIndexOffset(b.prevOffset || 0);
        }
    } catch { }
    delete activeBlinkers[id];
}

function raiseMarker(id, durationMs = 3000) {
    try {
        const markerObj = markers.find(m => m.id === id);
        if (!markerObj) return;
        const iconDiv = markerObj.marker._icon;
        if (!iconDiv) return;

        const existing = raisedMarkers[id];
        if (existing) {
            clearTimeout(existing.timeout);
            try {
                if (existing.iconDiv) {
                    existing.iconDiv.style.zIndex = existing.prev.zIndex || '';
                }
                existing.markerObj.marker.setZIndexOffset(existing.prevOffset || 0);
            } catch { }
            delete raisedMarkers[id];
        }

        const prev = {
            zIndex: iconDiv.style.zIndex
        };
        const prevOffset = markerObj.marker.options.zIndexOffset || 0;
        markerObj.marker.setZIndexOffset(500000);
        iconDiv.style.zIndex = '1000000';
        if (iconDiv.parentElement) iconDiv.parentElement.appendChild(iconDiv);

        const timeout = setTimeout(() => {
            try {
                iconDiv.style.zIndex = prev.zIndex || '';
                markerObj.marker.setZIndexOffset(prevOffset || 0);
            } catch { }
            delete raisedMarkers[id];
        }, durationMs);

        raisedMarkers[id] = { timeout, iconDiv, prev, prevOffset, markerObj };
    } catch (e) {
        console.warn('raiseMarker failed', e);
    }
}

function selectMarker(id) {
    try {
        if (selectedMarker) {
            try {
                if (selectedMarker.iconDiv) {
                    selectedMarker.iconDiv.style.filter = selectedMarker.prev.filter || 'none';
                    selectedMarker.iconDiv.style.boxShadow = selectedMarker.prev.boxShadow || 'none';
                    selectedMarker.iconDiv.style.zIndex = selectedMarker.prev.zIndex || '';
                }
                if (selectedMarker.markerObj && selectedMarker.markerObj.marker) {
                    selectedMarker.markerObj.marker.setZIndexOffset(selectedMarker.prevOffset || 0);
                }
            } catch { }
            selectedMarker = null;
        }

        const markerObj = markers.find(m => m.id === id);
        if (!markerObj) return;
        const iconDiv = markerObj.marker._icon;
        if (!iconDiv) return;

        const prev = {
            zIndex: iconDiv.style.zIndex,
            filter: iconDiv.style.filter,
            boxShadow: iconDiv.style.boxShadow
        };
        const prevOffset = markerObj.marker.options.zIndexOffset || 0;

        markerObj.marker.setZIndexOffset(900000);
        iconDiv.style.zIndex = '1000000';
        if (iconDiv.parentElement) iconDiv.parentElement.appendChild(iconDiv);

        iconDiv.style.filter = 'brightness(1.35) saturate(1.1)';

        selectedMarker = { id, iconDiv, prev, prevOffset, markerObj };
    } catch (e) {
        console.warn('selectMarker failed', e);
    }
}

function clearSelectedMarker() {
    try {
        if (!selectedMarker) return;
        if (selectedMarker.iconDiv) {
            selectedMarker.iconDiv.style.filter = selectedMarker.prev.filter || 'none';
            selectedMarker.iconDiv.style.boxShadow = selectedMarker.prev.boxShadow || 'none';
            selectedMarker.iconDiv.style.zIndex = selectedMarker.prev.zIndex || '';
        }
        if (selectedMarker.markerObj && selectedMarker.markerObj.marker) {
            selectedMarker.markerObj.marker.setZIndexOffset(selectedMarker.prevOffset || 0);
        }
        selectedMarker = null;
    } catch (e) {
        console.warn('clearSelectedMarker failed', e);
    }
}

function scrollToElementId(id) {
    try {
        const el = document.getElementById(id);
        if (el && typeof el.scrollIntoView === 'function') {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    } catch (e) {
        console.warn('scrollToElementId failed', e);
    }
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
            if (activeTimers[markerObj.id]) {
                clearInterval(activeTimers[markerObj.id]);
                delete activeTimers[markerObj.id];
            }
            stopMarkerBlink(markerObj.id);
            return false;
        }
        return true;
    });
}

let bmBeepCtx = null;
function bmPlayBlinkBeep(durationMs = 2000, volume = 0.05, frequency = 880) {
    try {
        const Ctx = window.AudioContext || window.webkitAudioContext;
        if (!Ctx) return;
        bmBeepCtx = bmBeepCtx || new Ctx();
        const osc = bmBeepCtx.createOscillator();
        const gain = bmBeepCtx.createGain();
        osc.type = 'sine';
        osc.frequency.value = frequency;
        gain.gain.value = volume;
        osc.connect(gain);
        gain.connect(bmBeepCtx.destination);
        osc.start();
        setTimeout(() => {
            try { osc.stop(); osc.disconnect(); gain.disconnect(); } catch { }
        }, durationMs);
    } catch (e) {
        console.warn('bmPlayBlinkBeep failed', e);
    }
}

window.initMap = initMap;
window.addCustomMarker = addCustomMarker;
window.removeMarker = removeMarker;
window.removeCustomMarkers = removeCustomMarkers;
window.CenterOnMap = CenterOnMap;
window.bmPlayBlinkBeep = bmPlayBlinkBeep;
window.startMarkerBlink = startMarkerBlink;
window.stopMarkerBlink = stopMarkerBlink;
window.raiseMarker = raiseMarker;
window.selectMarker = selectMarker;
window.clearSelectedMarker = clearSelectedMarker;
window.scrollToElementId = scrollToElementId;
