using Game1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq.Expressions;

namespace FishSim
{
    class Fish : Body
    {
        public bool ctrlW, ctrlS, ctrlA, ctrlD;
        public Vector3[] posErrors;
        // Simitott kanyar-jelzes: -1 (D, jobbra) .. +1 (A, balra)
        private float turnSignal;
        // Cel dontes (pitch) -1..1, az egertol jon (Game1 allitja, "ragado" ertek);
        // a hal orra ennek iranyaba dol, es elorehajtassal ez adja a sullyedest/emelkedest.
        public float pitchTarget;
        private float pitchSignal;
        // A 0-3 pontok (tavoli befoglalo keret) mar nincsenek bekotve a fizikaba, ezert a poziciot
        // es az iranyokat a gerincbol (27-34) es az uszotovekbol szamoljuk.
        public Vector3 Position => (verlets[27].Pos + verlets[28].Pos + verlets[29].Pos + verlets[30].Pos
                                    + verlets[31].Pos + verlets[32].Pos + verlets[33].Pos + verlets[34].Pos) / 8f;
        public Vector3 Direction => verlets[34].Pos - verlets[27].Pos; // farok -> fej, elore
        public Vector3 Up => (verlets[18].Pos + verlets[20].Pos) * 0.5f - verlets[16].Pos; // hat-uszo tove - has-uszo tove
        public Vector3 Right => Vector3.Cross(Vector3.Normalize(Up), Vector3.Normalize(Direction));
        public Matrix WorldTransform => Matrix.CreateWorld(Position, Vector3.Normalize(Direction), Vector3.Normalize(Up));
        Model model;
        FishMeshMorph meshMorph;
        Matrix localTransform = Matrix.CreateScale(0.1f) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateRotationX(MathHelper.PiOver2);
        public Fish(Fish fish) : base(fish)
        {
            model = fish.model;
            posErrors = new Vector3[verlets.Length];
        }
        public Fish(GraphicsDevice dev, Model model, Texture2D texture, Texture2D normalTexture, Effect fishEffect, Vector3 sunDir)
        {
            this.model = model;
            foreach (var mesh in model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    var effect = fishEffect.Clone();
                    effect.Parameters["Texture"].SetValue(texture);
                    effect.Parameters["NormalTexture"].SetValue(normalTexture);
                    effect.Parameters["AmbientColor"].SetValue(new Vector3(0.3f, 0.3f, 0.3f));
                    effect.Parameters["Light0Dir"].SetValue(Vector3.Normalize(-sunDir));
                    effect.Parameters["Light0Color"].SetValue(new Vector3(0.7f, 0.7f, 0.7f));
                    effect.Parameters["Light1Dir"].SetValue(Vector3.Down);
                    effect.Parameters["Light1Color"].SetValue(new Vector3(0.2f, 0.2f, 0.2f));
                    part.Effect = effect;
                }
            }
            float w = 0.2f, l = 1;
            var rng = new Random();
            // y=-10: kenyelmes uszasi melysegben indul (a felszin y=0, a fenek kb. y=-55),
            // igy a lagy hatarok nem fejtenek ki azonnal nagy, aszimmetrikus eroket.
            var pos = new Vector3(
                (float)rng.NextDouble() * 20, -10,
                (float)rng.NextDouble() * 20);
            verlets = new Verlet[] {
                // messzi keretezés nem lesz része a halnak csak ha kiveszem minden másik pont elcsúszik
                new Verlet(pos + new Vector3(5*l, 0, -5*w)),
                new Verlet(pos + new Vector3(5 * l, 0, 5*w)),
                new Verlet(pos + new Vector3(-5*l, 0, 5*w)),
                new Verlet(pos + new Vector3(-5*l, 0, -5*w)),

                new Verlet(pos + new Vector3(-1.16f, 0.08f, 0)),                            //farok alsó vége
                new Verlet(pos + new Vector3(-0.76f, 0.25f, 0)),                            //farok alsó töve
                new Verlet(pos + new Vector3(-1.16f, 0.85f, 0)),                            //farok felső vége
                new Verlet(pos + new Vector3(-0.76f, 0.65f, 0)),                            //farok felső töve
                        
                new Verlet(pos + new Vector3(1.1f, -0.25f, -0.075f)),                       //bal melső úszó alsó vége
                new Verlet(pos + new Vector3(1, -0.02f, -0.084f)),                          //bal melső úszó hátsó vége
                new Verlet(pos + new Vector3(1.17f, 0.08f, -0.074f)),                       //bal melső úszó töve

                new Verlet(pos + new Vector3(1.1f, -0.25f, 0.075f)),                        //jobb melső úszó alsó vége
                new Verlet(pos + new Vector3(1, -0.02f, 0.084f)),                           //jobb melső úszó hátsó vége
                new Verlet(pos + new Vector3(1.17f, 0.08f, 0.074f)),                        //jobb melső úszó töve

                new Verlet(pos + new Vector3(0, -0.16f, 0)),                                //has középi úszó alsó vége
                new Verlet(pos + new Vector3(-0.26f, 0.1f, 0)),                             //has középi úszó felső vége
                new Verlet(pos + new Vector3(0.17f, 0.16f, 0)),                             //has középi úszó töve

                new Verlet(pos + new Vector3(0.15f, 1.1f, 0)),                              //hát középi úszó első vége
                new Verlet(pos + new Vector3(0.45f, 0.8f, 0)),                              //hát középi úszó első töve
                new Verlet(pos + new Vector3(0.1f, 0.7f, 0)),                               //hát középi úszó hátsó vége
                new Verlet(pos + new Vector3(-0.19f, 0.8f, 0)),                             //hát középi úszó hátsó töve

                new Verlet(pos + new Vector3(1.3f, 0.3f, 0.3f)),                            //jobb első úszó töve
                new Verlet(pos + new Vector3(1.03f, 0.43f, 0.6f)),                          //jobb első úszó felső vége
                new Verlet(pos + new Vector3(1.03f, 0.23f, 0.6f)),                          //jobb első úszó alsó vége

                new Verlet(pos + new Vector3(1.3f, 0.3f, -0.3f)),                           //bal első úszó töve
                new Verlet(pos + new Vector3(1.21f, 0.43f, -0.6f)),                         //bal első úszó felső vége
                new Verlet(pos + new Vector3(1.09f, 0.23f, -0.6f)),                         //bal első úszó alsó vége

                new Verlet(pos + new Vector3(-0.5f, 0.45f, 0)),                            //7. csigolya a faroknál
                new Verlet(pos + new Vector3(-0.1f, 0.45f, 0)),                            //6. csigolya
                new Verlet(pos + new Vector3(0.3f, 0.45f, 0)),                             //5. csigolya 
                new Verlet(pos + new Vector3(0.7f, 0.45f, 0)),                             //4. csigolya
                new Verlet(pos + new Vector3(1.1f, 0.45f, 0)),                             //3. csigolya
                new Verlet(pos + new Vector3(1.5f, 0.45f, 0)),                             //2. csigolya 
                new Verlet(pos + new Vector3(1.9f, 0.45f, 0)),                             //1. csigolya a fejnél

                new Verlet(pos + new Vector3(2.25f, 0.45f, 0)),                            //fej legeleje
            };
            GenerateFishBody();
            meshMorph = new FishMeshMorph(model, localTransform * WorldTransform, verlets, 27, 34);
        }
        // Anatomiai constraint-halo: gerinclanc (hajlas-ellenallassal), uszotovek kozel rigid
        // kapcsolata a legkozelebbi csigolyahoz, es az uszovegek kozotti laza constraint-ek.
        // A 0-3 pontok (tavoli befoglalo keret a regi modellbol) nem kapnak constraint-et.
        private void GenerateFishBody()
        {
            cPairs.Clear(); cLengths.Clear(); cStiffness.Clear();

            const float spineStiff = 0.98f;
            const float spineSkipStiff = 0.65f;
            const float baseStiff = 0.99f;
            const float basePairStiff = 0.95f;
            const float tipStiff = 0.15f;

            // Gerinclanc: szomszedos csigolyak kozott feszes constraint
            for (int i = 27; i < 34; i++)
                AddConstraint(i, i + 1, null, spineStiff);
            // "Skip" constraint-ek (i -> i+2): ezek adjak a hajlas-ellenallast
            for (int i = 27; i < 33; i++)
                AddConstraint(i, i + 2, null, spineSkipStiff);

            // Uszotovek a legkozelebbi gerinccsigolyahoz, kozel rigid (mintha csont kotne ossze)
            int[] finBases = { 5, 7, 10, 13, 16, 18, 20, 21, 24 };
            foreach (int b in finBases)
                AddConstraint(b, NearestSpineIndex(b), null, baseStiff);

            // Parositott tovek egymashoz (uszogyok stabilitasa)
            AddConstraint(5, 7, null, basePairStiff);
            AddConstraint(18, 20, null, basePairStiff);

            // Uszovegek egymashoz, nagyon enyhe constraint-tel -> passziv lebegest/lebbenest enged
            (int, int)[] tipPairs = { (4, 6), (8, 9), (11, 12), (14, 15), (17, 19), (22, 23), (25, 26) };
            foreach (var (a, b) in tipPairs)
                AddConstraint(a, b, null, tipStiff);

            // Uszovegek a sajat tovukhoz is nagyon enyhen kotve, kulonben a vege-vege constraint
            // egy onmagaban lebego (a testtol elszakado) par lenne.
            (int, int)[] tipToBase = {
                (4, 5), (6, 7),       // farok also/felso vege -> sajat tove
                (8, 10), (9, 10),     // bal melso uszo vegei -> tove
                (11, 13), (12, 13),   // jobb melso uszo vegei -> tove
                (14, 16), (15, 16),   // has-uszo vegei -> tove
                (17, 18), (19, 20),   // hat-uszo elso/hatso vege -> sajat tove
                (22, 21), (23, 21),   // jobb elso uszo vegei -> tove
                (25, 24), (26, 24),   // bal elso uszo vegei -> tove
            };
            foreach (var (tip, baseIdx) in tipToBase)
                AddConstraint(tip, baseIdx, null, tipStiff);
        }
        // A legkozelebbi gerincponti index (27-34) a kezdeti pozicio alapjan
        private int NearestSpineIndex(int idx)
        {
            int best = 27;
            float bestDist = (verlets[idx].Pos - verlets[27].Pos).LengthSquared();
            for (int s = 28; s <= 34; s++)
            {
                float d = (verlets[idx].Pos - verlets[s].Pos).LengthSquared();
                if (d < bestDist) { bestDist = d; best = s; }
            }
            return best;
        }
        public void UpdateCaustics(float time)
        {
            // Kekes ambient a hal aktualis magassagahoz tartozo viz-szin alapjan.
            var ambient = WaterColorSettings.GetColorAtHeight(Position.Y) * 0.6f;
            foreach (var mesh in model.Meshes)
                foreach (var part in mesh.MeshParts)
                {
                    CausticsSettings.Apply(part.Effect, time);
                    part.Effect.Parameters["AmbientColor"]?.SetValue(ambient);
                }
        }
        public void Draw(Camera cam)
        {
            var world = localTransform * WorldTransform;
            // TEMP: a kontrollpontok altal hajtott vertex-morph kikommentezve - a mesh most
            // rigid (bind-pose), csak a teljes test mozgasat/iranyitasat figyeljuk.
            // meshMorph.Update(world, verlets);
            var worldIT = Matrix.Transpose(Matrix.Invert(world));
            foreach (var mesh in model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    var effect = part.Effect;
                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["View"].SetValue(cam.View);
                    effect.Parameters["Projection"].SetValue(cam.Projection);
                    effect.Parameters["WorldIT"].SetValue(worldIT);
                    WaterColorSettings.Apply(effect, cam.Position);
                }
                mesh.Draw();
            }
        }
        public void DrawDebugVerlets(Camera cam, BasicGeometry sphere)
        {
            foreach (var v in verlets)
            {
                var world = Matrix.CreateScale(0.05f) * Matrix.CreateTranslation(v.Pos);
                sphere.Draw(world, cam.View, cam.Projection);
            }
        }
        // Minden constraint-et egy szinezett hengerrel rajzol ki: piros = merev, zold = laza.
        public void DrawDebugConstraints(Camera cam, BasicGeometry cylinder)
        {
            const float thickness = 0.01f;
            for (int i = 0; i < cLengths.Count; i++)
            {
                Vector3 p1 = verlets[cPairs[2 * i]].Pos;
                Vector3 p2 = verlets[cPairs[2 * i + 1]].Pos;
                Vector3 dir = p2 - p1;
                float length = dir.Length();
                if (length < 1e-6f) continue;
                Vector3 dirN = dir / length;
                Vector3 axis = Vector3.Cross(Vector3.Up, dirN);
                Matrix rot;
                if (axis.LengthSquared() < 1e-6f)
                    rot = Vector3.Dot(Vector3.Up, dirN) > 0 ? Matrix.Identity : Matrix.CreateRotationX(MathHelper.Pi);
                else
                    rot = Matrix.CreateFromAxisAngle(Vector3.Normalize(axis),
                        (float)Math.Acos(MathHelper.Clamp(Vector3.Dot(Vector3.Up, dirN), -1f, 1f)));
                var world = Matrix.CreateScale(thickness, length, thickness) * rot * Matrix.CreateTranslation((p1 + p2) * 0.5f);
                cylinder.Effect.DiffuseColor = Color.Lerp(Color.LightGreen, Color.Red, cStiffness[i]).ToVector3();
                cylinder.Draw(world, cam.View, cam.Projection);
            }
        }
        public void Step()
        {
            ApplyForces();
            for (int i = 0; i < verlets.Length; i++)
            {
                verlets[i].Step();
                if (posErrors != null)
                {
                    verlets[i].Pos += posErrors[i] * 0.01f;
                    verlets[i].pPos += posErrors[i] * 0.01f;
                }
            }
            ApplyConstraints();
        }
        private void ApplyForces()
        {
            Vector3 g = new Vector3(0, -9.81f, 0);
            for (int i = 0; i < verlets.Length; i++)
                verlets[i].Acc = g;
            Vector3 d = Vector3.Normalize(Direction);
            Vector3 r = Vector3.Normalize(Right);
            Vector3 u = Vector3.Normalize(Up);
            // 0-3: tavoli, nem a halhoz tartozo pontok (lasd Fish konstruktor), nem kapnak erot.
            for (int i = 4; i < verlets.Length; i++)
            {
                // Semleges felhajtoero: kioltja a gravitaciot, a hal magatol sem nem merul, sem
                // nem all fel - a fuggleges mozgast a dontes (ApplyPitching) es az elorehajtas
                // (a Direction Y-komponense) adja.
                verlets[i].Acc += Vector3.Up * 9.81f;

                // Viz-ellenallas a hal sajat tengelyei szerint (elore/oldal/fuggleges), hogy
                // stabil maradjon es ne pörögjön/sodródjon el korlatlanul.
                verlets[i].AddSqFriction(d, 0.05f);
                verlets[i].AddSqFriction(r, 0.5f);
                verlets[i].AddSqFriction(u, 1f);

                // Lagy hatarok: ne torjon a vizfelszin (y=0) fole, es ne merüljon a tengerfenek ala.
                float height = verlets[i].Pos.Y;
                if (height > 0)
                    verlets[i].Acc -= Vector3.Up * height * 20f;
                else if (height < -55)
                    verlets[i].Acc += Vector3.Up * (-55f - height) * 20f;
            }
            if (ctrlW)
                verlets[5].Acc += d * 10;
            if (ctrlS)
                verlets[5].Acc -= d * 5;

            ApplyTurning(r);
            ApplyPitching();
            ApplyRighting(d, u);
        }
        // Gorges-stabilizacio: a hal "Up" iranya tartson a hal sajat haladasi iranyahoz
        // (Direction) tartozo, oldalra nem dolt "ideal" fel-irany fele. Ez NEM suly/gravitacio
        // alapu - csak megakadalyozza, hogy a test folyamatosan oldalra/hasra forduljon
        // (felboruljon), miközben a szandekolt dontes (pitch) szabadon ervenyesülhet, mert az
        // ideal-up a Direction dolesevel egyutt mozog.
        private void ApplyRighting(Vector3 d, Vector3 u)
        {
            Vector3 idealUp = Vector3.Up - Vector3.Dot(Vector3.Up, d) * d;
            if (idealUp.LengthSquared() < 1e-6f)
                return;
            idealUp = Vector3.Normalize(idealUp);

            Vector3 upError = idealUp - u;
            const float rightingStrength = 3f;
            verlets[18].Acc += upError * rightingStrength;
            verlets[20].Acc += upError * rightingStrength;
            verlets[16].Acc -= upError * rightingStrength;
        }
        // Dontes (pitch): a gerinc fuggleges iranyban hajlik a cel-dontes ("pitchTarget", egerrel
        // allitva) fele, hasonloan az ApplyTurning-hoz, csak vilag-Y tengely menten.
        private void ApplyPitching()
        {
            pitchSignal = MathHelper.Lerp(pitchSignal, pitchTarget, 0.1f);
            if (Math.Abs(pitchSignal) < 1e-4f)
                return;

            for (int i = 27; i < 34; i++)
            {
                float weight = (34 - i) / 7f;
                verlets[i].Acc += Vector3.Up * pitchSignal * weight * 6f;
            }

            verlets[4].Acc -= Vector3.Up * pitchSignal * 8f;
            verlets[6].Acc -= Vector3.Up * pitchSignal * 8f;
        }
        // Kanyarodas: a gerinc a kanyar iranyaba hajlik (farok fele nagyobb mertekben), a farok
        // es az oldaluszok pedig aktivan a kanyar iranyaba "csapnak" - lapatolo/usz0 mozgas.
        private void ApplyTurning(Vector3 r)
        {
            float turnTarget = (ctrlA ? 1f : 0f) - (ctrlD ? 1f : 0f);
            turnSignal = MathHelper.Lerp(turnSignal, turnTarget, 0.1f);
            if (Math.Abs(turnSignal) < 1e-4f)
                return;

            // Gerinc hajlitasa: oldalirany ero a csigolyakra, a farok fele (27) novekvo sullyal
            for (int i = 27; i < 34; i++)
            {
                float weight = (34 - i) / 7f;
                verlets[i].Acc += r * turnSignal * weight * 6f;
            }

            // Farok vegei a gerinc hajlasaval ellentetes iranyba csapnak (uszo-mozgas)
            verlets[4].Acc -= r * turnSignal * 8f;
            verlets[6].Acc -= r * turnSignal * 8f;

            // Oldaluszok: bal/jobb par ellentetes elojellel - lapatolo mozgas
            verlets[8].Acc += r * turnSignal * 4f;
            verlets[9].Acc += r * turnSignal * 4f;
            verlets[25].Acc += r * turnSignal * 4f;
            verlets[26].Acc += r * turnSignal * 4f;

            verlets[11].Acc -= r * turnSignal * 4f;
            verlets[12].Acc -= r * turnSignal * 4f;
            verlets[22].Acc -= r * turnSignal * 4f;
            verlets[23].Acc -= r * turnSignal * 4f;
        }
    }
}
