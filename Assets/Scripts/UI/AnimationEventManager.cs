using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class AnimationEventManager : MonoBehaviour
{
    [SerializeField] UnityEvent[] myEvents;
    public void ApplyEvent(int eventNum)
    {
        myEvents[eventNum]?.Invoke();
    }
}
