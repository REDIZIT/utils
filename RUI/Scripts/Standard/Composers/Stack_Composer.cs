using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class Stack_LayoutSolver : CanvasComponent, ILayoutSolver
    {
        public StackAxis axis = StackAxis.Vertical;
        public StackDirection direction = StackDirection.Negative;
        
        public float4 padding;
        public float spacing;
        public bool fitContent = true;

        public void Solve(SizeConstraints constraints)
        {
	        int forwardIndex = (int)axis;
	        int crossIndex = 1 - forwardIndex;

	        float2 containerSize = Transform.size;
	        if (fitContent)
	        {
		        float? crossConstraint = constraints.GetMax(crossIndex);
		        if (crossConstraint.HasValue) containerSize[crossIndex] = crossConstraint.Value;
	        }
	        
	        float2 paddingSum = new(padding.x + padding.z, padding.y + padding.w);
	        
	        float paddingForwardStart = padding[forwardIndex * 2 + 0];
	        float paddingForwardEnd = padding[forwardIndex * 2 + 1];
	        float paddingCrossStart = padding[crossIndex * 2 + 0];
	        float paddingCrossEnd = padding[crossIndex * 2 + 1];

	        SizeConstraints childConstraints = new SizeConstraints();
	        childConstraints.SetMax(crossIndex, containerSize[crossIndex]);


	        //
	        // Measure
	        //
	        float childrenTotalForwardSize = 0;
	        foreach (CanvasElement child in Element.Children)
	        {
		        child.Solve(childConstraints);
		        childrenTotalForwardSize += child.transform.size[forwardIndex];
	        }

	        float totalSpacing = spacing * Mathf.Max(0, Element.Children.Count - 1);
	        float totalForwardSize = childrenTotalForwardSize + totalSpacing;
	        
	        //
	        // Placement
	        //
	        float cursor = direction == StackDirection.Positive ? 0 : totalForwardSize;
	        float cursorSign = direction == StackDirection.Negative ? -1 : 1;
	        
	        cursor += paddingForwardStart;
	        
	        foreach (CanvasElement child in Element.Children)
	        {
		        float childForwardSize = child.transform.size[forwardIndex];
		        if (direction == StackDirection.Negative)
		        {
			        child.transform.pos[forwardIndex] = cursor - childForwardSize;
		        }
		        else
		        {
			        child.transform.pos[forwardIndex] = cursor;
		        }
		        
		        
		        child.transform.pos[crossIndex] = paddingCrossStart;

		        cursor += cursorSign * (childForwardSize + spacing);
		        
		        child.transform.size[crossIndex] = containerSize[crossIndex] - paddingSum[crossIndex];
	        }

	        Transform.size[forwardIndex] = paddingSum[forwardIndex] + totalForwardSize;
	        Transform.size[crossIndex] = containerSize[crossIndex];
        }
    }
}