using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class WorldInteractable : MonoBehaviour
{
    private static readonly HashSet<WorldInteractable> ActiveInteractables = new();

    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool oneShot;
    [SerializeField] private UnityEvent interacted;

    private bool consumed;

    public event Action Triggered;

    public bool CanInteract => isActiveAndEnabled && !consumed;
    public Vector3 InteractionPosition => interactionPoint != null
        ? interactionPoint.position
        : transform.position;

    public static WorldInteractable FindNearest(Vector3 origin, float radius)
    {
        WorldInteractable nearest = null;
        float nearestSqrDistance = radius * radius;

        foreach (WorldInteractable candidate in ActiveInteractables)
        {
            if (candidate == null || !candidate.CanInteract)
            {
                continue;
            }

            float sqrDistance = (candidate.InteractionPosition - origin).sqrMagnitude;
            if (sqrDistance > nearestSqrDistance)
            {
                continue;
            }

            nearest = candidate;
            nearestSqrDistance = sqrDistance;
        }

        return nearest;
    }

    public bool TryInteract()
    {
        if (!CanInteract)
        {
            return false;
        }

        interacted?.Invoke();
        Triggered?.Invoke();
        if (oneShot)
        {
            consumed = true;
        }

        return true;
    }

    public void ResetInteraction()
    {
        consumed = false;
    }

    private void OnEnable()
    {
        ActiveInteractables.Add(this);
    }

    private void OnDisable()
    {
        ActiveInteractables.Remove(this);
    }
}
