using System;
using System.Collections.Generic;
using System.Diagnostics;
using TAOM.EquipmentTestShop;

internal static class Program
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        Check(EquipmentIds.All.Length == 75 && new HashSet<string>(EquipmentIds.All).Count == 75, "Exact unique catalogue");
        var stock = new Dictionary<string, int>();
        foreach (string id in EquipmentIds.All) stock[id] = 0;
        for (int i = 0; i < 10000; i++) stock["unrelated_" + i] = 17;
        stock[EquipmentIds.All[0] + ":fine"] = 1;
        int reads = 0, writes = 0;
        Func<string, int> count = id => { reads++; return stock.TryGetValue(id, out int value) ? value : -1; };
        Action<string, int> add = (id, amount) => { writes++; stock[id] += amount; };

        Check(TestStock.Apply("town_EW2", true, count, add) == 0 && reads == 0 && writes == 0, "Wrong town must not touch stock");
        Check(TestStock.Apply("town_EW1", false, count, add) == 0 && reads == 0, "Non-town rejected");
        Check(TestStock.Apply(null, true, count, add) == 0 && reads == 0, "Missing settlement rejected");
        Check(TestStock.Apply("town_EW1", true, count, add) == 225 && reads == 75 && writes == 75, "Initial full catalogue stock");
        Check(TestStock.Apply("town_EW1", true, count, add) == 0 && writes == 75, "Reload/entry must be idempotent");
        stock[EquipmentIds.All[0]] = 1;
        stock[EquipmentIds.All[1]] = 9;
        stock.Remove(EquipmentIds.All[2]);
        Check(TestStock.Apply("town_EW1", true, count, add) == 2, "Only top up deficit, skip missing registered item");
        Check(stock[EquipmentIds.All[0]] == 3 && stock[EquipmentIds.All[1]] == 9, "Never truncate surplus");
        Check(stock[EquipmentIds.All[0] + ":fine"] == 1, "Leave modified stacks alone");
        Check(!stock.ContainsKey(EquipmentIds.All[2]) && stock["unrelated_9999"] == 17, "No invented items or unrelated changes");

        reads = writes = 0;
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++) TestStock.Apply("town_EW1", true, count, add);
        timer.Stop();
        Check(reads == 75000 && writes == 0, "75-count-operation budget independent of world/stock size");
        Check(timer.ElapsedMilliseconds < 2000, "Portable policy benchmark exceeded 2 seconds/1000 entries");
        Console.WriteLine("PASS: catalogue, town isolation, null/non-town, initial stock, repeat, deficit, surplus, missing item, unrelated stock, operation budget.");
        Console.WriteLine("Policy benchmark: 1000 entries / 10000 unrelated stock rows / " + timer.ElapsedMilliseconds + " ms; native roster/runtime not measured.");
    }
}
