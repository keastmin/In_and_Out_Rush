using UnityEngine;

public class HexaTileMap
{
    public float HexSize;
    public bool IsFlatTop;

    public Vector2 SnapToHexTile(Vector2 position)
    {
        if (IsFlatTop)
        {
            // Flat-Top Hex 좌표 변환
            float q = position.x * 2f / 3f / HexSize;
            float r = (-position.x / 3f + Mathf.Sqrt(3f) / 3f * position.y) / HexSize;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(-q - r);

            // 큐브 좌표 보정
            float q_diff = Mathf.Abs(rq - q);
            float r_diff = Mathf.Abs(rr - r);
            float s_diff = Mathf.Abs(rs + q + r);

            if (q_diff > r_diff && q_diff > s_diff)
                rq = -rr - rs;
            else if (r_diff > s_diff)
                rr = -rq - rs;

            // 큐브 좌표를 다시 2D로 변환
            float x = HexSize * 1.5f * rq;
            float y = HexSize * Mathf.Sqrt(3f) * (rr + 0.5f * rq);
            return new Vector2(x, y);
        }
        else
        {
            // Pointy-Top Hex 좌표 변환
            float q = (position.x * Mathf.Sqrt(3f) / 3f - position.y / 3f) / HexSize;
            float r = position.y * 2f / 3f / HexSize;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(-q - r);

            // 큐브 좌표 보정
            float q_diff = Mathf.Abs(rq - q);
            float r_diff = Mathf.Abs(rr - r);
            float s_diff = Mathf.Abs(rs + q + r);

            if (q_diff > r_diff && q_diff > s_diff)
                rq = -rr - rs;
            else if (r_diff > s_diff)
                rr = -rq - rs;

            // 큐브 좌표를 다시 2D로 변환
            float x = HexSize * Mathf.Sqrt(3f) * (rq + 0.5f * rr);
            float y = HexSize * 1.5f * rr;
            return new Vector2(x, y);
        }
    }
}