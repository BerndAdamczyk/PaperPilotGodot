using System.Collections.Generic;
using Godot;

namespace PaperPilot
{
    public static class NodeExtensions
    {
        /// <summary>
        /// Recursively gets all child nodes of the given type T (including this node if matching).
        /// </summary>
        public static List<T> GetComponentsInChildren<T>(this Node node) where T : Node
        {
            var result = new List<T>();
            if (node is T tNode)
                result.Add(tNode);

            foreach (Node child in node.GetChildren())
                result.AddRange(child.GetComponentsInChildren<T>());

            return result;
        }
        public static List<T> GetComponentsInChildren<T>(this Node node, string name) where T : Node
        {
            var result = new List<T>();

            // Check self
            if (node is T tNode && node.Name == name)
                result.Add(tNode);

            // Check children recursively
            foreach (Node child in node.GetChildren())
                result.AddRange(child.GetComponentsInChildren<T>(name));

            return result;
        }
    }

}
