using System.Collections.Generic;

namespace REDIZIT.RUI
{
    public abstract class Node
    {
        public abstract IEnumerable<Node> EnumerateChildren();

        public IEnumerable<Node> EnumerateEachChild()
        {
            foreach (Node child in EnumerateChildren())
            {
                yield return child;
                foreach (Node subchild in child.EnumerateEachChild())
                {
                    yield return subchild;
                }
            }
        }
    }

    public class Node_Root : Node
    {
        public List<Node_Element> elements = new();

        public override IEnumerable<Node> EnumerateChildren()
        {
            return elements;
        }
    }

    public class Node_Element : Node
    {
	    public string key;
	    public bool isTemplate;
    
	    public List<Node_Property> properties = new();
	    public List<Node_Component> components = new();
	    public List<Node_Element> children = new();

	    public override IEnumerable<Node> EnumerateChildren()
	    {
		    foreach (var prop in properties) yield return prop;
		    foreach (var comp in components) yield return comp;
		    foreach (var child in children) yield return child;
	    }
    }

    public class Node_Component : Node
    {
        public string typeName;
        public string id;
        public List<Node_Property> properties = new();

        public override IEnumerable<Node> EnumerateChildren()
        {
            return properties;
        }
    }

    public class Node_Property : Node
    {
        public string name;
        public Node_Expression value;

        public override IEnumerable<Node> EnumerateChildren()
        {
            if (value != null) yield return value;
        }
    }

    public abstract class Node_Expression : Node { }

    public class Node_NumberLiteral : Node_Expression
    {
        public float value;
        public override IEnumerable<Node> EnumerateChildren() { yield break; }
    }

    public class Node_StringLiteral : Node_Expression
    {
        public string value;
        public override IEnumerable<Node> EnumerateChildren() { yield break; }
    }

    public class Node_ColorLiteral : Node_Expression
    {
        public string hex;
        public override IEnumerable<Node> EnumerateChildren() { yield break; }
    }

    public class Node_BooleanLiteral : Node_Expression
    {
        public bool value;
        public override IEnumerable<Node> EnumerateChildren() { yield break; }
    }

    public class Node_TupleLiteral : Node_Expression
    {
	    public List<Node_Expression> elements = new List<Node_Expression>();

	    public override IEnumerable<Node> EnumerateChildren()
	    {
		    return elements;
	    }
    }

    public class Node_IdentifierReference : Node_Expression
    {
        public string name;
        public override IEnumerable<Node> EnumerateChildren() { yield break; }
    }
}