let audio = new Audio('pwa/sounds/shutter.wav'); // Stelle sicher, dass du hier den richtigen Pfad zu deiner Audiodatei angibst.
let enableAudio = true;
function enableAudioOnClick() {
    const elements = document.querySelectorAll('.shutter-click, .shutter-click-button'); // Wählt alle Radio-Inputs, Checkboxen und Buttons mit der entsprechenden Klasse

    elements.forEach(element => {
        element.addEventListener('change', () => {
            if (element.checked) { // Überprüfen, ob das Radio-Element oder die Checkbox ausgewählt ist
                audio.currentTime = 0; // Setzt die Wiedergabezeit zurück
                audio.play().catch(err => {
                    console.error("Audio konnte nicht abgespielt werden:", err);
                });
            }
        });

        // Für Buttons: das 'click' Event hinzufügen
        if (element.tagName === 'BUTTON') {
            element.addEventListener('click', () => {
                audio.currentTime = 0; // Setzt die Wiedergabezeit zurück
                audio.play().catch(err => {
                    console.error("Audio konnte nicht abgespielt werden:", err);
                });
            });
        }
    });
}

// Diese Funktion wird von Blazor aufgerufen
document.addEventListener("DOMContentLoaded", () => {
    if (enableAudio) {
        enableAudioOnClick();
    }
});
