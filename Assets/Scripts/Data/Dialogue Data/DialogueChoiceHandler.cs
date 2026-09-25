using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogueChoiceHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public event Action<int> OnHover;
    public event Action<int> OnClick;
    
    private int index;
 
    public void Setup(int index)
    {
        this.index = index;
    }
 
    public void OnPointerEnter(PointerEventData eventData) => OnHover?.Invoke(index);
    public void OnPointerClick(PointerEventData eventData) => OnClick?.Invoke(index);
}