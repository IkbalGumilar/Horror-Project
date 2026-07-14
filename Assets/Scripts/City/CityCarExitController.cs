using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CityCarExitController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CityRoadVehicleMover carMover;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Behaviour[] enableAfterExit;
    [SerializeField] private Collider[] enableCollidersAfterExit;

    [Header("Exit")]
    [SerializeField, Min(0f)] private float exitDelay = 3f;
    [SerializeField] private Vector3 exitLocalOffset = new Vector3(-1.8f, 0f, 0.6f);
    [SerializeField] private bool alignPlayerYawToCar = true;

    [Header("Exit Animation")]
    [SerializeField, Min(0.01f)] private float exitAnimationDuration = 1.25f;
    [SerializeField, Min(0f)] private float exitStepHeight = 0.25f;
    [SerializeField] private Vector3 exitMidLocalOffset = new Vector3(-0.9f, 0.25f, 0.45f);

    private bool hasStartedExit;

    private void Awake()
    {
        if (carMover == null)
        {
            carMover = GetComponent<CityRoadVehicleMover>();
        }

        SetPlayerControlEnabled(false);
    }

    private void Update()
    {
        if (hasStartedExit || carMover == null || !carMover.HasFinished)
        {
            return;
        }

        hasStartedExit = true;
        StartCoroutine(ExitAfterDelay());
    }

    private IEnumerator ExitAfterDelay()
    {
        if (exitDelay > 0f)
        {
            yield return new WaitForSeconds(exitDelay);
        }

        if (playerRoot != null)
        {
            yield return AnimatePlayerExit();
        }

        SetPlayerControlEnabled(true);
    }

    private IEnumerator AnimatePlayerExit()
    {
        Vector3 startPosition = playerRoot.position;
        Quaternion startRotation = playerRoot.rotation;
        Vector3 middlePosition = transform.TransformPoint(exitMidLocalOffset);
        Vector3 endPosition = transform.TransformPoint(exitLocalOffset);
        Quaternion endRotation = alignPlayerYawToCar
            ? Quaternion.Euler(0f, transform.eulerAngles.y, 0f)
            : startRotation;

        playerRoot.SetParent(null, true);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, exitAnimationDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = progress * progress * (3f - (2f * progress));
            Vector3 position = QuadraticBezier(
                startPosition,
                middlePosition,
                endPosition,
                smoothProgress);
            position.y += Mathf.Sin(smoothProgress * Mathf.PI) * exitStepHeight;

            playerRoot.SetPositionAndRotation(
                position,
                Quaternion.Slerp(startRotation, endRotation, smoothProgress));

            yield return null;
        }

        playerRoot.SetPositionAndRotation(endPosition, endRotation);
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        for (int i = 0; i < enableAfterExit.Length; i++)
        {
            if (enableAfterExit[i] != null)
            {
                enableAfterExit[i].enabled = enabled;
            }
        }

        for (int i = 0; i < enableCollidersAfterExit.Length; i++)
        {
            if (enableCollidersAfterExit[i] != null)
            {
                enableCollidersAfterExit[i].enabled = enabled;
            }
        }
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 middle, Vector3 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * middle + t * t * end;
    }
}
