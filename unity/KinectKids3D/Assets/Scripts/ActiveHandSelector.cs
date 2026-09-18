using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Gemensamt aktiv-hand-val för alla Unity-spel. En spelare får ett stabilt
    /// sikte och kan byta hand genom att börja röra eller kasta med den andra.
    /// </summary>
    public sealed class ActiveHandSelector
    {
        private readonly Dictionary<int, State> states = new Dictionary<int, State>();

        public IReadOnlyList<AimSample> Select(IReadOnlyList<AimSample> samples, int playerCount)
        {
            var selected = new List<AimSample>(playerCount);
            for (int player = 0; player < playerCount; player++)
            {
                AimSample[] hands = samples.Where(item => item.PlayerIndex == player).ToArray();
                if (hands.Length == 0) continue;
                if (hands.Length == 1)
                {
                    selected.Add(hands[0]);
                    continue;
                }

                AimSample left = hands.FirstOrDefault(item => item.HandId % 2 == 0);
                AimSample right = hands.FirstOrDefault(item => item.HandId % 2 == 1);
                State state;
                if (!states.TryGetValue(player, out state))
                {
                    state = new State
                    {
                        RightActive = true,
                        LeftPosition = left.Position,
                        RightPosition = right.Position,
                        LastSwitch = Time.unscaledTime
                    };
                    states[player] = state;
                }

                float leftMove = Vector2.Distance(left.Position, state.LeftPosition);
                float rightMove = Vector2.Distance(right.Position, state.RightPosition);
                state.LeftActivity = state.LeftActivity * 0.82f + Mathf.Min(0.18f, leftMove)
                    + (left.Fire ? 0.42f : left.GestureProgress * 0.035f);
                state.RightActivity = state.RightActivity * 0.82f + Mathf.Min(0.18f, rightMove)
                    + (right.Fire ? 0.42f : right.GestureProgress * 0.035f);

                bool forceLeft = left.Fire;
                bool forceRight = right.Fire;
                if (forceLeft != forceRight)
                {
                    state.RightActive = forceRight;
                    state.LastSwitch = Time.unscaledTime;
                }
                else if (Time.unscaledTime - state.LastSwitch >= 0.52f)
                {
                    float active = state.RightActive ? state.RightActivity : state.LeftActivity;
                    float other = state.RightActive ? state.LeftActivity : state.RightActivity;
                    if (other > active * 1.32f + 0.012f)
                    {
                        state.RightActive = !state.RightActive;
                        state.LastSwitch = Time.unscaledTime;
                    }
                }

                state.LeftPosition = left.Position;
                state.RightPosition = right.Position;
                selected.Add(state.RightActive ? right : left);
            }
            return selected;
        }

        public void Reset() => states.Clear();

        private sealed class State
        {
            public bool RightActive;
            public Vector2 LeftPosition;
            public Vector2 RightPosition;
            public float LeftActivity;
            public float RightActivity;
            public float LastSwitch;
        }
    }
}
