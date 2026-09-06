using System;
using UnityEngine;

namespace REDIZIT.RUI
{
    public abstract class GestureRecognizer : IGestureArenaMember
    {
        public abstract void OnPointerDown(PointerDownEvent e, GestureArena arena);
        public virtual void OnPointerMove(PointerMoveEvent e, GestureArena arena) { }
        public virtual void OnPointerUp(PointerUpEvent e, GestureArena arena) { }

        public abstract void AcceptGesture();
        public abstract void RejectGesture();
    }

    public class TapGestureRecognizer : GestureRecognizer
    {
        public Action onDown;
        public Action onTap;
        public Action onCancel;

        private Vector2 startPos;
        private bool isTracking;
        private const float kSlop = 8f;

        public override void OnPointerDown(PointerDownEvent e, GestureArena arena)
        {
            if (arena == null) return;
            startPos = e.position;
            isTracking = true;
            arena.Add(this);
            onDown?.Invoke();
        }

        public override void OnPointerMove(PointerMoveEvent e, GestureArena arena)
        {
            if (!isTracking || arena == null) return;

            if (Vector2.Distance(startPos, e.position) > kSlop)
            {
                arena.Resolve(this, GestureDisposition.Rejected);
                isTracking = false;
            }
        }

        public override void OnPointerUp(PointerUpEvent e, GestureArena arena)
        {
            if (!isTracking) return;
            isTracking = false;

            arena?.Resolve(this, GestureDisposition.Accepted);
        }

        public override void AcceptGesture()
        {
            onTap?.Invoke();
        }

        public override void RejectGesture()
        {
            isTracking = false;
            onCancel?.Invoke();
        }
    }

    public class DragGestureRecognizer : GestureRecognizer
    {
        public Action onDragStart;
        public Action<Vector2> onDragUpdate;
        public Action onDragEnd;

        private Vector2 startPos;
        private bool isDragging;
        private bool isTracking; // Защита: отслеживаем только при нажатой кнопке мыши
        private const float kDragSlop = 6f;

        public override void OnPointerDown(PointerDownEvent e, GestureArena arena)
        {
            if (arena == null) return;
            startPos = e.position;
            isDragging = false;
            isTracking = true;
            arena.Add(this);
        }

        public override void OnPointerMove(PointerMoveEvent e, GestureArena arena)
        {
            if (!isTracking || arena == null) return;

            if (!isDragging)
            {
                if (Mathf.Abs(e.position.y - startPos.y) > kDragSlop || Mathf.Abs(e.position.x - startPos.x) > kDragSlop)
                {
                    isDragging = true;
                    arena.Resolve(this, GestureDisposition.Accepted);
                    onDragStart?.Invoke();
                }
            }
            else
            {
                onDragUpdate?.Invoke(e.delta);
            }
        }

        public override void OnPointerUp(PointerUpEvent e, GestureArena arena)
        {
            if (!isTracking) return;
            isTracking = false;

            if (isDragging)
            {
                isDragging = false;
                onDragEnd?.Invoke();
            }
            else
            {
                arena?.Resolve(this, GestureDisposition.Rejected);
            }
        }

        public override void AcceptGesture() { }
        public override void RejectGesture()
        {
            isTracking = false;
            isDragging = false;
        }
    }
}