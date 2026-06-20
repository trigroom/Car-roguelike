
    using System.Collections.Generic;

    struct ShopComponent {
        public List<List<int>> ListShopItemsForRares;
        public List<float> CurrentShopItemsChances;
        public List<int> ShopModulesIds;
        public int CurrentUpdateCountInRound;
        public bool IsCarShopMode;
        public bool IsModulesShop;
        public int ShopLevel;

        public Dictionary<ulong, int> PlayersGivenMoneyDictionary;
    }