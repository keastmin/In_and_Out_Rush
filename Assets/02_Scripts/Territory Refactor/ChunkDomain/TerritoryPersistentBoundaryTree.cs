using System;
using System.Collections.Generic;
using System.Numerics;

namespace ProjectIO.Territory
{
    public sealed class TerritoryPersistentBoundaryTree
    {
        private readonly Node _root;
        private readonly TerritoryPersistentAvlMap<TerritoryBoundarySegmentId, OrderKey> _keysById;

        private TerritoryPersistentBoundaryTree(
            Node root,
            TerritoryPersistentAvlMap<TerritoryBoundarySegmentId, OrderKey> keysById)
        {
            _root = root;
            _keysById = keysById;
        }

        public int Count => CountOf(_root);
        public decimal SignedTwiceArea => AreaOf(_root);
        public decimal AbsoluteTwiceArea => Math.Abs(SignedTwiceArea);
        public object RootIdentity => _root;

        public static bool TryCreate(
            IReadOnlyList<TerritoryCompactBoundarySegment> orderedSegments,
            out TerritoryPersistentBoundaryTree tree,
            out string reason)
        {
            tree = null;
            reason = string.Empty;
            if (orderedSegments == null)
            {
                reason = "A persistent Boundary tree requires ordered segments.";
                return false;
            }
            if (orderedSegments.Count < 3)
            {
                reason = "A persistent Boundary tree requires at least three segments.";
                return false;
            }

            var ids = new HashSet<TerritoryBoundarySegmentId>();
            var keysById = new TerritoryPersistentAvlMap<TerritoryBoundarySegmentId, OrderKey>();
            Node root = null;
            try
            {
                for (int i = 0; i < orderedSegments.Count; i++)
                {
                    TerritoryCompactBoundarySegment segment = orderedSegments[i];
                    TerritoryCompactBoundarySegment next = orderedSegments[(i + 1) % orderedSegments.Count];
                    if (!ids.Add(segment.Id))
                    {
                        reason = $"Boundary identity {segment.Id} is duplicated.";
                        return false;
                    }
                    if (segment.GlobalEnd != next.GlobalStart)
                    {
                        reason = $"Boundary identity {segment.Id} does not connect to {next.Id}.";
                        return false;
                    }

                    var key = OrderKey.FromInteger(i);
                    int created = 0;
                    root = Insert(root, key, segment, ref created, out bool inserted);
                    if (!inserted)
                    {
                        reason = "Initial Boundary ordering contains a duplicate key.";
                        return false;
                    }
                    keysById = keysById.Set(key: segment.Id, value: key, out _, out _);
                }
            }
            catch (OverflowException)
            {
                reason = "Boundary endpoints or area exceed the fixed coordinate range.";
                return false;
            }

            if (AreaOf(root) <= 0m)
            {
                reason = "Compact Boundary must use canonical counter-clockwise ordering.";
                return false;
            }

            tree = new TerritoryPersistentBoundaryTree(root, keysById);
            return true;
        }

        public bool TryGetSegment(
            TerritoryBoundarySegmentId id,
            out TerritoryCompactBoundarySegment segment)
        {
            if (!_keysById.TryGetValue(id, out OrderKey key))
            {
                segment = default;
                return false;
            }

            return TryGet(_root, key, out segment);
        }

        public bool TryGetOrderedSegment(
            int ordinal,
            out TerritoryCompactBoundarySegment segment)
        {
            if (ordinal < 0 || ordinal >= Count)
            {
                segment = default;
                return false;
            }

            Node current = _root;
            int remaining = ordinal;
            while (current != null)
            {
                int leftCount = CountOf(current.Left);
                if (remaining < leftCount)
                    current = current.Left;
                else if (remaining == leftCount)
                {
                    segment = current.Segment;
                    return true;
                }
                else
                {
                    remaining -= leftCount + 1;
                    current = current.Right;
                }
            }

            segment = default;
            return false;
        }

        public bool TryGetNext(
            TerritoryBoundarySegmentId id,
            out TerritoryCompactBoundarySegment segment)
            => TryGetNeighbor(id, true, out segment);

        public bool TryGetPrevious(
            TerritoryBoundarySegmentId id,
            out TerritoryCompactBoundarySegment segment)
            => TryGetNeighbor(id, false, out segment);

        public decimal GetForwardArcTwiceArea(
            TerritoryBoundarySegmentId fromId,
            FixedTerritoryPoint fromPoint,
            TerritoryBoundarySegmentId toId,
            FixedTerritoryPoint toPoint)
        {
            if (!_keysById.TryGetValue(fromId, out OrderKey fromKey) ||
                !_keysById.TryGetValue(toId, out OrderKey toKey) ||
                !TryGet(_root, fromKey, out TerritoryCompactBoundarySegment from) ||
                !TryGet(_root, toKey, out TerritoryCompactBoundarySegment to))
            {
                throw new ArgumentException("Boundary contact identity is unavailable.");
            }
            if (!TerritoryBoundaryLoopIndex.PointOnSegment(fromPoint, from.GlobalStart, from.GlobalEnd) ||
                !TerritoryBoundaryLoopIndex.PointOnSegment(toPoint, to.GlobalStart, to.GlobalEnd))
            {
                throw new ArgumentException("Boundary contact does not lie on its stable segment.");
            }
            if (fromId == toId && fromPoint == toPoint)
                return 0m;

            if (fromId == toId && IsBeforeOnSegment(from, fromPoint, toPoint))
                return TerritoryBoundaryLoopIndex.Cross(fromPoint, toPoint);

            decimal area = TerritoryBoundaryLoopIndex.Cross(fromPoint, from.GlobalEnd);
            if (fromKey.CompareTo(toKey) < 0)
            {
                area += RangeAreaExclusive(fromKey, toKey);
            }
            else
            {
                area += AreaGreaterThan(fromKey);
                area += AreaLessThan(toKey);
            }
            area += TerritoryBoundaryLoopIndex.Cross(to.GlobalStart, toPoint);
            return area;
        }

        public bool TryValidateForwardArc(
            IReadOnlyList<TerritoryBoundarySegmentId> removedIds,
            out TerritoryBoundarySegmentId predecessorId,
            out TerritoryBoundarySegmentId successorId,
            out bool wraps,
            out string reason)
        {
            predecessorId = default;
            successorId = default;
            wraps = false;
            reason = string.Empty;
            if (removedIds == null || removedIds.Count == 0 || removedIds.Count >= Count)
            {
                reason = "A Boundary splice must remove a non-empty proper arc.";
                return false;
            }

            var unique = new HashSet<TerritoryBoundarySegmentId>();
            for (int i = 0; i < removedIds.Count; i++)
            {
                TerritoryBoundarySegmentId current = removedIds[i];
                if (!unique.Add(current) || !TryGetSegment(current, out _))
                {
                    reason = $"Boundary splice identity {current} is stale or duplicated.";
                    return false;
                }
                if (i > 0)
                {
                    if (!TryGetNext(removedIds[i - 1], out TerritoryCompactBoundarySegment next) ||
                        next.Id != current)
                    {
                        reason = "Boundary splice identities do not form one contiguous forward arc.";
                        return false;
                    }
                }
            }

            if (!TryGetPrevious(removedIds[0], out TerritoryCompactBoundarySegment predecessor) ||
                !TryGetNext(removedIds[removedIds.Count - 1], out TerritoryCompactBoundarySegment successor))
            {
                reason = "Boundary splice adjacency is unavailable.";
                return false;
            }

            predecessorId = predecessor.Id;
            successorId = successor.Id;
            _keysById.TryGetValue(removedIds[0], out OrderKey firstKey);
            _keysById.TryGetValue(removedIds[removedIds.Count - 1], out OrderKey lastKey);
            wraps = firstKey.CompareTo(lastKey) > 0;
            return true;
        }

        internal TerritoryPersistentBoundaryTree Remove(
            TerritoryBoundarySegmentId id,
            out bool removed,
            out int nodesCreated)
        {
            nodesCreated = 0;
            if (!_keysById.TryGetValue(id, out OrderKey key))
            {
                removed = false;
                return this;
            }

            Node root = Remove(_root, key, ref nodesCreated, out removed);
            TerritoryPersistentAvlMap<TerritoryBoundarySegmentId, OrderKey> keys =
                _keysById.Remove(id, out _, out int idNodes);
            nodesCreated += idNodes;
            return new TerritoryPersistentBoundaryTree(root, keys);
        }

        internal bool TryInsertBetween(
            TerritoryBoundarySegmentId predecessorId,
            TerritoryBoundarySegmentId successorId,
            bool wraps,
            TerritoryCompactBoundarySegment segment,
            out TerritoryPersistentBoundaryTree tree,
            out int nodesCreated,
            out string reason)
        {
            tree = this;
            nodesCreated = 0;
            reason = string.Empty;
            if (_keysById.TryGetValue(segment.Id, out _))
            {
                reason = $"Boundary identity {segment.Id} already exists.";
                return false;
            }
            if (!_keysById.TryGetValue(predecessorId, out OrderKey left) ||
                !_keysById.TryGetValue(successorId, out OrderKey right))
            {
                reason = "Boundary splice neighbors are unavailable.";
                return false;
            }

            OrderKey key = wraps ? left.AddOne() : OrderKey.Between(left, right);
            Node root = Insert(_root, key, segment, ref nodesCreated, out bool inserted);
            if (!inserted)
            {
                reason = "Boundary order key collision prevented splice insertion.";
                return false;
            }

            TerritoryPersistentAvlMap<TerritoryBoundarySegmentId, OrderKey> keys =
                _keysById.Set(segment.Id, key, out _, out int idNodes);
            nodesCreated += idNodes;
            tree = new TerritoryPersistentBoundaryTree(root, keys);
            return true;
        }

        internal void CopyOrderedTo(List<TerritoryCompactBoundarySegment> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            CopyOrderedTo(_root, destination);
        }

        private bool TryGetNeighbor(
            TerritoryBoundarySegmentId id,
            bool next,
            out TerritoryCompactBoundarySegment segment)
        {
            if (!_keysById.TryGetValue(id, out OrderKey key))
            {
                segment = default;
                return false;
            }

            Node neighbor = next ? Successor(_root, key) : Predecessor(_root, key);
            if (neighbor == null)
                neighbor = next ? Minimum(_root) : Maximum(_root);
            if (neighbor == null)
            {
                segment = default;
                return false;
            }

            segment = neighbor.Segment;
            return true;
        }

        private decimal RangeAreaExclusive(OrderKey minimum, OrderKey maximum)
            => AreaLessThan(maximum) - AreaLessThanOrEqual(minimum);

        private decimal AreaLessThan(OrderKey key)
        {
            decimal area = 0m;
            Node current = _root;
            while (current != null)
            {
                if (key.CompareTo(current.Key) <= 0)
                {
                    current = current.Left;
                }
                else
                {
                    area += AreaOf(current.Left) + current.Segment.TwiceArea;
                    current = current.Right;
                }
            }
            return area;
        }

        private decimal AreaLessThanOrEqual(OrderKey key)
        {
            decimal area = 0m;
            Node current = _root;
            while (current != null)
            {
                if (key.CompareTo(current.Key) < 0)
                {
                    current = current.Left;
                }
                else
                {
                    area += AreaOf(current.Left) + current.Segment.TwiceArea;
                    current = current.Right;
                }
            }
            return area;
        }

        private decimal AreaGreaterThan(OrderKey key)
            => AreaOf(_root) - AreaLessThanOrEqual(key);

        private static bool IsBeforeOnSegment(
            TerritoryCompactBoundarySegment segment,
            FixedTerritoryPoint from,
            FixedTerritoryPoint to)
        {
            FixedTerritoryPoint start = segment.GlobalStart;
            FixedTerritoryPoint end = segment.GlobalEnd;
            long edgeX = (long)end.X - start.X;
            long edgeY = (long)end.Y - start.Y;
            decimal progress = (decimal)(to.X - (long)from.X) * edgeX +
                               (decimal)(to.Y - (long)from.Y) * edgeY;
            return progress > 0m;
        }

        private static bool TryGet(Node node, OrderKey key, out TerritoryCompactBoundarySegment segment)
        {
            while (node != null)
            {
                int comparison = key.CompareTo(node.Key);
                if (comparison == 0)
                {
                    segment = node.Segment;
                    return true;
                }
                node = comparison < 0 ? node.Left : node.Right;
            }
            segment = default;
            return false;
        }

        private static Node Insert(
            Node node,
            OrderKey key,
            TerritoryCompactBoundarySegment segment,
            ref int nodesCreated,
            out bool inserted)
        {
            if (node == null)
            {
                inserted = true;
                nodesCreated++;
                return new Node(key, segment, null, null);
            }

            int comparison = key.CompareTo(node.Key);
            if (comparison == 0)
            {
                inserted = false;
                return node;
            }

            Node left = node.Left;
            Node right = node.Right;
            if (comparison < 0)
                left = Insert(left, key, segment, ref nodesCreated, out inserted);
            else
                right = Insert(right, key, segment, ref nodesCreated, out inserted);
            if (!inserted)
                return node;

            nodesCreated++;
            return Balance(new Node(node.Key, node.Segment, left, right), ref nodesCreated);
        }

        private static Node Remove(
            Node node,
            OrderKey key,
            ref int nodesCreated,
            out bool removed)
        {
            if (node == null)
            {
                removed = false;
                return null;
            }

            int comparison = key.CompareTo(node.Key);
            if (comparison < 0)
            {
                Node left = Remove(node.Left, key, ref nodesCreated, out removed);
                if (!removed)
                    return node;
                nodesCreated++;
                return Balance(new Node(node.Key, node.Segment, left, node.Right), ref nodesCreated);
            }
            if (comparison > 0)
            {
                Node right = Remove(node.Right, key, ref nodesCreated, out removed);
                if (!removed)
                    return node;
                nodesCreated++;
                return Balance(new Node(node.Key, node.Segment, node.Left, right), ref nodesCreated);
            }

            removed = true;
            if (node.Left == null)
                return node.Right;
            if (node.Right == null)
                return node.Left;
            Node successor = Minimum(node.Right);
            Node newRight = RemoveMinimum(node.Right, ref nodesCreated);
            nodesCreated++;
            return Balance(new Node(successor.Key, successor.Segment, node.Left, newRight), ref nodesCreated);
        }

        private static Node RemoveMinimum(Node node, ref int nodesCreated)
        {
            if (node.Left == null)
                return node.Right;
            Node left = RemoveMinimum(node.Left, ref nodesCreated);
            nodesCreated++;
            return Balance(new Node(node.Key, node.Segment, left, node.Right), ref nodesCreated);
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
                    node = new Node(node.Key, node.Segment, rotated, node.Right);
                }
                return RotateRight(node, ref nodesCreated);
            }
            if (balance < -1)
            {
                if (HeightOf(node.Right.Right) < HeightOf(node.Right.Left))
                {
                    Node rotated = RotateRight(node.Right, ref nodesCreated);
                    nodesCreated++;
                    node = new Node(node.Key, node.Segment, node.Left, rotated);
                }
                return RotateLeft(node, ref nodesCreated);
            }
            return node;
        }

        private static Node RotateLeft(Node node, ref int nodesCreated)
        {
            Node pivot = node.Right;
            nodesCreated += 2;
            Node left = new Node(node.Key, node.Segment, node.Left, pivot.Left);
            return new Node(pivot.Key, pivot.Segment, left, pivot.Right);
        }

        private static Node RotateRight(Node node, ref int nodesCreated)
        {
            Node pivot = node.Left;
            nodesCreated += 2;
            Node right = new Node(node.Key, node.Segment, pivot.Right, node.Right);
            return new Node(pivot.Key, pivot.Segment, pivot.Left, right);
        }

        private static Node Minimum(Node node)
        {
            while (node?.Left != null)
                node = node.Left;
            return node;
        }

        private static Node Maximum(Node node)
        {
            while (node?.Right != null)
                node = node.Right;
            return node;
        }

        private static Node Successor(Node root, OrderKey key)
        {
            Node candidate = null;
            while (root != null)
            {
                if (key.CompareTo(root.Key) < 0)
                {
                    candidate = root;
                    root = root.Left;
                }
                else
                {
                    root = root.Right;
                }
            }
            return candidate;
        }

        private static Node Predecessor(Node root, OrderKey key)
        {
            Node candidate = null;
            while (root != null)
            {
                if (key.CompareTo(root.Key) > 0)
                {
                    candidate = root;
                    root = root.Right;
                }
                else
                {
                    root = root.Left;
                }
            }
            return candidate;
        }

        private static void CopyOrderedTo(Node node, List<TerritoryCompactBoundarySegment> destination)
        {
            if (node == null)
                return;
            CopyOrderedTo(node.Left, destination);
            destination.Add(node.Segment);
            CopyOrderedTo(node.Right, destination);
        }

        private static int HeightOf(Node node) => node?.Height ?? 0;
        private static int CountOf(Node node) => node?.Count ?? 0;
        private static decimal AreaOf(Node node) => node?.TwiceArea ?? 0m;

        private sealed class Node
        {
            public Node(
                OrderKey key,
                TerritoryCompactBoundarySegment segment,
                Node left,
                Node right)
            {
                Key = key;
                Segment = segment;
                Left = left;
                Right = right;
                Height = Math.Max(HeightOf(left), HeightOf(right)) + 1;
                Count = CountOf(left) + CountOf(right) + 1;
                TwiceArea = AreaOf(left) + segment.TwiceArea + AreaOf(right);
            }

            public OrderKey Key { get; }
            public TerritoryCompactBoundarySegment Segment { get; }
            public Node Left { get; }
            public Node Right { get; }
            public int Height { get; }
            public int Count { get; }
            public decimal TwiceArea { get; }
        }

        private readonly struct OrderKey : IComparable<OrderKey>, IEquatable<OrderKey>
        {
            private OrderKey(BigInteger numerator, BigInteger denominator)
            {
                if (denominator <= BigInteger.Zero)
                    throw new ArgumentOutOfRangeException(nameof(denominator));
                BigInteger divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
                Numerator = numerator / divisor;
                Denominator = denominator / divisor;
            }

            private BigInteger Numerator { get; }
            private BigInteger Denominator { get; }

            public static OrderKey FromInteger(int value)
                => new(value, BigInteger.One);

            public static OrderKey Between(OrderKey left, OrderKey right)
            {
                if (left.CompareTo(right) >= 0)
                    throw new ArgumentException("A non-wrapping Boundary insertion requires increasing neighbors.");
                return new OrderKey(
                    left.Numerator * right.Denominator + right.Numerator * left.Denominator,
                    left.Denominator * right.Denominator * 2);
            }

            public OrderKey AddOne()
                => new(Numerator + Denominator, Denominator);

            public int CompareTo(OrderKey other)
                => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);

            public bool Equals(OrderKey other)
                => Numerator == other.Numerator && Denominator == other.Denominator;

            public override bool Equals(object obj)
                => obj is OrderKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Numerator.GetHashCode() * 397) ^ Denominator.GetHashCode();
                }
            }
        }
    }
}
