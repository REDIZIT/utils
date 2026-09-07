using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class Stack_Composer : IComposer
    {
        public CanvasElement e;
        public LayoutDirection direction = LayoutDirection.Vertical;

        public PreferredSize Measure(SizeConstraints c)
        {
	        int forwardIndex = (int)direction;
            int crossIndex = 1 - forwardIndex;

            SizeConstraints childConstraints = new SizeConstraints
            {
	            maxX = forwardIndex == 0 ? null : c.maxX,
	            maxY = forwardIndex == 1 ? null : c.maxY
            };

            float forwardAccum = 0;
            float crossMax = 0;
            
            foreach (CanvasElement child in e.Children)
            {
	            PreferredSize childPreferredSize = child.Measure(childConstraints);
	            Debug.Log($"stack child pref size: {childPreferredSize.size}");
	            
	            forwardAccum += childPreferredSize.size[forwardIndex];
	            crossMax = math.max(crossMax, childPreferredSize.size[crossIndex]);
            }

            float2 desired = float2.zero;
            desired[forwardIndex] = forwardAccum;
            desired[crossIndex] = c.GetMax(crossIndex) ?? crossMax;
            return new(desired);
        }
        
        public void Arrange(float2 size)
        {
	        Debug.Log($"Stack arrange: {size} for children ({e.Children.Count})");
	        
	        int forwardIndex = (int)direction;;
	        int crossIndex = 1 - forwardIndex;
	        float cursor = 0;

	        foreach (CanvasElement child in e.Children)
	        {
		        float childDesiredForward = child.preferredSize!.Value.size[forwardIndex];

		        // Формируем трансформ на основе ПОСЧИТАННОГО в Measure размера:
		        float2 childPos = float2.zero;
		        childPos[forwardIndex] = cursor;
		        childPos[crossIndex] = 0;

		        float2 childSize = float2.zero;
		        childSize[forwardIndex] = childDesiredForward;
		        childSize[crossIndex] = size[crossIndex]; // растягиваем по поперечной оси

		        // Выставляем ребенку и заставляем его выставить своих детей
		        child.Arrange(new(childPos, childSize));

		        cursor += childDesiredForward;
	        }
        }
    }
}