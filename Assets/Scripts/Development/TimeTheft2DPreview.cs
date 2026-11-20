using System;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTheft
{
    public sealed class TimeTheft2DPreview : MonoBehaviour
    {
        [SerializeField] private TimeTheftSettings settings;
        [SerializeField] private Text playerLife;
        [SerializeField] private Text opponentLife;
        [SerializeField] private Text bankTime;
        [SerializeField] private Text status;
        [SerializeField] private Button[] clocks;
        [SerializeField] private Text[] clockValues;
        [SerializeField] private Image[] clockFaces;
        [SerializeField] private Button collect;
        [SerializeField] private Button ration;
        [SerializeField] private Button advance;
        [SerializeField] private Text advanceLabel;

        private static readonly Color ClosedFace = new Color32(99, 117, 135, 255);
        private static readonly Color NormalFace = new Color32(122, 206, 185, 255);
        private static readonly Color RedFace = new Color32(237, 139, 145, 255);
        private static readonly Color SelectedFace = new Color32(244, 209, 132, 255);

        private TimeTheftMatch match;

        private void Start()
        {
            for (int i = 0; i < clocks.Length; i++)
            {
                int slot = i;
                clocks[i].onClick.AddListener(() => SelectClock(slot));
            }
            collect.onClick.AddListener(() => Settle(TimeAction.Collect));
            ration.onClick.AddListener(() => Settle(TimeAction.Ration));
            advance.onClick.AddListener(Advance);
            BeginPreview();
        }

        private void BeginPreview()
        {
            int seed = settings.RandomSeed == 0 ? Environment.TickCount : settings.RandomSeed;
            match = new TimeTheftMatch(settings.Rules, new System.Random(seed), new System.Random(seed ^ 0x2471));
            match.BeginBatch();
            Refresh();
        }

        private void Advance()
        {
            if (match.Stage == TurnStage.BatchPreview)
            {
                match.CloseAndShuffle();
                match.CompleteShuffle();
                match.EndItemPhase(Participant.Player);
                Refresh();
                return;
            }

            BeginPreview();
        }

        private void SelectClock(int index)
        {
            bool changed = match.CurrentClock == index
                ? match.CancelClockSelection(Participant.Player)
                : match.SelectClock(Participant.Player, index);
            if (changed)
                Refresh();
        }

        private void Settle(TimeAction action)
        {
            if (match.CommitAction(Participant.Player, action) && match.Settle())
                Refresh();
        }

        private void Refresh()
        {
            playerLife.text = GameTime.Format(match.PlayerLifeMinutes);
            opponentLife.text = GameTime.Format(match.AiLifeMinutes);
            bankTime.text = GameTime.Format(match.BankMinutes);

            bool playerTurn = match.ActiveParticipant == Participant.Player;
            bool preview = match.Stage == TurnStage.BatchPreview;
            bool choosing = playerTurn && match.Stage == TurnStage.AwaitingAction;
            bool settled = match.Stage == TurnStage.Settling || match.Stage == TurnStage.GameOver;
            for (int i = 0; i < clocks.Length; i++)
            {
                var face = match.GetClock(i).Face;
                bool selected = choosing && match.CurrentClock == i;
                clocks[i].interactable = match.CanSelectClock(Participant.Player, i) || selected;
                clockValues[i].text = face.HasValue
                    ? $"{face.Value.Hours}h\n<size=16>{(face.Value.Kind == ClockKind.Red ? "빨강" : "일반")}</size>"
                    : "?";
                clockFaces[i].color = face.HasValue
                    ? (face.Value.Kind == ClockKind.Red ? RedFace : NormalFace)
                    : (selected ? SelectedFace : ClosedFace);
            }

            collect.interactable = ration.interactable = choosing;
            advance.gameObject.SetActive(preview || settled || !playerTurn);
            advanceLabel.text = preview ? "가리고 섞기" : "다시 보기";
            if (preview)
                status.text = "시계의 숫자와 종류를 확인하세요.";
            else if (settled)
            {
                var transfer = match.LastTransfers[0];
                status.text = $"{AccountName(transfer.From)} → {AccountName(transfer.To)}  {GameTime.Format(transfer.Minutes)}";
            }
            else if (!playerTurn)
                status.text = "이 미리보기에서는 플레이어의 첫 행동을 시험할 수 있습니다.";
            else if (choosing)
                status.text = $"시계 {match.CurrentClock + 1} 선택 · 징수 또는 배급";
            else
                status.text = "닫힌 시계를 하나 선택하세요.";
        }

        private static string AccountName(TimeAccount account)
        {
            switch (account)
            {
                case TimeAccount.Player: return "플레이어";
                case TimeAccount.Ai: return "상대";
                default: return "시간고";
            }
        }
    }
}
