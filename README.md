# x1-logic-nodes

Logikbausteine für Gira X1/L1 KNX Server/Logikmodul.

Hinweise für **Entwickler**, die an der Weiterentwicklung mitwirken möchten,
finden sich in der Datei
[CONTRIBUTE.md](https://github.com/recomedia/x1-logic-nodes/blob/master/CONTRIBUTE.md)

Für **Endbenutzer** stehen die fertig kompilierten und signierten Pakete
im Download-Bereich des KNX-User-Forums zur Verfügung:
* [Generische Logikbausteine](https://service.knx-user-forum.de/?comm=download&id=20000011)
* [Visu- & Web-Logikbausteine](https://service.knx-user-forum.de/?comm=download&id=20000010)

Die ausführliche Benutzerdokumentation ist in die Pakete so integriert,
dass sie direkt im GPA zur Verfügung steht. Hier folgen daher nur
Kurzbeschreibungen der Pakete mit den enthaltenen Logikbausteinen.

## Generische Logikbausteine

Die Logikbausteine in diesem Paket stellen zum einen Verbesserungen und
Ergänzungen vorhandener Bausteine dar:
* Der Baustein **Send-By-Difference** funktioniert ähnlich wie Gira's
  "Send-By-Change", erlaubt aber die Filterung ähnlicher (nicht nur
  genau gleicher) Werte.
* Der Baustein **Eingangswahlschalter+** funktioniert wie Gira's
  "Eingangswahlschalter". Zusätzlich kann beim Auswählen eines
  Eingangs der zuletzt auf diesem Eingang empfangene Wert gesendet
  werden. In manchen Fällen erspart das zusätzliche Logik mit
  Wertgeneratoren.
* Der Baustein **Ausgangswahlschalter+** ist die logische Umkehrung
  des Eingangswahlschalter+. Wie dieser kann er beim Auswählen eines
  Ausgangs sofort einen wählbaren Eingangswert senden. Zusätzlich kann
  er beim Abwählen eines Ausgangs einen Ruhewert senden. Er kann auch
  – mit nur einem Ausgang – als erweiterte Sperre eingesetzt werden,
  wenn beim Sperren ein Ruhewert und/oder beim Aufheben der Sperre der
  Eingangswert gesendet werden soll.
* Der Baustein **Statistikfunktionen** ermöglicht nebenbei auch die
  Addition _vieler_ Werte, während der Gira-Baustein "Grundrechenart"
  nur _zwei_ Werte erlaubt.

Der zweite Schwerpunkt sind statistische Berechnungen, u. a. Durchschnitt,
Minimum und Maximum:
* Die **Statistikfunktionen** errechnen diese für _mehrere_ Eingänge, die
  in der Regel mit verschiedenen Datenpunktem verbunden sind.
* **Statistik für Zeitreihen** arbeitet mit einer Zeitreihe von Werten
  eines Datenpunkts, die nacheinander auf dem _gleichen_ Eingang
  eintreffen.

Anders als üblich berechnen dabei alle Bausteine mit mehreren gleichartigen
Eingängen ihre Ausgangswerte schon dann, wenn ein Eingang einen Wert erhält
(und nicht erst dann, wenn alle Werte gültig sind). Eine Vorbelegung der
Eingänge – die in vielen Fällen ohnehin nicht die gewünschte Wirkung hätte
– ist daher meist verzichtbar.

Eigentlich würde auch der Logikbaustein Formelberechnung thematisch gut in
dieses Paket passen. Aus technischen Gründen – er verwendet die gleiche
Platzhalter-Implementierung wie der "Textformatierer" – findet er sich
jedoch im zweiten Paket ...

## Visu- & Web-Logikbausteine

Die Logikbausteine in diesem Paket unterstützen Berechnungen, einfache
Visualisierungen und Integration mit Web-Services:
* Der Baustein **Formelberechnung** errechnet Werte aus Funktionen, die von
  einem oder mehreren Dateneingängen abhängen können. Gängige Operatoren
  (einschließlich bedingter Berechnungen) sowie umfassende Funktionen für
  mathematische Berechnungen stehen zur Verfügung. Dazu kommen eigene
  Funktionserweiterugen für Lichtsteuerung und Heizungs-/Lüftungsregelung.
  (Obwohl dieser Baustein thematisch besser ins Paket "Recomedia Generische
  Logikbausteine" passen würde, muss er aus technischen Gründen – er nutzt
  dieselbe Platzhalter-Implementierung – in diesem Paket ausgeliefert
  werden.)
* Der Baustein **Textformatierer** setzt aus aus Formatvorlagen und
  Dateneingängen einen oder mehrere Ausgabetexte zusammen. Diese lassen
  sich z. B. für einfache Visualisierungsaufgaben verwenden.
* Der **XML-/JSON-Parser** extrahiert aus einem XML- oder JSON-Datensatz
  am Eingang über XPath-Abfragen entsprechende Ausgabedaten. So lassen
  sich Daten aus Webservices (z. B. OpenWeatherMap) für die Visualisierung
  oder Weiterverarbeitung nutzen.

Für die Web-Integration ist zusätzlich der Baustein
[**Web Request** von Daniel Albuschat](https://github.com/daniel-kun/Albuschat.LogicNodes/releases)
oder der Baustein
[**HTTP GET Request** von Fabian Fischer](https://service.knx-user-forum.de/?comm=download&id=20000065)
notwendig.
