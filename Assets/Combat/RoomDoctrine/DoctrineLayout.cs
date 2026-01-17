using UnityEngine;

[System.Serializable]
public class DoctrineLayout
{
    public Vector3 frontOffset = Vector3.up * 1.5f;
    public Vector3 rearOffset = Vector3.down * 1.2f;

    public Vector3 flankLeftOffset = Vector3.left * 2f;
    public Vector3 flankRightOffset = Vector3.right * 2f;

    public Vector3 anchorOffset = Vector3.zero;
}
