using System.Collections.Generic;
using UnityEngine;

public class Zone : MonoBehaviour
{
    [SerializeField] private List<Transform> enterPoints;
    [SerializeField] private List<Transform> exitPoints;
    [SerializeField] private Transform winPoint;

    private LevelBuilder levelBuilder;

    public void SetLevelBuilder(LevelBuilder levelBuilder) => this.levelBuilder = levelBuilder;

    public List<Transform> GetEnterPoints() => enterPoints;

    public Transform GetRandomEnterPoint()
    {
        if (enterPoints != null && enterPoints.Count > 0)
            return enterPoints[Random.Range(0, enterPoints.Count)];

        return null;
    }

    public void RemoveEnterPoint(Transform point)
    {
        if (enterPoints.Contains(point))
        {
            enterPoints.Remove(point);
            Destroy(point.gameObject);
        }
    }

    public List<Transform> GetExitPoints() => exitPoints;

    public void RemoveExitPoint(Transform point)
    {
        if (exitPoints.Contains(point))
        {
            exitPoints.Remove(point);
            Destroy(point.gameObject);
        }
    }

    public Transform GetRandomExitPoint()
    {
        if (exitPoints != null && exitPoints.Count > 0)
            return exitPoints[Random.Range(0, exitPoints.Count)];

        return null;
    }

    public bool HasWinPoint() { return winPoint != null; }

    public Transform GetWinPoint() => winPoint;

}
