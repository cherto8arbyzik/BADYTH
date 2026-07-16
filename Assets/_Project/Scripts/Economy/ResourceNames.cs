namespace Hollowwest.Economy
{

public static class ResourceNames
{
    public static string Get(ResourceType type)
    {
        return type switch
        {
            ResourceType.Timber => "древесина",
            ResourceType.Stone => "камень",
            ResourceType.Clay => "глина",
            ResourceType.Food => "пища",
            ResourceType.Herb => "травы",
            ResourceType.Hide => "шкуры",
            ResourceType.Plank => "доски",
            ResourceType.Brick => "кирпич",
            ResourceType.Tool => "инструменты",
            ResourceType.Leather => "кожа",
            ResourceType.Grain => "зерно",
            ResourceType.Provisions => "провиант",
            ResourceType.Medicine => "лекарства",
            ResourceType.OldIron => "старое железо",
            ResourceType.Relic => "реликвии",
            ResourceType.WardStone => "обережный камень",
            ResourceType.SkyGlass => "небесное стекло",
            _ => type.ToString()
        };
    }

    public static string GetShort(ResourceType type)
    {
        return type switch
        {
            ResourceType.Timber => "Дерево",
            ResourceType.Stone => "Камень",
            ResourceType.Clay => "Глина",
            ResourceType.Food => "Еда",
            ResourceType.Herb => "Травы",
            ResourceType.Hide => "Шкуры",
            ResourceType.Plank => "Доски",
            ResourceType.Brick => "Кирпич",
            ResourceType.Tool => "Инстр.",
            ResourceType.Leather => "Кожа",
            ResourceType.Grain => "Зерно",
            ResourceType.Provisions => "Провиант",
            ResourceType.Medicine => "Лекарства",
            ResourceType.OldIron => "Железо",
            ResourceType.Relic => "Реликвии",
            ResourceType.WardStone => "Обереги",
            ResourceType.SkyGlass => "Неб. стекло",
            _ => type.ToString()
        };
    }
}
}
