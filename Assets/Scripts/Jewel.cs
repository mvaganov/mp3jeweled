using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jewel : MonoBehaviour
{
    public Jewelmatcher g;
    public Vector2Int rowcol;

    void OnMouseDown()
    {
        g.Click(this);
    }

    public bool IsAt(Vector3 position)
    {
        return Vector3.Distance(transform.position, position) < 1f / 1024;
    }

    public Vector3 IdealPosition()
    {
        return g.GetPosition(rowcol);
    }

    public void Update()
    {
        Vector3 p = IdealPosition();
        if (!IsAt(p))
        {
            Vector3 delta = p - transform.position;
            float dist = delta.magnitude;
            if (dist > Time.deltaTime / g.swapSpeed)
            {
                delta *= Time.deltaTime * g.swapSpeed;
            }
            transform.position += delta;
        }
    }
}
