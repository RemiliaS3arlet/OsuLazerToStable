import sqlite3, sys, os

db = os.path.expandvars(r"%LOCALAPPDATA%\osu!\osu!.db")
print("db:", db, "exists:", os.path.exists(db), "size:", os.path.getsize(db) if os.path.exists(db) else 0)

con = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
cur = con.cursor()

# 表结构
tables = [r[0] for r in cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%beatmap%'")]
print("tables:", tables)

for t in tables:
    cols = [c[1] for c in cur.execute(f"PRAGMA table_info({t})")]
    print(f"--- {t}: {cols}")

# 最近的谱面集（按本地 SetID 倒序）
cols = [c[1] for c in cur.execute("PRAGMA table_info(osu_beatmapsets)")]
print("\n--- 最近 12 个谱面集 ---")
q = f"SELECT {','.join(cols)} FROM osu_beatmapsets ORDER BY {cols[0]} DESC LIMIT 12"
for row in cur.execute(q):
    print(dict(zip(cols, row)))

con.close()
