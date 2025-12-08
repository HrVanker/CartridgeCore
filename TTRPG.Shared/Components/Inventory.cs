using System.Collections.Generic;

namespace TTRPG.Shared.Components
{
    public struct Inventory
    {
        public int Capacity;
        public List<string> Items; // Stores the 'Id' of the items held
    }
}