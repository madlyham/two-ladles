using System.Collections.Generic;

namespace TwoLadles.Networking
{
    /// <summary>
    /// Keeps one-shot LAN control packets (START_SHIFT, DAY_REPORT, ...) and resends them every
    /// RESEND_INTERVAL seconds until the partner acknowledges them, or MAX_TRIES resends pass.
    /// </summary>
    public class ReliableOutbox
    {
        public const int MAX_TRIES = 20;
        public const float RESEND_INTERVAL = 0.5f;

        private class Entry
        {
            public int seq;
            public string payload;
            public float lastSent;
            public int tries;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private int nextSeq = 1;

        public int Count => entries.Count;

        public int Add(string payload, float now)
        {
            int s = nextSeq++;
            entries.Add(new Entry { seq = s, payload = payload, lastSent = now, tries = 0 });
            return s;
        }

        public List<KeyValuePair<int, string>> Due(float now)
        {
            var due = new List<KeyValuePair<int, string>>();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                if (now - e.lastSent < RESEND_INTERVAL) continue;
                e.tries++;
                if (e.tries > MAX_TRIES) { entries.RemoveAt(i); continue; }
                e.lastSent = now;
                due.Add(new KeyValuePair<int, string>(e.seq, e.payload));
            }
            due.Reverse();
            return due;
        }

        public bool Ack(int seq)
        {
            int idx = entries.FindIndex(e => e.seq == seq);
            if (idx < 0) return false;
            entries.RemoveAt(idx);
            return true;
        }

        public void Clear() => entries.Clear();
    }
}
