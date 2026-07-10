# Würfelmodell

Die klassischen Grundbauteile basieren auf einem Würfel mit sechs möglichen Anschlussflächen. 

Rohr:
Left ↔ Right

90° Winkel:
Left ↔ Top

T-Stück:
Left ↔ Right ↔ Top

Kreuz:
Left ↔ Right ↔ Top ↔ Bottom

Langfristig könnten alle Strukturbauteile aus einer einzigen Klasse
"StructuralPart" mit aktiven Anschlussflächen erzeugt werden.

Spätere Spezialbauteile (z. B. 30°-Bögen) verwenden dieselbe Anschlusslogik, besitzen aber eine andere Geometrie.


## Darstellung der Grundbauteile

Alle Grundbauteile besitzen im Zentrum einen gemeinsamen
Körper mit dem Außendurchmesser des Rohres.

Ist dieser Körper vollständig von den Rohrarmen überdeckt,
ist er nicht sichtbar.

Entstehen freie Bereiche (z.B. beim Winkel oder Eck),
werden diese als Abrundung sichtbar.