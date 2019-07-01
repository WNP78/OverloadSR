using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace US.UI
{
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldScrollFixer : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, IPointerUpHandler
    {
        private ScrollRect _scrollRect;
        private TMP_InputField _input;
        private bool _isDragging;
        private bool _preventScrollRectDrag;

        private void Start()
        {
            _scrollRect = GetComponentInParent<ScrollRect>();
            _input = GetComponent<TMP_InputField>();
            _input.DeactivateInputField();
            _input.onDeselect.AddListener(_ => _preventScrollRectDrag = false);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (_preventScrollRectDrag)
                return;

            _scrollRect.OnBeginDrag(data);
            _isDragging = true;

            _input.DeactivateInputField();
        }

        public void OnDrag(PointerEventData data)
        {
            _scrollRect.OnDrag(data);
        }

        public void OnEndDrag(PointerEventData data)
        {
            _scrollRect.OnEndDrag(data);
            _isDragging = false;
        }

        public void OnScroll(PointerEventData data)
        {
            _scrollRect.OnScroll(data);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (!_isDragging && !data.dragging)
            {
                _input.ActivateInputField();
                _preventScrollRectDrag = true;
            }
        }
    }
}
