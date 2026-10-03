using UnityEditor;
using UnityEngine;
using KinectKids.Games.GreveGast;

namespace KinectKids3D.Editor
{
    public sealed class GastSongEditor : EditorWindow
    {
        private GastSongAsset song;
        private AudioSource preview;
        private Vector2 scroll;
        private string sectionFilter = "All";
        private float[] waveform;
        private GastSongAsset waveformOwner;

        [MenuItem("KinectKids/Greve Gast/Cue Editor")]
        private static void Open() => GetWindow<GastSongEditor>("Gast Cue Editor");

        [MenuItem("KinectKids/Greve Gast/Create song timeline")]
        public static void CreateTimeline()
        {
            const string path = "Assets/Resources/GastSong.asset";
            if (AssetDatabase.LoadAssetAtPath<GastSongAsset>(path) != null)
            { Selection.activeObject = AssetDatabase.LoadAssetAtPath<GastSongAsset>(path); return; }
            Selection.activeObject = CreateSongAsset();
        }

        [InitializeOnLoadMethod]
        private static void EnsureDefaultTimeline()
        {
            EditorApplication.delayCall += () =>
            {
                const string path = "Assets/Resources/GastSong.asset";
                if (AssetDatabase.LoadAssetAtPath<GastSongAsset>(path) != null) return;
                CreateSongAsset();
                Debug.Log("Created editable Greve Gast song timeline at " + path + ".");
            };
        }

        private void OnGUI()
        {
            song = (GastSongAsset)EditorGUILayout.ObjectField("Song data", song, typeof(GastSongAsset), false);
            if (song == null)
            {
                EditorGUILayout.HelpBox("Create the editable song timeline. Audio master: Audio/Music/GreveGastsJakt.wav.", MessageType.Info);
                if (GUILayout.Button("Create GastSong.asset")) song = CreateSongAsset();
                return;
            }
            if (song.master == null) EditorGUILayout.HelpBox("Audio master is missing from the asset reference.", MessageType.Warning);
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown)
                Undo.RecordObject(song, "Edit Gast song timeline");
            using (new EditorGUILayout.HorizontalScope())
            {
                sectionFilter = EditorGUILayout.TextField("Section filter", sectionFilter);
                if (GUILayout.Button("Add marker at playhead", GUILayout.Width(170))) AddCue();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play")) EnsurePreview().Play();
                if (GUILayout.Button("Pause")) { if (preview != null) preview.Pause(); }
                if (GUILayout.Button("Stop")) { if (preview != null) preview.Stop(); }
                if (GUILayout.Button("Validate")) ValidateSong();
                if (GUILayout.Button("Save")) AssetDatabase.SaveAssets();
                if (preview != null && preview.clip != null)
                    EditorGUILayout.LabelField(TimeLabel(preview.time), "/ " + TimeLabel(preview.clip.length), GUILayout.Width(115));
            }
            if (song.master != null)
            {
                float old = preview != null ? preview.time : 0f;
                float next = EditorGUILayout.Slider(old, 0f, song.master.length);
                if (preview != null && Mathf.Abs(next - old) > 0.005f) preview.time = next;
                Rect rect = GUILayoutUtility.GetRect(10, 46, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
                DrawWaveform(rect);
            }
            song.globalOffsetSeconds = EditorGUILayout.FloatField("Global offset", song.globalOffsetSeconds);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Sections", EditorStyles.boldLabel);
            if (song.sections != null) for (int i = 0; i < song.sections.Count; i++)
            {
                GastSongSection section = song.sections[i]; if (section == null) continue;
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    section.id = EditorGUILayout.TextField("ID", section.id);
                    section.title = EditorGUILayout.TextField("Title", section.title);
                    section.hasStartTime = EditorGUILayout.Toggle("Start is timed", section.hasStartTime);
                    if (section.hasStartTime)
                    {
                        section.startSeconds = EditorGUILayout.FloatField("Start (s)", section.startSeconds);
                        section.startTimingStatus = (GastTimingStatus)EditorGUILayout.EnumPopup("Start status", section.startTimingStatus);
                    }
                    section.hasEndTime = EditorGUILayout.Toggle("End is timed", section.hasEndTime);
                    if (section.hasEndTime)
                    {
                        section.endSeconds = EditorGUILayout.FloatField("End (s)", section.endSeconds);
                        section.endTimingStatus = (GastTimingStatus)EditorGUILayout.EnumPopup("End status", section.endTimingStatus);
                    }
                    section.offsetSeconds = EditorGUILayout.FloatField("Section offset", section.offsetSeconds);
                }
            }
            if (song.cues != null) for (int i = 0; i < song.cues.Count; i++)
            {
                GastSongCue cue = song.cues[i]; if (cue == null) continue;
                if (sectionFilter != "All" && (cue.sectionId ?? string.Empty).IndexOf(sectionFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(cue.id + "  |  " + cue.timingStatus, EditorStyles.boldLabel);
                    cue.sectionId = EditorGUILayout.TextField("Section", cue.sectionId);
                    cue.lyric = EditorGUILayout.TextField("Lyric / note", cue.lyric);
                    cue.hasVocalTime = EditorGUILayout.Toggle("Has vocal time", cue.hasVocalTime);
                    using (new EditorGUI.DisabledScope(!cue.hasVocalTime))
                    {
                        cue.vocalTimeSeconds = EditorGUILayout.FloatField("Vocal time (s)", cue.vocalTimeSeconds);
                        EditorGUILayout.LabelField("Effective time", TimeLabel(cue.EffectiveTime(song)));
                    }
                    cue.timingStatus = (GastTimingStatus)EditorGUILayout.EnumPopup("Timing", cue.timingStatus);
                    cue.cueOffsetSeconds = EditorGUILayout.FloatField("Cue offset", cue.cueOffsetSeconds);
                    cue.gestureLeadSeconds = EditorGUILayout.FloatField("Gesture lead", cue.gestureLeadSeconds);
                    cue.warningLeadSeconds = EditorGUILayout.FloatField("Warning lead", cue.warningLeadSeconds);
                    cue.impactOffsetSeconds = EditorGUILayout.FloatField("Impact offset", cue.impactOffsetSeconds);
                    cue.windowBeforeSeconds = EditorGUILayout.FloatField("Window before", cue.windowBeforeSeconds);
                    cue.windowAfterSeconds = EditorGUILayout.FloatField("Window after", cue.windowAfterSeconds);
                    cue.action = (GastCueAction)EditorGUILayout.EnumPopup("Action", cue.action);
                    cue.gameplayCommand = EditorGUILayout.Toggle("Gameplay command", cue.gameplayCommand);
                    if (cue.gameplayCommand) cue.command = (GastGameplayCommand)EditorGUILayout.EnumPopup("Command", cue.command);
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUI.changed) { EditorUtility.SetDirty(song); }
        }

        private AudioSource EnsurePreview()
        {
            if (preview == null) { var go = EditorUtility.CreateGameObjectWithHideFlags("Gast song preview", HideFlags.HideAndDontSave, typeof(AudioSource)); preview = go.GetComponent<AudioSource>(); preview.clip = song.master; preview.loop = false; }
            return preview;
        }
        private void DrawWaveform(Rect rect)
        {
            if (waveformOwner != song) BuildWaveform();
            if (waveform == null || waveform.Length == 0) return;
            Handles.BeginGUI();
            Handles.color = new Color(0.35f, 0.8f, 0.95f);
            float mid = rect.y + rect.height * 0.5f;
            for (int i = 0; i < waveform.Length; i++)
            {
                float x = rect.x + rect.width * i / Mathf.Max(1, waveform.Length - 1);
                float h = waveform[i] * rect.height * 0.46f;
                Handles.DrawLine(new Vector3(x, mid - h), new Vector3(x, mid + h));
            }
            Handles.EndGUI();
        }
        private void BuildWaveform()
        {
            waveformOwner = song;
            waveform = null;
            if (song == null || song.master == null || !song.master.LoadAudioData()) return;
            const int count = 1200;
            const int window = 128;
            float[] samples = new float[window * song.master.channels];
            waveform = new float[count];
            for (int i = 0; i < count; i++)
            {
                int sampleOffset = (int)((long)i * song.master.samples / count);
                if (!song.master.GetData(samples, sampleOffset)) { waveform = null; return; }
                float peak = 0f;
                for (int j = 0; j < samples.Length; j++) peak = Mathf.Max(peak, Mathf.Abs(samples[j]));
                waveform[i] = Mathf.Sqrt(peak);
            }
        }
        private void AddCue()
        {
            Undo.RecordObject(song, "Add Gast cue");
            float t = preview != null ? preview.time : 0f;
            song.cues.Add(new GastSongCue { id = "cue_" + System.Guid.NewGuid().ToString("N").Substring(0, 8), sectionId = "section_01", lyric = "", hasVocalTime = true, vocalTimeSeconds = t, timingStatus = GastTimingStatus.Estimated });
            EditorUtility.SetDirty(song);
        }
        private void ValidateSong()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            int errors = 0;
            foreach (var cue in song.cues)
            {
                if (cue == null) continue;
                if (string.IsNullOrEmpty(cue.id) || !ids.Add(cue.id)) { Debug.LogError("Gast cue has an empty or duplicate ID: " + cue.id, song); errors++; }
                if (cue.gestureLeadSeconds < 0 || cue.warningLeadSeconds < 0 || cue.windowBeforeSeconds < 0 || cue.windowAfterSeconds < 0) { Debug.LogError("Negative lead/window in " + cue.id, song); errors++; }
                if (cue.hasVocalTime && song.master != null && (cue.vocalTimeSeconds < 0 || cue.EffectiveTime(song) > song.master.length)) { Debug.LogError("Cue time is outside the master: " + cue.id, song); errors++; }
            }
            Debug.Log(errors == 0 ? "Gast song validation passed." : "Gast song validation found " + errors + " issue(s).", song);
        }
        private static string TimeLabel(float seconds) { int m = Mathf.FloorToInt(seconds / 60f); return m.ToString("00") + ":" + (seconds - 60f * m).ToString("00.000"); }
        private static GastSongAsset CreateSongAsset()
        {
            const string folder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Resources");
            var asset = CreateInstance<GastSongAsset>();
            asset.master = Resources.Load<AudioClip>("Audio/Music/GreveGastsJakt");
            string[] names = { "Introduktion", "Första versen", "Första refrängen", "Instrumental jakt", "Köket", "Rörelseföljd", "Refräng och vasen", "Galleriet", "Ny rörelsepaus", "Falsk trygghet", "Källaren", "Kommandon och svar", "Sista uppbyggnaden", "Slutrefräng", "Rörelser", "Nedräkning", "Slutet" };
            for (int i = 0; i < names.Length; i++) asset.sections.Add(new GastSongSection { id = "section_" + (i + 1).ToString("00"), title = names[i], hasStartTime = false, hasEndTime = false });
            asset.cues.Add(new GastSongCue { id = "intro_spring_01", sectionId = "section_01", lyric = "SPRING (startmarkör från Dennis)", hasVocalTime = true, vocalTimeSeconds = 21.5f, timingStatus = GastTimingStatus.Estimated, action = GastCueAction.ChaseLaunch, command = GastGameplayCommand.RunStart, gameplayCommand = false });
            AddUntimed(asset, "command_jump_01", "section_03", "HOPPA", GastCueAction.CommandJump, GastGameplayCommand.Jump);
            AddUntimed(asset, "command_slide_01", "section_03", "DUCKA", GastCueAction.CommandSlide, GastGameplayCommand.Slide);
            AddUntimed(asset, "command_left_01", "section_03", "VÄNSTER", GastCueAction.CommandLaneLeft, GastGameplayCommand.LaneLeft);
            AddUntimed(asset, "command_right_01", "section_03", "HÖGER", GastCueAction.CommandLaneRight, GastGameplayCommand.LaneRight);
            const string path = "Assets/Resources/GastSong.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
        private static void AddUntimed(GastSongAsset asset, string id, string section, string lyric, GastCueAction action, GastGameplayCommand command)
        { asset.cues.Add(new GastSongCue { id = id, sectionId = section, lyric = lyric, timingStatus = GastTimingStatus.Untimed, hasVocalTime = false, action = action, gameplayCommand = true, command = command }); }
        private void OnDisable() { if (preview != null) DestroyImmediate(preview.gameObject); }
    }
}
