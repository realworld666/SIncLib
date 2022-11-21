using System.Collections.Generic;

namespace SIncLib
{
    public class AddonSaleItem
    {
        public AddOnProduct product;
        public List<AddonSale> sales;
    }
    public class AddonSale
    {
        public int Digital;
        public int Physical;
        public uint Cumulative;
        public SDateTime Date;
    }
}