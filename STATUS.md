# FishSim – Fejlesztési állapot (2026-06-14)

Ez a fájl a hal fizikai/animációs modelljéről szóló munka (lásd a
`~/.claude/plans/wobbly-leaping-perlis.md` eredeti M0-M5 tervet) jelenlegi
állását rögzíti, hogy a munka innen folytatható legyen.

## Mi készült el (M0-M5 a tervből)

- **M0-M1 – Anatómiai constraint-háló** (`Fish.GenerateFishBody()`, `Body.cs`):
  - 35 verlet pont: `0-3` régi, már nem használt befoglaló keret (nincs
    constraint-jük, fizikailag nem mozognak érdemben).
  - Gerinclánc `27-34` (farok→fej), szomszédos constraint (`spineStiff=0.98`)
    + "skip" constraint `i→i+2` a hajlás-ellenállásért (`spineSkipStiff=0.65`).
  - Úszótövek (`finBases = {5,7,10,13,16,18,20,21,24}`) a legközelebbi
    gerinc-csigolyához kötve, közel rigid (`baseStiff=0.99`).
  - Úszóvégek egymáshoz és a saját tövükhöz lazán kötve (`tipStiff=0.15`).
  - `Body.cs`: `AddConstraint(a, b, length?, stiffness)` + `cPairs/cLengths/cStiffness`
    listák, `ApplyConstraints()` ezt használja.
  - Debug vizualizáció: `DrawDebugVerlets` (F1) és `DrawDebugConstraints` (F2,
    színezett hengerek, piros=merev, zöld=laza) – **jelenleg ki vannak
    kommentezve** `Game1.Draw()`-ban (lásd lent).

- **M2 – Kanyarodás / gerinchajlás** (`Fish.ApplyTurning`):
  - `turnSignal` simítva A/D billentyűkből, gerinc oldalirányú hajlítása
    (farok felé nagyobb súllyal), farok- és oldalúszó-végek aktív
    "lapátoló" ellenirányú mozgása.

- **M3 – CPU vertex-morphing** (`FishMeshMorph.cs`):
  - Nearest-3, inverz-négyzetes távolság szerint súlyozott linear blend
    skinning, bind-pose/model-local space-ben, `DynamicVertexBuffer`-rel.
  - Csak a gerinc (27-34) pontokat használja "csontként" (az úszók nem).
  - `maxInfluenceRadius = 0.8 * fishBodyLength` (a 27↔34 távolságból számolva).
  - **Jelenleg ki van kapcsolva**: a konstruktor lefut (buffer felépül), de a
    `meshMorph.Update(world, verlets)` hívás ki van kommentezve
    `Fish.Draw()`-ban → a mesh rigid/bind-pose, csak a teljes test mozgása
    látszik (a felhasználó kérésére, hogy a vezérlés/viselkedés tisztán
    tesztelhető legyen a deformáció zaja nélkül).

- **M5 – Erő-/úszásmodell + kamera/irányítás**:
  - `ApplyForces()`: gravitációt pontosan kioltó "semleges felhajtóerő"
    (`Acc += Up * 9.81`), irány szerinti vízellenállás (`AddSqFriction`
    előre/oldal/fel: `0.05 / 0.5 / 1.0`), lágy víztükör (y>0) és
    tengerfenék (y<-55) határoló erők.
  - `ApplyPitching()`: egér Y mozgásból jövő `pitchTarget` (Game1 állítja,
    "ragadó" érték) → gerinc hajlítása világ-Y mentén, ez adja az
    emelkedést/süllyedést az előrehajtással kombinálva.
  - `ApplyRighting()`: nem súly-alapú dönt-stabilizáció – a hal "Up"
    vektorát a `Direction`-re merőleges "ideális" felfelé irányhoz közelíti
    (hát-/has-úszótövekön ható erőkülönbséggel), hogy ne forduljon
    folyamatosan oldalra/hasra, de a szándékolt pitch szabadon érvényesülhet.
  - Kamera (`Game1.Update`):
    - `Tab` – fish-cam / free-cam váltás.
    - Fish-cam + **LMB lenyomva** → orbit nézet a hal körül, egér=forgatás,
      görgő=zoom (`orbitDistance`, `OrbitMinDistance/MaxDistance`).
    - Fish-cam + LMB **nem** nyomva → követő kamera a hal mögött/fölött,
      egér fel/le → `fish.pitchTarget` (a hal orrának dőlése).
    - Free-cam: WASD+QE+Shift, szabad nézet.

## Legutóbbi javítások (ezen a beszélgetésen)

1. **Pitch-irány megfordítva** (`Game1.cs`): az egér fel/le mozgás és a
   `pitchTarget` előjele rossz volt – egér felfelé mozgatása a halat
   lefelé döntötte (süllyedés érzete kamera-iránytól függetlenül is). Mostantól
   `pitchTarget += dy * PitchSteerSensitivity` (volt: `-= dy * ...`), így
   egér felfelé → orr felfelé dől → emelkedés.
2. **Remegés/"vissza-vissza dobás" csökkentése** (`Fish.cs`):
   - A függőleges irányú négyzetes vízellenállás (`AddSqFriction(u, ...)`)
     `5.0` → `1.0`-ra csökkentve (10x volt az oldalirányúhoz és ~100x az
     előrehaladásihoz képest – ez okozta, hogy a `Verlet.Step` anti-overshoot
     korlátozása minden frame-ben "lenullázta" a függőleges sebességet,
     látható visszarántást okozva).
   - `ApplyRighting` `rightingStrength`: `6.0` → `3.0` (lágyabb,
     kevésbé lengő konvergencia az "ideális" felfelé irányhoz).

## Jelenlegi ismert hiányosságok / TODO

- **Mesh-morph (M3) ki van kapcsolva** – csak a teljes test mozgása/
  irányítása látszik, a hal vizuálisan rigid. Ha a mozgás/irányítás
  stabilnak/megfelelőnek bizonyul, vissza kell kapcsolni:
  - `Fish.cs` `Draw()`-ban töröld a kommentet a
    `meshMorph.Update(world, verlets);` sorról.
- **F1/F2 debug nézetek ki vannak kommentezve** `Game1.Draw()`-ban
  (`showDebugVerlets` / `showDebugConstraints` blokkok) – ha kell, vissza
  lehet kapcsolni (a mezők és a billentyű-togglek megvannak, csak a Draw-beli
  hívás van kikommentezve).
- **Még nem történt érdemi felhasználói teszt** a legutóbbi pitch-irány és
  drag-csökkentés után – a következő lépés a W/A/S/D + egér-pitch + LMB-orbit
  kamera tesztelése: kanyar/emelkedés/süllyedés érzete, billegés/remegés
  mértéke.
- **M4 (hangolás)** a tervből még nem történt meg érdemben:
  - Stiffness/kormányerő finomhangolás a (majd visszakapcsolt) morphed mesh
    alapján.
  - Normál-újraszámolás a deformált úszóknál, ha a fényezés furcsa.
- A `posErrors` mező (`Fish.Step()`) még létezik és minden frame-ben
  hozzáadódik a pozícióhoz/`pPos`-hoz (`* 0.01f`), de mindig `Vector3.Zero`
  tömbként van inicializálva → jelenleg no-op, de ha nem kell, törölhető
  egyszerűsítésként.

## Hogyan folytasd

1. Build + futtatás: `dotnet build` majd `dotnet run --project FishSim`.
2. Teszteld a vezérlést (fish-cam, alapértelmezett):
   - `W`/`S` – előre/hátra hajtás (a farokra hat).
   - `A`/`D` – kanyar (gerinchajlás + úszó-lapátolás).
   - Egér (LMB nélkül) – pitch (orr fel/le → emelkedés/süllyedés).
   - LMB lenyomva – orbit kamera a hal körül, görgő = zoom.
   - `Tab` – fish-cam/free-cam váltás.
3. Ha a mozgás stabil és irányítható: kapcsold vissza a mesh-morphot
   (`meshMorph.Update(...)` a `Fish.Draw()`-ban) és az F1/F2 debug nézeteket,
   majd folytasd az M4 hangolással.
4. Ha még billeg/remeg: a gyanús paraméterek
   `Fish.ApplyForces`/`ApplyRighting`/`ApplyPitching`-ban vannak
   (drag-együtthatók, `rightingStrength`, `ApplyPitching`/`ApplyTurning`
   erő-szorzók és súlyok).
