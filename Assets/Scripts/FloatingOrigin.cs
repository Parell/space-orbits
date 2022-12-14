using System.Collections.Generic;
using UnityEngine;

public class FloatingOrigin : MonoBehaviour
{
    public static FloatingOrigin Instance;

    public int threshold;
    [SerializeField] private Transform localCamera;
    [SerializeField] private Transform scaledCamera;
    public List<Transform> localTransforms;
    public List<Transform> scaledTransforms;

    [HideInInspector] public Vector3d currentPosition;
    [HideInInspector] public Vector3d originPosition;
    [HideInInspector] public Vector3d currentPositionScaled;
    [HideInInspector] public Vector3d originPositionScaled;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        currentPosition = (Vector3d)localCamera.position + originPosition;
        currentPositionScaled = (Vector3d)scaledCamera.position + originPositionScaled;

        if (localCamera.position.magnitude > threshold)
        {
            MoveOrigin(localCamera.position);

        }

        if (scaledCamera.position.magnitude > threshold)
        {
            MoveOriginScaled(scaledCamera.position);

            // var lines = FindObjectsOfType<LineRenderer>() as LineRenderer[];
            // foreach (var line in lines)
            // {
            //     Vector3[] positions = new Vector3[line.positionCount];

            //     int positionCount = line.GetPositions(positions);
            //     for (int i = 0; i < positionCount; ++i)
            //         positions[i] -= localCamera.position;

            //     line.SetPositions(positions);
            // }
        }
    }

    private void MoveOrigin(Vector3 delta)
    {
        foreach (Transform target in localTransforms)
        {
            target.position -= delta;
        }

        originPosition += (Vector3d)delta;
    }

    private void MoveOriginScaled(Vector3 delta)
    {
        foreach (Transform target in scaledTransforms)
        {
            target.position -= delta;
        }

        originPositionScaled += (Vector3d)delta;
    }

    public void RegisterTransform(Transform target)
    {
        localTransforms.Add(target);
    }

    public void RegisterTransformScaled(Transform target)
    {
        scaledTransforms.Add(target);
    }
}
