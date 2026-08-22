using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    internal sealed class TerritoryPersistentAvlMap<TKey, TValue>
    {
        private readonly IComparer<TKey> _comparer;
        private readonly Node _root;

        public TerritoryPersistentAvlMap(IComparer<TKey> comparer = null)
            : this(null, comparer ?? Comparer<TKey>.Default)
        {
        }

        private TerritoryPersistentAvlMap(Node root, IComparer<TKey> comparer)
        {
            _root = root;
            _comparer = comparer;
        }

        public int Count => CountOf(_root);
        public object RootIdentity => _root;

        public bool TryGetValue(TKey key, out TValue value)
        {
            Node current = _root;
            while (current != null)
            {
                int comparison = _comparer.Compare(key, current.Key);
                if (comparison == 0)
                {
                    value = current.Value;
                    return true;
                }

                current = comparison < 0 ? current.Left : current.Right;
            }

            value = default;
            return false;
        }

        public TerritoryPersistentAvlMap<TKey, TValue> Set(
            TKey key,
            TValue value,
            out bool added,
            out int nodesCreated)
        {
            nodesCreated = 0;
            Node root = Set(_root, key, value, ref nodesCreated, out added);
            return ReferenceEquals(root, _root)
                ? this
                : new TerritoryPersistentAvlMap<TKey, TValue>(root, _comparer);
        }

        public TerritoryPersistentAvlMap<TKey, TValue> Remove(
            TKey key,
            out bool removed,
            out int nodesCreated)
        {
            nodesCreated = 0;
            Node root = Remove(_root, key, ref nodesCreated, out removed);
            return removed
                ? new TerritoryPersistentAvlMap<TKey, TValue>(root, _comparer)
                : this;
        }

        public void CopyTo(List<KeyValuePair<TKey, TValue>> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            CopyTo(_root, destination);
        }

        private Node Set(
            Node node,
            TKey key,
            TValue value,
            ref int nodesCreated,
            out bool added)
        {
            if (node == null)
            {
                added = true;
                nodesCreated++;
                return new Node(key, value, null, null);
            }

            int comparison = _comparer.Compare(key, node.Key);
            if (comparison == 0)
            {
                added = false;
                if (EqualityComparer<TValue>.Default.Equals(value, node.Value))
                    return node;

                nodesCreated++;
                return new Node(node.Key, value, node.Left, node.Right);
            }

            Node left = node.Left;
            Node right = node.Right;
            if (comparison < 0)
                left = Set(left, key, value, ref nodesCreated, out added);
            else
                right = Set(right, key, value, ref nodesCreated, out added);

            nodesCreated++;
            return Balance(new Node(node.Key, node.Value, left, right), ref nodesCreated);
        }

        private Node Remove(
            Node node,
            TKey key,
            ref int nodesCreated,
            out bool removed)
        {
            if (node == null)
            {
                removed = false;
                return null;
            }

            int comparison = _comparer.Compare(key, node.Key);
            if (comparison < 0)
            {
                Node left = Remove(node.Left, key, ref nodesCreated, out removed);
                if (!removed)
                    return node;
                nodesCreated++;
                return Balance(new Node(node.Key, node.Value, left, node.Right), ref nodesCreated);
            }
            if (comparison > 0)
            {
                Node right = Remove(node.Right, key, ref nodesCreated, out removed);
                if (!removed)
                    return node;
                nodesCreated++;
                return Balance(new Node(node.Key, node.Value, node.Left, right), ref nodesCreated);
            }

            removed = true;
            if (node.Left == null)
                return node.Right;
            if (node.Right == null)
                return node.Left;

            Node successor = Minimum(node.Right);
            Node newRight = RemoveMinimum(node.Right, ref nodesCreated);
            nodesCreated++;
            return Balance(new Node(successor.Key, successor.Value, node.Left, newRight), ref nodesCreated);
        }

        private static Node Minimum(Node node)
        {
            while (node.Left != null)
                node = node.Left;
            return node;
        }

        private static Node RemoveMinimum(Node node, ref int nodesCreated)
        {
            if (node.Left == null)
                return node.Right;

            Node left = RemoveMinimum(node.Left, ref nodesCreated);
            nodesCreated++;
            return Balance(new Node(node.Key, node.Value, left, node.Right), ref nodesCreated);
        }

        private static Node Balance(Node node, ref int nodesCreated)
        {
            int balance = HeightOf(node.Left) - HeightOf(node.Right);
            if (balance > 1)
            {
                if (HeightOf(node.Left.Left) < HeightOf(node.Left.Right))
                {
                    Node rotated = RotateLeft(node.Left, ref nodesCreated);
                    nodesCreated++;
                    node = new Node(node.Key, node.Value, rotated, node.Right);
                }
                return RotateRight(node, ref nodesCreated);
            }
            if (balance < -1)
            {
                if (HeightOf(node.Right.Right) < HeightOf(node.Right.Left))
                {
                    Node rotated = RotateRight(node.Right, ref nodesCreated);
                    nodesCreated++;
                    node = new Node(node.Key, node.Value, node.Left, rotated);
                }
                return RotateLeft(node, ref nodesCreated);
            }

            return node;
        }

        private static Node RotateLeft(Node node, ref int nodesCreated)
        {
            Node pivot = node.Right;
            nodesCreated += 2;
            Node left = new Node(node.Key, node.Value, node.Left, pivot.Left);
            return new Node(pivot.Key, pivot.Value, left, pivot.Right);
        }

        private static Node RotateRight(Node node, ref int nodesCreated)
        {
            Node pivot = node.Left;
            nodesCreated += 2;
            Node right = new Node(node.Key, node.Value, pivot.Right, node.Right);
            return new Node(pivot.Key, pivot.Value, pivot.Left, right);
        }

        private static void CopyTo(Node node, List<KeyValuePair<TKey, TValue>> destination)
        {
            if (node == null)
                return;
            CopyTo(node.Left, destination);
            destination.Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            CopyTo(node.Right, destination);
        }

        private static int HeightOf(Node node) => node?.Height ?? 0;
        private static int CountOf(Node node) => node?.Count ?? 0;

        private sealed class Node
        {
            public Node(TKey key, TValue value, Node left, Node right)
            {
                Key = key;
                Value = value;
                Left = left;
                Right = right;
                Height = Math.Max(HeightOf(left), HeightOf(right)) + 1;
                Count = CountOf(left) + CountOf(right) + 1;
            }

            public TKey Key { get; }
            public TValue Value { get; }
            public Node Left { get; }
            public Node Right { get; }
            public int Height { get; }
            public int Count { get; }
        }
    }
}
