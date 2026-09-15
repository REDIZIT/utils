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

				// Точный расчет позиции каретки по координате клика с учетом выравнивания
				if (targetLabel != null && targetLabel.subpixelFont != null && !string.IsNullOrEmpty(text))
				{
					float localClickX = Element.LocalToRoot.inverse.MultiplyPoint3x4(Input.mousePosition).x;
					float startX = GetTextStartX(text);
					float clickOffset = localClickX - startX;

					if (clickOffset <= 0f)
					{
						caretPosition = 0;
					}
					else
					{
						float accumulated = 0f;
						int pos = text.Length;

						for (int i = 0; i < text.Length; i++)
						{
							var glyph = targetLabel.subpixelFont.GetGlyph(text[i]);
							float adv = glyph != null ? glyph.advance : 0f;
							if (accumulated + adv * 0.5f >= clickOffset)
							{
								pos = i;
								break;
							}
							accumulated += adv;
						}

						caretPosition = pos;
					}
				}
				else
				{
					caretPosition = text.Length;
				}

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

				caretBlinkTimer += Time.unscaledDeltaTime;
				if (caretBlinkTimer >= 0.5f)
				{
					caretBlinkTimer = 0f;
					caretVisible = !caretVisible;
					MarkDirty();
				}

				HandleKeyboardInput();
			}
		}

		private void HandleKeyboardInput()
		{
			bool textChanged = false;

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
				// Печатаемый символ
				else if (c >= 32)
				{
					text = text.Insert(caretPosition, c.ToString());
					caretPosition++;
					textChanged = true;
				}
			}

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

		// Вычисляет начальную точку отрисовки текста по оси X в зависимости от выравнивания
		private float GetTextStartX(string str)
		{
			if (targetLabel == null || targetLabel.subpixelFont == null) return 0f;

			float containerWidth = Transform.size.x;
			float textWidth = 0f;
			if (!string.IsNullOrEmpty(str))
			{
				textWidth = TextEngine.MeasureSubpixel(str, targetLabel.subpixelFont).x;
			}

			TextAlignmentOptions alignment = targetLabel.alignment;
			if (alignment == TextAlignmentOptions.Center || alignment == TextAlignmentOptions.Midline)
			{
				return Mathf.Round((containerWidth - textWidth) * 0.5f);
			}
			if (alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.MidlineRight)
			{
				return Mathf.Round(containerWidth - textWidth);
			}

			return 0f;
		}

		public override void GenerateMesh(CanvasGenerationContext ctx, Matrix4x4 localToRoot)
		{
			base.GenerateMesh(ctx, localToRoot);

			if (!isFocused || !caretVisible || targetLabel == null || targetLabel.subpixelFont == null)
				return;

			// 1. Вычисляем смещение текста по X согласно выравниванию Label
			float startX = GetTextStartX(text);

			// 2. Добавляем длину текста до каретки
			float textToCaretWidth = 0f;
			int safeCaretPos = Mathf.Clamp(caretPosition, 0, text.Length);
			string textToCaret = text.Substring(0, safeCaretPos);

			for (int i = 0; i < textToCaret.Length; i++)
			{
				var glyph = targetLabel.subpixelFont.GetGlyph(textToCaret[i]);
				if (glyph != null)
				{
					textToCaretWidth += glyph.advance;
				}
			}

			float caretX = startX + textToCaretWidth;

			// 3. Вертикальное центрирование каретки
			float boxHeight = Transform.size.y;
			float caretHeight = targetLabel.fontSize;
			float caretY = Mathf.Round((boxHeight - caretHeight) * 0.5f);
			float caretWidth = 1.5f;

			Material solidMat = service.defaultCombinedMaterial;
			ctx.SetLayer(CanvasGenerationContext.Layer.Text);
			ctx.SetMaterial(solidMat);

			// 4. Отрисовка каретки
			ctx.AppendQuad(
				new float2(caretWidth, caretHeight),
				localToRoot * Matrix4x4.Translate(new Vector3(caretX, caretY, 0f)),
				caretColor,
				float4.zero
			);
		}
	}
}