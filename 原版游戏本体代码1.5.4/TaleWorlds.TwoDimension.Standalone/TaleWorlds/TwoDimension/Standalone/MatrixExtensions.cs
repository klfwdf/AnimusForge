using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension.Standalone;

public static class MatrixExtensions
{
	public static Matrix4x4 ToMatrix4x4(this MatrixFrame matrixFrame)
	{
		return new Matrix4x4(matrixFrame.rotation.s.x, matrixFrame.rotation.s.y, matrixFrame.rotation.s.z, matrixFrame.rotation.s.w, matrixFrame.rotation.f.x, matrixFrame.rotation.f.y, matrixFrame.rotation.f.z, matrixFrame.rotation.f.w, matrixFrame.rotation.u.x, matrixFrame.rotation.u.y, matrixFrame.rotation.u.z, matrixFrame.rotation.u.w, matrixFrame.origin.x, matrixFrame.origin.y, matrixFrame.origin.z, matrixFrame.origin.w);
	}

	public static MatrixFrame ToMatrixFrame(this Matrix4x4 matrix)
	{
		return new MatrixFrame(matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44);
	}

	public static bool AreAllComponentsValid(this Matrix4x4 matrix)
	{
		if (!float.IsNaN(matrix.M11) && !float.IsNaN(matrix.M12) && !float.IsNaN(matrix.M13) && !float.IsNaN(matrix.M14) && !float.IsNaN(matrix.M21) && !float.IsNaN(matrix.M22) && !float.IsNaN(matrix.M23) && !float.IsNaN(matrix.M24) && !float.IsNaN(matrix.M31) && !float.IsNaN(matrix.M32) && !float.IsNaN(matrix.M33) && !float.IsNaN(matrix.M34) && !float.IsNaN(matrix.M41) && !float.IsNaN(matrix.M42) && !float.IsNaN(matrix.M43) && !float.IsNaN(matrix.M44) && !float.IsInfinity(matrix.M11) && !float.IsInfinity(matrix.M12) && !float.IsInfinity(matrix.M13) && !float.IsInfinity(matrix.M14) && !float.IsInfinity(matrix.M21) && !float.IsInfinity(matrix.M22) && !float.IsInfinity(matrix.M23) && !float.IsInfinity(matrix.M24) && !float.IsInfinity(matrix.M31) && !float.IsInfinity(matrix.M32) && !float.IsInfinity(matrix.M33) && !float.IsInfinity(matrix.M34) && !float.IsInfinity(matrix.M41) && !float.IsInfinity(matrix.M42) && !float.IsInfinity(matrix.M43))
		{
			return !float.IsInfinity(matrix.M44);
		}
		return false;
	}

	public static bool AreAllComponentsValid(this MatrixFrame matrix)
	{
		if (matrix.origin.IsValidXYZW && matrix.rotation.s.IsValidXYZW && matrix.rotation.f.IsValidXYZW)
		{
			return matrix.rotation.u.IsValidXYZW;
		}
		return false;
	}

	public static MatrixFrame CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
	{
		return Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane).ToMatrixFrame();
	}
}
