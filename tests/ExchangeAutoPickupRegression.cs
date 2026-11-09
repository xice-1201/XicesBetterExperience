using System;
using XicesBetterExperience;

public static class ExchangeAutoPickupRegression
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static void Run(ModEntry entry)
    {
        Check(ExperienceSettings.ExchangeAutoPickup && ModConfig.Instance.ExchangeAutoPickupEntry.Value,
            "Exchange auto pickup defaults on");
        var manager = new ExchangeManager();
        manager.itemsBuffer.Add(new GameItem());

        // Native TransferBuffer must always run: it is the game's supported
        // path that moves exchange rewards into the trashcan overnight.
        Check(ExchangeAutoPickup.Prefix(manager),
            "Native exchange transfer remains enabled");
        Check(manager.itemsBuffer.Count == 1,
            "Mod does not clear or relocate the native exchange buffer");

        Check(ExchangeAutoPickup.Prefix(new ExchangeManager()), "Empty buffer remains safe");

        // The game recursively stores trash beneath its trashcan item. A captured
        // reward must be found there and transferred to the actual counter.
        var reward = new GameItem { uniqueId = 8172 };
        var exchange = new ExchangeManager();
        exchange.itemsBuffer.Add(reward);
        Check(ExchangeAutoPickup.Prefix(exchange), "Capture runs before native transfer");
        var trashCan = new GameItem();
        var nestedTrashInventory = new GameGridInventory();
        trashCan.children.Add(nestedTrashInventory);
        nestedTrashInventory.TryAcceptAll(reward);
        var backInventory = new GameGridInventory();
        var counter = new GameGridInventory();
        EmporiumEntry.Instance = new EmporiumEntry { trashcan = trashCan, trashcanInvElement = new GameGridInventory(), backInvinvElement = backInventory, backInvinvElementCounter = counter };
        MapUIManager.Instance = new MapUIManager();
        // Simulate the original TransferBuffer inserting the reward, then run
        // the Harmony postfix. The old ClearTrash prefix ran before this point.
        ExchangeAutoPickup.Postfix();
        Check(backInventory.childItems.Contains(reward) && reward.parentInventory == backInventory,
            "Captured reward remains in persistent back inventory during night settlement");
        Check(counter.childItems.Count == 0,
            "Night settlement does not put rewards into the temporary counter scene");
        ExchangeAutoPickupAtStoreArrival.Postfix();
        Check(counter.childItems.Contains(reward) && reward.parentInventory == counter,
            "Store arrival transfers staged reward through the native counter routine");
        Check(backInventory.childItems.Count == 0,
            "Native counter transfer consumes the staged back inventory item");
        Check(!nestedTrashInventory.childItems.Contains(reward),
            "Transferred reward is removed from nested trash inventory");

        entry.OnDisable();
        Check(ExchangeAutoPickup.Prefix(manager), "Disabled mod keeps native transfer");
        entry.OnEnable();
        Console.WriteLine("PASS: exchange auto pickup leaves native overnight trashcan transfer untouched.");
    }
}
