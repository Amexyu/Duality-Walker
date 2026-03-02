using UnityEngine;

public class ShapeBlock : MonoBehaviour
{
    [SerializeField] private bool isBlack = true;
    [SerializeField] private float borderScale = 1.12f;

    public bool IsBlack => isBlack;

    public void Initialize(bool black, Sprite sprite)
    {
        isBlack = black;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = black ? Color.black : Color.white;
        sr.sortingOrder = 20;

        // Íâ¿ò£ººÚ¿éÅä°×¿ò£¬°×¿éÅäºÚ¿ò
        SetupBorder(sprite, black ? Color.white : Color.black, sr.sortingOrder - 1);
    }

    private void SetupBorder(Sprite sprite, Color borderColor, int sortingOrder)
    {
        Transform border = transform.Find("Border");
        GameObject borderGo;
        if (border == null)
        {
            borderGo = new GameObject("Border");
            borderGo.transform.SetParent(transform, false);
        }
        else
        {
            borderGo = border.gameObject;
        }

        borderGo.transform.localPosition = Vector3.zero;
        borderGo.transform.localRotation = Quaternion.identity;
        borderGo.transform.localScale = new Vector3(borderScale, borderScale, 1f);

        var borderSr = borderGo.GetComponent<SpriteRenderer>();
        if (borderSr == null) borderSr = borderGo.AddComponent<SpriteRenderer>();
        borderSr.sprite = sprite;
        borderSr.color = borderColor;
        borderSr.sortingOrder = sortingOrder;
    }
}