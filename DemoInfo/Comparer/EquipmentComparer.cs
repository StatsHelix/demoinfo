using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo.Comparer
{
    class EquipmentComparer : IEqualityComparer<Equipment>
    {
        public bool Equals(Equipment x, Equipment y)
        {
            return x.EntityID == y.EntityID;
        }

        public int GetHashCode(Equipment obj)
        {
            return obj.EntityID;
        }
    }
}
