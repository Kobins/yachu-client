using UnityEngine;

namespace Yachu.Client {
public static class QuaternionUtil {
    private static int[] _nextIndex = { 1, 2, 0 };

    public static Quaternion FromMatrix(in Matrix4x4 matrix) {
        Quaternion q = new Quaternion(0f, 0f, 0f, 1f);
        q.FromMatrix(matrix);
        return q;
    }
    public static Quaternion FromMatrix(this ref Quaternion q, Matrix4x4 matrix) {
        float root;
        matrix = matrix.transpose;
        float trace = matrix[0, 0] + matrix[1, 1] + matrix[2, 2];

        if (!Mathf.Approximately(matrix.GetColumn(0).sqrMagnitude, 1f)
            || !Mathf.Approximately(matrix.GetColumn(1).sqrMagnitude, 1f)
            || !Mathf.Approximately(matrix.GetColumn(2).sqrMagnitude, 1f)
        ) {
            q.Set(0f, 0f, 0f, 1f);
            return q;
        }

        if (trace > 0f) {
            root = Mathf.Sqrt(trace + 1f);
            q.w = 0.5f * root;
            root = 0.5f / root;
            
            q.x = (matrix[1, 2] - matrix[2, 1]) * root;
            q.y = (matrix[2, 0] - matrix[0, 2]) * root;
            q.z = (matrix[0, 1] - matrix[1, 0]) * root;
        }
        else {
            int i = 0;

            if (matrix[1, 1] > matrix[0, 0]) { i = 1; }
            if (matrix[2, 2] > matrix[i, i]) { i = 2; }
            int j = _nextIndex[i];
            int k = _nextIndex[j];

            root = Mathf.Sqrt(matrix[i, i] - matrix[j, j] - matrix[k, k] + 1f);

            q[i] = 0.5f * root;
            root = 0.5f / root;

            q[j] = (matrix[i, j] + matrix[j, i]) * root;
            q[k] = (matrix[i, k] + matrix[k, i]) * root;

            q.w = (matrix[j, k] - matrix[k, j]) * root;
        }

        return q;
    }
}
}