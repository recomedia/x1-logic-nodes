Der **Quelltext** ist für jedes Paket nach Produktiv- und Testcode getrennt.
Zusätzlich zum hier bereitgestellten Quelltext wird folgendes benötigt:
* Das [Gira X1/L1 Logikbaustein SDK](https://partner.gira.de/service/developer.html)
  (der ausgepackte Ordner muss im gleichen Ordner liegen, wie die Quelltextordner)
* Eine DeveloperId und ein Entwicklerzertifikat (Wie man das bekommt und
  verwendet, steht in der mit dem SDK gelieferten PDF-Dokumentation)
* [Microsoft Visual Studio](https://visualstudio.microsoft.com/de/downloads/)
  (die kostenlose Community Edition reicht)
* AxoCover (kann direkt innerhalb von Visual Studio installiert werden)
  zur Messung der Abdeckung automatisierter Tests
* Weitere Abhängigkeiten wie auf github unter Insights -> Dependency graph
  angegeben

**Codeänderungen** werden nur über Pull Requests integriert und nur dann,
wenn die folgenden Voraussetzungen erfüllt sind:
* Vorherige Absprache von neuen Bausteinen, Paketzuordnung und Design
  (änderungen)
* Die Benutzerdokumentation ist für alle Änderungen aktualisiert, die für
  Endbenutzer sichtbar sind.
* Änderungen werden mit neuen automatisierten Testfällen getestet, die
  _mit_ den Änderungen "Passed"-Ergebnisse liefern, _ohne_ sie aber
  "Failed"-Ergebnisse
* Alle vorher existierenden und neu hinzugefügten automatisierten Testfälle
  liefern "Passed"-Ergebnisse
* Die automatisierten Testfälle müssen 100% aller Klassen und Methoden,
  mindestens 98% aller Codezeilen und mindestens 94% aller Zweige
  abdecken
* Geänderte oder neue Logikbausteine müssen interaktiv im GPA (Simulation)
  und nach der Inbetriebnahme auf einem L1 oder X1 funktionieren