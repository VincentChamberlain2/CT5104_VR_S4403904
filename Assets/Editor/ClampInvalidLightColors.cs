#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ClampInvalidLightColors
{
    [MenuItem("Tools/Lighting/Clamp Invalid Light Colors")]
    public static void ClampLights()
    {
        // Unity 6–safe API: no sorting needed, include inactive objects
        Light[] lights = Object.FindObjectsByType<Light>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int fixedCount = 0;

        foreach (Light light in lights)
        {
            Color c = light.color;

            bool invalid =
                float.IsNaN(c.r) || float.IsNaN(c.g) || float.IsNaN(c.b) ||
                float.IsInfinity(c.r) || float.IsInfinity(c.g) || float.IsInfinity(c.b) ||
                c.r < 0f || c.g < 0f || c.b < 0f;

            if (!invalid)
                continue;

            Color original = c;

            c.r = Sanitize(c.r);
            c.g = Sanitize(c.g);
            c.b = Sanitize(c.b);
            c.a = Mathf.Clamp01(c.a);

            Undo.RecordObject(light, "Clamp Invalid Light Color");
            light.color = c;
            EditorUtility.SetDirty(light);

            Debug.LogWarning(
                $"[Lighting Clamp] Fixed invalid light color on '{light.name}'\n" +
                $"    Before: {original}\n" +
                $"    After : {c}",
                light
            );

            fixedCount++;
        }

        if (fixedCount == 0)
        {
            Debug.Log("[Lighting Clamp] No invalid light colours found.");
        }
        else
        {
            Debug.Log($"[Lighting Clamp] Fixed {fixedCount} light(s).");
        }
    }

    private static float Sanitize(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0f;

        if (value < 0f)
            return 0f;

        // Upper clamp for teaching safety (HDR sanity)
        return Mathf.Min(value, 10f);
    }
}
#endif
