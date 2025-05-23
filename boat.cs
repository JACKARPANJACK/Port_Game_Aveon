using System.Collections;
using UnityEngine;

public class boat : MonoBehaviour
{
    public parkingManager parkingManager;
    public float speed = 2f;
    public float obstacleCheckDistance = 1.5f;
    public float dockDistance = 5f;
    public float dockThreshold = 5f;
    public float rotateSpeed = 180f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private bool isMoving = false;
    private bool movingToDock = false;
    private bool returning = false;
    private bool docking = false;
    private bool docked = false;
    private bool dockCoroutineStarted = false;

    private int slotIndex = -1;
    private Camera cam;

    // New flag to know if slot assigned
    private bool slotAssigned = false;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        cam = Camera.main;
    }

    void Update()
    {
        if (returning)
        {
            ReturnToStart();
            return;
        }

        if (!isMoving || docking) return;

        if (!slotAssigned)
        {
            AssignSlotBasedOnRotation();
        }

        if (CheckObstacleAhead()) return;

        Debug.DrawLine(transform.position, targetPosition, Color.green);

        if (!movingToDock)
        {
            Vector3 nextPos = transform.position + transform.forward * speed * Time.deltaTime;

            if (IsWithinCamera(nextPos))
            {
                transform.position = nextPos;

                float dist = Vector3.Distance(transform.position, targetPosition);
                if (dist <= dockDistance)
                {
                    movingToDock = true;
                }
            }
            else
            {
                Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);

                if (viewportPos.x > 0.55f)
                {
                    transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime);
                }
                else if (viewportPos.x < 0.45f)
                {
                    transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position += transform.forward * speed * Time.deltaTime;
                }

                float dist = Vector3.Distance(transform.position, targetPosition);
                if (dist <= dockDistance)
                {
                    movingToDock = true;
                }
            }
        }
        else
        {
            Vector3 dir = targetPosition - transform.position;
            float dist = dir.magnitude;

            float facingDot = Vector3.Dot(transform.forward.normalized, dir.normalized);

            if ((dist <= dockThreshold || (dist <= dockDistance * 1.5f && facingDot > 0.7f)))
            {
                if (!dockCoroutineStarted)
                {
                    dockCoroutineStarted = true;
                    StartCoroutine(Dock());
                }
                return;
            }

            dir.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

            float moveDist = Mathf.Min(speed * Time.deltaTime, dist);
            transform.position += transform.forward * moveDist;
        }
    }

    // New method to assign slot after detecting rotation direction
    void AssignSlotBasedOnRotation()
    {
        float rotationY = transform.rotation.eulerAngles.y;

        // Convert rotationY to -180 to 180 range for easier logic
        float rotY = (rotationY > 180) ? rotationY - 360 : rotationY;

        // Determine rotation direction by comparing current rotation with original rotation
        // Positive rotY means rotating right, negative means rotating left roughly

        // If rotating right (rotY > 5 degrees)
        if (rotY > 5f)
        {
            slotIndex = parkingManager.RequestRightSlot();
            if (slotIndex == -1)
            {
                Debug.Log("No right-side docking slot available.");
                // fallback: try left slots
                slotIndex = parkingManager.RequestLeftSlot();
                if (slotIndex == -1)
                {
                    Debug.Log("No docking slot available.");
                    isMoving = false;
                    return;
                }
                targetPosition = parkingManager.GetLeftSlotPosition(slotIndex);
                targetRotation = parkingManager.GetLeftSlotRotation(slotIndex);
                Debug.Log($"Fallback assigned left slot {slotIndex}");
            }
            else
            {
                targetPosition = parkingManager.GetRightSlotPosition(slotIndex);
                targetRotation = parkingManager.GetRightSlotRotation(slotIndex);
                Debug.Log($"Assigned right slot {slotIndex}");
            }
            slotAssigned = true;
        }
        // If rotating left (rotY < -5 degrees)
        else if (rotY < -5f)
        {
            slotIndex = parkingManager.RequestLeftSlot();
            if (slotIndex == -1)
            {
                Debug.Log("No left-side docking slot available.");
                // fallback: try right slots
                slotIndex = parkingManager.RequestRightSlot();
                if (slotIndex == -1)
                {
                    Debug.Log("No docking slot available.");
                    isMoving = false;
                    return;
                }
                targetPosition = parkingManager.GetRightSlotPosition(slotIndex);
                targetRotation = parkingManager.GetRightSlotRotation(slotIndex);
                Debug.Log($"Fallback assigned right slot {slotIndex}");
            }
            else
            {
                targetPosition = parkingManager.GetLeftSlotPosition(slotIndex);
                targetRotation = parkingManager.GetLeftSlotRotation(slotIndex);
                Debug.Log($"Assigned left slot {slotIndex}");
            }
            slotAssigned = true;
        }
        // else no significant rotation yet, wait
    }

    public void OnMouseDown()
    {
        if (isMoving || docking || docked) return;

        isMoving = true;
        movingToDock = false;
        returning = false;
        slotAssigned = false;  // reset slot assigned flag
    }

    IEnumerator Dock()
    {
        docking = true;
        isMoving = false;

        float t = 0;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        docking = false;
        docked = true;
        dockCoroutineStarted = false;

        Debug.Log("Boat docked.");
    }

    bool CheckObstacleAhead()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, obstacleCheckDistance))
        {
            if (hit.transform != transform)
            {
                Debug.Log("Obstacle: " + hit.transform.name);
                StartReturn();
                return true;
            }
        }

        return false;
    }

    void StartReturn()
    {
        isMoving = false;
        movingToDock = false;
        returning = true;
        docking = false;

        if (slotIndex != -1)
        {
            // Release the slot properly regardless of current position
            // Using the assigned slot side based on where it was assigned
            if (slotAssigned)
            {
                // We know from assignment which side slot was assigned
                Vector3 viewportPos = cam.WorldToViewportPoint(targetPosition);
                bool assignedRightSide = viewportPos.x >= 0.5f;

                if (assignedRightSide)
                    parkingManager.ReleaseRightSlot(slotIndex);
                else
                    parkingManager.ReleaseLeftSlot(slotIndex);
            }
            else
            {
                // fallback, try releasing both sides to be safe
                parkingManager.ReleaseRightSlot(slotIndex);
                parkingManager.ReleaseLeftSlot(slotIndex);
            }

            slotIndex = -1;
        }
    }

    void ReturnToStart()
    {
        float dist = Vector3.Distance(transform.position, originalPosition);

        if (dist > 0.1f)
        {
            Vector3 dir = (originalPosition - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * 3f);
        }
        else
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            returning = false;
            docked = false;
        }
    }

    bool IsWithinCamera(Vector3 worldPos)
    {
        Vector3 viewport = cam.WorldToViewportPoint(worldPos);
        return viewport.x > 0.05f && viewport.x < 0.95f &&
               viewport.y > 0.05f && viewport.y < 0.95f &&
               viewport.z > 0;
    }
}
