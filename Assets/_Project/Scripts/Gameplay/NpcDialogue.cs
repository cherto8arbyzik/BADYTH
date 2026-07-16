using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Gameplay
{

[Serializable]
public sealed class DialogueTopic
{
    public DialogueTopic(string prompt, string response, bool endsConversation = false)
    {
        Prompt = prompt;
        Response = response;
        EndsConversation = endsConversation;
    }

    public string Prompt { get; }
    public string Response { get; }
    public bool EndsConversation { get; }
}

public sealed class NpcDialogue : MonoBehaviour
{
    private List<DialogueTopic> _topics = new();
    private int _profileSeed;

    public string DisplayName { get; private set; }
    public string Role { get; private set; }
    public string Greeting { get; private set; }
    public string StoryHook { get; private set; }
    public IReadOnlyList<DialogueTopic> Topics => _topics;

    public void ConfigureDefault(string sourceName, int seed)
    {
        if (_topics == null)
        {
            _topics = new List<DialogueTopic>();
        }

        _profileSeed = seed;
        _topics.Clear();
        if (sourceName.Contains("Mara"))
        {
            DisplayName = "Мара";
            Role = "плотница и старшая среди первых поселенцев";
            Greeting = "Опять считаю целые балки. Их меньше, чем надежд, но пока хватает.";
            StoryHook = "Мара пришла с нижних островов и носит костяной плотницкий угольник прежнего мастера.";
            AddCommonTopics(
                "До падения я ставила кровли в Белом Посаде. Когда земля треснула, мой мастер остался держать мост, а я вывела детей. Этот угольник — всё, что осталось от артели.",
                "Ратуша держится. Людям нужен сухой дом, склад и место, где можно чинить инструмент. Красота придёт после первой зимы.",
                "В старой мастерской под золой я видела метки нашей артели. Если расчистим завал, может найтись чертёж подъёмного крана.");
            return;
        }

        if (sourceName.Contains("Yarik"))
        {
            DisplayName = "Ярик";
            Role = "следопыт и добытчик";
            Greeting = "Тише. Ветер сегодня приносит звон снизу, хотя колоколов там давно нет.";
            StoryHook = "Ярик знает тропы обломков и утверждает, что под островом кто-то отвечает на сигналы костров.";
            AddCommonTopics(
                "Я ходил по обломкам ещё до того, как встретил вас. Внизу есть целые улицы, висящие вверх ногами. Однажды оттуда мне ответили трижды на один удар.",
                "Дерева вокруг много, но рубить всё подряд нельзя. Оставим семенные деревья — лес будет возвращаться. Каменная жила у края почти бездонная.",
                "У большого озера видел следы лодки, хотя лодки у нас нет. След был свежий и уходил прямо в воду.");
            return;
        }

        if (sourceName.Contains("Toma"))
        {
            DisplayName = "Тома";
            Role = "дозорная";
            Greeting = "С высоты всё выглядит спокойно. Именно поэтому я не отвожу глаз.";
            StoryHook = "Тома помнит момент разлома и ищет огни острова, на котором осталась её сестра.";
            AddCommonTopics(
                "Когда мир раскололся, наш двор поднялся, а соседняя улица ушла в облака. На ней была моя сестра Лада. Иногда ночью на востоке вижу два огня — наш старый знак.",
                "Нам нужны дозорная башня и свободные проходы между домами. Руины прячут больше, чем дают укрытия.",
                "Волки держатся у восточного гребня, а олени выходят к плодородной поляне на рассвете. Охотникам стоит знать.");
            return;
        }

        int variant = Mathf.Abs(seed) % 3;
        DisplayName = sourceName.StartsWith("Спасённый") ? sourceName : "Поселенец";
        Role = variant == 0 ? "ремесленник с нижних обломков" : variant == 1 ? "беженец и собиратель" : "путник, нашедший заставу";
        Greeting = variant == 0
            ? "Спасибо, что здесь можно говорить, не прислушиваясь к каждому шороху."
            : variant == 1
                ? "Я ещё привыкаю, что земля под ногами не качается."
                : "У вашего огня впервые за долгое время пахнет домом.";
        StoryHook = variant == 0
            ? "Он потерял артель во время разлома и помнит устройство старых мастерских."
            : variant == 1
                ? "Она выживала на малом обломке и знает съедобные растения небесных островов."
                : "Путник много месяцев переходил между островами по цепям древних мостов.";
        AddCommonTopics(
            variant == 0
                ? "Мы чинили насосы под городом. Перед падением вода в них пошла вверх — будто сам мир перевернул дыхание."
                : variant == 1
                    ? "На моём обломке росла одна рябина. Я делила ягоды с птицами и потому дожила до вашей вылазки."
                    : "Я шёл по цепным мостам. Некоторые звенья тёплые даже ночью, словно внутри течёт кровь.",
            "Пока есть работа и место у огня, я останусь. Назначь меня туда, где нужнее руки.",
            "Люди на вылазках говорили о целом монастыре на дальнем острове. Над ним никогда не расходятся тучи.");
    }

    private void Start()
    {
        if (_topics == null || _topics.Count == 0)
        {
            ConfigureDefault(gameObject.name, _profileSeed == 0 ? gameObject.name.GetHashCode() : _profileSeed);
        }
    }

    private void AddCommonTopics(string past, string settlement, string rumor)
    {
        _topics.Add(new DialogueTopic("Расскажи о себе.", past));
        _topics.Add(new DialogueTopic("Что думаешь о поселении?", settlement));
        _topics.Add(new DialogueTopic("Слышал что-нибудь важное?", rumor));
        _topics.Add(new DialogueTopic("Как ты сейчас?", "Пока держусь. Работа помогает не считать тех, кого мы не успели вывести."));
        _topics.Add(new DialogueTopic("До встречи.", "До встречи. Если услышишь колокол в тумане — сначала позови остальных.", true));
    }
}
}
