using System.IO;
using GalacticRupture.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GalacticRupture.EditorTools
{
    // Builds every generated asset and the playable scene from code, so nothing depends on
    // hand-edited YAML. Run from the menu or: unity run . -- -executeMethod
    // GalacticRupture.EditorTools.ProjectBuilder.BuildAll
    public static class ProjectBuilder
    {
        const string ArtDir = "Assets/Art";
        const string MatDir = "Assets/Art/Materials";
        const string TexDir = "Assets/Art/Textures";
        const string ScenePath = "Assets/Scenes/TurretToy.unity";
        const string ArtAssetPath = "Assets/Data/GameArt.asset";
        const string ProfilePath = "Assets/Settings/SpaceVolumeProfile.asset";

        [MenuItem("Galactic Rupture/Build Everything")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(TexDir);
            ConfigurePipeline();
            ConfigurePlayer();
            var tex = Textures.Build(TexDir);
            var art = BuildMaterials(tex);
            var sky = BuildSkybox();
            var profile = BuildVolumeProfile();
            BuildScene(art, sky, profile);
            AssetDatabase.SaveAssets();
            Debug.Log("[builder] done");
        }

        static void ConfigurePipeline()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("m_SupportsHDR").boolValue = true;
            so.FindProperty("m_HDRColorBufferPrecision").intValue = 1; // 64-bit: smoother bloom
            so.FindProperty("m_MSAA").intValue = 4;
            so.FindProperty("m_ShadowDistance").floatValue = 3500f;
            so.FindProperty("m_ShadowCascadeCount").intValue = 2;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = 4096;
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_ShadowDepthBias").floatValue = 1.2f;
            so.FindProperty("m_ShadowNormalBias").floatValue = 0.8f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Galactic Rupture";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        static Material Lit(string name, Color color, float metallic, float smoothness, Textures.Set tex = null, float bump = 0.6f)
        {
            var m = LoadOrCreate(name, Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex.Albedo);
                m.SetTexture("_BumpMap", tex.Normal);
                m.SetFloat("_BumpScale", bump);
                m.EnableKeyword("_NORMALMAP");
            }
            return m;
        }

        static Material Emissive(string name, Color baseColor, Color emission)
        {
            var m = Lit(name, baseColor, 0f, 0.5f);
            m.SetColor("_EmissionColor", emission);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return m;
        }

        static Material Additive(string name, Color color, float intensity, bool tracer, Texture tex = null, float softness = 3f)
        {
            var m = LoadOrCreate(name, Shader.Find("GR/Additive"));
            m.SetColor("_Color", color);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_Mode", tracer ? 1f : 0f);
            m.SetFloat("_Softness", softness);
            if (tex != null) m.SetTexture("_MainTex", tex);
            m.enableInstancing = true;
            return m;
        }

        static Material LoadOrCreate(string name, Shader shader)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameArt BuildMaterials(Textures.Result tex)
        {
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(ArtAssetPath);
            if (art == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ArtAssetPath));
                art = ScriptableObject.CreateInstance<GameArt>();
                AssetDatabase.CreateAsset(art, ArtAssetPath);
            }

            art.HullPaint = new[]
            {
                Lit("Hull_Continuance", new Color(0.27f, 0.30f, 0.35f), 0.12f, 0.5f, tex.Panels),
                Lit("Hull_Remnant", new Color(0.38f, 0.06f, 0.07f), 0.12f, 0.5f, tex.Panels),
                Lit("Hull_Serac", new Color(0.14f, 0.30f, 0.27f), 0.12f, 0.5f, tex.Panels),
                Lit("Hull_Ilium", new Color(0.42f, 0.31f, 0.16f), 0.12f, 0.5f, tex.Panels),
            };
            art.Trim = new[]
            {
                Lit("Trim_Continuance", new Color(0.86f, 0.64f, 0.28f), 0.9f, 0.65f),
                Lit("Trim_Remnant", new Color(0.72f, 0.68f, 0.58f), 0.05f, 0.35f),
                Lit("Trim_Serac", new Color(0.82f, 0.80f, 0.70f), 0.05f, 0.4f),
                Lit("Trim_Ilium", new Color(0.33f, 0.22f, 0.13f), 0.8f, 0.5f),
            };
            art.DarkMetal = Lit("DarkMetal", new Color(0.12f, 0.125f, 0.135f), 0.35f, 0.45f, tex.Panels, 0.4f);
            art.Windows = Emissive("Windows", Color.black, new Color(1f, 0.72f, 0.42f) * 2.2f);
            art.EngineGlow = new[]
            {
                Emissive("Engine_Continuance", Color.black, new Color(0.35f, 0.65f, 1f) * 5f),
                Emissive("Engine_Remnant", Color.black, new Color(1f, 0.3f, 0.25f) * 5f),
                Emissive("Engine_Serac", Color.black, new Color(0.3f, 1f, 0.75f) * 5f),
                Emissive("Engine_Ilium", Color.black, new Color(1f, 0.7f, 0.3f) * 5f),
            };
            art.Heat = Emissive("Heat", Color.black, new Color(1f, 0.28f, 0.06f) * 1.6f);
            art.Wreck = Lit("Wreck", new Color(0.05f, 0.045f, 0.04f), 0.2f, 0.12f, tex.Panels, 1f);

            art.Plume = new[]
            {
                Additive("Plume_Continuance", new Color(0.35f, 0.6f, 1f), 2.2f, true, softness: 1.4f),
                Additive("Plume_Remnant", new Color(1f, 0.3f, 0.22f), 2.2f, true, softness: 1.4f),
                Additive("Plume_Serac", new Color(0.3f, 1f, 0.7f), 2.2f, true, softness: 1.4f),
                Additive("Plume_Ilium", new Color(1f, 0.65f, 0.25f), 2.2f, true, softness: 1.4f),
            };
            art.TracerBallistic = Additive("Tracer_Ballistic", new Color(1f, 0.6f, 0.28f), 14f, true, softness: 2.5f);
            art.TracerEnergy = Additive("Tracer_Energy", new Color(0.35f, 0.8f, 1f), 12f, true, softness: 2.5f);
            art.TracerPointDefence = Additive("Tracer_PD", new Color(1f, 0.85f, 0.5f), 9f, true, softness: 2f);
            art.TracerEnemy = Additive("Tracer_Enemy", new Color(1f, 0.22f, 0.36f), 12f, true, softness: 2.5f);

            art.HullModels = FindModels("hull_");
            art.TurretModels = FindModels("turret_");

            art.Flash = Additive("Fx_Flash", Color.white, 5f, false, tex.Glow);
            art.Spark = Additive("Fx_Spark", Color.white, 7f, false, tex.Glow);
            art.Fire = Additive("Fx_Fire", Color.white, 2.2f, false, tex.Glow);
            art.Shield = Additive("Fx_Shield", Color.white, 3f, false, tex.Ring);

            art.MapPlate = LoadOrCreate("Map_Plate", Shader.Find("GR/MapPlate"));
            art.Star = LoadOrCreate("Map_Star", Shader.Find("GR/Star"));
            art.Lane = Additive("Map_Lane", Color.white, 1.4f, false);
            art.Lane.SetFloat("_Mode", 2f);
            art.Planet = Lit("Map_Planet", new Color(0.5f, 0.5f, 0.5f), 0f, 0.25f);
            art.Planet.enableInstancing = true;
            art.UiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Rajdhani-Medium.ttf");
            art.UiFontBold = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Rajdhani-Bold.ttf");

            art.ShieldField = LoadOrCreate("Shield_Field", Shader.Find("GR/Shield"));
            art.Scorch = LoadOrCreate("Fx_Scorch", Shader.Find("GR/Scorch"));
            art.Scorch.SetTexture("_MainTex", tex.Glow);
            art.Scorch.enableInstancing = true;

            EditorUtility.SetDirty(art);
            return art;
        }

        static ModelEntry[] FindModels(string prefix)
        {
            var list = new System.Collections.Generic.List<ModelEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (!file.StartsWith(prefix)) continue;
                list.Add(new ModelEntry { Id = file.Substring(prefix.Length), Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) });
            }
            Debug.Log($"[builder] {prefix}: {string.Join(", ", list.ConvertAll(e => e.Id))}");
            return list.ToArray();
        }

        static Material BuildSkybox()
        {
            var m = LoadOrCreate("Skybox_Space", Shader.Find("GR/SpaceSkybox"));
            m.SetFloat("_NebulaIntensity", 0.32f);
            m.SetFloat("_StarBrightness", 3f);
            return m;
        }

        static VolumeProfile BuildVolumeProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null) AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.72f);
            bloom.highQualityFiltering.Override(true);
            bloom.tint.Override(new Color(1f, 0.96f, 0.92f));

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.2f);
            color.contrast.Override(18f);
            color.saturation.Override(8f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.06f);

            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.12f);

            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void BuildScene(GameArt art, Material sky, VolumeProfile profile)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.78f);
            sun.intensity = 1.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.92f;
            sunGo.transform.rotation = Quaternion.Euler(14f, -118f, 0f);

            // Cool fill from the camera side so shadowed hulls keep their shape.
            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.72f, 0.78f, 0.95f);
            fill.intensity = 1.15f;
            fill.shadows = LightShadows.None;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 120000f;
            cam.fieldOfView = 42f;
            cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.Skybox;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            var rig = camGo.AddComponent<CameraRig>();
            fillGo.AddComponent<ViewFill>().Camera = camGo.transform;

            var volGo = new GameObject("Post Processing");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;

            var probeGo = new GameObject("Sky Reflection");
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.cullingMask = 0;
            probe.size = Vector3.one * 200000f;
            probe.resolution = 256;
            probe.hdr = true;
            probe.intensity = 0.8f;

            var gameGo = new GameObject("Game");
            var runner = gameGo.AddComponent<SimRunner>();
            runner.Art = art;
            rig.Runner = runner;
            gameGo.AddComponent<SkyController>().Sun = sun;
            var input = gameGo.AddComponent<CommandInput>();
            input.Runner = runner;
            input.Rig = rig;
            var overlay = gameGo.AddComponent<CommandOverlay>();
            overlay.Runner = runner;
            overlay.Input = input;
            overlay.LineMaterial = Additive("Ui_Line", Color.white, 1.6f, false);
            var hud = gameGo.AddComponent<Hud>();
            hud.Runner = runner;
            hud.Rig = rig;
            hud.Input = input;

            // The campaign: galaxy map, its camera and screen, and the director that hands
            // battles to the tactical view and back.
            var mapGo = new GameObject("Galaxy Map");
            var map = mapGo.AddComponent<MapView>();
            map.Art = art;
            map.LineMaterial = overlay.LineMaterial;
            var mapCam = camGo.AddComponent<MapCamera>();
            var director = gameGo.AddComponent<CampaignDirector>();
            director.Runner = runner;
            director.TacticalCamera = rig;
            director.MapCamera = mapCam;
            director.Map = map;
            hud.Director = director;
            var campaignHud = gameGo.AddComponent<CampaignHud>();
            campaignHud.Director = director;
            campaignHud.Map = map;
            campaignHud.Camera = cam;
            campaignHud.Art = art;
            var campaignCapture = gameGo.AddComponent<CampaignCapture>();
            campaignCapture.Director = director;
            campaignCapture.Hud = campaignHud;
            campaignCapture.Map = map;
            campaignCapture.Input = input;
            campaignCapture.Runner = runner;

            var bench = gameGo.AddComponent<BenchMode>();
            bench.Runner = runner;
            bench.Rig = rig;
            var rtsCapture = gameGo.AddComponent<RtsCapture>();
            rtsCapture.Runner = runner;
            rtsCapture.Rig = rig;
            rtsCapture.Input = input;
            var capture = gameGo.AddComponent<CaptureMode>();
            capture.Runner = runner;
            capture.Rig = rig;

            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.13f, 0.145f, 0.2f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.08f, 0.1f);
            RenderSettings.ambientGroundColor = new Color(0.02f, 0.015f, 0.02f);
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
