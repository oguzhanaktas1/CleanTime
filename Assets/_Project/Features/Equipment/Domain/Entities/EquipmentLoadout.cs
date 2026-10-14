using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Domain
{
    public enum SwitchDirection
    {
        Previous = -1,
        Next = 1,
    }

    /// <summary>Ordered set of owned equipment with exactly one selected item.</summary>
    public sealed class EquipmentLoadout
    {
        private readonly List<EquipmentId> _items = new List<EquipmentId>();
        private int _selectedIndex = -1;

        public IReadOnlyList<EquipmentId> Items => _items;

        public bool HasSelection => _selectedIndex >= 0;

        public EquipmentId Selected => HasSelection ? _items[_selectedIndex] : default;

        /// <summary>Adds an owned item. The first item becomes the selection.</summary>
        public void Add(EquipmentId id)
        {
            if (_items.Contains(id))
                return;

            _items.Add(id);

            if (_selectedIndex < 0)
                _selectedIndex = 0;
        }

        public Result Select(EquipmentId id)
        {
            var index = _items.IndexOf(id);
            if (index < 0)
                return Result.Failure(EquipmentErrors.NotOwned, $"'{id}' is not in the loadout.");

            _selectedIndex = index;
            return Result.Success();
        }

        public Result Switch(SwitchDirection direction)
        {
            if (_items.Count == 0)
                return Result.Failure(EquipmentErrors.LoadoutEmpty, "No equipment to switch between.");

            var next = (_selectedIndex + (int)direction) % _items.Count;
            _selectedIndex = next < 0 ? next + _items.Count : next;
            return Result.Success();
        }
    }
}
