using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
	public class InputField : CanvasComponent, IPointerDownHandler
	{
		public string text = string.Empty;
		public string placeholder = "Type text...";
		public Color placeholderColor = new Color(1f, 1f, 1f, 0.35f);
		public Color textColor = Color.white;
		public Color caretColor = Color.white;

		public Action<string> onValueChanged;
		public Action<string> onSubmit;
		public Action onCancel;

		public bool isFocused = false;
		public int caretPosition = 0;

		private Label targetLabel;
		private float caretBlinkTimer = 0f;
		private bool caretVisible = true;

		// Ссылка на активное сфокусированное поле во всем приложении
		public static InputField ActiveField { get; private set; }

		[Inject] private CanvasService service;

		public override void OnAttached()
		{
			base.OnAttached();
			if (targetLabel == null && Element != null)
			{
				targetLabel = Element.TryGetComponent<Label>();
			}

			UpdateLabelVisual();
		}

		public void OnPointerDown(PointerDownEvent e, GestureArena arena)
		{
			if (e.button == 0)
			{
				Focus();

				// Ставим каретку в конец строки при клике (или рассчитываем по координатам)
				caretPosition = text.Length;
				ResetCaretBlink();
				MarkDirty();
			}
		}

		public void Focus()
		{
			if (ActiveField != null && ActiveField != this)
			{
				ActiveField.Unfocus();
			}

			ActiveField = this;
			isFocused = true;
			ResetCaretBlink();
			UpdateLabelVisual();
			MarkDirty();
		}

		public void Unfocus()
		{
			if (ActiveField == this)
			{
				ActiveField = null;
			}

			if (isFocused)
			{
				isFocused = false;
				UpdateLabelVisual();
				MarkDirty();
				onSubmit?.Invoke(text);
			}
		}

		public override void Update()
		{
			base.Update();

			// Закрытие фокуса по клику мимо поля
			if (isFocused)
			{
				if (Input.GetMouseButtonDown(0))
				{
					Rect bounds = Element.GetScreenBounds();
					bool clickedInside = bounds.Contains(Input.mousePosition);

					if (!clickedInside)
					{
						Unfocus();
						return;
					}
				}
				
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					onCancel?.Invoke();
					Unfocus();
					return;
				}

				// Мигание каретки (раз в 0.5 секунды)
				caretBlinkTimer += Time.unscaledDeltaTime;
				if (caretBlinkTimer >= 0.5f)
				{
					caretBlinkTimer = 0f;
					caretVisible = !caretVisible;
					MarkDirty(); // Перерисовываем каретку
				}

				HandleKeyboardInput();
			}
		}

		private void HandleKeyboardInput()
		{
			bool textChanged = false;

			// 1. Ввод символов (поддержка русского, английского и спецсимволов)
			string input = Input.inputString;
			for (int i = 0; i < input.Length; i++)
			{
				char c = input[i];

				// Backspace
				if (c == '\b')
				{
					if (caretPosition > 0 && text.Length > 0)
					{
						text = text.Remove(caretPosition - 1, 1);
						caretPosition--;
						textChanged = true;
					}
				}
				// Enter / Return
				else if (c == '\n' || c == '\r')
				{
					Unfocus();
					return;
				}
				// Обычный печатаемый символ
				else if (c >= 32)
				{
					text = text.Insert(caretPosition, c.ToString());
					caretPosition++;
					textChanged = true;
				}
			}

			// 2. Управляющие клавиши (стрелочки, Delete)
			if (Input.GetKeyDown(KeyCode.Delete))
			{
				if (caretPosition < text.Length)
				{
					text = text.Remove(caretPosition, 1);
					textChanged = true;
				}
			}
			else if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				if (caretPosition > 0)
				{
					caretPosition--;
					ResetCaretBlink();
					MarkDirty();
				}
			}
			else if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				if (caretPosition < text.Length)
				{
					caretPosition++;
					ResetCaretBlink();
					MarkDirty();
				}
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				caretPosition = 0;
				ResetCaretBlink();
				MarkDirty();
			}
			else if (Input.GetKeyDown(KeyCode.End))
			{
				caretPosition = text.Length;
				ResetCaretBlink();
				MarkDirty();
			}

			if (textChanged)
			{
				ResetCaretBlink();
				UpdateLabelVisual();
				MarkDirty();
				onValueChanged?.Invoke(text);
			}
		}

		private void ResetCaretBlink()
		{
			caretBlinkTimer = 0f;
			caretVisible = true;
		}

		private void UpdateLabelVisual()
		{
			if (targetLabel == null && Element != null)
			{
				targetLabel = Element.TryGetComponent<Label>();
			}

			if (targetLabel == null) return;

			if (string.IsNullOrEmpty(text) && !isFocused)
			{
				targetLabel.text = placeholder;
				targetLabel.color = placeholderColor;
			}
			else
			{
				targetLabel.text = text;
				targetLabel.color = textColor;
			}
		}

		public void SetTextWithoutNotify(string newText)
		{
			text = newText ?? string.Empty;
			caretPosition = Mathf.Clamp(caretPosition, 0, text.Length);
			UpdateLabelVisual();
			MarkDirty();
		}

		public override void GenerateMesh(CanvasGenerationContext ctx)
		{
			base.GenerateMesh(ctx);

			// Каретка рисуется только при активном фокусе и в фазе мигания
			if (!isFocused || !caretVisible || targetLabel == null || targetLabel.subpixelFont == null)
				return;

			// 1. Вычисляем точную позицию каретки по оси X
			float caretX = 0f;
			int safeCaretPos = Mathf.Clamp(caretPosition, 0, text.Length);
			string textToCaret = text.Substring(0, safeCaretPos);

			for (int i = 0; i < textToCaret.Length; i++)
			{
				var glyph = targetLabel.subpixelFont.GetGlyph(textToCaret[i]);
				if (glyph != null)
				{
					caretX += glyph.advance;
				}
			}

			// 2. Размеры и вертикальное позиционирование каретки
			float boxHeight = Transform.size.y;
			float caretHeight = targetLabel.fontSize;
	
			// Центрируем каретку по вертикали внутри контейнера
			float caretY = Mathf.Round((boxHeight - caretHeight) * 0.5f);
			float caretWidth = 1.5f; // Четкая ширина в 1.5 - 2 пикселя

			// 3. ВАЖНО: Используем стандартный материал для закраски цветом (НЕ шейдер шрифта!)
			Material solidMat = service.defaultCombinedMaterial;

			// Каретка рисуется на слое контента/текста, поверх фона
			ctx.SetLayer(CanvasGenerationContext.Layer.Text);
			ctx.SetMaterial(solidMat);

			Matrix4x4 localToRoot = Element.LocalToRoot;

			// 4. Отрисовываем полоску каретки
			// Белый квад с цветом caretColor
			ctx.AppendQuad(
				new float2(caretWidth, caretHeight),
				localToRoot * Matrix4x4.Translate(new Vector3(caretX, caretY, 0f)),
				caretColor,
				float4.zero
			);
		}
	}
}