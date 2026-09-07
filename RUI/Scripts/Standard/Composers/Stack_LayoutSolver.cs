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
        public bool fitContent = true;

        public void Solve(SizeConstraints constraints)
        {
	        int forwardIndex = (int)axis;
	        int crossIndex = 1 - forwardIndex;

	        float2 containerSize = Transform.size;
	        // if (fitContent)
	        // {
		       //  float? crossConstraint = constraints.GetMax(crossIndex);
		       //  if (crossConstraint.HasValue) containerSize[crossIndex] = crossConstraint.Value;
	        // }
	        
	        float2 paddingSum = new(padding.x + padding.z, padding.y + padding.w);
	        
	        float paddingForwardStart = padding[forwardIndex * 2 + 0];
	        float paddingForwardEnd = padding[forwardIndex * 2 + 1];
	        float paddingCrossStart = padding[crossIndex * 2 + 0];
	        float paddingCrossEnd = padding[crossIndex * 2 + 1];

	        SizeConstraints childConstraints = constraints;
	        // childConstraints.ClampMax(forwardIndex, null);
	        // childConstraints.SetMax(forwardIndex, null);
	        // if (fitContent) childConstraints.SetMax(crossIndex, constraints.GetMax(crossIndex));

	        // Debug.Log($"Stack container/child constraints: {constraints} / {childConstraints}");

	        //
	        // Children Measure
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
	        // Children Placement
	        //
	        bool shouldReverse = reverse;
	        if (axis == StackAxis.Vertical) shouldReverse = !shouldReverse; // Sugaring: Invert reverse flag for Vertical (more popular case)
	        
	        float cursor = 0;
	        
	        cursor += paddingForwardStart;

	        IEnumerable<CanvasElement> children = shouldReverse ? Element.Children.Reverse() : Element.Children;
	        
	        foreach (CanvasElement child in children)
	        {
		        float childForwardSize = child.transform.size[forwardIndex];
		        child.transform.pos[forwardIndex] = cursor;
		        
		        
		        child.transform.pos[crossIndex] = paddingCrossStart;

		        cursor += childForwardSize + spacing;
		        
		        child.transform.size[crossIndex] = containerSize[crossIndex] - paddingSum[crossIndex];
	        }

	        Transform.size[forwardIndex] = paddingSum[forwardIndex] + totalForwardSize;

	        if (constraints.GetMax(crossIndex).HasValue)
	        {
		        // Debug.Log($"Stack max[{crossIndex}] has value = {constraints.GetMax(crossIndex)!.Value}");
		        Transform.size[crossIndex] = constraints.GetMax(crossIndex)!.Value;
	        }
	        // Transform.size[crossIndex] = containerSize[crossIndex];

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