using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
    [SerializeField] private Transform mainGrid;
    [SerializeField] private GenerateMode mode = GenerateMode.noGenerate;
    [Space(5)]
    [SerializeField] private Vector2 classicGenXBorder;
    [SerializeField] private Vector2 classicGenYBorder;
    [SerializeField] private Vector2 onlyUpGenXBorder;
    [SerializeField] private Vector2 onlyUpGenYBorder;
    [SerializeField] private int zonesToCreate;
    [SerializeField] private List<GameObject> startZones;
    [SerializeField] private GameObject winPointPrefab;
    [SerializeField] private int maxAttempts = 100;
    [SerializeField] private float radiusX = 1f;
    [SerializeField] private float radiusY = 0.5f;
    [SerializeField] private bool stopGenerationOnFail = true;
    [SerializeField] private bool enableDebugLogs = true;

    // --- Рандомизация границ classic ---
    [SerializeField] private bool randomizeClassicXBorder;
    [SerializeField] private Vector2 classicXBorderRange = new Vector2(-10f, 10f);
    [SerializeField] private bool randomizeClassicYBorder;
    [SerializeField] private Vector2 classicYBorderRange = new Vector2(-10f, 10f);

    // --- Рандомизация границ onlyUp ---
    [SerializeField] private bool randomizeOnlyUpXBorder;
    [SerializeField] private Vector2 onlyUpXBorderRange = new Vector2(-10f, 10f);
    [SerializeField] private bool randomizeOnlyUpYBorder;
    [SerializeField] private Vector2 onlyUpYBorderRange = new Vector2(-10f, 10f);

    // --- Рандомизация количества зон ---
    [SerializeField] private bool randomizeZonesToCreate;
    [SerializeField] private Vector2Int zonesCountRange = new Vector2Int(5, 15);

    // --- Рандомизация радиусов onlyUp ---
    [SerializeField] private bool randomizeRadiusX;
    [SerializeField] private Vector2 radiusXRange = new Vector2(0.5f, 2f);
    [SerializeField] private bool randomizeRadiusY;
    [SerializeField] private Vector2 radiusYRange = new Vector2(0.2f, 1f);

    private readonly List<GameObject> createdZones = new();
    private readonly List<Transform> allExitPoints = new();
    private readonly Dictionary<Transform, GameObject> exitPointToZone = new();

    private GameObject previousZone;
    private GameObject winPointInstance;
    private float currentLevelHeight;
    private bool initialized;

    private const int maxClassicGenerationAttemps = 40;

    public enum GenerateMode
    {
        noGenerate,
        classic,
        onlyUp
    }

    private void Start()
    {
        if(!UnityEngine.Application.isEditor)
            mode = LevelBuilderData.GetGenerateMode();

        ApplyRandomization();
        RegenerateLevel();
    }

    public void SetDebugLogsEnabled(bool enabled)
    {
        enableDebugLogs = enabled;
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    private void ApplyRandomization()
    {
        if (randomizeClassicXBorder)
            classicGenXBorder.x = Random.Range(classicXBorderRange.x, classicXBorderRange.y);
        if (randomizeClassicYBorder)
            classicGenYBorder.y = Random.Range(classicYBorderRange.x, classicYBorderRange.y);

        if (randomizeOnlyUpXBorder)
            onlyUpGenXBorder.x = Random.Range(onlyUpXBorderRange.x, onlyUpXBorderRange.y);
        if (randomizeOnlyUpYBorder)
            onlyUpGenYBorder.y = Random.Range(onlyUpYBorderRange.x, onlyUpYBorderRange.y);

        if (randomizeZonesToCreate)
            zonesToCreate = Random.Range(zonesCountRange.x, zonesCountRange.y + 1);

        if (randomizeRadiusX)
            radiusX = Random.Range(radiusXRange.x, radiusXRange.y);
        if (randomizeRadiusY)
            radiusY = Random.Range(radiusYRange.x, radiusYRange.y);
    }

    public void RegenerateLevel()
    {
        ClearLevel();

        if (mode == GenerateMode.noGenerate)
        {
            initialized = true;
            return;
        }

        StartCoroutine(GenerateLevel());
    }

    private void RandomizeFloatValue(ref float value, float maxValue, float minValue) { value = Random.Range(minValue, maxValue); }

    private void RandomizeIntValue(ref int value, int maxValue, int minValue) { value = Random.Range(minValue, maxValue); }

    private void RandomizeVector2(ref Vector2 value, float minValueX, float maxValueX, float minValueY, float maxValueY)
    {
        value = new Vector2(Random.Range(minValueX, maxValueX), Random.Range(minValueY, maxValueY));
    }

    private void ClearLevel()
    {
        foreach (var zone in createdZones)
        {
            if (zone != null) Destroy(zone);
        }

        if(winPointInstance != null)
        {
            Destroy(winPointInstance);
            winPointInstance = null;
        }

        createdZones.Clear();
        allExitPoints.Clear();
        exitPointToZone.Clear();
        previousZone = null;
        currentLevelHeight = 0f;
    }

    private IEnumerator GenerateLevel()
    {
        int currentGenerationAttempt = 0;

        for (int i = 0; i < zonesToCreate; i++)
        {
            bool success = mode == GenerateMode.classic
                ? TryCreateClassicZone(i)
                : TryCreateOnlyUpZone(i);

            yield return null;

            if (!success)
            {
                if (stopGenerationOnFail)
                {
                    if (enableDebugLogs) Debug.LogWarning($"Can't place zone {i + 1}, stop generation");
                    break;
                }
                else
                {
                    currentGenerationAttempt++;

                    if (enableDebugLogs) Debug.LogWarning($"Can't place zone {i + 1}, trying again...");
                    if (currentGenerationAttempt >= maxClassicGenerationAttemps) break;
                    continue;
                }
            }
        }

        if (mode == GenerateMode.classic)
                PlaceWinPoint();

        if (enableDebugLogs) Debug.Log($"Total zones created: {createdZones.Count}");

        initialized = true;
    }

    private bool TryCreateClassicZone(int index)
    {
        previousZone = null;

        for (int i = 0; i <= maxAttempts; i++)
        {
            if (createdZones.Count == 0)
            {
                GameObject newZone = InstantiateRandomZone(startZones);
                createdZones.Add(newZone);
            }
            else
            {
                previousZone = createdZones[Random.Range(0, createdZones.Count)];

                previousZone.TryGetComponent(out Zone previousZoneScript);
                Transform exitPoint = previousZoneScript.GetRandomExitPoint();

                if (exitPoint == null)
                {
                    if (enableDebugLogs) Debug.LogWarning($"[179str, {index + 1} Zone] Сouldn't get exit point of {previousZone.name} or exit point is null.");
                    continue;
                }

                exitPoint.TryGetComponent(out ZoneExitPoint zoneEnterPointScript);
                List<GameObject> zonesToCreate = zoneEnterPointScript.GetZones();

                if (zonesToCreate == null || zonesToCreate.Count == 0)
                {
                    if (enableDebugLogs) Debug.LogWarning($"[195str, {index + 1} Zone] Couldn't get list of zones to create of {previousZone.name} or there are no zones for creation.");
                    continue;
                }

                //Очистка списка от пустых элементов
                for (int j = zonesToCreate.Count - 1; j >= 0; j--)
                {
                    if (zonesToCreate[j] == null)
                        zonesToCreate.RemoveAt(j);
                }

                GameObject zone = Instantiate(zonesToCreate[Random.Range(0, zonesToCreate.Count)], mainGrid);

                if (zone == null)
                {
                    if (enableDebugLogs) Debug.LogWarning($"[204str, {index + 1} Zone] Couldn't create zone because there are no zones in the list of zones to create");
                    continue;
                }

                zone.TryGetComponent(out BoxCollider2D collider);
                if (collider == null)
                {
                    collider = zone.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                }

                zone.TryGetComponent(out Zone currentZoneScript);
                Transform enterPoint = currentZoneScript.GetRandomEnterPoint();

                if (enterPoint == null)
                {
                    if (enableDebugLogs) Debug.LogWarning($"[219str, {index + 1} Zone] Couldn't get entry point of {zone.name} or entry point is null.");
                    continue;
                }

                Vector3 difference = new Vector2(zone.transform.position.x - enterPoint.position.x, zone.transform.position.y - enterPoint.position.y);
                zone.transform.position = exitPoint.position - difference;

                if (!IsWithinBorders(collider, classicGenXBorder, classicGenYBorder) || OverlapsZones(zone, previousZone))
                {
                    Destroy(zone);
                    continue;
                }

                createdZones.Add(zone);
                previousZoneScript.RemoveExitPoint(exitPoint);
            }

            return true;
        }

        return false;
    }

    private bool TryCreateOnlyUpZone(int index)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            GameObject newZone = InstantiateRandomZone(startZones);
            if (newZone == null)
            {
                if (enableDebugLogs) Debug.LogWarning($"Attempt {attempt + 1}: Failed to instantiate random zone");
                continue;
            }

            var collider = EnsureCollider(newZone);
            collider.isTrigger = false;

            if (previousZone != null)
            {
                float xOffset = Random.Range(-radiusX, radiusX);
                float yOffset = Random.Range(-radiusY, radiusY);

                float xPos = Mathf.Clamp(previousZone.transform.position.x + xOffset,
                    onlyUpGenXBorder.x, onlyUpGenXBorder.y);

                float prevHeight = previousZone.GetComponent<Collider2D>().bounds.size.y;
                float yPos = previousZone.transform.position.y + prevHeight + yOffset;

                newZone.transform.position = new Vector3(xPos, yPos, 0);
            }
            else
            {
                newZone.transform.position = GetSafeStartPosition(onlyUpGenXBorder, onlyUpGenYBorder);
            }

            Physics2D.SyncTransforms();

            if (!IsWithinBorders(collider, onlyUpGenXBorder, onlyUpGenYBorder))
            {
                if (enableDebugLogs)
                {
                    Bounds b = collider.bounds;
                    Debug.LogWarning($"Attempt {attempt + 1}: Zone {newZone.name} out of bounds. " +
                                     $"Bounds: min({b.min.x:F2},{b.min.y:F2}) max({b.max.x:F2},{b.max.y:F2}). " +
                                     $"Allowed X: {onlyUpGenXBorder.x:F2}..{onlyUpGenXBorder.y:F2}, Y: {onlyUpGenYBorder.x:F2}..{onlyUpGenYBorder.y:F2}");
                }
                Destroy(newZone);
                continue;
            }

            collider.isTrigger = true;
            createdZones.Add(newZone);
            previousZone = newZone;
            currentLevelHeight = newZone.transform.position.y;

            if (enableDebugLogs)
                Debug.Log($"Zone {index + 1} created: {newZone.name} at {newZone.transform.position}");

            return true;
        }

        Debug.LogError($"Failed to place only-up zone {index + 1} after {maxAttempts} attempts.");
        return false;
    }

    private void PlaceWinPoint()
    {
        if (winPointPrefab == null || createdZones.Count == 0) return;

        GameObject farthestZone = null;
        float maxDist = -1f;
        foreach (var zone in createdZones)
        {
            float dist = zone.transform.position.sqrMagnitude;
            if (dist > maxDist)
            {
                maxDist = dist;
                farthestZone = zone;
            }
        }

        var zoneComponent = farthestZone.GetComponent<Zone>();
        if (zoneComponent == null)
        {
            Debug.LogError("Farthest zone missing Zone component");
            return;
        }

        if (zoneComponent.HasWinPoint())
        {
            winPointInstance = Instantiate(winPointPrefab);
            winPointInstance.tag = "NextRoom";
            winPointInstance.transform.position = zoneComponent.GetWinPoint().position;
        }
    }

    private GameObject InstantiateRandomZone(List<GameObject> zones)
    {
        if (zones == null || zones.Count == 0)
        {
            Debug.LogError("No zone prefabs assigned");
            return null;
        }
        int index = Random.Range(0, zones.Count);
        return Instantiate(zones[index], mainGrid);
    }

    private bool ValidateZoneScript(GameObject zone, Zone script)
    {
        if (script != null) return true;
        Debug.LogError($"Prefab {zone.name} missing Zone component");
        Destroy(zone);
        return false;
    }

    private bool ValidatePoints(GameObject zone, List<Transform> enterPoints)
    {
        if (enterPoints != null && enterPoints.Count > 0) return true;
        Debug.LogError($"Zone {zone.name} has no enter points");
        Destroy(zone);
        return false;
    }

    private Collider2D EnsureCollider(GameObject zone)
    {
        BoxCollider2D col = zone.GetComponent<BoxCollider2D>();
        if (col == null) col = zone.AddComponent<BoxCollider2D>();
        return col;
    }

    private Vector3 GetSafeStartPosition(Vector2 xBorder, Vector2 yBorder)
    {
        float x = Mathf.Clamp(0f, xBorder.x, xBorder.y);
        float y = Mathf.Clamp(-0.5f, yBorder.x, yBorder.y);
        return new Vector3(x, y, 0);
    }

    private bool OverlapsZones(GameObject newZone, GameObject parentZone)
    {
        var filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Zone")
        };

        Collider2D[] zoneColliders = newZone.GetComponentsInChildren<Collider2D>();

        foreach (var zoneCollider in zoneColliders)
        {
            var results = new List<Collider2D>();
            Physics2D.OverlapCollider(zoneCollider, filter, results);

            foreach (var overlap in results)
            {
                if (overlap.gameObject == newZone || overlap.transform.IsChildOf(newZone.transform))
                    continue;

                if (parentZone != null &&
                    (overlap.gameObject == parentZone || overlap.transform.IsChildOf(parentZone.transform)))
                    continue;

                if (enableDebugLogs) Debug.Log($"Overlap: {newZone.name} -> {overlap.gameObject.name}");
                return true;
            }
        }

        return false;
    }

    private bool IsWithinBorders(Collider2D col, Vector2 xBorder, Vector2 yBorder)
    {
        if (col == null) return false;
        Bounds bounds = col.bounds;
        return bounds.min.x >= xBorder.x && bounds.max.x <= xBorder.y &&
               bounds.min.y >= yBorder.x && bounds.max.y <= yBorder.y;
    }

    public void SetZoneDirty(GameObject zone) { }

    public bool IsInitialized() => initialized;

    private void OnDrawGizmos()
    {
        DrawBoundaryGizmo();
        DrawCreatedZonesGizmo();
        DrawExitPointsGizmo();
    }

    private void DrawBoundaryGizmo()
    {
        Vector2 xBorder, yBorder;
        Color color;

        if (mode == GenerateMode.classic)
        {
            xBorder = classicGenXBorder;
            yBorder = classicGenYBorder;
            color = Color.cyan;
        }
        else if (mode == GenerateMode.onlyUp)
        {
            xBorder = onlyUpGenXBorder;
            yBorder = onlyUpGenYBorder;
            color = Color.yellow;
        }
        else return;

        Gizmos.color = color;
        Vector3 center = new Vector3(
            (xBorder.x + xBorder.y) * 0.5f,
            (yBorder.x + yBorder.y) * 0.5f,
            0);
        Vector3 size = new Vector3(
            xBorder.y - xBorder.x,
            yBorder.y - yBorder.x,
            0);
        Gizmos.DrawWireCube(center, size);
    }

    private void DrawCreatedZonesGizmo()
    {
        Gizmos.color = Color.green;
        foreach (var zone in createdZones)
        {
            if (zone == null) continue;
            var col = zone.GetComponent<Collider2D>();
            if (col != null)
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }

    private void DrawExitPointsGizmo()
    {
        Gizmos.color = Color.red;
        foreach (var exit in allExitPoints)
        {
            if (exit == null) continue;
            Gizmos.DrawWireSphere(exit.position, 0.2f);
            Gizmos.DrawLine(exit.position, exit.position + exit.right * 0.5f);
        }
    }
}

public static class LevelBuilderData
{
    private static LevelBuilder.GenerateMode generateMode = LevelBuilder.GenerateMode.noGenerate;

    public static void SetMode(LevelBuilder.GenerateMode mode) => generateMode = mode;
    public static LevelBuilder.GenerateMode GetGenerateMode() => generateMode;
}