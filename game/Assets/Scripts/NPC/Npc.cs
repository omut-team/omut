using System;
using System.Collections.Generic;
using Omut.UI;
using UnityEngine;

namespace Omut.NPC
{
    // Житель причала, с которым можно поговорить (E). Разговор открывает DialogueUI:
    // при первой встрече реплики знакомства, дальше меню: задания, торговля, ремонт, тайны.
    public class Npc : Interactable
    {
        [SerializeField] private NpcId id = NpcId.Taras;
        [SerializeField] private string displayName = "Тарас";
        [SerializeField] private Sprite portrait;

        [Tooltip("Реплики при первой встрече")]
        [SerializeField] private DialogueData firstMeeting;

        [Tooltip("Чем встречает в следующий раз (одна случайная фраза)")]
        [SerializeField] private List<string> greetings = new List<string> { "Ну что, клюёт?" };

        public NpcId Id => id;
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public DialogueData FirstMeeting => firstMeeting;

        public override string Prompt => "поговорить: " + displayName;

        // Житель ещё не появился (дед Язэп до финала): с ним нельзя говорить, табличку не видно.
        public bool Hidden { get; set; }

        public override bool CanInteract => !Hidden;

        public string RandomGreeting() => greetings.Count == 0 ? "..." : greetings[UnityEngine.Random.Range(0, greetings.Count)];

        public override void Interact() => DialogueUI.Instance?.Talk(this);

        // Свои пункты меню разговора: (текст кнопки, что делать). Тарас: торговать, Михалыч: ремонт.
        public virtual List<(string label, Action action)> ExtraOptions() => new List<(string, Action)>();

        public void Setup(NpcId npcId, string npcName, Sprite face, DialogueData meeting, List<string> hello)
        {
            id = npcId;
            displayName = npcName;
            portrait = face;
            firstMeeting = meeting;
            greetings = hello;
        }
    }
}
