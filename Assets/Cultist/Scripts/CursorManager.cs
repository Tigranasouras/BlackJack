using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorTexture;  // Drag your PNG here in the Inspector
    public Vector2 hotSpot = Vector2.zero;  // Offset where the "click" happens

    void Start()
    {
        Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }
}
//void OnMouseEnter()
//{
//    Cursor.SetCursor(hoverTexture, hotSpot, CursorMode.Auto);
//}

//void OnMouseExit()
//{
//    Cursor.SetCursor(defaultTexture, hotSpot, CursorMode.Auto);
//}
