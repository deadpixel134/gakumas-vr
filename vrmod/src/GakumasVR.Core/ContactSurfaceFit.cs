using System.Numerics;

namespace GakumasVR.Core;

public readonly struct ContactSurfaceAnchor
{
    public Vector3 LocalCenter { get; }
    public Vector3 LocalOutward { get; }
    public float LocalRadius { get; }

    private ContactSurfaceAnchor(Vector3 center, Vector3 outward, float radius)
    { LocalCenter = center; LocalOutward = outward; LocalRadius = radius; }

    public static bool TryBind(Vector3 center, Vector3 outward, float radius, Matrix4x4 boneToWorld,
        out ContactSurfaceAnchor anchor)
    {
        anchor = default;
        if (!TryScale(boneToWorld, out float scale) || !Matrix4x4.Invert(boneToWorld, out var inverse) ||
            !ContactResponse.Finite(center) || !ContactResponse.Finite(outward) || outward.LengthSquared() < .01f ||
            !float.IsFinite(radius) || radius <= 0) return false;
        Vector3 localCenter = Vector3.Transform(center, inverse);
        Vector3 localOutward = Vector3.Normalize(Vector3.TransformNormal(outward, inverse));
        if (!ContactResponse.Finite(localCenter) || !ContactResponse.Finite(localOutward)) return false;
        anchor = new(localCenter, localOutward, radius / scale);
        return true;
    }

    public bool TryMap(Matrix4x4 boneToWorld, out Vector3 center, out Vector3 outward, out float radius)
    {
        center = outward = default; radius = 0;
        if (LocalRadius <= 0 || !TryScale(boneToWorld, out float scale)) return false;
        center = Vector3.Transform(LocalCenter, boneToWorld);
        outward = Vector3.Normalize(Vector3.TransformNormal(LocalOutward, boneToWorld));
        radius = LocalRadius * scale;
        return ContactResponse.Finite(center) && ContactResponse.Finite(outward) && float.IsFinite(radius) && radius > 0;
    }

    private static bool TryScale(Matrix4x4 matrix, out float scale)
    {
        float x = new Vector3(matrix.M11, matrix.M12, matrix.M13).Length();
        float y = new Vector3(matrix.M21, matrix.M22, matrix.M23).Length();
        float z = new Vector3(matrix.M31, matrix.M32, matrix.M33).Length();
        scale = (x + y + z) / 3f;
        float min = MathF.Min(x, MathF.Min(y, z)), max = MathF.Max(x, MathF.Max(y, z));
        return float.IsFinite(scale) && min > 0.000001f && max / min < 1.2f &&
            MathF.Abs(matrix.M14) + MathF.Abs(matrix.M24) + MathF.Abs(matrix.M34) < .0001f &&
            MathF.Abs(matrix.M44 - 1) < .0001f;
    }
}

public static class ContactSurfaceFit
{
    // Fit the outward weighted patch, with the sphere's front at the observed surface.
    public static bool TryFit(ReadOnlySpan<Vector3> points, ReadOnlySpan<Vector3> normals,
        out Vector3 center, out Vector3 outward, out float radius)
    {
        center = outward = default; radius = 0;
        if (points.Length < 12 || points.Length > 4096 || points.Length != normals.Length) return false;
        Vector3 normalSum = default, centroid = default;
        for (int i = 0; i < points.Length; i++)
        {
            if (!ContactResponse.Finite(points[i]) || !ContactResponse.Finite(normals[i]) || normals[i].LengthSquared() < .01f)
                return false;
            normalSum += Vector3.Normalize(normals[i]);
            centroid += points[i];
        }
        if (normalSum.Length() / points.Length < .2f) return false;
        outward = Vector3.Normalize(normalSum);
        centroid /= points.Length;
        var depths = new List<float>(points.Length);
        var widths = new List<float>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            if (Vector3.Dot(Vector3.Normalize(normals[i]), outward) < .2f) continue;
            Vector3 delta = points[i] - centroid;
            float depth = Vector3.Dot(delta, outward);
            depths.Add(depth);
            widths.Add((delta - outward * depth).Length());
        }
        if (depths.Count < 12) return false;
        depths.Sort(); widths.Sort();
        float width = widths[(int)((widths.Count - 1) * .75f)];
        if (!float.IsFinite(width) || width < .005f || width > .18f) return false;
        radius = Math.Clamp(width, .015f, .09f);
        center = centroid + outward * (depths[(int)((depths.Count - 1) * .85f)] - radius);
        return ContactResponse.Finite(center);
    }
}
