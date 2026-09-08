using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class Stack_LayoutSolver : CanvasComponent, ILayoutSolver
    {
        public StackAxis axis = StackAxis.Vertical;
        public StackSnap snap = StackSnap.None;
        public bool reverse = false;
        
        public float4 padding;
        public float spacing;
        public bool fitCross = false;

        public void Solve(SizeConstraints constraints)
        {
	        int forwardIndex = (int)axis;
	        int crossIndex = 1 - forwardIndex;
	        
	        float2 paddingSum = new(padding.x + padding.z, padding.y + padding.w);
	        
	        bool shouldReverse = reverse;
	        if (axis == StackAxis.Vertical) shouldReverse = !shouldReverse; // Sugaring: Invert reverse flag for Vertical (more popular case)

	        
	        float paddingForwardStart = forwardIndex == 0 
		        ? (shouldReverse ? padding.z : padding.x) 
		        : (shouldReverse ? padding.w : padding.y);
	        float paddingCrossStart = crossIndex == 0 ? padding.x : padding.y;

	        
	        SizeConstraints childConstraints = constraints;

	        //
	        // Children Measure
	        //
	        float childrenTotalForwardSize = 0;
	        float maxCrossSize = 0; 
	        foreach (CanvasElement child in Element.Children)
	        {
		        child.Solve(childConstraints);
		        childrenTotalForwardSize += child.transform.size[forwardIndex];
		        maxCrossSize = Mathf.Max(maxCrossSize, child.transform.size[crossIndex]);
	        }

	        float totalSpacing = spacing * Mathf.Max(0, Element.Children.Count - 1);
	        float totalForwardSize = childrenTotalForwardSize + totalSpacing;
	        
	        
	        //
	        // Children Placement
	        //
	        
	        float cursor = 0;
	        float childCrossSize = maxCrossSize == 0 ? childConstraints.GetMax(crossIndex) ?? 0 : maxCrossSize;
	        
	        cursor += paddingForwardStart;

	        IEnumerable<CanvasElement> children = shouldReverse ? Element.Children.Reverse() : Element.Children;
	        
	        foreach (CanvasElement child in children)
	        {
		        float childForwardSize = child.transform.size[forwardIndex];
		        child.transform.pos[forwardIndex] = cursor;
		        child.transform.pos[crossIndex] = paddingCrossStart;

		        cursor += childForwardSize + spacing;

		        child.transform.size[crossIndex] = childCrossSize;
		        // if (childConstraints.GetMax(crossIndex).HasValue) child.transform.size[crossIndex] = childConstraints.GetMax(crossIndex)!.Value - paddingSum[crossIndex];
	        }

	        Transform.size[forwardIndex] = paddingSum[forwardIndex] + totalForwardSize;
	        Transform.size[crossIndex] = maxCrossSize;
	        // Debug.Log($"Stack {Element.GetPath()} solved size: {Transform.size} from constraints: {constraints} and maxCrossSize: {maxCrossSize}");

	        //
	        // Self Snapping
	        //
	        if (snap != StackSnap.None)
	        {
		        if (snap == StackSnap.End)
		        {
			        // Debug.Log(constraints.GetMax(forwardIndex)!.Value);
			        Transform.pos[forwardIndex] = constraints.GetMax(forwardIndex)!.Value - Transform.size[forwardIndex];
		        }
	        }
        }
    }
}