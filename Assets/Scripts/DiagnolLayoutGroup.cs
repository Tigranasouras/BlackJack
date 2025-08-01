using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiagnolLayoutGroup : MonoBehaviour
{
    public Vector2 offset = new Vector2(35f, -30f); // X = horizontal spacing, Y = vertical spacing

    void Update()
    {
        ApplyLayout();
    }

    void ApplyLayout()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null) continue;

            child.anchoredPosition = i * offset;
        }
    }
}
