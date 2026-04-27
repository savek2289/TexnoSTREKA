using static LevelBuilder;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelBuilder))]
public class LevelBuilderEditor : Editor
{
    SerializedProperty mainGridProp;
    SerializedProperty modeProp;
    SerializedProperty classicGenXBorderProp;
    SerializedProperty classicGenYBorderProp;
    SerializedProperty onlyUpGenXBorderProp;
    SerializedProperty onlyUpGenYBorderProp;
    SerializedProperty zonesToCreateProp;
    SerializedProperty zonesProp;
    SerializedProperty winPointProp;
    SerializedProperty maxAttemptsProp;
    SerializedProperty radiusXProp;
    SerializedProperty radiusYProp;
    SerializedProperty stopGenerationOnFailProp;
    SerializedProperty enableDebugLogsProp;

    // Рандомизация
    SerializedProperty randomizeClassicXBorderProp;
    SerializedProperty classicXBorderRangeProp;
    SerializedProperty randomizeClassicYBorderProp;
    SerializedProperty classicYBorderRangeProp;

    SerializedProperty randomizeOnlyUpXBorderProp;
    SerializedProperty onlyUpXBorderRangeProp;
    SerializedProperty randomizeOnlyUpYBorderProp;
    SerializedProperty onlyUpYBorderRangeProp;

    SerializedProperty randomizeZonesToCreateProp;
    SerializedProperty zonesCountRangeProp;

    SerializedProperty randomizeRadiusXProp;
    SerializedProperty radiusXRangeProp;
    SerializedProperty randomizeRadiusYProp;
    SerializedProperty radiusYRangeProp;

    private LevelBuilder lb;

    private void OnEnable()
    {
        lb = (LevelBuilder)target;

        mainGridProp = serializedObject.FindProperty("mainGrid");
        modeProp = serializedObject.FindProperty("mode");
        classicGenXBorderProp = serializedObject.FindProperty("classicGenXBorder");
        classicGenYBorderProp = serializedObject.FindProperty("classicGenYBorder");
        onlyUpGenXBorderProp = serializedObject.FindProperty("onlyUpGenXBorder");
        onlyUpGenYBorderProp = serializedObject.FindProperty("onlyUpGenYBorder");
        zonesToCreateProp = serializedObject.FindProperty("zonesToCreate");
        zonesProp = serializedObject.FindProperty("startZones");
        winPointProp = serializedObject.FindProperty("winPointPrefab");
        maxAttemptsProp = serializedObject.FindProperty("maxAttempts");
        radiusXProp = serializedObject.FindProperty("radiusX");
        radiusYProp = serializedObject.FindProperty("radiusY");
        stopGenerationOnFailProp = serializedObject.FindProperty("stopGenerationOnFail");
        enableDebugLogsProp = serializedObject.FindProperty("enableDebugLogs");

        randomizeClassicXBorderProp = serializedObject.FindProperty("randomizeClassicXBorder");
        classicXBorderRangeProp = serializedObject.FindProperty("classicXBorderRange");
        randomizeClassicYBorderProp = serializedObject.FindProperty("randomizeClassicYBorder");
        classicYBorderRangeProp = serializedObject.FindProperty("classicYBorderRange");

        randomizeOnlyUpXBorderProp = serializedObject.FindProperty("randomizeOnlyUpXBorder");
        onlyUpXBorderRangeProp = serializedObject.FindProperty("onlyUpXBorderRange");
        randomizeOnlyUpYBorderProp = serializedObject.FindProperty("randomizeOnlyUpYBorder");
        onlyUpYBorderRangeProp = serializedObject.FindProperty("onlyUpYBorderRange");

        randomizeZonesToCreateProp = serializedObject.FindProperty("randomizeZonesToCreate");
        zonesCountRangeProp = serializedObject.FindProperty("zonesCountRange");

        randomizeRadiusXProp = serializedObject.FindProperty("randomizeRadiusX");
        radiusXRangeProp = serializedObject.FindProperty("radiusXRange");
        randomizeRadiusYProp = serializedObject.FindProperty("randomizeRadiusY");
        radiusYRangeProp = serializedObject.FindProperty("radiusYRange");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(mainGridProp);
        EditorGUILayout.Space(1);
        EditorGUILayout.PropertyField(modeProp);

        GenerateMode mode = (GenerateMode)modeProp.enumValueIndex;

        EditorGUILayout.Space(5);

        switch (mode)
        {
            case GenerateMode.noGenerate:
                break;

            case GenerateMode.classic:
                DrawPropertyWithRandomize(classicGenXBorderProp, randomizeClassicXBorderProp, classicXBorderRangeProp);
                DrawPropertyWithRandomize(classicGenYBorderProp, randomizeClassicYBorderProp, classicYBorderRangeProp);
                DrawPropertyWithRandomize(zonesToCreateProp, randomizeZonesToCreateProp, zonesCountRangeProp, true);
                EditorGUILayout.PropertyField(zonesProp);
                EditorGUILayout.PropertyField(winPointProp);
                EditorGUILayout.PropertyField(maxAttemptsProp);
                break;

            case GenerateMode.onlyUp:
                DrawPropertyWithRandomize(onlyUpGenXBorderProp, randomizeOnlyUpXBorderProp, onlyUpXBorderRangeProp);
                DrawPropertyWithRandomize(onlyUpGenYBorderProp, randomizeOnlyUpYBorderProp, onlyUpYBorderRangeProp);
                DrawPropertyWithRandomize(zonesToCreateProp, randomizeZonesToCreateProp, zonesCountRangeProp, true);
                EditorGUILayout.PropertyField(zonesProp);
                EditorGUILayout.PropertyField(maxAttemptsProp);

                // Радиусы в одной строке
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(radiusXProp, GUIContent.none);
                EditorGUILayout.PropertyField(radiusYProp, GUIContent.none);
                EditorGUILayout.EndHorizontal();

                // Чекбоксы Rand для радиусов
                EditorGUILayout.BeginHorizontal();
                DrawRandToggleOnly(randomizeRadiusXProp, "Rand X");
                DrawRandToggleOnly(randomizeRadiusYProp, "Rand Y");
                EditorGUILayout.EndHorizontal();

                // Диапазоны для радиусов (если включены)
                if (randomizeRadiusXProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawVector2Range(radiusXRangeProp, "Radius X Range");
                    EditorGUI.indentLevel--;
                }
                if (randomizeRadiusYProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawVector2Range(radiusYRangeProp, "Radius Y Range");
                    EditorGUI.indentLevel--;
                }
                break;
        }

        serializedObject.ApplyModifiedProperties();

        // --- Debug Mode Toggle ---
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        bool debugEnabled = enableDebugLogsProp.boolValue;
        GUI.backgroundColor = debugEnabled ? Color.green : Color.gray;
        if (GUILayout.Button($"Debug Logs: {(debugEnabled ? "ON" : "OFF")}", GUILayout.Height(25)))
        {
            enableDebugLogsProp.boolValue = !debugEnabled;
            serializedObject.ApplyModifiedProperties();
            lb.SetDebugLogsEnabled(enableDebugLogsProp.boolValue);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // --- Stop On Fail Toggle ---
        EditorGUILayout.BeginHorizontal();
        bool stopOnFail = stopGenerationOnFailProp.boolValue;
        GUI.backgroundColor = stopOnFail ? Color.red : Color.gray;
        if (GUILayout.Button($"Stop On Fail: {(stopOnFail ? "ON" : "OFF")}", GUILayout.Height(25)))
        {
            stopGenerationOnFailProp.boolValue = !stopOnFail;
            serializedObject.ApplyModifiedProperties();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // --- Regenerate Button ---
        EditorGUILayout.Space(5);
        bool canRegenerate = Application.isPlaying && lb.IsInitialized();
        GUI.enabled = canRegenerate;
        if (GUILayout.Button("ReGenerate Level", GUILayout.Height(30)))
        {
            lb.RegenerateLevel();
        }
        GUI.enabled = true;
        if (!canRegenerate)
        {
            EditorGUILayout.HelpBox("Regenerate available only in Play Mode after initialization.", MessageType.Info);
        }
    }

    private void DrawPropertyWithRandomize(SerializedProperty mainProp, SerializedProperty toggleProp, SerializedProperty rangeProp, bool isInt = false)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(mainProp, GUIContent.none);
        toggleProp.boolValue = EditorGUILayout.ToggleLeft("Rand", toggleProp.boolValue, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        if (toggleProp.boolValue)
        {
            EditorGUI.indentLevel++;
            if (isInt)
                DrawVector2IntRange(rangeProp, "Count Range");
            else
                DrawVector2Range(rangeProp, "Range");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawRandToggleOnly(SerializedProperty toggleProp, string label)
    {
        toggleProp.boolValue = EditorGUILayout.ToggleLeft(label, toggleProp.boolValue, GUILayout.Width(70));
    }

    private void DrawVector2Range(SerializedProperty rangeProp, string label)
    {
        Vector2 range = rangeProp.vector2Value;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        range.x = EditorGUILayout.FloatField("Min", range.x);
        range.y = EditorGUILayout.FloatField("Max", range.y);
        rangeProp.vector2Value = range;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawVector2IntRange(SerializedProperty rangeProp, string label)
    {
        Vector2Int range = rangeProp.vector2IntValue;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        range.x = EditorGUILayout.IntField("Min", range.x);
        range.y = EditorGUILayout.IntField("Max", range.y);
        rangeProp.vector2IntValue = range;
        EditorGUILayout.EndHorizontal();
    }
}