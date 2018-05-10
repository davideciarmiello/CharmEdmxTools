using CharmEdmxTools.Core.Containers;

namespace CharmEdmxTools.Core.Interfaces
{
    public interface IRemovable
    {
        void Remove(EdmxContainer container);
        bool Removed { get; set; }
    }
}