using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class HoverableButtonFx : MonoBehaviour
{
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float scaleLerp = 12f;
    [SerializeField] private float colorLerp = 12f;
    [SerializeField] private Color hoverColor = Color.white; // white on hover

    private Vector3 _baseScale;
    private Color _baseColor;   // set this by giving the Image a grey color in the Inspector
    private Graphic _graphic;
    private bool _hovered;

    private void Awake()
    {
        _graphic = GetComponent<Graphic>();
        _baseScale = transform.localScale;
        _baseColor = _graphic.color; // read “default grey” from the Image
    }

    public void SetHovered(bool value) => _hovered = value;

    private void Update()
    {
        var targetScale = _hovered ? _baseScale * hoverScale : _baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerp);

        var targetColor = _hovered ? hoverColor : _baseColor;
        _graphic.color = Color.Lerp(_graphic.color, targetColor, Time.deltaTime * colorLerp);
    }

    private void OnDisable()
    {
        transform.localScale = _baseScale;
        _graphic.color = _baseColor;
        _hovered = false;
    }
}
