
// Copyright (c) 2019 Alberto Morvillo
// Distributed under MIT license
// https://opensource.org/licenses/MIT

namespace AStarNet
{
    /// <summary>
    /// Defines generalized methods to allow the use of A* algorithm on a data structure
    /// </summary>
    public interface INodeMap<T>
    {
        /// <summary>
        /// Get the start <see cref="Node{T}"/> in the map
        /// </summary>
        /// <returns>The start <see cref="Node{T}"/> in the map</returns>
        Node<T> GetStartNode();

        /// <summary>
        /// Get the destination <see cref="Node{T}"/> in the map
        /// </summary>
        /// <returns>The destination <see cref="Node{T}"/> in the map</returns>
        Node<T> GetDestinationNode();

        /// <summary>
        /// Get the childs of a specific <see cref="Node{T}"/> in the map
        /// </summary>
        /// <param name="currentNode">The <see cref="Node{T}"/> from which get the childs</param>
        /// <returns>A <see cref="Node{T}"/> array with the childs of the specific <see cref="Node{T}"/> in the map</returns>
        Node<T>[] GetChildNodes(Node<T> currentNode);
    }
}
