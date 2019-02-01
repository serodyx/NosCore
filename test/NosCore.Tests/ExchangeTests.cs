﻿//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemBuilderService;
using NosCore.GameObject.Services.ItemBuilderService.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.Tests
{
    [TestClass]
    public class ExchangeTests
    {
        private ExchangeService _exchangeService;

        private WorldConfiguration _worldConfiguration;

        private ItemBuilderService _itemBuilderService;

        [TestInitialize]
        public void Setup()
        {
            _worldConfiguration = new WorldConfiguration
            {
                MaxItemAmount = 999,
                BackpackSize = 48,
                MaxGoldAmount = 1000000000,
                MaxBankGoldAmount = 100000000000
            };

            var items = new List<Item>
            {
                new Item { Type = PocketType.Main, VNum = 1012 },
                new Item { Type = PocketType.Main, VNum = 1013 },
            };

            _itemBuilderService = new ItemBuilderService(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());
            _exchangeService = new ExchangeService(_itemBuilderService, _worldConfiguration);
        }

        [TestMethod]
        public void Test_Set_Gold()
        {
            _exchangeService.OpenExchange(1, 2);
            _exchangeService.SetGold(1, 1000, 1000);
            _exchangeService.SetGold(2, 2000, 2000);

            var data1 = _exchangeService.GetData(1);
            var data2 = _exchangeService.GetData(2);

            Assert.IsTrue(data1.Gold == 1000 && data1.BankGold == 1000 && data2.Gold == 2000 && data2.BankGold == 2000);
        }

        [TestMethod]
        public void Test_Confirm_Exchange()
        {
            _exchangeService.OpenExchange(1, 2);
            _exchangeService.ConfirmExchange(1);
            _exchangeService.ConfirmExchange(2);

            var data1 = _exchangeService.GetData(1);
            var data2 = _exchangeService.GetData(2);

            Assert.IsTrue(data1.ExchangeConfirmed && data2.ExchangeConfirmed);
        }

        [TestMethod]
        public void Test_Add_Items()
        {
            _exchangeService.OpenExchange(1, 2);

            var item = new ItemInstance
            {
                Amount = 1,
                ItemVNum = 1012
            };

            _exchangeService.AddItems(1, item, item.Amount);

            var data1 = _exchangeService.GetData(1);

            Assert.IsTrue(data1.ExchangeItems.Any(s => s.Key.ItemVNum == 1012 && s.Key.Amount == 1));
        }

        [TestMethod]
        public void Test_Check_Exchange()
        {
            var wrongExchange = _exchangeService.CheckExchange(1);
            _exchangeService.OpenExchange(1, 2);
            var goodExchange = _exchangeService.CheckExchange(1);

            Assert.IsTrue(!wrongExchange && goodExchange);
        }

        [TestMethod]
        public void Test_Close_Exchange()
        {
            var wrongClose = _exchangeService.CloseExchange(1, ExchangeResultType.Failure);

            Assert.IsNull(wrongClose);

            _exchangeService.OpenExchange(1, 2);
            var goodClose = _exchangeService.CloseExchange(1, ExchangeResultType.Failure);
            Assert.IsTrue(goodClose != null && goodClose.Type == ExchangeResultType.Failure);
        }

        [TestMethod]
        public void Test_Open_Exchange()
        {
            var exchange = _exchangeService.OpenExchange(1, 2);
            Assert.IsTrue(exchange);
        }

        [TestMethod]
        public void Test_Open_Second_Exchange()
        {
            var exchange = _exchangeService.OpenExchange(1, 2);
            Assert.IsTrue(exchange);

            var wrongExchange = _exchangeService.OpenExchange(1, 3);
            Assert.IsFalse(wrongExchange);
        }

        [TestMethod]
        public void Test_Process_Exchange()
        {
            IInventoryService inventory1 = new InventoryService(new List<Item> { new Item {VNum = 1012, Type = PocketType.Main } }, _worldConfiguration);
            IInventoryService inventory2 = new InventoryService(new List<Item> { new Item { VNum = 1013, Type = PocketType.Main } }, _worldConfiguration);
            var item1 = inventory1.AddItemToPocket(_itemBuilderService.Create(1012, 1)).First();
            var item2 = inventory2.AddItemToPocket(_itemBuilderService.Create(1013, 1)).First();

            _exchangeService.OpenExchange(1, 2);
            _exchangeService.AddItems(1, item1, 1);
            _exchangeService.AddItems(2, item2, 1);
            var itemList = _exchangeService.ProcessExchange(1, 2, inventory1, inventory2);
            Assert.IsTrue(itemList.Count(s => s.Key == 1) == 2 && itemList.Count(s => s.Key == 2) == 2);
        }
    }
}
