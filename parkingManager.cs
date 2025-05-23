using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class parkingManager : MonoBehaviour
{
    public Transform[] leftSlots;
    public Transform[] rightSlots;
    private bool[] leftOccupied;
    private bool[] rightOccupied;


    void Awake()
    {
        leftOccupied = new bool[leftSlots.Length];
        rightOccupied = new bool[rightSlots.Length];
    }

    // Request available left slot
    public int RequestLeftSlot()
    {
        for (int i = 0; i < leftSlots.Length; i++)
        {
            if (!leftOccupied[i])
            {
                leftOccupied[i] = true;
                return i;
            }
        }
        return -1;
    }

    // Request available right slot
    public int RequestRightSlot()
    {
        for (int i = 0; i < rightSlots.Length; i++)
        {
            if (!rightOccupied[i])
            {
                rightOccupied[i] = true;
                return i;
            }
        }
        return -1;
    }

    // Release left slot by index
    public void ReleaseLeftSlot(int index)
    {
        if (index >= 0 && index < leftOccupied.Length)
            leftOccupied[index] = false;
    }

    // Release right slot by index
    public void ReleaseRightSlot(int index)
    {
        if (index >= 0 && index < rightOccupied.Length)
            rightOccupied[index] = false;
    }

    // Get left slot position and rotation
    public Vector3 GetLeftSlotPosition(int index)
    {
        if (index >= 0 && index < leftSlots.Length)
            return leftSlots[index].position;
        return Vector3.zero;
    }

    public Quaternion GetLeftSlotRotation(int index)
    {
        if (index >= 0 && index < leftSlots.Length)
            return leftSlots[index].rotation;
        return Quaternion.identity;
    }

    // Get right slot position and rotation
    public Vector3 GetRightSlotPosition(int index)
    {
        if (index >= 0 && index < rightSlots.Length)
            return rightSlots[index].position;
        return Vector3.zero;
    }

    public Quaternion GetRightSlotRotation(int index)
    {
        if (index >= 0 && index < rightSlots.Length)
            return rightSlots[index].rotation;
        return Quaternion.identity;
    }
}
