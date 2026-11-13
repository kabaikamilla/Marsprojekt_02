// DSZP Unity 11 – Marsbázis: tanári segédeszközök (Editor-script).
// Használat: tedd az Assets/Editor mappába, majd a DSZP menüből futtasd.
//   DSZP → Hengerek Collidere javítása  : a nyitott jelenetben minden henger Capsule Colliderét
//                                         Mesh Colliderre cseréli (LeszalloPlatform, VedelmiPont_1–3, Antenna).
//   DSZP → Tanári drón hozzáadása       : a nyitott jelenetbe felépíti a Kenney-stílusú drónt
//                                         (Rigidbody, DronMozgas) és a DronKamerát.
//   DSZP → Modell illesztése a graybox helyére : a kijelölt graybox kocka helyére méretezi
//                                         a gyerekként behúzott modellt (Kenney Space Kit).
// A SABLONREPÓBA NEM KERÜLHET: commit előtt töröld az Assets/Editor mappából.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DszpTanariEszkozok
{
    // ------------------------------------------------------------------
    // 1. Hengerek Collidere
    // ------------------------------------------------------------------
    [MenuItem("DSZP/Hengerek Collidere javítása")]
    public static void HengerColliderJavitas()
    {
        int javitott = 0;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || mf.sharedMesh.name != "Cylinder") continue;
            var cap = mf.GetComponent<CapsuleCollider>();
            if (cap == null) continue;

            Undo.DestroyObjectImmediate(cap);
            // Mozgó testen (Rigidbody a szülőn, pl. a drón rotorjai) nem lehet nem-convex Mesh Collider:
            // ott csak töröljük a kapszulát.
            if (mf.GetComponentInParent<Rigidbody>() == null)
                Undo.AddComponent<MeshCollider>(mf.gameObject);

            Debug.Log("[DSZP] Henger Collider javítva: " + Utvonal(mf.transform), mf.gameObject);
            javitott++;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[DSZP] Kész: " + javitott + " henger javítva. Mentés: Ctrl+S.");
    }

    // ------------------------------------------------------------------
    // 2. Tanári drón
    // ------------------------------------------------------------------
    [MenuItem("DSZP/Tanári drón hozzáadása")]
    public static void TanariDron()
    {
        var jatekmenet = GameObject.Find("MarsBazis/Jatekmenet");
        if (jatekmenet == null)
        {
            EditorUtility.DisplayDialog("Tanári drón", "Nem találom a MarsBazis/Jatekmenet objektumot. Nyisd meg a MarsBase_02 jelenetet.", "OK");
            return;
        }

        var regiDron = jatekmenet.transform.Find("Dron");
        var regiKamera = GameObject.Find("DronKamera");
        if ((regiDron != null || regiKamera != null) &&
            !EditorUtility.DisplayDialog("Tanári drón", "Már van Dron és/vagy DronKamera a jelenetben. Lecseréljem őket?", "Csere", "Mégse"))
            return;
        if (regiDron != null) Undo.DestroyObjectImmediate(regiDron.gameObject);
        if (regiKamera != null) Undo.DestroyObjectImmediate(regiKamera);

        var mDronTest = DronTestMaterial();
        var mSotetFem = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_SotetFem.mat");
        var mJelzofeny = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jelzofeny.mat");
        if (mSotetFem == null || mJelzofeny == null)
            Debug.LogWarning("[DSZP] Hiányzik az M_SotetFem vagy az M_Jelzofeny material az Assets/Materials mappából.");

        // Dron szülő
        var dron = new GameObject("Dron");
        Undo.RegisterCreatedObjectUndo(dron, "Tanári drón");
        dron.transform.SetParent(jatekmenet.transform, false);
        dron.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        // Alkatrészek (lokális értékek, a projektdokumentum táblázata szerint)
        Alkatresz("Torzs", PrimitiveType.Sphere, dron.transform, new Vector3(0f, 0f, 0f), 0f, new Vector3(1.2f, 0.5f, 1.4f), mDronTest);
        Alkatresz("Kar_1", PrimitiveType.Cube, dron.transform, Vector3.zero, 45f, new Vector3(2.6f, 0.2f, 0.3f), mSotetFem);
        Alkatresz("Kar_2", PrimitiveType.Cube, dron.transform, Vector3.zero, -45f, new Vector3(2.6f, 0.2f, 0.3f), mSotetFem);
        var rotorSkala = new Vector3(0.8f, 0.03f, 0.8f);
        Alkatresz("Rotor_1", PrimitiveType.Cylinder, dron.transform, new Vector3(0.92f, 0.12f, 0.92f), 0f, rotorSkala, mSotetFem);
        Alkatresz("Rotor_2", PrimitiveType.Cylinder, dron.transform, new Vector3(-0.92f, 0.12f, 0.92f), 0f, rotorSkala, mSotetFem);
        Alkatresz("Rotor_3", PrimitiveType.Cylinder, dron.transform, new Vector3(0.92f, 0.12f, -0.92f), 0f, rotorSkala, mSotetFem);
        Alkatresz("Rotor_4", PrimitiveType.Cylinder, dron.transform, new Vector3(-0.92f, 0.12f, -0.92f), 0f, rotorSkala, mSotetFem);
        Alkatresz("Orrfeny", PrimitiveType.Sphere, dron.transform, new Vector3(0f, 0.05f, 0.72f), 0f, new Vector3(0.25f, 0.25f, 0.25f), mJelzofeny);

        // Rigidbody a szülőn
        var rb = dron.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // DronMozgas script, ha létezik
        var mozgas = System.Type.GetType("DronMozgas, Assembly-CSharp");
        if (mozgas != null) dron.AddComponent(mozgas);
        else Debug.LogWarning("[DSZP] Nincs DronMozgas script az Assets/Scripts mappában – a drón script nélkül készült el.");

        DronKamera(dron.transform);

        Selection.activeGameObject = dron;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[DSZP] Tanári drón kész. Mentés: Ctrl+S.");
    }

    // ------------------------------------------------------------------
    // 3. Modellcsere: a graybox Cube helyére a gyerekként behúzott modell
    // ------------------------------------------------------------------
    // Menete: húzd a modellt (FBX) a Project ablakból a graybox objektumra a Hierarchyban
    // (így a gyereke lesz), ha kell, forgasd el Y körül, majd jelöld ki a GRAYBOXOT, és futtasd.
    // A script: a graybox Scale-jét 1-re állítja, leveszi róla a kockát (Mesh Renderer + Mesh Filter),
    // a modellt arányosan akkorára méretezi, hogy beleférjen a graybox alapterületébe, a talpát
    // a graybox aljára teszi, végül a Box Collidert a modell méretére igazítja.
    // Név, szülő, scriptek (pl. EnergiaKapcsolo) maradnak. Visszavonás: Ctrl+Z.
    [MenuItem("DSZP/Modell illesztése a graybox helyére")]
    public static void ModellIllesztes()
    {
        var gb = Selection.activeGameObject;
        if (gb == null || gb.transform.childCount == 0)
        {
            EditorUtility.DisplayDialog("Modellcsere", "Jelöld ki a graybox objektumot (pl. KozpontiModul), amelynek a gyereke a behúzott modell.", "OK");
            return;
        }
        var mr = gb.GetComponent<MeshRenderer>();
        var mf = gb.GetComponent<MeshFilter>();
        var box = gb.GetComponent<BoxCollider>();
        if (mr == null || mf == null || box == null)
        {
            EditorUtility.DisplayDialog("Modellcsere", "A kijelölt objektum nem graybox kocka (Mesh Renderer + Box Collider kell rajta). Ha már egyszer illesztetted, Ctrl+Z-vel lépj vissza.", "OK");
            return;
        }

        var t = gb.transform;
        var modell = t.GetChild(0);
        Undo.RegisterFullObjectHierarchyUndo(gb, "Modellcsere");

        // A graybox eredeti mérete és helye (világkoordinátában; a rendszerező szülők Scale-je 1)
        Vector3 meret = Vector3.Scale(box.size, t.lossyScale);
        Vector3 kozep = t.TransformPoint(box.center);
        float alja = kozep.y - meret.y / 2f;

        // A modellt ideiglenesen kivesszük, hogy a graybox átméretezése ne torzítsa
        Undo.SetTransformParent(modell, null, "Modellcsere");
        Undo.RecordObject(t, "Modellcsere");
        t.localScale = Vector3.one;
        Undo.DestroyObjectImmediate(mr);
        Undo.DestroyObjectImmediate(mf);
        Undo.SetTransformParent(modell, t, "Modellcsere");

        Undo.RecordObject(modell, "Modellcsere");
        modell.name = "Modell";
        modell.localPosition = Vector3.zero;
        Vector3 s0 = modell.localScale;      // az FBX saját skálája (és forgatása) megmarad

        var b = Hatarok(modell);
        if (b.size.x < 1e-4f || b.size.z < 1e-4f)
        {
            Debug.LogWarning("[DSZP] A modellnek nincs látható része (Renderer) – nem tudom méretezni.");
            return;
        }
        float k = Mathf.Min(meret.x / b.size.x, meret.z / b.size.z);
        modell.localScale = s0 * k;

        b = Hatarok(modell);
        modell.position += new Vector3(kozep.x - b.center.x, alja - b.min.y, kozep.z - b.center.z);

        // Box Collider a modell méretére
        b = Hatarok(modell);
        Undo.RecordObject(box, "Modellcsere");
        box.center = t.InverseTransformPoint(b.center);
        box.size = new Vector3(b.size.x / t.lossyScale.x, b.size.y / t.lossyScale.y, b.size.z / t.lossyScale.z);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[DSZP] {gb.name}: modell illesztve. Alapterület {b.size.x:0.0} × {b.size.z:0.0}, magasság {b.size.y:0.0} " +
                  $"(a graybox {meret.x:0.0} × {meret.z:0.0}, magasság {meret.y:0.0} volt). Mentés: Ctrl+S.", gb);
    }

    static Bounds Hatarok(Transform gyoker)
    {
        var rs = gyoker.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(gyoker.position, Vector3.zero);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // ------------------------------------------------------------------
    // segédfüggvények
    // ------------------------------------------------------------------
    static void Alkatresz(string nev, PrimitiveType tipus, Transform szulo, Vector3 pos, float forgY, Vector3 skala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipus);
        go.name = nev;
        go.transform.SetParent(szulo, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = new Vector3(0f, forgY, 0f);
        go.transform.localScale = skala;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        // A rotorokon nem kell Collider (a lapos hengeren a kapszula gömbbé torzulna).
        if (tipus == PrimitiveType.Cylinder) Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
    }

    static Material DronTestMaterial()
    {
        const string utvonal = "Assets/Materials/M_DronTest.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(utvonal);
        if (m != null) return m; // ha már van (pl. pipettával átszínezted), nem írjuk felül

        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(sh);
        ColorUtility.TryParseHtmlString("#D8DCE0", out var szin);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", szin);
        if (m.HasProperty("_Color")) m.SetColor("_Color", szin);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(m, utvonal);
        AssetDatabase.SaveAssets();
        return m;
    }

    // A Cinemachine 3 típusait név alapján keressük, így a script akkor is lefordul,
    // ha a csomag hiányzik (akkor csak a kamera marad el).
    static void DronKamera(Transform cel)
    {
        var tCamera = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        var tFollow = System.Type.GetType("Unity.Cinemachine.CinemachineFollow, Unity.Cinemachine");
        var tComposer = System.Type.GetType("Unity.Cinemachine.CinemachineRotationComposer, Unity.Cinemachine");
        var tBrain = System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
        if (tCamera == null || tFollow == null || tBrain == null)
        {
            Debug.LogWarning("[DSZP] Nem találom a Cinemachine 3 csomagot – a DronKamera nem készült el.");
            return;
        }

        var kamera = new GameObject("DronKamera");
        Undo.RegisterCreatedObjectUndo(kamera, "Tanári drón");
        kamera.transform.position = cel.position + new Vector3(0f, 6f, -10f);

        var cm = kamera.AddComponent(tCamera);
        var follow = kamera.AddComponent(tFollow);
        if (tComposer != null) kamera.AddComponent(tComposer);

        // Tracking Target = Dron
        var soCam = new SerializedObject(cm);
        var target = soCam.FindProperty("Target.TrackingTarget");
        if (target != null) { target.objectReferenceValue = cel; soCam.ApplyModifiedPropertiesWithoutUndo(); }
        else Debug.LogWarning("[DSZP] A Tracking Target mezőt kézzel kell beállítani (Dron).");

        // Follow: World Space, offset (0, 6, -10)
        var soF = new SerializedObject(follow);
        var offset = soF.FindProperty("FollowOffset");
        if (offset != null) offset.vector3Value = new Vector3(0f, 6f, -10f);
        var mod = soF.FindProperty("TrackerSettings.BindingMode");
        if (mod != null)
        {
            int idx = System.Array.IndexOf(mod.enumNames, "WorldSpace");
            if (idx >= 0) mod.enumValueIndex = idx;
            else Debug.LogWarning("[DSZP] A Binding Mode-ot kézzel állítsd World Space-re.");
        }
        soF.ApplyModifiedPropertiesWithoutUndo();

        // Cinemachine Brain a Main Camerán
        var main = Camera.main;
        if (main != null && main.GetComponent(tBrain) == null) Undo.AddComponent(main.gameObject, tBrain);
    }

    static string Utvonal(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
