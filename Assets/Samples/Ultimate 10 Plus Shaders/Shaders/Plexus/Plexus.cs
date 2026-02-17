using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Safer Plexus implementation for Unity 6 + VR:
/// - Reuses ComputeBuffers (no per-frame GPU allocations)
/// - Reuses Mesh (no per-frame Mesh allocations)
/// - Fixes coroutine enable logic
/// - Avoids NaNs in line extrusion
/// </summary>
public class PlexusSafe : MonoBehaviour
{
    [Header("Simulation")]
    public ComputeShader plexus;
    public int amountOfPoints = 100;
    [Tooltip("Connections recalculated per frame step (higher = more CPU).")]
    public int pointsProcessedPerFrame = 2;
    public Vector3 box = new Vector3(4, 4, 4);
    public float particleSpeed = 1.0f;
    public float maxConnDistance = 3.0f;

    [Header("Rendering")]
    [Range(0.0001f, 0.2f)]
    public float lineWidth = 0.02f;
    public Material lineMaterial;

    [Header("Control")]
    public bool isEnabled = true;

    // Data
    private Vector3[] positions;
    private Vector3[] defaultPositions;
    private Vector3[] velocities;

    // Connections
    private readonly List<KeyValuePair<int, int>> connected = new();
    private float maxConnDistanceSqr;

    // GPU buffers
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer defaultPositionsBuffer;
    private ComputeBuffer velocitiesBuffer;
    private int kernelIndex = -1;

    // Mesh + CPU build buffers
    private Mesh lineMesh;
    private readonly List<Vector3> lineVerts = new(4096);
    private readonly List<int> lineTris = new(6144);

    private Coroutine connectRoutine;

    private void OnEnable()
    {
        if (!lineMaterial)
        {
            Debug.LogWarning($"{nameof(PlexusSafe)} on {name}: No lineMaterial assigned. Disabling.");
            enabled = false;
            return;
        }

        // init arrays
        positions = new Vector3[amountOfPoints];
        defaultPositions = new Vector3[amountOfPoints];
        velocities = new Vector3[amountOfPoints];

        for (int i = 0; i < amountOfPoints; i++)
        {
            positions[i] = new Vector3(
                Random.Range(-box.x, box.x),
                Random.Range(-box.y, box.y),
                Random.Range(-box.z, box.z));

            defaultPositions[i] = positions[i];
            velocities[i] = Vector3.zero;
        }

        maxConnDistanceSqr = maxConnDistance * maxConnDistance;

        // setup material uniform once
        lineMaterial.SetVector("_BoxDims", new Vector4(box.x, box.y, box.z, 1));

        // mesh created once
        lineMesh = new Mesh { name = "PlexusLines" };
        lineMesh.MarkDynamic();

        // compute shader setup (optional)
        if (plexus != null)
        {
            kernelIndex = plexus.FindKernel("MoveParticels");

            positionsBuffer = new ComputeBuffer(amountOfPoints, sizeof(float) * 3);
            defaultPositionsBuffer = new ComputeBuffer(amountOfPoints, sizeof(float) * 3);
            velocitiesBuffer = new ComputeBuffer(amountOfPoints, sizeof(float) * 3);

            positionsBuffer.SetData(positions);
            defaultPositionsBuffer.SetData(defaultPositions);
            velocitiesBuffer.SetData(velocities);
        }
        else
        {
            Debug.LogWarning($"{nameof(PlexusSafe)} on {name}: No ComputeShader assigned. Will not move points on GPU.");
        }

        // start connection refresh
        connectRoutine = StartCoroutine(ConnectDotsLoop());
    }

    private void OnDisable()
    {
        if (connectRoutine != null) StopCoroutine(connectRoutine);
        connectRoutine = null;

        positionsBuffer?.Release();
        defaultPositionsBuffer?.Release();
        velocitiesBuffer?.Release();

        positionsBuffer = null;
        defaultPositionsBuffer = null;
        velocitiesBuffer = null;

        if (lineMesh != null)
        {
            Destroy(lineMesh);
            lineMesh = null;
        }
    }

    private void Update()
    {
        if (!isEnabled) return;

        MovePoints();
        RenderLines();
    }

    private void MovePoints()
    {
        if (plexus == null || kernelIndex < 0) return;

        // feed buffers
        positionsBuffer.SetData(positions);
        defaultPositionsBuffer.SetData(defaultPositions);
        velocitiesBuffer.SetData(velocities);

        plexus.SetBuffer(kernelIndex, "positions", positionsBuffer);
        plexus.SetBuffer(kernelIndex, "defaultPositions", defaultPositionsBuffer);
        plexus.SetBuffer(kernelIndex, "velocities", velocitiesBuffer);

        plexus.SetFloat("deltaTime", Time.deltaTime);
        plexus.SetFloat("elapsedTime", Time.time);
        plexus.SetFloat("particleSpeed", particleSpeed);

        // dispatch (1 thread group per point is unusual; keeping it consistent with your original)
        plexus.Dispatch(kernelIndex, amountOfPoints, 1, 1);

        // read back
        positionsBuffer.GetData(positions);
        velocitiesBuffer.GetData(velocities);
    }

    private static float DistanceSqr(in Vector3 a, in Vector3 b)
    {
        Vector3 d = a - b;
        return d.x * d.x + d.y * d.y + d.z * d.z;
    }

    private IEnumerator ConnectDotsLoop()
    {
        var wait = new WaitForEndOfFrame();
        int index = 0;

        while (true)
        {
            yield return wait;

            if (!isEnabled) continue;

            // refresh connections gradually (keeps CPU manageable)
            for (int j = 0; j < pointsProcessedPerFrame; j++)
            {
                if (index >= amountOfPoints) index = 0;

                Vector3 current = positions[index];

                // remove connections involving this point
                for (int k = connected.Count - 1; k >= 0; k--)
                {
                    var pair = connected[k];
                    if (pair.Key == index || pair.Value == index)
                        connected.RemoveAt(k);
                }

                for (int i = 0; i < amountOfPoints; i++)
                {
                    if (i == index) continue;

                    if (DistanceSqr(current, positions[i]) < maxConnDistanceSqr)
                        connected.Add(new KeyValuePair<int, int>(index, i));
                }

                index++;
            }
        }
    }

    private void RenderLines()
    {
        if (connected.Count == 0) return;

        lineMesh.Clear(false);
        lineVerts.Clear();
        lineTris.Clear();

        // Build quads per line (4 verts, 6 tris)
        // Use a stable side vector: camera-facing if possible.
        Camera cam = Camera.main;
        Vector3 camForward = cam ? cam.transform.forward : Vector3.forward;

        for (int i = 0; i < connected.Count; i++)
        {
            Vector3 p1 = positions[connected[i].Key];
            Vector3 p2 = positions[connected[i].Value];

            Vector3 dir = (p2 - p1);
            float dirMag = dir.magnitude;
            if (dirMag < 1e-5f) continue;

            dir /= dirMag;

            // Side vector: perpendicular to line and camera forward
            Vector3 side = Vector3.Cross(dir, camForward);
            float sideMag = side.magnitude;

            // Fallback if nearly parallel (avoid NaN)
            if (sideMag < 1e-5f)
                side = Vector3.Cross(dir, Vector3.up);

            side = side.normalized;

            Vector3 offset = side * (lineWidth * 0.5f);

            int baseIndex = lineVerts.Count;

            lineVerts.Add(p1 + offset);
            lineVerts.Add(p1 - offset);
            lineVerts.Add(p2 + offset);
            lineVerts.Add(p2 - offset);

            // two triangles
            lineTris.Add(baseIndex + 0);
            lineTris.Add(baseIndex + 1);
            lineTris.Add(baseIndex + 2);

            lineTris.Add(baseIndex + 3);
            lineTris.Add(baseIndex + 2);
            lineTris.Add(baseIndex + 1);
        }

        lineMesh.SetVertices(lineVerts);
        lineMesh.SetTriangles(lineTris, 0, true);
        lineMesh.RecalculateBounds();

        // Draw
        Graphics.DrawMesh(lineMesh, transform.localToWorldMatrix, lineMaterial, 0);
    }
}
