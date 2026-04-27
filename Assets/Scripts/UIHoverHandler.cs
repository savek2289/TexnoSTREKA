using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class ScrollViewHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [System.Serializable]
    public class HoverEvent : UnityEvent { }

    [Header("События")]
    public HoverEvent onPointerEnter;
    public HoverEvent onPointerExit;

    [Header("Настройки анимации")]
    [Tooltip("Минимальное время наведения для запуска анимации (секунды)")]
    public float minHoverTime = 0.15f;

    [Tooltip("Минимальная задержка между анимациями (секунды)")]
    public float minAnimationInterval = 0.1f;

    [Header("Настройки для Scroll View")]
    [Tooltip("Блокировать события при перетаскивании Scroll View")]
    public bool blockEventsDuringDrag = true;

    [Tooltip("Задержка после отпускания Scroll View (секунды)")]
    public float dragReleaseDelay = 0.5f;

    [Header("Обработка быстрых наведений")]
    [Tooltip("Игнорировать слишком быстрые наведения (меньше minHoverTime)")]
    public bool ignoreQuickHovers = true;

    [Header("Настройки кнопки")]
    [Tooltip("Кнопка, которая появляется при анимации")]
    public Button targetButton;

    [Tooltip("Задержка перед проверкой кнопки (секунды)")]
    public float buttonCheckDelay = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private ScrollRect scrollRect;
    private bool isDragging = false;
    private float lastDragEndTime;

    private enum HoverState
    {
        Idle,
        Hovering,
        Entering,
        Exiting,
        PendingExit
    }

    private HoverState currentState = HoverState.Idle;
    private float hoverStartTime = 0f;
    private float lastAnimationTime = 0f;
    private bool pointerIsOver = false;
    private bool buttonIsActive = false;
    private bool isOverButton = false;
    private bool isButtonPressed = false;

    private Coroutine hoverTimerCoroutine;
    private Coroutine exitTimerCoroutine;
    private Coroutine buttonCheckCoroutine;

    private PointerEventData simulatedPointerData;

    private void Start()
    {
        scrollRect = GetComponentInParent<ScrollRect>();

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        simulatedPointerData = new PointerEventData(EventSystem.current);

        if (showDebugInfo)
        {
            Debug.Log($"[HoverHandler] Инициализирован на {gameObject.name}, minHoverTime: {minHoverTime}s");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetButton != null && IsPointerOverButton(eventData.position))
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Курсор над кнопкой, игнорируем ховер");
            return;
        }

        if (blockEventsDuringDrag && IsScrollViewDragging())
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Игнорируем enter - скролл dragging");
            return;
        }

        pointerIsOver = true;

        if (showDebugInfo)
        {
            Debug.Log($"[HoverHandler] OnPointerEnter - текущее состояние: {currentState}");
        }

        if (currentState == HoverState.Exiting)
        {
            CancelExitAnimation();
        }

        if (currentState == HoverState.Hovering || currentState == HoverState.Entering)
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Уже в состоянии наведения, игнорируем");
            return;
        }

        if (currentState == HoverState.PendingExit)
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Сбрасываем ожидание выхода");
            currentState = HoverState.Idle;
        }

        // КРИТИЧНО: используем unscaledTime
        hoverStartTime = Time.unscaledTime;

        if (ignoreQuickHovers)
        {
            if (hoverTimerCoroutine != null)
                StopCoroutine(hoverTimerCoroutine);

            hoverTimerCoroutine = StartCoroutine(HoverTimer());
        }
        else
        {
            StartEnterAnimation();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonIsActive && targetButton != null && IsPointerOverButton(eventData.position))
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Курсор перешел на активную кнопку, не закрываем");
            return;
        }

        pointerIsOver = false;

        if (showDebugInfo)
        {
            Debug.Log($"[HoverHandler] OnPointerExit - текущее состояние: {currentState}");
        }

        if (hoverTimerCoroutine != null)
        {
            StopCoroutine(hoverTimerCoroutine);
            hoverTimerCoroutine = null;
        }

        if (ignoreQuickHovers && currentState == HoverState.Idle)
        {
            // КРИТИЧНО: используем unscaledTime
            float hoverDuration = Time.unscaledTime - hoverStartTime;

            if (hoverDuration < minHoverTime)
            {
                if (showDebugInfo) Debug.Log($"[HoverHandler] Игнорируем быстрое наведение: {hoverDuration:F2}s < {minHoverTime}s");
                return;
            }
        }

        if (currentState == HoverState.Entering)
        {
            // КРИТИЧНО: используем unscaledTime
            float hoverDuration = Time.unscaledTime - hoverStartTime;

            if (hoverDuration < minHoverTime)
            {
                if (showDebugInfo) Debug.Log($"[HoverHandler] Быстрое наведение во время анимации входа, ждем завершения");
                currentState = HoverState.PendingExit;
                return;
            }

            StartExitAfterDelay();
            return;
        }

        if (currentState == HoverState.Hovering)
        {
            StartExitAnimation();
            return;
        }

        if (currentState == HoverState.PendingExit || currentState == HoverState.Exiting)
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Уже в процессе выхода, игнорируем");
            return;
        }
    }

    private bool IsPointerOverButton(Vector2 screenPosition)
    {
        if (targetButton == null) return false;

        PointerEventData checkData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(checkData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == targetButton.gameObject ||
                result.gameObject.transform.IsChildOf(targetButton.transform))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateButtonState()
    {
        if (targetButton == null || !buttonIsActive) return;

        Vector2 mousePosition = Input.mousePosition;
        bool wasOverButton = isOverButton;
        isOverButton = IsPointerOverButton(mousePosition);

        simulatedPointerData.position = mousePosition;

        if (isOverButton != wasOverButton)
        {
            if (isOverButton)
            {
                targetButton.OnPointerEnter(simulatedPointerData);
                if (showDebugInfo) Debug.Log($"[HoverHandler] Кнопка: PointerEnter (имитация)");
            }
            else
            {
                targetButton.OnPointerExit(simulatedPointerData);
                if (showDebugInfo) Debug.Log($"[HoverHandler] Кнопка: PointerExit (имитация)");
                isButtonPressed = false;
            }
        }

        if (isOverButton)
        {
            if (Input.GetMouseButtonDown(0))
            {
                targetButton.OnPointerDown(simulatedPointerData);
                isButtonPressed = true;
                if (showDebugInfo) Debug.Log($"[HoverHandler] Кнопка: PointerDown (имитация)");
            }
            else if (Input.GetMouseButtonUp(0))
            {
                targetButton.OnPointerUp(simulatedPointerData);

                if (isButtonPressed)
                {
                    targetButton.OnPointerClick(simulatedPointerData);
                    if (showDebugInfo) Debug.Log($"[HoverHandler] Кнопка: CLICK! (имитация)");
                }

                isButtonPressed = false;
            }
        }
        else if (isButtonPressed && Input.GetMouseButtonUp(0))
        {
            targetButton.OnPointerUp(simulatedPointerData);
            isButtonPressed = false;
        }
    }

    private System.Collections.IEnumerator HoverTimer()
    {
        if (showDebugInfo) Debug.Log($"[HoverHandler] Таймер наведения запущен");

        // КРИТИЧНО: используем unscaledTime
        float startTime = Time.unscaledTime;

        while (Time.unscaledTime - startTime < minHoverTime)
        {
            if (!pointerIsOver)
            {
                if (showDebugInfo) Debug.Log($"[HoverHandler] Курсор ушел до завершения таймера");
                yield break;
            }
            yield return null;
        }

        if (pointerIsOver)
        {
            StartEnterAnimation();
        }

        hoverTimerCoroutine = null;
    }

    private void StartEnterAnimation()
    {
        // КРИТИЧНО: используем unscaledTime
        if (Time.unscaledTime - lastAnimationTime < minAnimationInterval)
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Слишком скоро после предыдущей анимации, ждем");
            return;
        }

        if (showDebugInfo) Debug.Log($"[HoverHandler] Запускаем анимацию входа");

        currentState = HoverState.Entering;
        lastAnimationTime = Time.unscaledTime;

        onPointerEnter?.Invoke();

        StartCoroutine(CompleteEnterAnimation());
    }

    private System.Collections.IEnumerator CompleteEnterAnimation()
    {
        // КРИТИЧНО: используем WaitForSecondsRealtime вместо WaitForSeconds
        yield return new WaitForSecondsRealtime(0.05f);

        if (currentState == HoverState.Entering)
        {
            currentState = HoverState.Hovering;
            buttonIsActive = true;

            if (showDebugInfo) Debug.Log($"[HoverHandler] Анимация входа завершена, состояние: Hovering, кнопка активна");

            if (buttonCheckCoroutine != null)
                StopCoroutine(buttonCheckCoroutine);
            buttonCheckCoroutine = StartCoroutine(ButtonCheckRoutine());

            if (!pointerIsOver)
            {
                if (showDebugInfo) Debug.Log($"[HoverHandler] Курсор ушел во время анимации входа, запускаем выход");
                StartExitAfterDelay();
            }
        }
    }

    private System.Collections.IEnumerator ButtonCheckRoutine()
    {
        // КРИТИЧНО: используем WaitForSecondsRealtime вместо WaitForSeconds
        yield return new WaitForSecondsRealtime(buttonCheckDelay);

        if (showDebugInfo) Debug.Log($"[HoverHandler] Начинаю проверку кнопки");

        while (buttonIsActive && currentState == HoverState.Hovering)
        {
            UpdateButtonState();
            yield return null;
        }

        if (targetButton != null)
        {
            targetButton.OnPointerExit(simulatedPointerData);
        }

        buttonCheckCoroutine = null;
    }

    private void StartExitAfterDelay()
    {
        if (exitTimerCoroutine != null)
            StopCoroutine(exitTimerCoroutine);

        exitTimerCoroutine = StartCoroutine(ExitAfterDelay());
    }

    private System.Collections.IEnumerator ExitAfterDelay()
    {
        // КРИТИЧНО: используем WaitForSecondsRealtime вместо WaitForSeconds
        yield return new WaitForSecondsRealtime(0.05f);

        StartExitAnimation();
        exitTimerCoroutine = null;
    }

    private void StartExitAnimation()
    {
        if (currentState != HoverState.Hovering && currentState != HoverState.PendingExit)
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Не могу запустить выход из состояния: {currentState}");
            return;
        }

        if (showDebugInfo) Debug.Log($"[HoverHandler] Запускаем анимацию выхода");

        if (buttonCheckCoroutine != null)
        {
            StopCoroutine(buttonCheckCoroutine);
            buttonCheckCoroutine = null;
        }

        if (targetButton != null)
        {
            targetButton.OnPointerExit(simulatedPointerData);
        }

        buttonIsActive = false;
        isOverButton = false;
        isButtonPressed = false;

        currentState = HoverState.Exiting;
        lastAnimationTime = Time.unscaledTime;

        onPointerExit?.Invoke();

        StartCoroutine(CompleteExitAnimation());
    }

    private System.Collections.IEnumerator CompleteExitAnimation()
    {
        // КРИТИЧНО: используем WaitForSecondsRealtime вместо WaitForSeconds
        yield return new WaitForSecondsRealtime(0.05f);

        currentState = HoverState.Idle;

        if (showDebugInfo) Debug.Log($"[HoverHandler] Анимация выхода завершена, состояние: Idle");
    }

    private void CancelExitAnimation()
    {
        if (exitTimerCoroutine != null)
        {
            StopCoroutine(exitTimerCoroutine);
            exitTimerCoroutine = null;
        }

        if (currentState == HoverState.Exiting)
        {
            currentState = HoverState.Hovering;
            if (showDebugInfo) Debug.Log($"[HoverHandler] Анимация выхода отменена");
        }
    }

    public void OnEnterAnimationComplete()
    {
        if (currentState == HoverState.Entering)
        {
            currentState = HoverState.Hovering;
            buttonIsActive = true;

            if (showDebugInfo) Debug.Log($"[HoverHandler] Анимация входа завершена (из Animation Event)");

            if (buttonCheckCoroutine != null)
                StopCoroutine(buttonCheckCoroutine);
            buttonCheckCoroutine = StartCoroutine(ButtonCheckRoutine());

            if (!pointerIsOver)
            {
                StartExitAfterDelay();
            }
        }
    }

    public void OnExitAnimationComplete()
    {
        currentState = HoverState.Idle;
        buttonIsActive = false;
        isOverButton = false;
        isButtonPressed = false;

        if (showDebugInfo) Debug.Log($"[HoverHandler] Анимация выхода завершена (из Animation Event)");
    }

    public void ForceExit()
    {
        if (currentState == HoverState.Hovering || currentState == HoverState.Entering)
        {
            if (showDebugInfo) Debug.Log($"[HoverHandler] Принудительный выход");
            StartExitAnimation();
        }
    }

    private bool IsScrollViewDragging()
    {
        if (scrollRect == null) return false;

        bool recentlyDragged = Time.unscaledTime - lastDragEndTime < dragReleaseDelay;
        return isDragging || recentlyDragged;
    }

    private void OnScrollValueChanged(Vector2 position)
    {
        if (scrollRect == null) return;

        bool wasDragging = isDragging;
        isDragging = scrollRect.velocity.magnitude > 0.1f;

        if (wasDragging && !isDragging)
        {
            lastDragEndTime = Time.unscaledTime;

            if (pointerIsOver)
            {
                ForceExit();
            }
        }
    }

    private void OnDestroy()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        if (hoverTimerCoroutine != null)
            StopCoroutine(hoverTimerCoroutine);

        if (exitTimerCoroutine != null)
            StopCoroutine(exitTimerCoroutine);

        if (buttonCheckCoroutine != null)
            StopCoroutine(buttonCheckCoroutine);
    }

    public void ResetState()
    {
        currentState = HoverState.Idle;
        pointerIsOver = false;
        buttonIsActive = false;
        isOverButton = false;
        isButtonPressed = false;

        if (targetButton != null)
        {
            targetButton.OnPointerExit(simulatedPointerData);
        }

        if (hoverTimerCoroutine != null)
        {
            StopCoroutine(hoverTimerCoroutine);
            hoverTimerCoroutine = null;
        }

        if (exitTimerCoroutine != null)
        {
            StopCoroutine(exitTimerCoroutine);
            exitTimerCoroutine = null;
        }

        if (buttonCheckCoroutine != null)
        {
            StopCoroutine(buttonCheckCoroutine);
            buttonCheckCoroutine = null;
        }
    }

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.textColor = Color.white;
            style.fontSize = 12;

            string stateText = $"Состояние: {currentState}\n";
            stateText += $"Курсор над: {pointerIsOver}\n";
            stateText += $"Кнопка активна: {buttonIsActive}\n";
            stateText += $"Над кнопкой: {isOverButton}\n";
            stateText += $"Кнопка нажата: {isButtonPressed}\n";
            stateText += $"Время наведения: {Time.unscaledTime - hoverStartTime:F2}s";

            GUI.Box(new Rect(10, 10, 200, 120), stateText, style);
        }
    }
}