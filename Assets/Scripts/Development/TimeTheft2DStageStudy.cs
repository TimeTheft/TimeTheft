using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TimeTheft
{
    public sealed class TimeTheft2DStageStudy : MonoBehaviour
    {
        [Serializable]
        private sealed class ClockView
        {
            public Button button;
            public CanvasGroup visibility;
            public Image rim;
            public Text value;

            public RectTransform Transform => (RectTransform)button.transform;
        }

        private struct Pose
        {
            public Vector3 position;
            public Vector3 scale;
            public Quaternion rotation;

            public Pose(RectTransform transform)
            {
                position = transform.anchoredPosition3D;
                scale = transform.localScale;
                rotation = transform.localRotation;
            }

            public void Apply(RectTransform transform)
            {
                transform.anchoredPosition3D = position;
                transform.localScale = scale;
                transform.localRotation = rotation;
            }

            public static Pose Lerp(Pose from, Pose to, float t)
            {
                return new Pose
                {
                    position = Vector3.Lerp(from.position, to.position, t),
                    scale = Vector3.Lerp(from.scale, to.scale, t),
                    rotation = Quaternion.Slerp(from.rotation, to.rotation, t)
                };
            }
        }

        [SerializeField] private TimeTheftSettings settings;
        [SerializeField] private ClockView[] watches;
        [SerializeField] private RectTransform opponent;
        [SerializeField] private RectTransform focusPoint;
        [SerializeField] private RectTransform shufflePoint;
        [SerializeField] private Image focusRing;
        [SerializeField] private Text playerLife;
        [SerializeField] private Text opponentLife;
        [SerializeField] private Text bankTime;
        [SerializeField] private Text prompt;
        [SerializeField] private Button collect;
        [SerializeField] private Button ration;
        [SerializeField] private Button cancel;
        [SerializeField] private RectTransform actions;
        [SerializeField] private Text actionTitle;
        [SerializeField] private Text transfers;
        [SerializeField] private Button advance;
        [SerializeField] private Text advanceLabel;

        private static readonly Color Closed = new Color32(150, 167, 181, 255);
        private static readonly Color Normal = new Color32(135, 219, 196, 255);
        private static readonly Color Red = new Color32(249, 154, 147, 255);
        private static readonly Color Accent = new Color32(249, 210, 126, 255);

        private TimeTheftMatch match;
        private ClockView[] slots;
        private Pose[] homes;
        private Pose opponentHome;
        private ClockView foregroundWatch;
        private int foregroundHomeIndex;
        private bool busy;
        private bool scriptedMotion;
        private float opponentImpulse;
        private float effectStartedAt;

        private void Start()
        {
            slots = new ClockView[watches.Length];
            homes = new Pose[watches.Length];
            for (int i = 0; i < watches.Length; i++)
            {
                ClockView watch = watches[i];
                homes[i] = new Pose(watch.Transform);
                watch.button.onClick.AddListener(() => Select(Array.IndexOf(slots, watch)));
            }

            opponentHome = new Pose(opponent);
            collect.onClick.AddListener(() => Commit(TimeAction.Collect));
            ration.onClick.AddListener(() => Commit(TimeAction.Ration));
            cancel.onClick.AddListener(Cancel);
            advance.onClick.AddListener(Advance);
            BeginPreview();
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Cancel();
        }

        private void LateUpdate()
        {
            if (match == null)
                return;

            float blend = 1f - Mathf.Exp(-13f * Time.unscaledDeltaTime);
            int selected = match.CurrentClock;
            if (!scriptedMotion)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    ClockView watch = slots[i];
                    Pose target = homes[i];
                    if (i == selected)
                    {
                        target.position = focusPoint.anchoredPosition3D;
                        target.scale = Vector3.Scale(target.scale, focusPoint.localScale);
                        target.rotation = focusPoint.localRotation;
                    }
                    Pose.Lerp(new Pose(watch.Transform), target, blend).Apply(watch.Transform);
                    watch.visibility.alpha = Mathf.Lerp(watch.visibility.alpha, selected >= 0 && i != selected ? .43f : 1f, blend);
                }
            }

            Pose reaction = opponentHome;
            float lean = selected >= 0 ? 1f : 0f;
            reaction.position += new Vector3(13f * lean + 17f * opponentImpulse, 11f * opponentImpulse, 0f);
            reaction.rotation *= Quaternion.Euler(0f, 0f, -4f * lean + 7f * opponentImpulse);
            reaction.scale *= 1f + .025f * lean + .045f * Mathf.Abs(opponentImpulse);
            Pose.Lerp(new Pose(opponent), reaction, blend).Apply(opponent);
            opponentImpulse = Mathf.MoveTowards(opponentImpulse, 0f, Time.unscaledDeltaTime * 2.3f);

            if (selected >= 0)
                UpdateRing(slots[selected]);
        }

        private void BeginPreview()
        {
            int seed = settings.RandomSeed == 0 ? Environment.TickCount : settings.RandomSeed;
            match = new TimeTheftMatch(settings.Rules, new System.Random(seed), new System.Random(seed ^ 0x2471));
            match.BeginBatch();
            Array.Copy(watches, slots, watches.Length);
            opponentImpulse = .4f;
            StartCoroutine(Appear());
        }

        private IEnumerator Appear()
        {
            busy = scriptedMotion = true;
            Refresh();
            yield return Animate(.54f, progress =>
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - .09f * i) / .64f));
                    Pose pose = homes[i];
                    pose.position.y -= 24f * (1f - t);
                    pose.scale *= Mathf.Lerp(.78f, 1f, t);
                    pose.Apply(slots[i].Transform);
                    slots[i].visibility.alpha = t;
                }
            });
            busy = scriptedMotion = false;
            Refresh();
        }

        private void Advance()
        {
            if (busy)
                return;

            if (match.CloseAndShuffle())
                StartCoroutine(Shuffle());
            else if (IsSettled || match.ActiveParticipant != Participant.Player)
                BeginPreview();
        }

        private IEnumerator Shuffle()
        {
            busy = scriptedMotion = true;
            Refresh();
            ClockShuffle shuffle = match.LastShuffle;
            yield return Animate(settings.ShuffleDuration, progress =>
            {
                // 모든 시계가 겹치는 구간에서 출발 슬롯과 도착 슬롯의 대응을 가린다.
                for (int source = 0; source < slots.Length; source++)
                {
                    Pose from = homes[source];
                    Pose to = homes[shuffle.SourceToDestination[source]];
                    Pose center = from;
                    center.position = shufflePoint.anchoredPosition3D;
                    center.scale *= .54f;
                    center.rotation = shufflePoint.localRotation;

                    bool gathering = progress < .5f;
                    float t = Mathf.SmoothStep(0f, 1f, gathering ? progress * 2f : (1f - progress) * 2f);
                    Pose pose = Pose.Lerp(gathering ? from : to, center, t);
                    pose.position.y += Mathf.Sin(t * Mathf.PI) * (36f + 12f * (gathering ? source : shuffle.SourceToDestination[source]));
                    pose.Apply(slots[source].Transform);
                    slots[source].visibility.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, .96f, t));
                }
            });

            var shuffled = new ClockView[slots.Length];
            for (int source = 0; source < slots.Length; source++)
                shuffled[shuffle.SourceToDestination[source]] = slots[source];
            slots = shuffled;

            match.CompleteShuffle();
            match.EndItemPhase(Participant.Player);
            opponentImpulse = -.36f;
            busy = scriptedMotion = false;
            Refresh();
        }

        private void Select(int slot)
        {
            if (busy)
                return;
            if (slot == match.CurrentClock)
            {
                Cancel();
                return;
            }
            if (match.SelectClock(Participant.Player, slot))
            {
                effectStartedAt = Time.unscaledTime;
                Refresh();
            }
        }

        private void Cancel()
        {
            if (!busy && match != null && match.CancelClockSelection(Participant.Player))
                Refresh();
        }

        private void Commit(TimeAction action)
        {
            if (!busy && match.CommitAction(Participant.Player, action))
                StartCoroutine(RevealAndSettle());
        }

        private IEnumerator RevealAndSettle()
        {
            busy = true;
            Refresh();
            yield return Animate(settings.RevealDuration, _ => { });

            if (match.Settle())
            {
                effectStartedAt = Time.unscaledTime;
                int before = 0;
                foreach (TimeTransfer transfer in match.LastTransfers)
                {
                    if (transfer.To == TimeAccount.Ai) before += transfer.Minutes;
                    if (transfer.From == TimeAccount.Ai) before -= transfer.Minutes;
                }
                opponentImpulse = before == 0 ? .38f : before > 0 ? .85f : -1f;
                Refresh();
                yield return Animate(settings.TransferDuration + settings.ResultHoldDuration, _ => { });
            }
            busy = false;
            Refresh();
        }

        private bool IsSettled => match.Stage == TurnStage.Settling || match.Stage == TurnStage.GameOver;

        private void Refresh()
        {
            playerLife.text = GameTime.Format(match.PlayerLifeMinutes);
            opponentLife.text = GameTime.Format(match.AiLifeMinutes);
            bankTime.text = GameTime.Format(match.BankMinutes);

            int selected = match.CurrentClock;
            bool choosing = !busy && match.Stage == TurnStage.AwaitingAction;
            bool preview = match.Stage == TurnStage.BatchPreview;
            bool playerTurn = match.ActiveParticipant == Participant.Player;
            for (int i = 0; i < slots.Length; i++)
            {
                ClockView watch = slots[i];
                ClockFace? face = match.GetClock(i).Face;
                watch.rim.color = face.HasValue ? (face.Value.Kind == ClockKind.Red ? Red : Normal) : (i == selected ? Accent : Closed);
                watch.value.text = face.HasValue
                    ? $"{face.Value.Hours}h\n<size=15>{(face.Value.Kind == ClockKind.Red ? "빨강" : "일반")}</size>"
                    : "?";
                watch.button.interactable = !busy && (match.CanSelectClock(Participant.Player, i) || choosing && i == selected);
            }

            SetForeground(selected >= 0 ? slots[selected] : null);
            focusRing.gameObject.SetActive(selected >= 0);
            actions.gameObject.SetActive(choosing);
            bool committed = match.Stage == TurnStage.Revealing || IsSettled;
            actionTitle.gameObject.SetActive(committed);
            if (committed)
                actionTitle.text = match.Action == TimeAction.Collect ? "징수" : "배급";
            transfers.gameObject.SetActive(IsSettled);
            if (IsSettled)
            {
                transfers.text = string.Join("\n", match.LastTransfers.Select(transfer =>
                    $"{AccountName(transfer.From)} → {AccountName(transfer.To)}\n<size=30>{GameTime.Format(transfer.Minutes)}</size>"));
            }

            advance.gameObject.SetActive(!busy && (preview || IsSettled || !playerTurn));
            advanceLabel.text = preview ? "가리고 섞기" : "다시 공개";
            if (preview)
                prompt.text = "시계의 시간과 종류를 확인하세요.";
            else if (match.Stage == TurnStage.Shuffling)
                prompt.text = "시계를 섞는 중";
            else if (IsSettled)
                prompt.text = match.Winner == GameWinner.None ? "정산 완료" : (match.Winner == GameWinner.Player ? "승리" : "패배");
            else if (committed)
                prompt.text = "선택한 시계를 공개합니다.";
            else if (!playerTurn)
                prompt.text = "플레이어가 선공인 묶음에서 행동을 시험할 수 있습니다.";
            else
                prompt.text = choosing ? "행동을 선택하거나 시계를 내려놓으세요." : "닫힌 시계를 하나 선택하세요.";
        }

        private void SetForeground(ClockView next)
        {
            if (foregroundWatch == next)
                return;
            if (foregroundWatch != null)
                foregroundWatch.Transform.SetSiblingIndex(foregroundHomeIndex);

            foregroundWatch = next;
            if (next != null)
            {
                foregroundHomeIndex = next.Transform.GetSiblingIndex();
                next.Transform.SetAsLastSibling();
            }
        }

        private void UpdateRing(ClockView watch)
        {
            float elapsed = Time.unscaledTime - effectStartedAt;
            float pulse = Mathf.Clamp01(elapsed / .55f);
            float expansion = IsSettled ? Mathf.Lerp(1.12f, 1.7f, pulse) : 1.13f + .035f * Mathf.Sin(elapsed * 8f);
            RectTransform ring = focusRing.rectTransform;
            ring.anchoredPosition3D = watch.Transform.anchoredPosition3D;
            ring.sizeDelta = watch.Transform.sizeDelta * expansion;
            ring.localScale = watch.Transform.localScale;

            Color color = IsSettled ? watch.rim.color : Accent;
            color.a = IsSettled ? Mathf.Lerp(.75f, .16f, pulse) : .34f;
            focusRing.color = color;
        }

        private static IEnumerator Animate(float seconds, Action<float> sample)
        {
            seconds = Mathf.Max(.01f, seconds);
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                sample(elapsed / seconds);
                yield return null;
            }
            sample(1f);
        }

        private static string AccountName(TimeAccount account)
        {
            switch (account)
            {
                case TimeAccount.Player: return "내 수명";
                case TimeAccount.Ai: return "상대 수명";
                default: return "시간고";
            }
        }
    }
}
