using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player
{
    private Camera m_Camera;

    private Ray GetMouseRay()
    {
        m_Camera = Camera.main;
#if ENABLE_INPUT_SYSTEM
        if (m_Camera == null) return new Ray();
        var pos = new Vector3(
            Mouse.current.position.ReadValue().x,
            Mouse.current.position.ReadValue().y,
            0f
        );
        return m_Camera.ScreenPointToRay(pos);

#else
        return m_Camera.ScreenPointToRay(Input.mousePosition);
#endif
    }

    /// <summary>
    /// Raycast against terrain and return the 2D plane coordinates of the hit point.
    /// Uses <see cref="WorldExtensions.GetCoordinates"/> for plane abstraction.
    /// </summary>
    private bool GetPointerInput(out Vector2 hitPoint)
    {
        var ray = GetMouseRay();
        if (Physics.Raycast(ray, out var hit,
                120.0f, LayerMask.GetMask("Terrain")))
        {
            hitPoint = hit.point.GetCoordinates();
            return true;
        }

        hitPoint = Vector2.zero;
        return false;
    }
}
