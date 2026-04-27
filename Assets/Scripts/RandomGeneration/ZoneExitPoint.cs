using System.Collections.Generic;
using UnityEngine;

public class ZoneExitPoint : MonoBehaviour
{
    [SerializeField] private List<GameObject> zones;

    public List<GameObject> GetZones() => zones;
}
