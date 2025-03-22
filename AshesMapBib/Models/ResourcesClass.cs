namespace AshesMapBib.Models
{

        public class Node
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public int RespawnTimer { get; set; }
            public string NodeImageUrl { get; set; }
        }

    public class NodePosition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ResourceId { get; set; }
        public Node? Node { get; set; }
        public string? Description { get; set; }
        public double Lat { get; set; }  // Breitengrad
        public double Lng { get; set; }  // Längengrad
        public string Rarity { get; set; }
        public string? Image { get; set; }
        public DateTime LastHarvest { get; set; }
        //public DateTime FirstHarvest { get; set; }
        public int? TimerMod { get; set; }
        public bool Ghost { get; set; }
    }
    public class NodePositionView : NodePosition
    {
        public bool AlertSet { get; set; } = true;

        // Berechnet die Standard-Respawn-Zeit basierend auf LastHarvest und Node.RespawnTimer
        public DateTime RespawnAt => LastHarvest.AddMinutes(Node?.RespawnTimer ?? 0);

        // Berechnet die tatsächliche Respawn-Zeit mit TimerMod
        public DateTime ActualRespawnAt => LastHarvest.AddMinutes((Node?.RespawnTimer ?? 0) + TimerMod);

        // Berechnet die verbleibende Zeit bis zum Respawn
        public TimeSpan TimeRemaining => ActualRespawnAt - DateTime.Now;
        //public DateTime PossibleNewHarvest
        //{
        //    get
        //    {
        //        if (Node == null) return DateTime.Now; // Falls Node null ist, gib einfach die aktuelle Zeit zurück

        //        int respawnTime = Node.RespawnTimer; // Respawn-Zeit in Minuten
        //        if (respawnTime <= 0) return DateTime.Now; // Sicherstellen, dass kein Fehler entsteht

        //        // Berechnung: Wie viele Respawns hätten seit FirstHarvest passieren können?
        //        TimeSpan seitErstemAbbau = DateTime.Now - FirstHarvest;
        //        int möglicheRespawns = (int)(seitErstemAbbau.TotalMinutes / respawnTime);

        //        // Nächster theoretischer Respawn seit FirstHarvest
        //        DateTime nächsterMöglicherRespawn = FirstHarvest.AddMinutes(möglicheRespawns * respawnTime);

        //        // Falls der nächste mögliche Respawn in der Vergangenheit liegt, korrigieren
        //        while (nächsterMöglicherRespawn < DateTime.Now)
        //        {
        //            nächsterMöglicherRespawn = nächsterMöglicherRespawn.AddMinutes(respawnTime);
        //        }

        //        return nächsterMöglicherRespawn;
        //    }
        //}
        //public string PossibleNewHarvestString
        //{
        //    get
        //    {
        //        DateTime nextHarvest = PossibleNewHarvest;
        //        TimeSpan verbleibendeZeit = nextHarvest - DateTime.Now;

        //        if (verbleibendeZeit.TotalSeconds <= 0)
        //            return "Bereit zur Ernte";

        //        return $"Nächste Ernte möglich in {verbleibendeZeit.Hours}h {verbleibendeZeit.Minutes}m {verbleibendeZeit.Seconds}s ({nextHarvest:HH:mm})";
        //    }
        //}

        public int TimeLeft => (int)(ActualRespawnAt - DateTime.Now).TotalSeconds;


        // Gibt die verbleibende Zeit als formatierte Zeichenkette zurück
        public string TimeRemainingString => TimeRemaining.TotalSeconds > 0
            ? $"{TimeRemaining.Hours}h {TimeRemaining.Minutes}m {TimeRemaining.Seconds}s"
            : "Bereit zur Ernte";

        // Gibt true zurück, wenn die Ressource noch nicht bereit ist
        public bool IsRespawning => TimeRemaining.TotalSeconds > 0;

        // Gibt true zurück, wenn die Ressource geerntet werden kann
        public bool CanHarvest => !IsRespawning;

        // Farbe basierend auf der verbleibenden Zeit
        public string TimeRemainingColor
        {
            get
            {
                if (TimeRemaining.TotalMinutes > 30) return "red";     // Mehr als 30 Min → Rot
                if (TimeRemaining.TotalMinutes > 15) return "orange";  // 15–30 Min → Orange
                if (TimeRemaining.TotalMinutes > 5) return "green";    // 5–15 Min → Grün
                return "blue";                                         // Weniger als 5 Min → Blau
            }
        }

        // Modifikator für die Zeit (wird beim Stoppen des Timers verwendet)
        public double TimerMod { get; set; }

        // Stoppt oder startet den Timer basierend auf der TimerStopped-Variable
        public void HandleTimer(bool isStopped)
        {
            if (isStopped)
            {
                // Timer stoppen und die verbleibende Zeit speichern
                TimerMod += (ActualRespawnAt - DateTime.Now).TotalMinutes;
            }
            else
            {
                // Timer fortsetzen, die gespeicherte Zeit wird wieder angehängt
                LastHarvest = DateTime.Now;
            }
        }
    }


    public class Coordinates
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }
   
}
