using UnityEngine;

public class FrameConverterUnityToRos
{
    private static Matrix4x4 conventionTransformUnityToRos = new Matrix4x4(
        new Vector4(0, -1, 0, 0),
        new Vector4(0, 0, 1, 0),
        new Vector4(1, 0, 0, 0),
        new Vector4(0, 0, 0, 1)
    );

    public static Matrix4x4 ApplyFrameConventionConversionToMatrix(Matrix4x4 matrix)
    {
        return conventionTransformUnityToRos * matrix;
    }

    public static Vector3 ApplyFrameConventionConversionToPoint(Vector3 point)
    {
        return conventionTransformUnityToRos.MultiplyPoint3x4(point);
    }

    public static Quaternion ConvertQuaternion(Quaternion unityRotation)
    {
        Matrix4x4 unityRotationMatrix = Matrix4x4.Rotate(unityRotation);
        return (conventionTransformUnityToRos * unityRotationMatrix * conventionTransformUnityToRos.transpose).rotation;
       // unity_t_ros o* unity0_to_unity1 * ros-to-unity 
    }


}