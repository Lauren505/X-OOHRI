using UnityEngine;

public class FrameConverterRosToUnity
{
    private static Matrix4x4 conventionTransform = new Matrix4x4(
        new Vector4(0, 0, 1, 0),
        new Vector4(-1, 0, 0, 0),
        new Vector4(0, 1, 0, 0),
        new Vector4(0, 0, 0, 1)
    );

    public static Matrix4x4 ApplyFrameConventionConversionToMatrix(Matrix4x4 matrix)
    {
        return conventionTransform * matrix;
    }

    public static Vector3 ApplyFrameConventionConversionToPoint(Vector3 point)
    {
        return conventionTransform.MultiplyPoint3x4(point);
    }
}